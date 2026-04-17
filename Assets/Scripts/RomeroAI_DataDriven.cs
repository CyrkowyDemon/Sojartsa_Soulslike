using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// --- ROMERO AI: Defensywny, Kontrujący, Multi-Hitbox ---
public class RomeroAI_DataDriven : EnemyBase
{
    public enum AIState { Idle, Chasing, Strafing, Attacking, Returning }

    [Header("Zasięgi Romero")]
    [SerializeField] private float strafeDistance = 4f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float maxChaseDistance = 25f;

    [Header("Reaktywność (Defensywa)")]
    [SerializeField] [Range(0, 1)] private float dodgeChance = 0.5f;
    [SerializeField] [Range(0, 1)] private float counterKickChance = 0.4f;
    [SerializeField] private float reactionCooldown = 2f;

    [Header("PULA ATAKÓW (ScriptableObjects)")]
    [SerializeField] private List<EnemyAttackData> availableAttacks = new List<EnemyAttackData>();

    private NavMeshAgent _agent;
    private Animator _animator;
    private Animator _playerAnimator;
    private WeaponHitbox[] _allHitboxes;

    private Vector3 _spawnPosition;
    private AIState _currentState = AIState.Idle;
    private float _lastAttackTime = -10f;
    private float _lastReactionTime = -10f;
    private float _currentAttackCooldown = 1.5f;
    
    private int _strafeDir = 1;
    private float _nextStrafeChangeTime = 0f;
    private float _minStrafeEndTime = 0f;
    private bool _canRotate = true;
    
    private int _attackTagHash;
    private int _heavyAttackTagHash;

    protected override void Start()
    {
        base.Start();
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        if (_target != null) _playerAnimator = _target.GetComponent<Animator>();

        _allHitboxes = GetComponentsInChildren<WeaponHitbox>();

        _spawnPosition = transform.position;
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _strafeDir = Random.value > 0.5f ? 1 : -1;

        _attackTagHash = Animator.StringToHash("Attack");
        _heavyAttackTagHash = Animator.StringToHash("HeavyAttack");
    }

    protected override void UpdateBehavior()
    {
        float sqrDistance = (_target.position - transform.position).sqrMagnitude;
        float sqrDistanceFromSpawn = (_spawnPosition - transform.position).sqrMagnitude;

        float distance = Mathf.Sqrt(sqrDistance); // Wymagane dla CheckForCounterAction i wzroku
        CheckForCounterAction(distance);

        DecideNextState(sqrDistance, sqrDistanceFromSpawn);
        UpdateStateActions();

        if (_agent.isOnNavMesh) _agent.nextPosition = transform.position;
    }

    private void CheckForCounterAction(float distance)
    {
        // 1. Jeśli Romero nie jest w walce lub jest za daleko, ignoruje machanie gracza
        if (!_isInCombat || distance > strafeDistance * 1.5f) return;

        if (_currentState == AIState.Attacking || Time.time < _lastReactionTime + reactionCooldown) return;

        if (IsPlayerAttacking())
        {
            // 2. SPRAWDZAMY KIERUNEK (Dot Product) - czy gracz w ogóle patrzy w stronę tego Romero?
            Vector3 dirToRomero = (transform.position - _target.position).normalized;
            float dot = Vector3.Dot(_target.forward, dirToRomero);
            
            // Jeśli dot jest mniejszy niż 0.5, to gracz bije kogoś innego (lub patrzy w inną stronę)
            if (dot < 0.5f) return; 

            _lastReactionTime = Time.time;

            if (distance < 2.5f && Random.value < counterKickChance)
            {
                ExecuteAttackByTrigger("Attack2", 1.2f);
                return; 
            }
            
            if (Random.value < dodgeChance)
            {
                _animator.SetTrigger("Dodge");
                SetState(AIState.Attacking); 
                _lastAttackTime = Time.time; 
            }
        }
    }

    private void DecideNextState(float sqrDistance, float sqrDistanceFromSpawn)
    {
        if (_currentState == AIState.Attacking) return;
        
        if (_currentState == AIState.Returning)
        {
            if (sqrDistanceFromSpawn < 1.5f * 1.5f) { _isInCombat = false; SetState(AIState.Idle); }
            return;
        }

        if (!_isInCombat)
        {
            if (CanSeePlayer(sqrDistance)) _isInCombat = true; 
            else { SetState(AIState.Idle); return; }
        }
        else if (sqrDistanceFromSpawn > maxChaseDistance * maxChaseDistance || sqrDistance > maxChaseDistance * maxChaseDistance)
        {
            _isInCombat = false;
            SetState(AIState.Returning);
            return;
        }

        if (_currentState == AIState.Strafing && Time.time < _minStrafeEndTime) return;

        if (Time.time < _lastAttackTime + _currentAttackCooldown)
        {
            if (sqrDistance <= strafeDistance * strafeDistance) SetState(AIState.Strafing);
            else SetState(AIState.Chasing);
            return;
        }

        EnemyAttackData selectedAttack = PickValidAttack(sqrDistance);
        if (selectedAttack != null) ExecuteAttack(selectedAttack);
        else
        {
            if (sqrDistance <= strafeDistance * strafeDistance) SetState(AIState.Strafing);
            else SetState(AIState.Chasing);
        }
    }

    private EnemyAttackData PickValidAttack(float sqrDistance)
    {
        List<EnemyAttackData> validAttacks = new List<EnemyAttackData>();
        float totalWeight = 0f;
        foreach (var attack in availableAttacks)
        {
            // Optymalizacja: Porównujemy kwadraty dystansów
            if (sqrDistance >= attack.minDistance * attack.minDistance && sqrDistance <= attack.maxDistance * attack.maxDistance)
            {
                validAttacks.Add(attack);
                totalWeight += attack.weight;
            }
        }
        if (validAttacks.Count == 0) return null;

        float randomRoll = Random.Range(0f, totalWeight);
        float sum = 0f;
        foreach (var attack in validAttacks)
        {
            sum += attack.weight;
            if (randomRoll <= sum) return attack;
        }
        return validAttacks[0];
    }

    private void ExecuteAttack(EnemyAttackData data)
    {
        SetState(AIState.Attacking);
        _lastAttackTime = Time.time; // POPRAWKA 5: Zabezpieczenie cooldownu od razu na starcie
        _currentAttackCooldown = data.attackCooldown;
        _animator.SetTrigger(data.animationTrigger);
        if (data.isJumpAttack) SnapToTarget();
    }

    private void ExecuteAttackByTrigger(string trigger, float cooldown)
    {
        SetState(AIState.Attacking);
        _lastAttackTime = Time.time; // POPRAWKA 5: Zabezpieczenie cooldownu
        _currentAttackCooldown = cooldown;
        _animator.SetTrigger(trigger);
        SnapToTarget();
    }

    private void SetState(AIState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;
        if (newState == AIState.Strafing) _minStrafeEndTime = Time.time + Random.Range(2f, 4f);
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
            _nextStrafeChangeTime = Time.time + Random.Range(2f, 4f);
        }
        if (_agent.isOnNavMesh) _agent.isStopped = true;

        float targetSideways = _strafeDir;
        float targetForward = 0f;
        if ((_target.position - transform.position).sqrMagnitude < 2.5f * 2.5f) targetForward = -0.5f;

        _animator.SetFloat("SidewaysSpeed", Mathf.Lerp(_animator.GetFloat("SidewaysSpeed"), targetSideways, Time.deltaTime * 5f));
        _animator.SetFloat("ForwardSpeed", Mathf.Lerp(_animator.GetFloat("ForwardSpeed"), targetForward, Time.deltaTime * 5f));
        LookAtTarget();
    }

    private void MovementStop() { if (_agent.isOnNavMesh) _agent.isStopped = true; }
    private void MovementReturn() { _agent.SetDestination(_spawnPosition); LookAtTarget(); }

    private void SyncAnimatorToMoveDir()
    {
        Vector3 velocity = _agent.desiredVelocity;
        if (velocity.magnitude > 0.1f)
        {
            Vector3 localVel = transform.InverseTransformDirection(velocity);
            _animator.SetFloat("ForwardSpeed", localVel.z, 0.1f, Time.deltaTime);
            _animator.SetFloat("SidewaysSpeed", localVel.x, 0.1f, Time.deltaTime);
        }
    }

    private void LookAtTarget()
    {
        if (!_canRotate) return;
        Vector3 dir = _target.position - transform.position; 
        dir.y = 0;
        
        // POPRAWKA 4: Ochrona przed wektorem zerowym
        if (dir.sqrMagnitude > 0.001f) 
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), Time.deltaTime * rotationSpeed);
        }
    }

    private void SnapToTarget()
    {
        if (!_canRotate) return;
        Vector3 dir = _target.position - transform.position; 
        dir.y = 0;
        
        // POPRAWKA 4: Ochrona przed wektorem zerowym
        if (dir.sqrMagnitude > 0.001f) 
        {
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }
    }

    private bool IsPlayerAttacking()
    {
        if (_playerAnimator == null) return false;
        
        AnimatorStateInfo currentState = _playerAnimator.GetCurrentAnimatorStateInfo(2);
        AnimatorStateInfo nextState = _playerAnimator.GetNextAnimatorStateInfo(2); // POPRAWKA 3: Przewidywanie ataku w fazie przejścia (transition)
        
        bool isAttacking = currentState.tagHash == _attackTagHash || currentState.tagHash == _heavyAttackTagHash || 
                           nextState.tagHash == _attackTagHash || nextState.tagHash == _heavyAttackTagHash;
        
        if (isAttacking) Debug.Log("<color=yellow>[ROMERO] Widzę Twój atak! Reaguję!</color>");

        return isAttacking;
    }

    public void OpenDamageByID(int id)
    {
        _canRotate = false;
        foreach (var hitbox in _allHitboxes)
        {
            if (hitbox.hitboxID == id) hitbox.OpenDamageWindow();
        }
    }

    public void CloseDamageByID(int id)
    {
        foreach (var hitbox in _allHitboxes)
        {
            if (hitbox.hitboxID == id) hitbox.CloseDamageWindow();
        }
    }

    public void OpenDamage() { OpenDamageByID(1); }
    public void CloseDamage() 
    { 
        foreach (var hitbox in _allHitboxes) hitbox.CloseDamageWindow(); 
    }

    public void ResetAttack() 
    {
        _currentState = AIState.Idle; 
        _lastAttackTime = Time.time; 
        _canRotate = true;
        CloseDamage();
    }

    public void ForceInterrupt()
    {
        CloseDamage();
        if (_currentState == AIState.Attacking) { _currentState = AIState.Idle; _lastAttackTime = Time.time; }
        if (_animator != null) { _animator.ResetTrigger("Attack"); _animator.ResetTrigger("Attack2"); _animator.ResetTrigger("Dodge"); }
    }

    // WAŻNE: W tym designie "OnAnimatorMove" obsługuje Root Motion
    private void OnAnimatorMove()
    {
        if (Time.deltaTime > 0 && _animator != null)
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
}