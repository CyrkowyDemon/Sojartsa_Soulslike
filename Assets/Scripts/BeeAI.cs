using UnityEngine;
using UnityEngine.AI;

// Pszczoła latająca! Dziedziczy wzrok z EnemyBase.
// Używa NavMeshAgent do nawigacji (Option A: baseOffset podnosi ją w powietrze).
// W Unity Inspector ustaw NavMeshAgent.Base Offset na wysokość lotu (np. 2-3).
public class BeeAI : EnemyBase
{
    public enum BeeState { Idle, Chasing, Orbiting, Attacking, Returning }

    [Header("Zasięgi Latające")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float orbitDistance = 5f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float attackCooldownDuration = 2f;
    [SerializeField] private float maxChaseDistance = 25f;

    [Header("Orbita (Krążenie)")]
    [SerializeField] private float minOrbitTime = 2f;
    [SerializeField] private float maxOrbitTime = 4f;

    [Header("System Walki")]
    [SerializeField] private WeaponHitbox weaponHitbox;

    // Referencje
    private NavMeshAgent _agent;
    private Animator _animator;

    // Stan wewnętrzny
    private Vector3 _spawnPosition;
    private BeeState _currentState = BeeState.Idle;
    private float _lastAttackTime = -10f;
    private float _minOrbitEndTime = 0f;
    private int _orbitDir = 1;
    private float _nextOrbitChangeTime = 0f;

    protected override void Start()
    {
        base.Start(); // Szuka gracza i EnemyHealth z bazy

        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _spawnPosition = transform.position;

        // Tak samo jak rycerz: Root Motion steruje ruchem, NavMesh tylko nawiguje
        _agent.updatePosition = false;
        _agent.updateRotation = false;

        _orbitDir = Random.value > 0.5f ? 1 : -1;
    }

    // ========================
    // GŁÓWNA PĘTLA (z EnemyBase)
    // ========================
    protected override void UpdateBehavior()
    {
        float sqrDistance = (_target.position - transform.position).sqrMagnitude;
        float sqrDistanceFromSpawn = (_spawnPosition - transform.position).sqrMagnitude;

        DecideNextState(sqrDistance, sqrDistanceFromSpawn);
        ExecuteState();

        if (_agent.isOnNavMesh) _agent.nextPosition = transform.position;
    }

    // ========================
    // MÓZG PSZCZOŁY
    // ========================
    private void DecideNextState(float sqrDistance, float sqrDistanceFromSpawn)
    {
        if (_currentState == BeeState.Attacking) return;

        // Powrót do gniazda
        if (_currentState == BeeState.Returning)
        {
            if (sqrDistanceFromSpawn < 2f * 2f)
            {
                _isInCombat = false;
                SetState(BeeState.Idle);
            }
            return;
        }

        // Wejście w walkę
        if (!_isInCombat)
        {
            if (CanSeePlayer(sqrDistance)) _isInCombat = true;
            else
            {
                SetState(BeeState.Idle);
                return;
            }
        }
        else if (sqrDistanceFromSpawn > maxChaseDistance * maxChaseDistance || sqrDistance > maxChaseDistance * maxChaseDistance)
        {
            _isInCombat = false;
            SetState(BeeState.Returning);
            return;
        }

        // Blokada orbitowania
        if (_currentState == BeeState.Orbiting && Time.time < _minOrbitEndTime)
        {
            return;
        }

        // Cooldown po ataku
        if (Time.time < _lastAttackTime + attackCooldownDuration)
        {
            if (sqrDistance <= (orbitDistance * 1.5f) * (orbitDistance * 1.5f)) SetState(BeeState.Orbiting);
            else SetState(BeeState.Chasing);
            return;
        }

        // --- DECYZJA ATAKU ---
        float sqrDashRange = 7f * 7f;

        if (sqrDistance <= attackRange * attackRange)
        {
            float roll = Random.value;
            if (roll < 0.7f)
            {
                SetState(BeeState.Attacking); // 70% żądlenie z bliska
            }
            else
            {
                SetState(BeeState.Orbiting);  // 30% odlot i krążenie
            }
        }
        else if (sqrDistance <= sqrDashRange)
        {
            float roll = Random.value;
            if (roll < 0.35f)
            {
                SetState(BeeState.Attacking); // 35% dash-atak
            }
            else
            {
                SetState(BeeState.Orbiting);  // 65% krąży zamiast lecieć
            }
        }
        else
        {
            SetState(BeeState.Chasing);
        }
    }

    private void SetState(BeeState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;

        if (newState == BeeState.Orbiting)
        {
            _minOrbitEndTime = Time.time + Random.Range(minOrbitTime, maxOrbitTime);
            _orbitDir = Random.value > 0.5f ? 1 : -1;
        }

        if (newState == BeeState.Attacking) StartAttack();
    }

    // ========================
    // WYKONANIE STANÓW
    // ========================
    private void ExecuteState()
    {
        switch (_currentState)
        {
            case BeeState.Idle:      DoIdle(); break;
            case BeeState.Chasing:   DoChase(); break;
            case BeeState.Orbiting:  DoOrbit(); break;
            case BeeState.Returning: DoReturn(); break;
            // Attacking: Root Motion z animacji
        }
    }

    private void DoIdle()
    {
        if (_agent.isOnNavMesh) _agent.isStopped = true;
        _animator.SetFloat("ForwardSpeed", 0, 0.1f, Time.deltaTime);
        _animator.SetFloat("SidewaysSpeed", 0, 0.1f, Time.deltaTime);
    }

    private void DoChase()
    {
        // NavMesh nawiguje do gracza (omija przeszkody!)
        if (_agent.isOnNavMesh) _agent.isStopped = false;
        _agent.SetDestination(_target.position);
        SyncAnimatorToMoveDir();
        LookAtTarget();
    }

    private void DoOrbit()
    {
        // Zmieniamy kierunek orbity co jakiś czas
        if (Time.time > _nextOrbitChangeTime)
        {
            _orbitDir = Random.value > 0.5f ? 1 : -1;
            _nextOrbitChangeTime = Time.time + Random.Range(2f, 4f);
        }

        if (_agent.isOnNavMesh) _agent.isStopped = true;

        float targetSideways = _orbitDir;
        float targetForward = 0f;

        // Jeśli za blisko gracza - odlot do tyłu
        float sqrDistToPlayer = (_target.position - transform.position).sqrMagnitude;
        if (sqrDistToPlayer < attackRange * attackRange) targetForward = -0.5f;
        // Jeśli za daleko - lekko podleć
        else if (sqrDistToPlayer > orbitDistance * orbitDistance) targetForward = 0.5f;

        float currentSideways = _animator.GetFloat("SidewaysSpeed");
        float currentForward = _animator.GetFloat("ForwardSpeed");

        _animator.SetFloat("SidewaysSpeed", Mathf.Lerp(currentSideways, targetSideways, Time.deltaTime * 5f));
        _animator.SetFloat("ForwardSpeed", Mathf.Lerp(currentForward, targetForward, Time.deltaTime * 5f));

        LookAtTarget();
    }

    private void DoReturn()
    {
        if (_agent.isOnNavMesh) _agent.isStopped = false;
        _agent.SetDestination(_spawnPosition);
        SyncAnimatorToMoveDir();

        Vector3 dir = _agent.velocity.normalized;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotationSpeed);
    }

    // ========================
    // ATAK
    // ========================
    private void StartAttack()
    {
        if (_agent.isOnNavMesh) _agent.isStopped = true;
        SnapToTarget();

        float sqrDist = (_target.position - transform.position).sqrMagnitude;

        if (sqrDist > attackRange * attackRange)
        {
            _animator.SetTrigger("Attack2"); // Dash-atak z dystansu
        }
        else
        {
            _animator.SetTrigger("Attack");  // Żądlenie z bliska
        }
    }

    // ========================
    // ANIMATION EVENTS (identyczne nazwy jak u rycerza!)
    // ========================
    public void ResetAttack()
    {
        _currentState = BeeState.Idle;
        _lastAttackTime = Time.time;
        if (weaponHitbox != null) weaponHitbox.CloseDamageWindow();
    }

    public void OpenDamage() { if (weaponHitbox != null) weaponHitbox.OpenDamageWindow(); }
    public void CloseDamage() { if (weaponHitbox != null) weaponHitbox.CloseDamageWindow(); }

    public void ForceInterrupt()
    {
        if (_currentState == BeeState.Attacking)
        {
            _currentState = BeeState.Idle;
            _lastAttackTime = Time.time;
            CloseDamage();
        }

        if (_animator != null)
        {
            _animator.ResetTrigger("Attack");
            _animator.ResetTrigger("Attack2");
        }
    }

    // ========================
    // REAKCJA NA CIOS
    // ========================
    public override void OnDamagedByPlayer()
    {
        base.OnDamagedByPlayer();
        SetState(BeeState.Orbiting); // Po ciosie pszczoła odlatuje i krąży
    }

    // ========================
    // POMOCNICZE
    // ========================
    private void SyncAnimatorToMoveDir()
    {
        Vector3 velocity = _agent.desiredVelocity;
        if (velocity.magnitude > 0.1f)
        {
            Vector3 localVel = transform.InverseTransformDirection(velocity);
            _animator.SetFloat("ForwardSpeed", localVel.z, 0.1f, Time.deltaTime);
            _animator.SetFloat("SidewaysSpeed", localVel.x, 0.1f, Time.deltaTime);
        }
        else
        {
            _animator.SetFloat("ForwardSpeed", 0, 0.1f, Time.deltaTime);
            _animator.SetFloat("SidewaysSpeed", 0, 0.1f, Time.deltaTime);
        }
    }

    private void LookAtTarget()
    {
        Vector3 dir = (_target.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotationSpeed);
    }

    private void SnapToTarget()
    {
        Vector3 dir = (_target.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
    }

    // Root Motion (identycznie jak u rycerza)
    private void OnAnimatorMove()
    {
        if (Time.deltaTime > 0)
        {
            Vector3 nextPos = transform.position + _animator.deltaPosition;
            if (_agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(nextPos, out hit, 1f, NavMesh.AllAreas)) nextPos = hit.position;
            }
            transform.position = nextPos;
            transform.rotation *= _animator.deltaRotation;
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, orbitDistance);
    }
}
