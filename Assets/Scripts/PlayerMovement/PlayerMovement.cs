using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    public InputReader InputReader => inputReader;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TargetHandler targetHandler;
    [SerializeField] private PlayerLookController lookController; // Skrypt Look-At na graczu

    private PlayerCombat _playerCombat;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 3f;      // Wolna bazowa rotacja ciała (głowa ją wyprzedza)
    [SerializeField] private float snapRotationMultiplier = 4f; // x razy szybciej podczas SNAP
    [SerializeField] private float dampingTime = 0.1f;
    [SerializeField] private float gravity = -30f;
    [Tooltip("Kąt (stopnie) od którego ciało zaczyna się obracać do celu. Poniżej tego progu robi to sam PlayerLookController (głowa/tułów).")]
    [SerializeField] private float bodyRotationAngle = 70f;


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
    private Quaternion _targetRotation; // NOWE: Przechowywanie celu rotacji dla Root Motion
    private bool _shouldManualRotate;   // NOWE: Czy skrypt ma przejąć kontrolę nad rotacją?
    private float _currentPelvisOffset;
    private float _originalAnimatorY;
    private float _leftFootWeight;
    private float _rightFootWeight;
    
    private int _leftWeightHash;
    private int _rightWeightHash;
    private bool _hasLeftParam;
    private bool _hasRightParam;
    
    // Cache Hashy dla wydajności
    private int _idleStateHash;
    private int _idleTagHash;
    private int _nothingStateHash;

    public bool isMovementLocked = false; // NOWE: Lock postaci podczas dialogu!

    // Cache dla Gizmosów (zamiast alokowania setek razy na sekundę)
    private float _cachedLeftGroundY;
    private float _cachedRightGroundY;
    
    // Zoptymalizowana tablica dla Raycastów (0 alokacji GC)
    private RaycastHit[] _raycastHitsAlloc = new RaycastHit[10];

    // Indeks warstwy Actions w Animatorze (Base=0, UpperBody=1, Actions=2)
    // Musi zgadzać się z PlayerCombat.cs!
    private const int ACTIONS_LAYER = 2;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        _playerCombat = GetComponent<PlayerCombat>();
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

            // Cache hashy stanów
            _idleStateHash = Animator.StringToHash("Idle");
            _idleTagHash = Animator.StringToHash("Idle");
            _nothingStateHash = Animator.StringToHash("Nothing");
        }
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead) return;

        // Jeśli gra zablokowała nam ruch (np. NPC wciągnął nas w dialog)
        if (isMovementLocked)
        {
            _moveInput = Vector2.zero;
            animator.SetFloat("ForwardSpeed", 0f, dampingTime, Time.deltaTime);
            animator.SetFloat("SidewaysSpeed", 0f, dampingTime, Time.deltaTime);
            ApplyGravity();
            return;
        }

        _moveInput = inputReader.MovementValue;
        _isGrounded = controller.isGrounded;

        // --- SYSTEM ROTACJI (REŻYSERSKA PRECYZJA) ---
        AnimatorStateInfo baseState = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo actionState = animator.GetCurrentAnimatorStateInfo(ACTIONS_LAYER);
        bool isActionPlaying = actionState.shortNameHash != _nothingStateHash || animator.IsInTransition(ACTIONS_LAYER);
        
        bool canRotate = true; 
        float currentRotationSpeed = rotationSpeed;

        if (_playerCombat != null)
        {
            // TWARDA BLOKADA (Dodge lub Aktywny Hitbox/Recovery do momentu Cancel)
            if (_playerCombat.IsDodgingAnim || _playerCombat.IsRotationLocked)
            {
                canRotate = false;
                
                // === FAILSAFE (SAMOLECZENIE) ===
                // Jeśli jakimś cudem flaga została (np. brak eventu), a my już biegamy lub stoimy w Idle - ODBLOKUJ.
                if (!isActionPlaying && (baseState.shortNameHash == _idleStateHash || baseState.tagHash == _idleTagHash))
                {
                    canRotate = true;
                    _playerCombat.ResetCombatFlags(); // Czyścimy syf w skrypcie Combat
                }
            }
            // FAZA WIND-UP (ZAMACH)
            else if (isActionPlaying)
            {
                canRotate = true;
                currentRotationSpeed = rotationSpeed * 0.30f;
            }
        }
        
        bool lockedOn = targetHandler != null && targetHandler.IsLockedOn;
        animator.SetFloat("LockedIn", lockedOn ? 1f : 0f, dampingTime, Time.deltaTime);

        if (lockedOn)
        {
            if (canRotate && targetHandler.CurrentTarget != null)
            {
                Vector3 dir = targetHandler.CurrentTarget.position - transform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.1f)
                {
                    bool isMoving = _moveInput.magnitude > 0.05f;
                    bool snappingFlag = lookController != null && (lookController.IsSnapping || lookController.CheckSnapThreshold());

                    // --- SYSTEM ROTACJI (NOWA LOGIKA) ---
                    _shouldManualRotate = snappingFlag || (canRotate && isMoving);
                    if (_shouldManualRotate)
                    {
                        _targetRotation = Quaternion.LookRotation(dir);
                        
                        // Logika pomocnicza dla Update (żeby np. kamera widziała że się obracamy)
                        if (snappingFlag && !isMoving) 
                            Debug.Log("<color=orange>[SNAP] Przygotowuję obrót w miejscu...</color>");
                    }
                }
            }

            animator.SetFloat("ForwardSpeed",   _moveInput.y, dampingTime, Time.deltaTime);
            animator.SetFloat("SidewaysSpeed",  _moveInput.x, dampingTime, Time.deltaTime);
        }
        else
        {
            float speed = _moveInput.magnitude;
            animator.SetFloat("ForwardSpeed", speed, dampingTime, Time.deltaTime);
            animator.SetFloat("SidewaysSpeed", 0f, dampingTime, Time.deltaTime);

            if (canRotate && speed > 0.01f)
            {
                Vector3 moveDir = CalculateMovement();
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), currentRotationSpeed * Time.deltaTime);
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

        // === FROMSOFTWARE FIX: Podczas ataku/uniku IK jest wygaszany ===
        // Gdy cokolwiek gra na warstwie Actions (atak, heavy, dodge),
        // animacja jest ręczna i IK by ją psuło – odpuszczamy wagę.
        AnimatorStateInfo actionState = animator.GetCurrentAnimatorStateInfo(ACTIONS_LAYER);
        bool isActionsPlaying = actionState.shortNameHash != _nothingStateHash || animator.IsInTransition(ACTIONS_LAYER);
        if (isActionsPlaying)
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
        // ===================================================================

        // Wartości absolutne World Y + wektory kąta nachylenia (Normal)
        Vector3 leftNormal, rightNormal;
        float leftGroundWorldY = SampleGroundHeight(leftFootBone, out leftNormal);
        float rightGroundWorldY = SampleGroundHeight(rightFootBone, out rightNormal);
        
        // Zapisujemy do cache'a, żeby OnDrawGizmos nie musiało tego liczyć od nowa
        _cachedLeftGroundY = leftGroundWorldY;
        _cachedRightGroundY = rightGroundWorldY;

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
        // OPTYMALIZACJA: RaycastNonAlloc nie generuje garbage collections
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

        // Używamy zbuforowanych wartości z LateUpdate ZAMIAST robić podwójnych strzałów Raycast
        DrawDebugMarker(leftFootBone, _cachedLeftGroundY, _leftFootWeight);
        DrawDebugMarker(rightFootBone, _cachedRightGroundY, _rightFootWeight);
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

        // Jeśli skrypt (Snap/Strafe) chce rotacji, to my tu rządzimy, a nie Idle!
        if (_shouldManualRotate)
        {
            // Sprawdzamy czy to SNAP czy zwykły ruch (dla prędkości)
            bool isSnapping = lookController != null && (lookController.IsSnapping || lookController.CheckSnapThreshold());
            
            if (isSnapping)
            {
                float snapSpeed = rotationSpeed * snapRotationMultiplier * 60f;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, snapSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            transform.rotation *= deltaRot;
        }
        
        _shouldManualRotate = false; // Reset na koniec klatki 
    }
}