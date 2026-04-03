using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Dog AI oparty na EnemyAI_DataDriven
public class DogAI : EnemyBase
{
    public enum AIState { Idle, Chasing, Strafing, Attacking, Returning }

    [Header("Zasięgi Lądowe (Pies)")]
    [SerializeField] private float strafeDistance = 5f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float maxChaseDistance = 25f;

    [Header("PULA ATAKÓW (ScriptableObjects)")]
    [SerializeField] private List<EnemyAttackData> availableAttacks = new List<EnemyAttackData>();

    [Header("System Walki")]
    private WeaponHitbox[] _allHitboxes;
    
    private NavMeshAgent _agent;
    private Animator _animator;
    private Animator _playerAnimator;

    private Vector3 _spawnPosition;
    private AIState _currentState = AIState.Idle;
    private float _lastAttackTime = -10f;
    private float _currentAttackCooldown = 1.5f;
    
    private int _strafeDir = 1;
    private float _nextStrafeChangeTime = 0f;
    private float _strafeForwardBias = 0f;
    private float _minStrafeEndTime = 0f;
    private bool _canRotate = true;

    protected override void Start()
    {
        base.Start();
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        if (_target != null) _playerAnimator = _target.GetComponent<Animator>();

        _spawnPosition = transform.position;
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _strafeDir = Random.value > 0.5f ? 1 : -1;

        // Znajdujemy wszystkie hithoxy
        _allHitboxes = GetComponentsInChildren<WeaponHitbox>();
    }

    protected override void UpdateBehavior()
    {
        float distance = Vector3.Distance(_target.position, transform.position);
        float distanceFromSpawn = Vector3.Distance(_spawnPosition, transform.position);

        DecideNextState(distance, distanceFromSpawn);
        UpdateStateActions();

        if (_agent.isOnNavMesh) _agent.nextPosition = transform.position;
    }

    private void DecideNextState(float distance, float distanceFromSpawn)
    {
        if (_currentState == AIState.Attacking) return;
        
        // --- Powrót ---
        if (_currentState == AIState.Returning)
        {
            if (distanceFromSpawn < 1.5f) { _isInCombat = false; SetState(AIState.Idle); }
            return;
        }

        // --- Wejście w tryb walki ---
        if (!_isInCombat)
        {
            if (CanSeePlayer(distance)) _isInCombat = true; 
            else { SetState(AIState.Idle); return; }
        }
        else if (distanceFromSpawn > maxChaseDistance || distance > maxChaseDistance)
        {
            _isInCombat = false;
            SetState(AIState.Returning);
            return;
        }

        // --- BLOKADA STRAFINGU ---
        if (_currentState == AIState.Strafing && Time.time < _minStrafeEndTime) return;

        // --- COOLDOWN ---
        if (Time.time < _lastAttackTime + _currentAttackCooldown)
        {
            if (distance <= strafeDistance) SetState(AIState.Strafing);
            else SetState(AIState.Chasing);
            return;
        }

        // --- NOWY SYSTEM: WYBÓR ATAKU Z LISTY ---
        EnemyAttackData selectedAttack = PickValidAttack(distance);

        if (selectedAttack != null)
        {
            ExecuteAttack(selectedAttack);
        }
        else
        {
            // Brak pasujących ataków w tym dystansie? To chociaż krąż lub podbiegnij
            if (distance <= strafeDistance) SetState(AIState.Strafing);
            else SetState(AIState.Chasing);
        }
    }

    private EnemyAttackData PickValidAttack(float distance)
    {
        List<EnemyAttackData> validAttacks = new List<EnemyAttackData>();
        float totalWeight = 0f;

        foreach (var attack in availableAttacks)
        {
            if (distance >= attack.minDistance && distance <= attack.maxDistance)
            {
                validAttacks.Add(attack);
                totalWeight += attack.weight;
            }
        }

        if (validAttacks.Count == 0) return null;

        float randomRoll = Random.Range(0f, totalWeight);
        float currentWeightSum = 0f;

        foreach (var attack in validAttacks)
        {
            currentWeightSum += attack.weight;
            if (randomRoll <= currentWeightSum) return attack;
        }

        return validAttacks[0];
    }

    private void ExecuteAttack(EnemyAttackData data)
    {
        SetState(AIState.Attacking);
        _currentAttackCooldown = data.attackCooldown;
        _animator.SetTrigger(data.animationTrigger);
        
        if (data.isJumpAttack) SnapToTarget(); // Doskok zawsze centruje na graczu
    }

    private void SetState(AIState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;

        if (newState == AIState.Strafing)
        {
            _minStrafeEndTime = Time.time + Random.Range(2f, 4f);
        }
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

        if (Vector3.Distance(transform.position, _target.position) < 2.5f) targetForward = -0.5f; 

        float currentSideways = _animator.GetFloat("SidewaysSpeed");
        float currentForward = _animator.GetFloat("ForwardSpeed");

        _animator.SetFloat("SidewaysSpeed", Mathf.Lerp(currentSideways, targetSideways, Time.deltaTime * 5f));
        _animator.SetFloat("ForwardSpeed", Mathf.Lerp(currentForward, targetForward, Time.deltaTime * 5f));

        LookAtTarget(); 
    }

    public void ForceInterrupt()
    {
        CloseDamage(); // ZAWSZE zamykamy hitbox

        if (_currentState == AIState.Attacking)
        {
            _currentState = AIState.Idle;
            _lastAttackTime = Time.time;
        }

        if (_animator != null)
        {
            foreach (var atk in availableAttacks) _animator.ResetTrigger(atk.animationTrigger);
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
        if (!_canRotate) return;
        Vector3 dir = (_target.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotationSpeed);
    }

    private void SnapToTarget()
    {
        if (!_canRotate) return;
        Vector3 dir = (_target.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
    }

    public void ResetAttack() 
    {
        _currentState = AIState.Idle;
        _lastAttackTime = Time.time;
        _canRotate = true;
        CloseDamage();
    }

    public void OpenDamageByID(int id)
    {
        _canRotate = false;
        foreach (var hb in _allHitboxes)
        {
            if (hb.hitboxID == id) hb.OpenDamageWindow();
        }
    }

    public void CloseDamageByID(int id)
    {
        foreach (var hb in _allHitboxes)
        {
            if (hb.hitboxID == id) hb.CloseDamageWindow();
        }
    }

    public void OpenDamage() { OpenDamageByID(1); }
    public void CloseDamage() { foreach (var hb in _allHitboxes) hb.CloseDamageWindow(); }

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

    public override void OnDamagedByPlayer()
    {
        base.OnDamagedByPlayer();
        SetState(AIState.Chasing);
    }
}
