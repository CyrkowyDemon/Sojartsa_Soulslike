using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Animator animator;
    [SerializeField] private TargetHandler targetHandler;

    [Header("Nowy System Miecza")]
    [SerializeField] private WeaponHitbox weaponHitbox; // Zamiast samej warstwy i kuli, trzymamy tu skrypt od miecza
    [SerializeField] private float bufferWindow = 0.5f;

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
        bool canAttack = animator.GetBool("CanCancel") || animator.GetCurrentAnimatorStateInfo(0).IsTag("Idle");

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
        bool canAttack = animator.GetBool("CanCancel") || animator.GetCurrentAnimatorStateInfo(0).IsTag("Idle");

        if (canAttack)
        {
            _attackBuffered = false;
            _bufferTimer = 0;
            animator.SetBool("CanCancel", false); 
            animator.SetTrigger("HeavyAttack");
        }
    }

    private void ExecuteAttack()
    {
        _attackBuffered = false;
        _bufferTimer = 0;

        animator.SetBool("CanCancel", false); 

        // POPRAWKA: Pucujemy zalegle klikniecia z pamieci Animatora
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("HeavyAttack");

        animator.SetTrigger("Attack"); 
    }

    private void HandleDodgeInput()
    {
        bool canDodge = animator.GetBool("CanCancel") || animator.GetCurrentAnimatorStateInfo(0).IsTag("Idle");

        if (canDodge)
        {
            _attackBuffered = false;
            _bufferTimer = 0;

            Vector2 dodgeDir = inputReader.MovementValue;

            bool lockedOn = targetHandler != null && targetHandler.IsLockedOn;

            if (!lockedOn)
            {
                // Bez Lock-ona: tylko przod/tyl, zero boku
                // Jesli gracz idzie gdziekolwiek -> unik do przodu
                // Jesli stoi -> unik do tylu
                if (dodgeDir.sqrMagnitude > 0.01f)
                    dodgeDir = new Vector2(0f, 1f);  // Do przodu
                else
                    dodgeDir = new Vector2(0f, -1f); // Do tylu
            }
            else
            {
                // Z Lock-onem: pelna swoboda kierunkow
                if (dodgeDir == Vector2.zero) dodgeDir = new Vector2(0f, -1f);
            }

            animator.SetFloat("DodgeX", dodgeDir.x);
            animator.SetFloat("DodgeY", dodgeDir.y);

            // Wymusza zamknięcie hitboksa, jeśli anulujemy atak unikiem z pomocą CanCancel
            if(weaponHitbox != null) weaponHitbox.CloseDamageWindow();

            animator.SetBool("CanCancel", false);
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


    // Reszta pozostaje bez zmian
    public void EnableCancel()
    {
        animator.SetBool("CanCancel", true);
        if (_attackBuffered) ExecuteAttack();
    }

    public void ResetCombatFlags()
    {
        _attackBuffered = false;
    }
}
