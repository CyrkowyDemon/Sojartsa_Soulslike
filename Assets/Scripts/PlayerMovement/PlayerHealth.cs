using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    [SerializeField] private Animator animator;
    private bool _isDead = false;
    public bool IsDead => _isDead;

    [SerializeField] private bool _isInvincible = false; // I-Frames z dasha

    [Header("UI")]
    [SerializeField] private PlayerHUD hud;

    // === SYSTEM RAN (POISE / POSTURA) ===
    [Header("System Ran (Poise)")]
    [SerializeField] private float maxWounds = 100f;
    [SerializeField] private float woundsPerHit = 25f;       // Ile ran dodaje każdy cios
    [SerializeField] private float woundDecayDelay = 2f;     // Ile sekund spokoju zanim pasek zacznie opadać
    [SerializeField] private float woundDecayRate = 20f;     // Jak szybko opada pasek (jednostki/sekundę)
    [SerializeField] private float staggerDuration = 1.5f;   // Ile sekund gracz jest ogłuszony

    private float _currentWounds = 0f;
    private float _lastHitTime = -999f;   // Kiedy ostatnio dostaliśmy cios
    private bool _isStaggered = false;    // Czy jesteśmy teraz ogłuszeni

    public bool IsStaggered => _isStaggered;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        if (hud != null) 
        {
            hud.UpdateHP(currentHealth, maxHealth);
            hud.UpdateWounds(0f, maxWounds);
        }
    }

    void Update()
    {
        // Jeśli rany są zerowe lub jesteśmy martwi/ogłuszeni - nic nie rób (zero kosztu)
        if (_currentWounds <= 0f || _isDead || _isStaggered) return;

        // Odczekaj chwilę po ostatnim ciosie zanim pasek zacznie opadać (jak w Sekiro)
        if (Time.time < _lastHitTime + woundDecayDelay) return;

        // Płynne opadanie paska (jak w Sekiro)
        _currentWounds -= woundDecayRate * Time.deltaTime;
        _currentWounds = Mathf.Max(0f, _currentWounds);

        if (hud != null) hud.UpdateWounds(_currentWounds, maxWounds);
    }

    public void TakeDamage(int damage, bool isKnockback = false)
    {
        if (_isDead) return;

        if (_isInvincible)
        {
            Debug.Log("<color=cyan>[I-FRAME] Gracz przetanczyl przez atak!</color>");
            return;
        }

        // === Dodaj rany (Poise damage) ===
        _lastHitTime = Time.time;
        _currentWounds += woundsPerHit;

        if (_currentWounds >= maxWounds)
        {
            // PĘKNIĘCIE - ogłuszenie!
            _currentWounds = 0f;
            if (hud != null) hud.UpdateWounds(0f, maxWounds);
            TriggerStagger();
        }
        else
        {
            if (hud != null) hud.UpdateWounds(_currentWounds, maxWounds);
        }
        // ====================================

        currentHealth -= damage;

        // ---- POPRAWKA: Resetuj rany tylko jesli wrog NIE jest w stanie Broken ----
        EnemyHealth[] allEnemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in allEnemies)
        {
            if (!enemy.IsBroken) 
            {
                enemy.ResetWounds();
            }
        }

        if (hud != null) hud.UpdateHP(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (!_isStaggered) // Nie przerywaj animacji ogłuszenia zwykłą reakcją na cios
        {
            // Zamykamy miecz, jeśli oberwaliśmy w trakcie wymachu!
            SendMessage("CloseDamage", SendMessageOptions.DontRequireReceiver);
            
            if (animator != null) 
            {
                if (isKnockback) 
                {
                    animator.SetTrigger("Knockback");
                }
                else 
                {
                    animator.SetTrigger("HitReaction");
                }
            }
        }
    }

    // Ogłuszenie po przepełnieniu paska ran (jak Sekiro)
    private void TriggerStagger()
    {
        if (_isStaggered) return; // Nie stackuj ogłuszenia

        _isStaggered = true;
        SendMessage("CloseDamage", SendMessageOptions.DontRequireReceiver);

        if (animator != null)
        {
            // Resetujemy inne triggery żeby Knockback nie czekał w kolejce
            animator.ResetTrigger("HitReaction");
            animator.SetTrigger("Knockback"); // <- Podmień na "Stagger" jak będziesz miał animację
        }

        // Po czasie ogłuszenia - odblokowujemy gracza
        Invoke(nameof(EndStagger), staggerDuration);
    }

    private void EndStagger()
    {
        _isStaggered = false;
    }

    void Die()
    {
        if (_isDead) return;
        _isDead = true;
        
        // Zamykamy miecz przy zgonie
        SendMessage("CloseDamage", SendMessageOptions.DontRequireReceiver);
        
        if (animator != null) animator.SetTrigger("Die");
        Invoke("RestartScene", 3f);
    }
    
    private void RestartScene() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    // === ANIMATION EVENTS: Dodaj na animacji dashu! ===
    public void EnableIFrames() { _isInvincible = true; }
    public void DisableIFrames() { _isInvincible = false; }

    /// <summary>
    /// Ożywia gracza i odnawia mu zdrowie (Używane przy teleportacji)
    /// </summary>
    public void Revive(int healthPercent = 100)
    {
        _isDead = false;
        _isStaggered = false;
        _currentWounds = 0f;
        currentHealth = Mathf.CeilToInt(maxHealth * (healthPercent / 100f));
        
        if (animator != null) 
        {
            animator.SetTrigger("Recover"); 
            animator.ResetTrigger("Die");
        }

        if (hud != null) 
        {
            hud.UpdateHP(currentHealth, maxHealth);
            hud.UpdateWounds(0f, maxWounds);
        }
    }
}
