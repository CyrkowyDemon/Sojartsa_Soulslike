using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private TargetHandler targetHandler;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float dampingTime = 0.1f;
    [SerializeField] private float gravity = -30f;

    private Vector2 _moveInput;
    private float _verticalVelocity;
    private bool _isGrounded;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public Vector2 MoveInput => _moveInput;
    public bool IsGrounded => _isGrounded;

    private void Update()
    {
        _moveInput = inputReader.MovementValue;
        _isGrounded = controller.isGrounded;

        bool canRotate = animator.GetBool("CanRotate") || animator.GetCurrentAnimatorStateInfo(0).IsTag("Idle") || animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
        bool lockedOn = targetHandler != null && targetHandler.IsLockedOn;

        // === POPRAWKA BUG: Miękkie przejście Lock-ona (Soft Blend) ===
        // Używamy dampingTime (0.15f), żeby przejście między Idle a Strafe nie było "puknięciem"
        animator.SetFloat("LockedIn", lockedOn ? 1f : 0f, 0.15f, Time.deltaTime);

        if (lockedOn)
        {
            if (canRotate && targetHandler.CurrentTarget != null)
            {
                Vector3 dir = targetHandler.CurrentTarget.position - transform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.1f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
                }
            }

            // === POPRAWKA BUG #2: Poprawna kolejnosc argumentow w SetFloat! ===
            // SetFloat(name, value, dampingTime, deltaTime)
            animator.SetFloat("ForwardSpeed", _moveInput.y, dampingTime, Time.deltaTime);
            animator.SetFloat("SidewaysSpeed", _moveInput.x, dampingTime, Time.deltaTime);
        }
        else
        {
            // BEZ LOCK-ONA: uzywamy tylko ForwardSpeed (magnitude)
            float intensity = _moveInput.magnitude;
            animator.SetFloat("ForwardSpeed", intensity, dampingTime, Time.deltaTime);
            animator.SetFloat("SidewaysSpeed", 0f, dampingTime, Time.deltaTime);

            if (canRotate && _moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 moveDir = CalculateMovement();
                if (moveDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), rotationSpeed * Time.deltaTime);
                }
            }
        }

        ApplyGravity();
    }

    private void ApplyGravity()
    {
        if (_isGrounded && _verticalVelocity < 0) _verticalVelocity = -2f;
        else _verticalVelocity += gravity * Time.deltaTime;
    }

    public Vector3 CalculateMovement()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        return (forward * _moveInput.y + right * _moveInput.x);
    }

    private void OnAnimatorMove()
    {
        Vector3 v = animator.deltaPosition;
        v.y = _verticalVelocity * Time.deltaTime;
        controller.Move(v);
    }
}
