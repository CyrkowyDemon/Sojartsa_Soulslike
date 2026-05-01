using UnityEngine;

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

    private float _verticalVelocity;
    private bool _isGrounded;
    private Vector2 _moveInput;
    private Quaternion _targetRotation; // NOWE: Przechowywanie celu rotacji dla Root Motion
    private bool _shouldManualRotate;   // NOWE: Czy skrypt ma przejąć kontrolę nad rotacją?
    private bool _isSnappingActive;     // NOWE: Flag dla LateUpdate
    private float _currentRotationSpeed; // NOWE: Prędkość dla LateUpdate
    
    // Cache Hashy dla wydajności
    private int _idleStateHash;
    private int _idleTagHash;
    private int _nothingStateHash;

    public bool isMovementLocked = false; // NOWE: Lock postaci podczas dialogu!

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
        if (animator != null) 
        {
            // --- FAILSAFE REFERENCJI ---
            if (lookController == null)
            {
                lookController = GetComponent<PlayerLookController>();
                if (lookController == null) 
                    lookController = GetComponentInChildren<PlayerLookController>();
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
                        _isSnappingActive = snappingFlag;
                        _currentRotationSpeed = currentRotationSpeed;
                        
                        // Logika pomocnicza dla Update (żeby np. kamera widziała że się obracamy)
                        if (snappingFlag && !isMoving) 
                            Debug.Log($"<color=orange>[SNAP] Wykryto twardy SNAP!</color>");
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
        // === OSTATECZNE WYMUSZENIE ROTACJI (PO ANIMATORZE) ===
        if (_shouldManualRotate && targetHandler != null && targetHandler.IsLockedOn)
        {
            if (_isSnappingActive)
            {
                // Zamiast RotateTowards (które jest mechaniczne i brutalne), używamy szybkiego Slerpa
                // Da to efekt płynnego "doskoczenia" do celu z ładnym miękkim lądowaniem ramy.
                float snapSpeed = rotationSpeed * snapRotationMultiplier;
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, snapSpeed * Time.deltaTime);
            }
            else
            {
                // Zwykły płynny obrót przy chodzeniu
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, _currentRotationSpeed * Time.deltaTime);
            }
            _shouldManualRotate = false; // zużyte
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

        // Jeśli NIE ma Snapa ani Strafe'u z lockiem, to Animator decyduje o obrocie
        // ManualRotation jest teraz robione w LateUpdate (aby nadpisało wszystko po Animatorze)
        if (!_shouldManualRotate)
        {
            transform.rotation *= deltaRot;
        }
    }
}