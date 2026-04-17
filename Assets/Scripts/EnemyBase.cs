using UnityEngine;

// Wymusza, żeby na obiekcie zawsze był EnemyHealth
[RequireComponent(typeof(EnemyHealth))] 
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Detekcja (Wzrok) - Z bazy!")]
    [SerializeField] protected float lookRange = 15f;
    [SerializeField] protected float fieldOfViewAngle = 120f;
    [SerializeField] protected LayerMask obstacleMask;

    [SerializeField] protected float lookInterval = 0.2f; // Co ile sekund sprawdzać wzrok
    
    protected Transform _target;
    protected EnemyHealth _health;
    protected bool _isInCombat = false;
    
    private float _lookTimer;
    private bool _cachedCanSeePlayer;

    protected virtual void Start()
    {
        _health = GetComponent<EnemyHealth>();
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _target = player.transform;
        }
    }

    protected virtual void Update()
    {
        // Jeśli wróg jest martwy, albo nie ma gracza - ignoruj
        if (_health != null && (_health.IsBroken || _health.IsDead)) return;
        if (_target == null) return;

        // Odpalamy zachowanie DZIECKA (np. chodzenie po ziemi albo latanie)
        UpdateBehavior();
    }

    // Ta funkcja ZMUSZA każde dziecko do napisania własnego ruchu!
    protected abstract void UpdateBehavior();

    // Gotowy system wzroku, który każde dziecko dostaje za darmo
    // Gotowy system wzroku, który każde dziecko dostaje za darmo
    // Optymalizacja: Używamy sqrDistance, by nie liczyć pierwiastka w Update
    protected bool CanSeePlayer(float sqrDistance)
    {
        // Jeśli jesteśmy już w walce, wzrok nas nie interesuje
        if (_isInCombat) return true;

        // Ticking: Sprawdzamy Linecast tylko co lookInterval sekund
        if (Time.time < _lookTimer) return _cachedCanSeePlayer;
        
        _lookTimer = Time.time + lookInterval;

        if (sqrDistance > lookRange * lookRange) 
        {
            _cachedCanSeePlayer = false;
            return false;
        }

        Vector3 directionToPlayer = (_target.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer < fieldOfViewAngle / 2f)
        {
            Vector3 rayStart = transform.position + Vector3.up * 1.5f; 
            Vector3 rayEnd = _target.position + Vector3.up * 1.5f;

            if (!Physics.Linecast(rayStart, rayEnd, obstacleMask))
            {
                _cachedCanSeePlayer = true;
                return true; 
            }
        }
        
        _cachedCanSeePlayer = false;
        return false;
    }

    // Odpalane przez miecz gracza
    public virtual void OnDamagedByPlayer()
    {
        if (!_isInCombat)
        {
            Debug.Log($"<color=orange>[AI] {gameObject.name}: Oberwałem! Wchodzę w tryb walki!</color>");
            _isInCombat = true;
        }
    }

    // Rysowanie zasięgu wzroku w Unity (Dla wszystkich wrogów na raz!)
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lookRange);

        Gizmos.color = Color.blue;
        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2f, 0) * transform.forward;
        Gizmos.DrawRay(eyePos, leftBoundary * lookRange);
        Gizmos.DrawRay(eyePos, rightBoundary * lookRange);
    }
}