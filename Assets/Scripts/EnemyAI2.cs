using UnityEngine;
using UnityEngine.AI;

// DZIEDZICZYMY Z EnemyBase! Otrzymujemy wzrok i szukanie gracza za darmo.
public class EnemyAI2 : EnemyBase
{
    public enum AIState { Idle, Chasing, Strafing, Attacking, Returning }

    [Header("Zasięgi Lądowe")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float strafeDistance = 5f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float attackCooldownDuration = 1.5f;
    [SerializeField] private float maxChaseDistance = 25f;

    [Header("Atak Obszarowy (AoE)")]
    [SerializeField] private float aoeRadius = 4f; 
    [SerializeField] private int aoeDamage = 20;   
    [SerializeField] private GameObject aoeEffectPrefab; 

    [Header("System Walki")]
    [SerializeField] private WeaponHitbox weaponHitbox;
    
    private NavMeshAgent _agent;
    private Animator _animator;
    private Animator _playerAnimator;

    private Vector3 _spawnPosition;
    private AIState _currentState = AIState.Idle;
    private float _lastAttackTime = -10f;
    private int _strafeDir = 1;
    private float _nextStrafeChangeTime = 0f;
    private float _strafeForwardBias = 0f;
    private float _minStrafeEndTime = 0f;
    private bool _canRotate = true; // Blokada rotacji podczas ciosu (FromSoft-style commitment)
    
    private int _attackTagHash;
    private int _heavyAttackTagHash;

    // --- OPTYMALIZACJA (Fizyka i Pamięć) ---
    private Collider[] _aoeOverlapCache = new Collider[10]; // Cache dla ataku AoE

    protected override void Start()
    {
        base.Start(); // Ważne! Odpala funkcję z Matki (szuka gracza)

        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        if (_target != null) _playerAnimator = _target.GetComponent<Animator>();

        _spawnPosition = transform.position;
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _strafeDir = Random.value > 0.5f ? 1 : -1;

        _attackTagHash = Animator.StringToHash("Attack");
        _heavyAttackTagHash = Animator.StringToHash("HeavyAttack");
    }

    // To zastępuje nam standardowe Update()
    protected override void UpdateBehavior()
    {
        Vector3 toTarget = _target.position - transform.position;
        float sqrDistance = toTarget.sqrMagnitude;

        Vector3 toSpawn = _spawnPosition - transform.position;
        float sqrDistanceFromSpawn = toSpawn.sqrMagnitude;

        DecideNextState(sqrDistance, sqrDistanceFromSpawn);
        UpdateStateActions();

        if (_agent.isOnNavMesh) _agent.nextPosition = transform.position;
    }

    private void DecideNextState(float sqrDistance, float sqrDistanceFromSpawn)
    {
        if (_currentState == AIState.Attacking) return;
        
        // --- Powrót na miejsce ---
        if (_currentState == AIState.Returning)
        {
            if (sqrDistanceFromSpawn < 1.5f * 1.5f) // 1.5m -> squared
            {
                _isInCombat = false;
                SetState(AIState.Idle);
            }
            return;
        }

        // --- Wejście w tryb walki ---
        if (!_isInCombat)
        {
            if (CanSeePlayer(sqrDistance)) _isInCombat = true; 
            else
            {
                SetState(AIState.Idle);
                return;
            }
        }
        else if (sqrDistanceFromSpawn > maxChaseDistance * maxChaseDistance || sqrDistance > maxChaseDistance * maxChaseDistance)
        {
            _isInCombat = false;
            SetState(AIState.Returning);
            return;
        }

        // --- BLOKADA STRAFINGU ---
        if (_currentState == AIState.Strafing && Time.time < _minStrafeEndTime)
        {
            return;
        }

        // --- COOLDOWN ---
        if (Time.time < _lastAttackTime + attackCooldownDuration)
        {
            if (sqrDistance <= strafeDistance * strafeDistance) SetState(AIState.Strafing);
            else SetState(AIState.Chasing);
            return;
        }

        float sqrJumpAttackRange = 7f * 7f; 

        if (sqrDistance <= attackRange * attackRange)
        {
            float diceRoll = Random.value;

            if (diceRoll < 0.6f) 
            {
                SetState(AIState.Attacking); 
            }
            else if (diceRoll < 0.85f) 
            {
                _animator.SetTrigger("Dodge"); 
                _lastAttackTime = Time.time;
                SetState(AIState.Strafing);  
            }
            else
            {
                // Opóźnienie ciosu - krąży dodatkowe 1-2 sekundy
                _lastAttackTime = Time.time - (attackCooldownDuration / 2f);
                SetState(AIState.Strafing);
            }
        }
        else if (sqrDistance > attackRange * attackRange && sqrDistance <= sqrJumpAttackRange)
        {
            if (Random.value < 0.4f) 
            {
                SetState(AIState.Attacking); 
            }
            else 
            {
                // Zamiast ponownie wejść w Chasing (i co klatkę rzucać kostką),
                // wchodzimy w Strafing żeby "poczekać" przed kolejnym rzutem
                SetState(AIState.Strafing);
            }
        }
        else
        {
            SetState(AIState.Chasing);
        }
    }

    private void SetState(AIState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;

        // Gdy wchodzimy w Strafing, ustawiamy minimalny czas krążenia
        if (newState == AIState.Strafing)
        {
            _minStrafeEndTime = Time.time + Random.Range(2f, 4f);
        }

        if (_currentState == AIState.Attacking) StartAttackSequence();
    }

    private void UpdateStateActions()
    {
        switch (_currentState)
        {
            case AIState.Idle: MovementStop(); break;
            case AIState.Chasing: MovementChase(); break;
            case AIState.Strafing: MovementStrafe(); break;
            case AIState.Returning: MovementReturn(); break;
        }
    }

    private void MovementReturn()
    {
        if (_agent.isOnNavMesh) _agent.isStopped = false;
        _agent.SetDestination(_spawnPosition);
        SyncAnimatorToMoveDir();
        Vector3 dir = _agent.velocity.normalized;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotationSpeed);
    }

    private void MovementStop()
    {
        if (_agent.isOnNavMesh) _agent.isStopped = true;
        _animator.SetFloat("ForwardSpeed", 0, 0.1f, Time.deltaTime);
        _animator.SetFloat("SidewaysSpeed", 0, 0.1f, Time.deltaTime);
    }

    private void MovementChase()
    {
        if (_agent.isOnNavMesh) _agent.isStopped = false;
        _agent.SetDestination(_target.position);
        SyncAnimatorToMoveDir();
        LookAtTarget();
    }

    private void MovementStrafe()
    {
        if (Time.time > _nextStrafeChangeTime)
        {
            _strafeDir = Random.value > 0.5f ? 1 : -1;
            _strafeForwardBias = Random.value > 0.5f ? 0f : 0.5f; 
            _nextStrafeChangeTime = Time.time + Random.Range(2f, 4f);
        }

        if (_agent.isOnNavMesh) _agent.isStopped = true; 

        float targetSideways = _strafeDir; 
        float targetForward = _strafeForwardBias; 

        if ((_target.position - transform.position).sqrMagnitude < attackRange * attackRange) targetForward = -0.5f; 

        float currentSideways = _animator.GetFloat("SidewaysSpeed");
        float currentForward = _animator.GetFloat("ForwardSpeed");

        _animator.SetFloat("SidewaysSpeed", Mathf.Lerp(currentSideways, targetSideways, Time.deltaTime * 5f));
        _animator.SetFloat("ForwardSpeed", Mathf.Lerp(currentForward, targetForward, Time.deltaTime * 5f));

        LookAtTarget(); 
    }

private void StartAttackSequence()
{
    if (_agent.isOnNavMesh) _agent.isStopped = true;
    SnapToTarget();

    float sqrDist = (_target.position - transform.position).sqrMagnitude;
    
    // Tu już nie decydujemy czy chce atakować, bo o tym zdecydował nowy mózg wyżej.
    // Skoro wywołał tę funkcję i dystans jest duży - to na pewno ma być Jump Attack.
    if (sqrDist > 3f * 3f) 
    {
        _animator.SetTrigger("Attack2"); // Skok
    }
    else
    {
        _animator.SetTrigger("Attack");  // Zwykłe kombo
    }
}
// --- NOWA FUNKCJA: Wymuszone przerwanie akcji ---
    public void ForceInterrupt()
    {
        CloseDamage(); // ZAWSZE zamykamy hitbox na wszelki wypadek!

        // Jeśli wróg właśnie atakował, przerywamy to
        if (_currentState == AIState.Attacking)
        {
            _currentState = AIState.Idle;
            _lastAttackTime = Time.time; // Wymuszamy start cooldownu!
        }

        // Czyścimy pamięć Animatora. Dzięki temu wróg nie zaatakuje "z opóźnieniem" 
        // zaraz po tym, jak skończy się odtwarzać animacja otrzymania obrażeń (Stagger).
        if (_animator != null)
        {
            _animator.ResetTrigger("Attack");
            _animator.ResetTrigger("Attack2");
            _animator.ResetTrigger("Dodge");
        }
    }
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
        if (!_canRotate) return; // Zablokowany podczas ciosu!
        Vector3 dir = (_target.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotationSpeed);
    }

    private void SnapToTarget()
    {
        if (!_canRotate) return; // Zablokowany podczas ciosu!
        Vector3 dir = (_target.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
    }

    private bool IsPlayerAttacking()
    {
        if (_playerAnimator == null) return false;
        // Sprawdzamy warstwę 2 (Actions), bo tam są ataki gracza!
        AnimatorStateInfo state = _playerAnimator.GetCurrentAnimatorStateInfo(2);
        return state.tagHash == _attackTagHash || state.tagHash == _heavyAttackTagHash;
    }

public void ResetAttack() 
{
    _currentState = AIState.Idle;
    _lastAttackTime = Time.time;
    _canRotate = true; // Odblokowujemy rotację po zakończeniu ataku
    if (weaponHitbox != null) weaponHitbox.CloseDamageWindow();
}

    public void OpenDamage()
    {
        _canRotate = false; // LOCK! Wróg commituje się w kierunku ciosu
        if (weaponHitbox != null) weaponHitbox.OpenDamageWindow();
    }
    public void CloseDamage() { if (weaponHitbox != null) weaponHitbox.CloseDamageWindow(); }

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

    public void ExecuteAoEAttack()
    {
        if (aoeEffectPrefab != null) 
        {
            // Instantiate(aoeEffectPrefab, transform.position, Quaternion.identity);
            SimplePool.Spawn(aoeEffectPrefab, transform.position, Quaternion.identity); // POOLING!
        }

        // OPTYMALIZACJA: NonAlloc nie tworzy nowej tablicy co klatkę
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, aoeRadius, _aoeOverlapCache);
        
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _aoeOverlapCache[i];
            if (hit.CompareTag("Player"))
            {
                PlayerHealth pHealth = hit.GetComponent<PlayerHealth>();
                if (pHealth != null) 
                {
                    Debug.Log("<color=red>[AOE] Trafienie gracza falą uderzeniową!</color>");
                    pHealth.TakeDamage(aoeDamage);
                    if (HitStop.Instance != null) HitStop.Instance.Freeze(0.07f);
                    SendMessage("GenerateImpulse", SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }

    public override void OnDamagedByPlayer()
    {
        base.OnDamagedByPlayer(); // Odpala kod z bazy!
        SetState(AIState.Chasing); // Dodatkowo lądowy wróg zaczyna gonić
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected(); // Rysuje stożek widzenia z bazy!
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}