using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Zdrowie")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("System Ran (Wounds)")]
    public int maxWounds = 10;
    public float timeToResetWounds = 4.0f;
    
    private int _currentWounds = 0;
    private float _woundTimer = 0f;
    private bool _isBroken = false; 
    private bool _isDead = false;

    [Header("Anty-Stunlock (Poise)")]
    [SerializeField] private float maxPoise = 100f;
    [SerializeField] private float poiseResetRate = 20f;
    [SerializeField] private float staggerCooldown = 0.5f; 
    
    private float _currentPoise;
    private float _lastStaggerTime = -10f;

    [Header("UI")]
    [SerializeField] private EnemyUI enemyUI;

    [Header("Odniesienia")]
    private Animator _animator;
    private NavMeshAgent _agent;
    private int _attackTagHash;

    public bool IsBroken => _isBroken;
    public bool IsDead => _isDead;

    void Start()
    {
        currentHealth = maxHealth;
        _currentPoise = maxPoise;
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _attackTagHash = Animator.StringToHash("Attack");
    }

    void Update()
    {
        if (_isBroken || _isDead) return;

        // Regeneracja Poise z czasem (fromsoftware style)
        if (_currentPoise < maxPoise)
        {
            _currentPoise = Mathf.MoveTowards(_currentPoise, maxPoise, poiseResetRate * Time.deltaTime);
        }

        if (_currentWounds > 0)
        {
            _woundTimer -= Time.deltaTime;
            if (_woundTimer <= 0) ResetWounds();
        }
    }

   public void TakeDamage(int damage, bool isKnockback = false, float poiseDamage = 50f)
    {
        if (_isDead) return;

        if (_isBroken)
        {
            ExecuteDeathblow();
            return;
        }

        currentHealth -= damage;

        // Budzimy AI (działa z KAŻDYM typem wroga dzięki EnemyBase!)
        EnemyBase ai = GetComponent<EnemyBase>();
        if (ai != null) ai.OnDamagedByPlayer();

        // Jeśli wróg jest w stanie Broken, nie dodajemy kolejnych ran - on już "pęka"
        if (!_isBroken) AddWound();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        CheckForStagger(isKnockback, poiseDamage);
        UpdateUI();
    }

    private void AddWound()
    {
        _currentWounds++;
        _woundTimer = timeToResetWounds; 
        UpdateUI();

        if (_currentWounds >= maxWounds) TriggerDeathblowState();
    }

    public void ResetWounds()
    {
        _currentWounds = 0;
        _woundTimer = 0f;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (enemyUI != null)
        {
            Debug.Log($"<color=cyan>[UI] Aktualizacja paska wroga: HP={currentHealth}/{maxHealth}</color>");
            enemyUI.UpdateStatus(currentHealth, maxHealth, _currentWounds, maxWounds, _isBroken);
        }
        else
        {
            Debug.LogWarning($"<color=red>[UI] BRAK ENEMY UI na obiekcie {gameObject.name}!</color>");
        }
    }

    private void CheckForStagger(bool isKnockback = false, float poiseDamage = 0)
    {
        if (_animator == null || _isBroken) return;

        _currentPoise -= poiseDamage;

        // Jeśli wróg ma jeszcze niewidzialną "pamięć" po poprzednim ciosie - ignorujemy fizyczny stagger (anty-stunlock)
        if (Time.time < _lastStaggerTime + staggerCooldown) return;

        // JEŚLI POISE NIE PĘKŁO - WRÓG PRZYJMUJE Cios "na klatę" (tylko efekt wizualny/dźwiękowy)
        if (_currentPoise > 0)
        {
            // Można tu dodać mały "flinch" lub krew, ale nie przerywamy animacji ataku!
            return;
        }

        // PĘKNIĘCIE POISE! Resetujemy pasek i odpalamy stagger
        _currentPoise = maxPoise;

        _lastStaggerTime = Time.time;
        if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;

        // Informujemy mózg AI, że dostał w twarz i ma zresetować swoje plany (przełączyć na stan Stagger)
        SendMessage("ForceInterrupt", SendMessageOptions.DontRequireReceiver);

        if (isKnockback) 
        {
            _animator.SetTrigger("Knockback");
        }
        else 
        {
            _animator.SetTrigger("HitReaction");
        }
    }

    void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _isBroken = false;

        // --- KLUCZOWE: Odcinamy AI od prądu i ZAMYKAMY HITBOXY! ---
        SendMessage("CloseDamage", SendMessageOptions.DontRequireReceiver); 
        SendMessage("ForceInterrupt", SendMessageOptions.DontRequireReceiver);
        
        EnemyBase aiBase = GetComponent<EnemyBase>();
        if (aiBase != null) aiBase.enabled = false;

        if (_agent != null && _agent.isOnNavMesh) 
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        if (_animator != null) 
        {
            _animator.ResetTrigger("HitReaction"); // Kasujemy chęć reakcji na cios
            _animator.ResetTrigger("Attack");      // Kasujemy ataki
            _animator.ResetTrigger("Attack2");
            _animator.SetTrigger("Die");           // Odpalamy śmierć od razu
        }
        
        if(enemyUI != null) enemyUI.gameObject.SetActive(false);

        Collider col = GetComponent<Collider>();
        if(col != null) col.enabled = false;
        
        Destroy(gameObject, 5f);
    }

    private void TriggerDeathblowState()
    {
        _isBroken = true;
        if (_agent != null && _agent.isOnNavMesh) 
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }
        if(_animator != null) _animator.SetBool("IsBroken", true);
        UpdateUI();
    }

    public void ExecuteDeathblow()
    {
        if (!_isBroken) return;
        _isBroken = false;
        if(_animator != null) _animator.SetBool("IsBroken", false);
        Die();
    }
}
