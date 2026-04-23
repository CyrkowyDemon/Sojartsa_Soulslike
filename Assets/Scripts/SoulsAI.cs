using UnityEngine;
using System.Collections.Generic;
using AI;
using UnityEngine.AI; // Dodano namespace dla NavMesh

/// <summary>
/// Nowoczesny mózg przeciwnika oparty na maszynie stanów (Sekiro/Souls Style).
/// </summary>
public class SoulsAI : EnemyBase
{
    [Header("Ustawienia SoulsAI")]
    [SerializeField] private List<EnemyAttackData> availableAttacks = new List<EnemyAttackData>();
    [SerializeField] private AIBehaviorConfig behaviorConfig;

    public AIBehaviorConfig BehaviorConfig => behaviorConfig;
    
    [Header("Referencje Hitboxów")]
    private WeaponHitbox[] _allHitboxes;

    // --- SYSTEM COOLDOWNÓW ---
    public float LastAttackTime { get; set; } = -100f;
    public float CurrentAttackCooldown { get; set; } = 1.0f;
    private EnemyAttackData _lastAttackUsed;

    public bool IsAttackOnCooldown => Time.time < (LastAttackTime + CurrentAttackCooldown);

    protected override void Start()
    {
        base.Start();

        _allHitboxes = GetComponentsInChildren<WeaponHitbox>();

        // Inicjalizacja Maszyny Stanów
        if (_stateMachine == null) _stateMachine = GetComponent<AI.AIStateMachine>();

        // DEBUG: Sprawdzenie Agenta i Root Motion
        if (_agent != null)
        {
            _agent.updatePosition = false;
            _agent.updateRotation = false;

            if (!_agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
                {
                    _agent.Warp(hit.position);
                    Debug.Log($"<color=cyan>[AI] {gameObject.name}: Przymusowo przypięto do NavMeshu!</color>");
                }
                else
                {
                    Debug.LogError($"<color=red>[AI] {gameObject.name}: BRAK NAVMESHU!</color>");
                }
            }
        }
        
        CloseDamage();

        if (_stateMachine != null)
        {
            _stateMachine.Initialize(new AIState_Idle(_stateMachine, this));
        }
        else
        {
            Debug.LogError($"<color=red>[AI] {gameObject.name}: BRAK AIStateMachine na obiekcie!</color>");
        }
    }

    // GŁÓWNA AKTUALIZACJA LOGIKI (Usunięto duplikat)
    protected override void UpdateBehavior() 
    { 
        // Synchronizacja Agenta z pozycją wynikającą z Root Motion
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.nextPosition = transform.position;
        }
    }

    public void MarkAttackUsed(EnemyAttackData data)
    {
        _lastAttackUsed = data;
        LastAttackTime = Time.time;
        CurrentAttackCooldown = data.attackCooldown;
    }

    public bool ShouldStrafe()
    {
        if (behaviorConfig == null) return true;
        return Random.Range(0f, 100f) <= behaviorConfig.strafeChance;
    }

    public void ForceInterrupt()
    {
        CloseDamage();
        if (_stateMachine != null)
            _stateMachine.ChangeState(new AIState_Stagger(_stateMachine, this));
    }

    public EnemyAttackData GetNextAttack(float distance)
    {
        if (availableAttacks == null || availableAttacks.Count == 0) return null;

        List<EnemyAttackData> possibleAttacks = new List<EnemyAttackData>();
        float totalWeight = 0f;

        foreach (var attack in availableAttacks)
        {
            if (attack == null) continue;

            if (distance >= attack.minDistance && distance <= attack.maxDistance)
            {
                float currentWeight = attack.weight;
                // Kara za powtórkę
                if (attack == _lastAttackUsed && availableAttacks.Count > 1) 
                {
                    currentWeight *= 0.2f; 
                }

                possibleAttacks.Add(attack);
                totalWeight += currentWeight;
            }
        }

        if (possibleAttacks.Count == 0 || totalWeight <= 0) return null;

        float randomRoll = Random.Range(0f, totalWeight);
        float currentWeightSum = 0f;

        foreach (var attack in possibleAttacks)
        {
            float w = attack.weight;
            if (attack == _lastAttackUsed && possibleAttacks.Count > 1) w *= 0.2f;

            currentWeightSum += w;
            if (randomRoll <= currentWeightSum) return attack;
        }

        return possibleAttacks[0];
    }

    // --- HITBOXY ---
    public void OpenDamage() { OpenDamageByID(1); }
    public void OpenDamageByID(int id)
    {
        if (_allHitboxes == null) return;
        foreach (var hb in _allHitboxes)
        {
            if (hb != null && hb.hitboxID == id) hb.OpenDamageWindow();
        }
    }

    public void CloseDamage()
    {
        if (_allHitboxes == null) return;
        foreach (var hb in _allHitboxes)
        {
            if (hb != null) hb.CloseDamageWindow();
        }
    }

    private void OnAnimatorMove()
    {
        if (_animator == null || Time.deltaTime <= 0) return;

        Vector3 nextPos = transform.position + _animator.deltaPosition;

        // Kontrola NavMesh (zabezpieczenie przed wypadnięciem z mapy)
        if (_agent != null && _agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(nextPos, out hit, 1f, NavMesh.AllAreas))
            {
                nextPos = hit.position;
            }
        }

        transform.position = nextPos;
        transform.rotation *= _animator.deltaRotation;
    }
}
