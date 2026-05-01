using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerFootIK : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private RigBuilder rigBuilder;

    [Header("IK Settings")]
    [SerializeField] private bool useFootIK = true; 
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float footOffset = 0.08f;
    [SerializeField] private float pelvisSpeed = 5f;
    [SerializeField] private Vector3 footTargetRotationOffset = new Vector3(0, 180f, 0); 
    
    [Header("Movement IK (Weights)")]
    [SerializeField] private string leftFootWeightParam = "LeftFootWeight";
    [SerializeField] private string rightFootWeightParam = "RightFootWeight";
    [SerializeField] private float footWeightSpeed = 20f;
    [SerializeField] private float footLiftThreshold = 0.15f; 
    [SerializeField] private float footAnkleHeight = 0.08f; 
    
    [Header("Debug & Calibration")]
    [SerializeField] private float manualHeightOffset = 0f; 
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Bones")]
    [SerializeField] private Transform leftFootBone;
    [SerializeField] private Transform rightFootBone;
    [SerializeField] private Transform hipsBone;
    
    [Header("IK Targets")]
    [SerializeField] private Transform leftFootIKTarget;
    [SerializeField] private Transform rightFootIKTarget;
    [SerializeField] private Transform leftKneeHint;
    [SerializeField] private Transform rightKneeHint;

    private float _currentPelvisOffset;
    private float _originalAnimatorY;
    private float _leftFootWeight;
    private float _rightFootWeight;
    
    private int _leftWeightHash;
    private int _rightWeightHash;
    private bool _hasLeftParam;
    private bool _hasRightParam;
    private int _nothingStateHash;

    private float _cachedLeftGroundY;
    private float _cachedRightGroundY;
    private RaycastHit[] _raycastHitsAlloc = new RaycastHit[10];

    private const int ACTIONS_LAYER = 2;

    private void Start()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (rigBuilder == null) rigBuilder = GetComponentInChildren<RigBuilder>();

        if (rigBuilder != null) rigBuilder.Build();
        
        if (animator != null) 
        {
            _originalAnimatorY = animator.transform.localPosition.y;
            
            _leftWeightHash = Animator.StringToHash(leftFootWeightParam);
            _rightWeightHash = Animator.StringToHash(rightFootWeightParam);
            
            foreach (var param in animator.parameters)
            {
                if (param.name == leftFootWeightParam) _hasLeftParam = true;
                if (param.name == rightFootWeightParam) _hasRightParam = true;
            }

            _nothingStateHash = Animator.StringToHash("Nothing");
        }
    }

    private void LateUpdate()
    {
        HandleFootIK();
    }

    private void HandleFootIK()
    {
        if (rigBuilder == null || rigBuilder.layers.Count == 0 || !useFootIK || animator == null || controller == null) return;
        var rig = rigBuilder.layers[0].rig;

        // === FROMSOFTWARE FIX: W powietrzu IK jest wyłączony ===
        if (!controller.isGrounded)
        {
            rig.weight = Mathf.Lerp(rig.weight, 0f, footWeightSpeed * Time.deltaTime);
            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, 0f, pelvisSpeed * Time.deltaTime);
            
            Vector3 localPos = animator.transform.localPosition;
            localPos.y = _originalAnimatorY + _currentPelvisOffset;
            animator.transform.localPosition = localPos;
            
            _leftFootWeight = 0f;
            _rightFootWeight = 0f;
            return;
        }

        // === FROMSOFTWARE FIX: Podczas ataku/uniku IK jest wygaszany ===
        AnimatorStateInfo actionState = animator.GetCurrentAnimatorStateInfo(ACTIONS_LAYER);
        bool isActionsPlaying = actionState.shortNameHash != _nothingStateHash || animator.IsInTransition(ACTIONS_LAYER);
        if (isActionsPlaying)
        {
            rig.weight = Mathf.Lerp(rig.weight, 0f, footWeightSpeed * Time.deltaTime);
            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, 0f, pelvisSpeed * Time.deltaTime);
            
            Vector3 localPos = animator.transform.localPosition;
            localPos.y = _originalAnimatorY + _currentPelvisOffset;
            animator.transform.localPosition = localPos;
            
            _leftFootWeight = 0f;
            _rightFootWeight = 0f;
            return;
        }

        Vector3 leftNormal, rightNormal;
        float leftGroundWorldY = SampleGroundHeight(leftFootBone, out leftNormal);
        float rightGroundWorldY = SampleGroundHeight(rightFootBone, out rightNormal);
        
        _cachedLeftGroundY = leftGroundWorldY;
        _cachedRightGroundY = rightGroundWorldY;

        float leftDist = (leftFootBone.position.y - leftGroundWorldY) - footAnkleHeight;
        float rightDist = (rightFootBone.position.y - rightGroundWorldY) - footAnkleHeight;

        float leftTargetWeight = Mathf.Pow(Mathf.Clamp01(1f - (Mathf.Max(0, leftDist) / footLiftThreshold)), 3);
        float rightTargetWeight = Mathf.Pow(Mathf.Clamp01(1f - (Mathf.Max(0, rightDist) / footLiftThreshold)), 3);

        if (_hasLeftParam) leftTargetWeight = Mathf.Max(leftTargetWeight, animator.GetFloat(_leftWeightHash));
        if (_hasRightParam) rightTargetWeight = Mathf.Max(rightTargetWeight, animator.GetFloat(_rightWeightHash));

        _leftFootWeight = Mathf.Lerp(_leftFootWeight, leftTargetWeight, footWeightSpeed * Time.deltaTime);
        _rightFootWeight = Mathf.Lerp(_rightFootWeight, rightTargetWeight, footWeightSpeed * Time.deltaTime);

        rig.weight = Mathf.Max(_leftFootWeight, _rightFootWeight);

        if (animator != null)
        {
            float baseY = controller.bounds.min.y;
            float leftRelative = leftGroundWorldY - baseY;
            float rightRelative = rightGroundWorldY - baseY;
            
            float targetPelvisOffset = 0f;
            float minRelative = Mathf.Min(leftRelative, rightRelative);
            
            if (minRelative < 0)
            {
                targetPelvisOffset = minRelative * rig.weight;
            }

            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, targetPelvisOffset, pelvisSpeed * Time.deltaTime);

            Vector3 localPos = animator.transform.localPosition;
            localPos.y = _originalAnimatorY + _currentPelvisOffset;
            animator.transform.localPosition = localPos;
        }

        if (rig.weight <= 0.01f) return;

        ProcessFoot(leftFootBone, leftFootIKTarget, leftKneeHint, leftGroundWorldY, leftNormal, _leftFootWeight);
        ProcessFoot(rightFootBone, rightFootIKTarget, rightKneeHint, rightGroundWorldY, rightNormal, _rightFootWeight);
    }

    private float SampleGroundHeight(Transform footBone, out Vector3 groundNormal)
    {
        groundNormal = Vector3.up;
        if (footBone == null) return 0f;

        Vector3 origin = new Vector3(footBone.position.x, controller.bounds.min.y + 0.8f, footBone.position.z);
        int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, _raycastHitsAlloc, 2.5f, groundLayer);
        
        float highestGround = -Mathf.Infinity;
        bool hitAnything = false;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = _raycastHitsAlloc[i];
            if (hit.transform.root == transform.root) continue;

            if (hit.point.y > highestGround)
            {
                highestGround = hit.point.y;
                groundNormal = hit.normal;
                hitAnything = true;
            }
        }

        if (hitAnything) return highestGround;
        return footBone.position.y;
    }

    private void ProcessFoot(Transform footBone, Transform target, Transform kneeHint, float groundWorldY, Vector3 groundNormal, float footWeight)
    {
        if (target == null || animator == null) return;

        float targetY = groundWorldY + footOffset + manualHeightOffset;
        Vector3 worldTargetPos = new Vector3(footBone.position.x, targetY, footBone.position.z);
        target.position = Vector3.Lerp(footBone.position, worldTargetPos, footWeight);
        
        Vector3 forwardDir = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
        if (forwardDir.sqrMagnitude < 0.01f) forwardDir = transform.forward;

        Quaternion slopeRotation = Quaternion.LookRotation(forwardDir, groundNormal);
        slopeRotation *= Quaternion.Euler(footTargetRotationOffset);

        target.rotation = Quaternion.Slerp(footBone.rotation, slopeRotation, footWeight);

        if (showDebugGizmos)
        {
            Debug.DrawLine(footBone.position, target.position, Color.Lerp(Color.blue, Color.red, footWeight));
        }

        if (kneeHint != null)
        {
            Vector3 kneePos = target.position + transform.forward * 0.8f + Vector3.up * 0.5f; 
            kneeHint.position = Vector3.Lerp(kneeHint.position, kneePos, 10f * Time.deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying || leftFootBone == null || animator == null) return;

        DrawDebugMarker(leftFootBone, _cachedLeftGroundY, _leftFootWeight);
        DrawDebugMarker(rightFootBone, _cachedRightGroundY, _rightFootWeight);
    }

    private void DrawDebugMarker(Transform foot, float groundY, float weight)
    {
        Gizmos.color = Color.Lerp(Color.blue, Color.red, weight);
        Vector3 start = new Vector3(foot.position.x, animator.transform.position.y + 0.5f, foot.position.z);
        Vector3 end = new Vector3(foot.position.x, groundY, foot.position.z);
        
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.05f);
        Gizmos.DrawWireSphere(foot.position, 0.05f);
    }
}