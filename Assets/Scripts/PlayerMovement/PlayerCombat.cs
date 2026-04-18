using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Animator animator;
    [SerializeField] private TargetHandler targetHandler;

    [Header("Nowy System Miecza")]
    [SerializeField] private WeaponHitbox weaponHitbox;
    [SerializeField] private float bufferWindow = 0.5f;
    [SerializeField] private float dodgeDelay = 0.5f; // NOWOŚĆ: Czas na obrót przed unikiem

    // Indeks warstwy "Actions" w Animatorze (Base=0, UpperBody=1, Actions=2)
    private const int ACTIONS_LAYER = 2;

    public bool IsDamageWindowOpen { get; private set; } 
    public bool IsDodgingAnim { get; private set; } 
    public bool IsRotationLocked { get; private set; } // NOWOŚĆ: Twarda blokada obrotu (od uderzenia do cancela)

    private bool _attackBuffered = false;
    private float _bufferTimer = 0f;
    private int _nothingStateHash;

    private Coroutine _dodgeCoroutine; // NOWOŚĆ: Referencja do asynchronicznego uniku

    private void Start()
    {
        _nothingStateHash = Animator.StringToHash("Nothing");
    }

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
        bool isIdle = animator.GetCurrentAnimatorStateInfo(ACTIONS_LAYER).shortNameHash == _nothingStateHash;
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
        bool isIdle = animator.GetCurrentAnimatorStateInfo(ACTIONS_LAYER).shortNameHash == _nothingStateHash;
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
        bool isIdle = animator.GetCurrentAnimatorStateInfo(ACTIONS_LAYER).shortNameHash == _nothingStateHash;
        bool canDodge = animator.GetBool("CanCancel") || isIdle;

        if (canDodge)
        {
            // Natychmiast zabezpieczamy flagi, by gracz nie wcisnął spamu guzików w trakcie opóźnienia!
            animator.SetBool("CanCancel", false); 
            _attackBuffered = false;
            _bufferTimer = 0;

            if (_dodgeCoroutine != null) StopCoroutine(_dodgeCoroutine);
            
            // ODŁAPUJEMY UNIK Z DELAYEM (używamy zmiennej dodgeDelay z Inspektora)
            _dodgeCoroutine = StartCoroutine(ExecuteDodgeWithDelay(dodgeDelay));
        }
    }

    private System.Collections.IEnumerator ExecuteDodgeWithDelay(float delay)
    {
        // 1. Oczekiwanie mgnienia oka - czas dla gracza na dogięcie analoga/klawiszy pod napięciem
        yield return new WaitForSeconds(delay);

        // 2. Pobieramy wektor dopiero "Z PRZYSZŁOŚCI", czyli te 0.15s później!
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

        // Zamykamy hitbox miecza awaryjnie w razie czego
        if(weaponHitbox != null) weaponHitbox.CloseDamageWindow();
        IsDamageWindowOpen = false;
        IsDodgingAnim = true; // --- ZACZYNAMY FIZYCZNY UNIK, BLOKUJEMY ROTACJĘ ---

        // Czyścimy zaległe triggery przed włożeniem nowego
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("HeavyAttack");
        animator.ResetTrigger("Dodge");

        animator.SetTrigger("Dodge");
    }

    // --- NOWE ANIMATION EVENTS ---

    // Zamiast uderzać od razu, po prostu Otwieramy miecz.
    // Dodaj to na początku wymachu
    public void OpenDamage()
    {
        if (weaponHitbox != null) weaponHitbox.OpenDamageWindow();
        IsDamageWindowOpen = true; 
        IsRotationLocked = true; // --- BLOKADA START ---
    }

    public void CloseDamage()
    {
        if (weaponHitbox != null) weaponHitbox.CloseDamageWindow();
        IsDamageWindowOpen = false;
        // UWAGA: Nie wyłączamy IsRotationLocked tutaj (zgodnie z prośbą Reżysera!)
    }

    public void EnableCancel()
    {
        animator.SetBool("CanCancel", true);
        IsRotationLocked = false; // --- BLOKADA KONIEC (nareszcie wolny!) ---

        if (_attackBuffered) ExecuteAttack();
    }

    public void ResetCombatFlags()
    {
        _attackBuffered = false;
        IsDamageWindowOpen = false; 
        IsDodgingAnim = false; 
        IsRotationLocked = false; // Awaryjne puszczenie na wszelki wypadek
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("HeavyAttack");
        animator.ResetTrigger("Dodge");
    }
}
