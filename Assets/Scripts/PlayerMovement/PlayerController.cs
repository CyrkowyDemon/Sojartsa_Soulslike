using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float dampingTime = 0.1f;
    [SerializeField] private float gravity = -30f;

    [Header("IK Settings")]
    [SerializeField] private bool useFootIK = true; // PRZEŁĄCZNIK W INSPEKTORZE
    [SerializeField] private RigBuilder rigBuilder;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float footOffset = 0.08f;
    [SerializeField] private float pelvisSpeed = 5f;
    [SerializeField] private float footIKLerpSpeed = 25f;
    [SerializeField] private Vector3 footTargetRotationOffset = new Vector3(0, 180f, 0); 
    
    [Header("Bones")]
    [SerializeField] private Transform leftFootBone;
    [SerializeField] private Transform rightFootBone;
    [SerializeField] private Transform hipsBone;

    [Header("IK Targets")]
    [SerializeField] private Transform leftFootIKTarget;
    [SerializeField] private Transform rightFootIKTarget;
    [SerializeField] private Transform leftKneeHint;
    [SerializeField] private Transform rightKneeHint;

    private Vector2 _moveInput;
    private float _verticalVelocity;
    private bool _isGrounded;
    private float _currentPelvisOffset;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        if (rigBuilder != null) rigBuilder.Build();
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead) return;

        _moveInput = inputReader.MovementValue;
        _isGrounded = controller.isGrounded;

        float speed = _moveInput.magnitude;
        animator.SetFloat("ForwardSpeed", speed, dampingTime, Time.deltaTime);

        if (speed > 0.01f)
        {
            Vector3 moveDir = CalculateMovement();
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), rotationSpeed * Time.deltaTime);
        }

        ApplyGravity();
    }

    private void LateUpdate()
    {
        HandleFootIK();
    }

    private void HandleFootIK()
    {
        if (rigBuilder == null || rigBuilder.layers.Count == 0) return;
        var rig = rigBuilder.layers[0].rig;

        float speed = _moveInput.magnitude;
        
        // Obliczamy wagę: musi być włączone w inspektorze I postać musi stać (speed < 0.1)
        float targetWeight = (useFootIK && speed <= 0.1f) ? 1f : 0f;
        rig.weight = Mathf.Lerp(rig.weight, targetWeight, 5f * Time.deltaTime);

        // Jeśli IK jest wyłączone (waga bliska 0), płynnie resetujemy offset bioder i kończymy
        if (rig.weight <= 0.01f) 
        {
            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, 0f, pelvisSpeed * Time.deltaTime);
            if (hipsBone != null && Mathf.Abs(_currentPelvisOffset) > 0.001f)
            {
                hipsBone.position += Vector3.up * _currentPelvisOffset;
            }
            return;
        }

        // --- Logika IK gdy waga > 0 ---
        float leftGroundHeight = SampleGroundHeight(leftFootBone);
        float rightGroundHeight = SampleGroundHeight(rightFootBone);

        float lowestGround = Mathf.Min(leftGroundHeight, rightGroundHeight);
        float targetPelvisOffset = Mathf.Clamp(lowestGround, -0.8f, 0f);

        _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, targetPelvisOffset, pelvisSpeed * Time.deltaTime);

        if (hipsBone != null)
        {
            hipsBone.position += Vector3.up * _currentPelvisOffset;
        }

        ProcessFoot(leftFootBone, leftFootIKTarget, leftKneeHint);
        ProcessFoot(rightFootBone, rightFootIKTarget, rightKneeHint);
    }

    private float SampleGroundHeight(Transform footBone)
    {
        if (footBone == null) return 0f;
        
        // Zmieniamy punkt startowy na pozycję kości + lekki margines w górę
        Vector3 origin = footBone.position + Vector3.up * 0.5f;
        
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.5f, groundLayer))
        {
            // Zwracamy różnicę między ziemią a pozycją stopy
            return hit.point.y - footBone.position.y;
        }
        return 0f;
    }

    private void ProcessFoot(Transform footBone, Transform target, Transform kneeHint)
    {
        if (footBone == null || target == null) return;

        target.rotation = transform.rotation * Quaternion.Euler(footTargetRotationOffset);

        Vector3 origin = footBone.position + Vector3.up * 0.5f;
        
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.5f, groundLayer))
        {
            Vector3 targetPos = hit.point + Vector3.up * footOffset;
            target.position = Vector3.Lerp(target.position, targetPos, footIKLerpSpeed * Time.deltaTime);

            if (kneeHint != null)
            {
                Vector3 kneePos = target.position + transform.forward * 0.8f + Vector3.up * 0.5f; 
                kneeHint.position = Vector3.Lerp(kneeHint.position, kneePos, 10f * Time.deltaTime);
            }
        }
    }

    private void ApplyGravity()
    {
        if (_isGrounded && _verticalVelocity < 0) _verticalVelocity = -2f;
        else _verticalVelocity += gravity * Time.deltaTime;
    }

    private Vector3 CalculateMovement()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0; right.y = 0;
        return (forward.normalized * _moveInput.y + right.normalized * _moveInput.x);
    }

    private void OnAnimatorMove()
    {
        Vector3 delta = animator.deltaPosition;
        delta.y = _verticalVelocity * Time.deltaTime;
        controller.Move(delta);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.yellow;
        DrawFootGizmo(leftFootBone);
        Gizmos.color = Color.cyan;
        DrawFootGizmo(rightFootBone);
    }

    private void DrawFootGizmo(Transform footBone)
    {
        if (footBone == null) return;
        Vector3 origin = footBone.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.5f, groundLayer))
        {
            Gizmos.DrawLine(origin, hit.point);
            Gizmos.DrawSphere(hit.point, 0.05f);
        }
    }
}