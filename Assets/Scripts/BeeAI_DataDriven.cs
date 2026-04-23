using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Wersja BeeAI DATA-DRIVEN
public class BeeAI_DataDriven : EnemyBase
{
    public enum BeeState { Idle, Chasing, Orbiting, Attacking, Returning }

    [Header("Zasięgi Latające")]
    [SerializeField] private float orbitDistance = 5f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float maxChaseDistance = 25f;

    [Header("PULA ATAKÓW (ScriptableObjects)")]
    [SerializeField] private List<EnemyAttackData> availableAttacks = new List<EnemyAttackData>();

    [Header("Orbita (Krążenie)")]
    [SerializeField] private float minOrbitTime = 2f;
    [SerializeField] private float maxOrbitTime = 4f;

    [Header("System Walki")]
    private WeaponHitbox[] _allHitboxes;

    private Vector3 _spawnPosition;
    private BeeState _currentState = BeeState.Idle;
    private float _lastAttackTime = -10f;
    private float _currentAttackCooldown = 2f;
    private float _minOrbitEndTime = 0f;
    private int _orbitDir = 1;
    private float _nextOrbitChangeTime = 0f;

    // --- OPTYMALIZACJA (Cache list, by nie śmiecić w pamięci) ---
    private List<EnemyAttackData> _validAttacksCache = new List<EnemyAttackData>();

    protected override void Start()
    {
        base.Start();
        _spawnPosition = transform.position;
        if (_agent != null)
        {
            _agent.updatePosition = false;
            _agent.updateRotation = false;
        }
        _orbitDir = Random.value > 0.5f ? 1 : -1;

        // Znajdujemy wszystkie hithoxy
        _allHitboxes = GetComponentsInChildren<WeaponHitbox>();
    }

    protected override void UpdateBehavior()
    {
        float sqrDistance = (_target.position - transform.position).sqrMagnitude;
        float sqrDistanceFromSpawn = (_spawnPosition - transform.position).sqrMagnitude;

        DecideNextState(sqrDistance, sqrDistanceFromSpawn);
        ExecuteState();

        if (_agent.isOnNavMesh) _agent.nextPosition = transform.position;
    }

    private void DecideNextState(float sqrDistance, float sqrDistanceFromSpawn)
    {
        if (_currentState == BeeState.Attacking) return;

        if (_currentState == BeeState.Returning)
        {
            if (sqrDistanceFromSpawn < 2f * 2f) { _isInCombat = false; SetState(BeeState.Idle); }
            return;
        }

        if (!_isInCombat)
        {
            if (CanSeePlayer(sqrDistance)) _isInCombat = true;
            else { SetState(BeeState.Idle); return; }
        }
        else if (sqrDistanceFromSpawn > maxChaseDistance * maxChaseDistance || sqrDistance > maxChaseDistance * maxChaseDistance)
        {
            _isInCombat = false;
            SetState(BeeState.Returning);
            return;
        }

        if (_currentState == BeeState.Orbiting && Time.time < _minOrbitEndTime) return;

        if (Time.time < _lastAttackTime + _currentAttackCooldown)
        {
            if (sqrDistance <= (orbitDistance * 1.5f) * (orbitDistance * 1.5f)) SetState(BeeState.Orbiting);
            else SetState(BeeState.Chasing);
            return;
        }

        // --- WYBÓR ATAKU ---
        EnemyAttackData selectedAttack = PickValidAttack(sqrDistance);

        if (selectedAttack != null)
        {
            ExecuteAttack(selectedAttack);
        }
        else
        {
            if (sqrDistance <= (orbitDistance * 1.5f) * (orbitDistance * 1.5f)) SetState(BeeState.Orbiting);
            else SetState(BeeState.Chasing);
        }
    }

    private EnemyAttackData PickValidAttack(float sqrDistance)
    {
        _validAttacksCache.Clear(); // Czyścimy zamiast tworzyć nową (0 alokacji!)
        float totalWeight = 0f;

        foreach (var attack in availableAttacks)
        {
            if (sqrDistance >= attack.minDistance * attack.minDistance && sqrDistance <= attack.maxDistance * attack.maxDistance)
            {
                _validAttacksCache.Add(attack);
                totalWeight += attack.weight;
            }
        }

        if (_validAttacksCache.Count == 0) return null;

        float randomRoll = Random.Range(0f, totalWeight);
        float currentWeightSum = 0f;
        foreach (var attack in _validAttacksCache)
        {
            currentWeightSum += attack.weight;
            if (randomRoll <= currentWeightSum) return attack;
        }
        return _validAttacksCache[0];
    }

    private void ExecuteAttack(EnemyAttackData data)
    {
        SetState(BeeState.Attacking);
        _currentAttackCooldown = data.attackCooldown;
        LookAtTarget();
        _animator.SetTrigger(data.animationTrigger);
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
    }

    private void ExecuteState()
    {
        switch (_currentState)
        {
            case BeeState.Idle: DoIdle(); break;
            case BeeState.Chasing: DoChase(); break;
            case BeeState.Orbiting: DoOrbit(); break;
            case BeeState.Returning: DoReturn(); break;
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
        if (_agent.isOnNavMesh) _agent.isStopped = false;
        _agent.SetDestination(_target.position);
        SyncAnimatorToMoveDir();
        LookAtTarget();
    }

    private void DoOrbit()
    {
        if (Time.time > _nextOrbitChangeTime)
        {
            _orbitDir = Random.value > 0.5f ? 1 : -1;
            _nextOrbitChangeTime = Time.time + Random.Range(2f, 4f);
        }

        if (_agent.isOnNavMesh) _agent.isStopped = true;

        float targetSideways = _orbitDir;
        float targetForward = 0f;

        float sqrDistToPlayer = (transform.position - _target.position).sqrMagnitude;
        if (sqrDistToPlayer < 2f * 2f) targetForward = -0.5f;
        else if (sqrDistToPlayer > orbitDistance * orbitDistance) targetForward = 0.5f;

        _animator.SetFloat("SidewaysSpeed", Mathf.Lerp(_animator.GetFloat("SidewaysSpeed"), targetSideways, Time.deltaTime * 5f));
        _animator.SetFloat("ForwardSpeed", Mathf.Lerp(_animator.GetFloat("ForwardSpeed"), targetForward, Time.deltaTime * 5f));

        LookAtTarget();
    }

    private void DoReturn()
    {
        if (_agent.isOnNavMesh) _agent.isStopped = false;
        _agent.SetDestination(_spawnPosition);
        SyncAnimatorToMoveDir();
        Vector3 dir = _agent.velocity.normalized;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotationSpeed);
    }

    public void ResetAttack() 
    {
        _currentState = BeeState.Idle;
        _lastAttackTime = Time.time;
        CloseDamage();
    }

    // Odpalane przez Animation Eventy ustawione w klatkach animacji "Distraction"
    // Parametr chanceToDash wpisujesz w edytorze Unity jako "Float" od 0.0 do 1.0
    public void CheckDistractionDash(float chanceToDash)
    {
        // Jeśli już wcześniej wylosowała atak podczas tego tańca - ignorujemy resztę prób
        if (_currentState == BeeState.Attacking) return;

        // Rzut kostką (np. 0.2 to 20% szans)
        if (Random.value <= chanceToDash)
        {
            Debug.Log($"<color=yellow>[BEE] Zmyłka udana! Atakuje z zaskoczenia!</color>");
            _animator.SetTrigger("Attack2"); // lub inna nazwa pod którą masz kropnięcie w dół
            _currentState = BeeState.Attacking;
        }
    }

    public void OpenDamageByID(int id)
    {
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

    public void ForceInterrupt()
    {
        CloseDamage(); // ZAWSZE zamykamy hitbox

        if (_currentState == BeeState.Attacking)
        {
            _currentState = BeeState.Idle;
            _lastAttackTime = Time.time;
        }
        if (_animator != null)
        {
            foreach (var atk in availableAttacks) _animator.ResetTrigger(atk.animationTrigger);
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
    }

    private void LookAtTarget()
    {
        Vector3 dir = (_target.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotationSpeed);
    }

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
        _currentState = BeeState.Orbiting;
    }
}
