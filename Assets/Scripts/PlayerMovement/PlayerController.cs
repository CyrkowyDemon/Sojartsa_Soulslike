using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TargetHandler targetHandler;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float dampingTime = 0.1f;
    [SerializeField] private float gravity = -30f;

    [Header("IK Settings")]
    [SerializeField] private bool useFootIK = true; 
    [SerializeField] private RigBuilder rigBuilder; // Referencja do RigBuildera na modelu gracza
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float footOffset = 0.08f;
    [SerializeField] private float pelvisSpeed = 5f;
    [SerializeField] private float footIKLerpSpeed = 25f;
    [SerializeField] private Vector3 footTargetRotationOffset = new Vector3(0, 180f, 0); 
    
    [Header("Movement IK (Weights)")]
    [SerializeField] private string leftFootWeightParam = "LeftFootWeight";
    [SerializeField] private string rightFootWeightParam = "RightFootWeight";
    [SerializeField] private float footWeightSpeed = 20f;
    [SerializeField] private float footLiftThreshold = 0.15f; 
    [SerializeField] private float footAnkleHeight = 0.08f; // Odległość kości Mixamo od podeszwy
    
    [Header("Debug & Calibration")]
    [SerializeField] private float manualHeightOffset = 0f; // Dodatkowy reczny przesuw gora/dol
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

    private Vector2 _moveInput;
    private float _verticalVelocity;
    private bool _isGrounded;
    private float _currentPelvisOffset;
    private float _originalAnimatorY;
    private float _leftFootWeight;
    private float _rightFootWeight;
    
    private int _leftWeightHash;
    private int _rightWeightHash;
    private bool _hasLeftParam;
    private bool _hasRightParam;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        if (rigBuilder != null) rigBuilder.Build();
        if (animator != null) 
        {
            _originalAnimatorY = animator.transform.localPosition.y;
            
            // Rejestrujemy parametry by nie spamować konsoli błędami
            _leftWeightHash = Animator.StringToHash(leftFootWeightParam);
            _rightWeightHash = Animator.StringToHash(rightFootWeightParam);
            
            foreach (var param in animator.parameters)
            {
                if (param.name == leftFootWeightParam) _hasLeftParam = true;
                if (param.name == rightFootWeightParam) _hasRightParam = true;
            }
        }
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead) return;

        _moveInput = inputReader.MovementValue;
        _isGrounded = controller.isGrounded;

        bool canRotate = animator.GetBool("CanRotate") || animator.GetCurrentAnimatorStateInfo(0).IsTag("Idle") || animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
        bool lockedOn = targetHandler != null && targetHandler.IsLockedOn;

        // == PRZYWRÓCONE: Informujemy Animatora czy mamy Lock-ona ==
        animator.SetFloat("LockedIn", lockedOn ? 1f : 0f, dampingTime, Time.deltaTime);

        if (lockedOn)
        {
            if (canRotate && targetHandler.CurrentTarget != null)
            {
                // == PRZYWRÓCONE: Zawsze patrzymy na przeciwnika ==
                Vector3 dir = targetHandler.CurrentTarget.position - transform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.1f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
                }
            }

            // == PRZYWRÓCONE: Przesyłamy X i Y do Animatora (strafowanie) ==
            animator.SetFloat("ForwardSpeed", _moveInput.y, dampingTime, Time.deltaTime);
            animator.SetFloat("SidewaysSpeed", _moveInput.x, dampingTime, Time.deltaTime);
        }
        else
        {
            float speed = _moveInput.magnitude;
            animator.SetFloat("ForwardSpeed", speed, dampingTime, Time.deltaTime);
            animator.SetFloat("SidewaysSpeed", 0f, dampingTime, Time.deltaTime);

            if (canRotate && speed > 0.01f)
            {
                Vector3 moveDir = CalculateMovement();
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), rotationSpeed * Time.deltaTime);
            }
        }

        ApplyGravity();
    }

    private void LateUpdate()
    {
        HandleFootIK();
    }

    private void HandleFootIK()
    {
        if (rigBuilder == null || rigBuilder.layers.Count == 0 || !useFootIK) return;
        var rig = rigBuilder.layers[0].rig;

        // === FROMSOFTWARE FIX: W powietrzu IK jest wyłączony ===
        if (!_isGrounded)
        {
            rig.weight = Mathf.Lerp(rig.weight, 0f, footWeightSpeed * Time.deltaTime);
            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, 0f, pelvisSpeed * Time.deltaTime);
            if (animator != null)
            {
                Vector3 localPos = animator.transform.localPosition;
                localPos.y = _originalAnimatorY + _currentPelvisOffset;
                animator.transform.localPosition = localPos;
            }
            _leftFootWeight = 0f;
            _rightFootWeight = 0f;
            return;
        }
        // ======================================================

        // Wartości absolutne World Y + wektory kąta nachylenia (Normal)
        Vector3 leftNormal, rightNormal;
        float leftGroundWorldY = SampleGroundHeight(leftFootBone, out leftNormal);
        float rightGroundWorldY = SampleGroundHeight(rightFootBone, out rightNormal);

        // --- DYNAMIKA WAG (AUTO-WEIGHTING) ---
        // Liczymy różnice wysokości kości kostki (ankle) względem podłoża
        float leftDist = (leftFootBone.position.y - leftGroundWorldY) - footAnkleHeight;
        float rightDist = (rightFootBone.position.y - rightGroundWorldY) - footAnkleHeight;

        // Celujemy w wage 1.0 gdy noga jest na podłodze, 0.0 gdy podnosi się wyżej niż próg.
        // Używamy Pow(3), by przejście było "ostre" - noga albo stoi, albo szybko odpuszcza.
        float leftTargetWeight = Mathf.Pow(Mathf.Clamp01(1f - (Mathf.Max(0, leftDist) / footLiftThreshold)), 3);
        float rightTargetWeight = Mathf.Pow(Mathf.Clamp01(1f - (Mathf.Max(0, rightDist) / footLiftThreshold)), 3);

        // Tylko jeśli parametry istnieją (brak spamu w konsoli)
        if (_hasLeftParam) leftTargetWeight = Mathf.Max(leftTargetWeight, animator.GetFloat(_leftWeightHash));
        if (_hasRightParam) rightTargetWeight = Mathf.Max(rightTargetWeight, animator.GetFloat(_rightWeightHash));

        _leftFootWeight = Mathf.Lerp(_leftFootWeight, leftTargetWeight, footWeightSpeed * Time.deltaTime);
        _rightFootWeight = Mathf.Lerp(_rightFootWeight, rightTargetWeight, footWeightSpeed * Time.deltaTime);

        // Rig jest aktywny jeśli choć jedna stopa potrzebuje IK
        rig.weight = Mathf.Max(_leftFootWeight, _rightFootWeight);

        // --- PELVIS OFFSET (KUCNIĘCIE MODELU) ---
        if (animator != null)
        {
            float baseY = controller.bounds.min.y;
            float leftRelative = leftGroundWorldY - baseY;
            float rightRelative = rightGroundWorldY - baseY;
            
            float targetPelvisOffset = 0f;
            float minRelative = Mathf.Min(leftRelative, rightRelative);
            
            // Kucamy tylko gdy ziemia ucieka w dół i tylko gdy nogi są "uziemione" (planted)
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

        // Debug wag (usuń gdy będzie działać idealnie)
        // Debug.Log($"IK Weights: L:{_leftFootWeight:F2} R:{_rightFootWeight:F2} Rig:{rig.weight:F2}");

        // Cele stóp z uwzględnieniem ich indywidualnej wagi
        ProcessFoot(leftFootBone, leftFootIKTarget, leftKneeHint, leftGroundWorldY, leftNormal, _leftFootWeight);
        ProcessFoot(rightFootBone, rightFootIKTarget, rightKneeHint, rightGroundWorldY, rightNormal, _rightFootWeight);
    }

    private float SampleGroundHeight(Transform footBone, out Vector3 groundNormal)
    {
        groundNormal = Vector3.up;

        // Safe Fallback w razie pustych referencji
        if (footBone == null) return 0f;

        // Strzelamy od absolutnej podstawy fizycznej kapsuły (twarde zakotwiczenie do kolajdera)
        // originY puszczamy wyżej niż kolana (np. +0.8f od spodu) by skanować co jest wyżej przed nami
        Vector3 origin = new Vector3(footBone.position.x, controller.bounds.min.y + 0.8f, footBone.position.z);
        
        // Zwiększamy dystans laseru, żeby zawsze złapał przepaście i nie wpadał w błędny cykl
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 2.5f, groundLayer);
        
        float highestGround = -Mathf.Infinity;
        bool hitAnything = false;

        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root) continue;

            if (hit.point.y > highestGround)
            {
                highestGround = hit.point.y;
                groundNormal = hit.normal; // Zapisujemy nachylenie zbocza
                hitAnything = true;
            }
        }

        if (hitAnything)
        {
            return highestGround;
        }

        return footBone.position.y;
    }

    private void ProcessFoot(Transform footBone, Transform target, Transform kneeHint, float groundWorldY, Vector3 groundNormal, float footWeight)
    {
        if (target == null || animator == null) return;

        // Jeśli noga jest w górze (krok), celem IK jest po prostu jej pozycja z animacji
        // Jeśli jest na ziemi, celem jest punkt uderzenia raycasta.
        float targetY = groundWorldY + footOffset + manualHeightOffset;
        Vector3 worldTargetPos = new Vector3(footBone.position.x, targetY, footBone.position.z);
        
        // Płynne przechodzenie między Animacją a IK
        target.position = Vector3.Lerp(footBone.position, worldTargetPos, footWeight);
        
        // --- PRO FIX: STABILNA ROTACJA (LookRotation) ---
        // Budujemy rotację od zera, by uniknąć "kręcenia bączków" (Helicoptera).
        // Przód = Kierunek w którym patrzy gracz, ale przyklejony do płaszczyzny ziemi.
        Vector3 forwardDir = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
        if (forwardDir.sqrMagnitude < 0.01f) forwardDir = transform.forward; // Fallback

        // Tworzymy rotację: UP to nachylenie terenu, FORWARD to kierunek patrzenia kontenera
        Quaternion slopeRotation = Quaternion.LookRotation(forwardDir, groundNormal);
        
        // MIXAMO FIX: Jeśli model stopy jest odwrócony w riggu, nakładamy offset
        slopeRotation *= Quaternion.Euler(footTargetRotationOffset);

        // Wybieramy między czystą animacją a naszą poprawką skosu
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
        if (!showDebugGizmos || !Application.isPlaying || leftFootBone == null) return;

        // Ponownie zbieramy dane do rysowania (tylko w Edytorze)
        Vector3 lNorm, rNorm;
        float lGround = SampleGroundHeight(leftFootBone, out lNorm);
        float rGround = SampleGroundHeight(rightFootBone, out rNorm);

        DrawDebugMarker(leftFootBone, lGround, _leftFootWeight);
        DrawDebugMarker(rightFootBone, rGround, _rightFootWeight);
    }

    private void DrawDebugMarker(Transform foot, float groundY, float weight)
    {
        Gizmos.color = Color.Lerp(Color.blue, Color.red, weight);
        
        // Promień rysujemy tylko DO ziemi (nie przebija w próżnie)
        Vector3 start = new Vector3(foot.position.x, animator.transform.position.y + 0.5f, foot.position.z);
        Vector3 end = new Vector3(foot.position.x, groundY, foot.position.z);
        
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.05f);
        Gizmos.DrawWireSphere(foot.position, 0.05f); // Kula na kości stopy
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

    /// <summary>
    /// Mordo, bezpieczna teleportacja. Wyłączamy CharacterController, 
    /// żeby fizyka Unity nie "odbiła" nas z powrotem.
    /// </summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (controller != null) controller.enabled = false;
        
        transform.position = position;
        transform.rotation = rotation;
        
        if (controller != null) controller.enabled = true;
        
        Debug.Log($"<color=cyan>[PLAYER] Teleportacja zakończona sukcesem!</color>");
    }

    // Publiczna metoda wywoływana przez RootMotionProxy na modelu
    public void ApplyBuiltInRootMotion(Vector3 deltaPos, Quaternion deltaRot)
    {
        // Obsługa fizyki (pudełka) na rodzicu
        Vector3 movement = deltaPos;
        movement.y = _verticalVelocity * Time.deltaTime;
        
        controller.Move(movement);

        // Dodajemy rotacje z animacji do kapsuly gracza
        transform.rotation *= deltaRot;
    }
}