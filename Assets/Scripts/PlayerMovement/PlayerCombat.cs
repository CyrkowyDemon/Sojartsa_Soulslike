using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Animator animator;
    [SerializeField] private TargetHandler targetHandler;

    [Header("Nowy System Miecza")]
    [SerializeField] private WeaponHitbox weaponHitbox;
    [SerializeField] private float bufferWindow = 0.5f;

    // Indeks warstwy "Actions" w Animatorze (Base=0, UpperBody=1, Actions=2)
    private const int ACTIONS_LAYER = 2;

    private bool _attackBuffered = false;
    private float _bufferTimer = 0f;

    private void OnEnable() 
    { 
        inputReader.AttackEvent += HandleAttackInput; 
        inputReader.HeavyAttackEvent += HandleHeavyAttackInput;
        inputReader.DodgeEvent += HandleDodgeInput; 
    }
    
    private void OnDisable() 
    { 
        inputReader.AttackEvent -= HandleAttackInput; 
        inputReader.HeavyAttackEvent -= HandleHeavyAttackInput;
        inputReader.DodgeEvent -= HandleDodgeInput; 
    }

    private void Update()
    {
        if (_bufferTimer > 0)
        {
            _bufferTimer -= Time.deltaTime;
            if (_bufferTimer <= 0) _attackBuffered = false;
        }
    }

    private void HandleAttackInput()
    {
        // Sprawdzamy warstwę ACTIONS (2), bo tam siedzą ataki!
        bool isIdle = animator.GetCurrentAnimatorStateInfo(ACTIONS_LAYER).IsName("Nothing");
        bool canAttack = animator.GetBool("CanCancel") || isIdle;

        if (canAttack)
        {
            ExecuteAttack();
        }
        else
        {
            _attackBuffered = true;
            _bufferTimer = bufferWindow;
        }
    }

    private void HandleHeavyAttackInput()
    {
        bool isIdle = animator.GetCurrentAnimatorStateInfo(ACTIONS_LAYER).IsName("Nothing");
        bool canAttack = animator.GetBool("CanCancel") || isIdle;

        if (canAttack)
        {
            _attackBuffered = false;
            _bufferTimer = 0;
            animator.SetBool("CanCancel", false);

            // Czyścimy zaległe triggery, żeby nie "czekały" w kolejce
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("HeavyAttack");
            animator.ResetTrigger("Dodge");

            animator.SetTrigger("HeavyAttack");
        }
    }

    private void ExecuteAttack()
    {
        _attackBuffered = false;
        _bufferTimer = 0;

        animator.SetBool("CanCancel", false);

        // Czyścimy WSZYSTKIE zaległe triggery z pamięci Animatora
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("HeavyAttack");
        animator.ResetTrigger("Dodge");

        animator.SetTrigger("Attack");
    }

    private void HandleDodgeInput()
    {
        bool isIdle = animator.GetCurrentAnimatorStateInfo(ACTIONS_LAYER).IsName("Nothing");
        bool canDodge = animator.GetBool("CanCancel") || isIdle;

        if (canDodge)
        {
            _attackBuffered = false;
            _bufferTimer = 0;

            Vector2 dodgeDir = inputReader.MovementValue;

            bool lockedOn = targetHandler != null && targetHandler.IsLockedOn;

            if (!lockedOn)
            {
                if (dodgeDir.sqrMagnitude > 0.01f)
                    dodgeDir = new Vector2(0f, 1f);  // Do przodu
                else
                    dodgeDir = new Vector2(0f, -1f); // Do tylu
            }
            else
            {
                if (dodgeDir == Vector2.zero) dodgeDir = new Vector2(0f, -1f);
            }

            animator.SetFloat("DodgeX", dodgeDir.x);
            animator.SetFloat("DodgeY", dodgeDir.y);

            // Zamykamy hitbox miecza, jeśli cancelujemy atak unikiem
            if(weaponHitbox != null) weaponHitbox.CloseDamageWindow();

            animator.SetBool("CanCancel", false);

            // Czyścimy zaległe triggery
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("HeavyAttack");
            animator.ResetTrigger("Dodge");

            animator.SetTrigger("Dodge");
        }
    }

    // --- NOWE ANIMATION EVENTS ---

    // Zamiast uderzać od razu, po prostu Otwieramy miecz.
    // Dodaj to na początku wymachu
    public void OpenDamage()
    {
        if (weaponHitbox != null) weaponHitbox.OpenDamageWindow();
    }

    // Kiedy wymach się kończy i miecz leci w dół / przed powrotem
    // Dodaj to w połowie / na końcu animacji uderzenia
    public void CloseDamage()
    {
        if (weaponHitbox != null) weaponHitbox.CloseDamageWindow();
    }


    // Wywoływane przez Animation Event w fazie recovery (~75-80% animacji)
    public void EnableCancel()
    {
        animator.SetBool("CanCancel", true);

        // Jeśli gracz kliknął w czasie ataku (bufor), odpala zapamiętany atak
        if (_attackBuffered) ExecuteAttack();
    }

    // Wywoływana przez Animation Event na końcu animacji (reset do czystego stanu)
    public void ResetCombatFlags()
    {
        _attackBuffered = false;
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("HeavyAttack");
        animator.ResetTrigger("Dodge");
    }
}
