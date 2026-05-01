using UnityEngine;
using UnityEngine.AI;
using SojartsaAI;

namespace SojartsaAI.v3
{
    /// <summary>
    /// AIBrain v3 (The Hub). 
    /// Centralny punkt komunikacji. Nie zawiera logiki walki, tylko komponenty i maszynę stanów.
    /// To jest "Kręgosłup", na którym wiszą stany.
    /// </summary>
    public class AIBrain : MonoBehaviour, IDamageable
    {
        [Header("Zasoby (Body)")]
        public Animator anim;
        public NavMeshAgent agent;
        public Transform target;

        [Header("Konfiguracja (Data)")]
        public AIArchetype archetype;
        public InputReader playerInput;

        [Header("AAA - Statystyki Postury")]
        public float currentPoise;
        private float _poiseRegenTimer;

        // System Stanów
        private AIState _currentState;
        public AIState CurrentState => _currentState;

        // --- PAMIĘĆ AI (Cooldowny) ---
        private System.Collections.Generic.Dictionary<AIActionData, float> _lastUsedTime = new System.Collections.Generic.Dictionary<AIActionData, float>();

        // Sensory (Osobny moduł)
        public AISensory Sensory { get; private set; }
        
        // --- AAA - Hitboxy i Akcje ---
        private WeaponHitbox[] _hitboxes;
        public AIActionData ActiveAction { get; set; }

        // Taktyka (Zarządzana przez Director)
        public int CombatSlotIndex { get; set; } = -1;
        public bool HasAttackToken { get; set; } = false;

        private void Awake()
        {
            if (anim == null) anim = GetComponentInChildren<Animator>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            
            // Znajdujemy wszystkie hitboxy na starcie
            _hitboxes = GetComponentsInChildren<WeaponHitbox>(true);

            if (agent != null)
            {
                agent.updatePosition = false;
                agent.updateRotation = false;
            }

            Sensory = new AISensory(transform, target, archetype, playerInput);
            currentPoise = 0f; // Zaczynamy od 0 (Sekiro Style)
        }

        private void Start()
        {
            ChangeState(new State_Passive(this));
        }

        private void Update()
        {
            if (target == null && Sensory != null && Sensory.Player != null)
            {
                target = Sensory.Player;
            }

            _currentState?.LogicUpdate();
            HandlePoiseRegen();
            
            // UI Debug
            gameObject.name = $"Enemy [{_currentState?.GetType().Name}] (Poise: {currentPoise:F0}/{archetype.maxPoise})";
        }

        private void OnDestroy()
        {
            Sensory?.Cleanup();
        }

        public bool IsActionReady(AIActionData action)
        {
            if (action == null) return false;
            if (!_lastUsedTime.ContainsKey(action)) return true;
            return Time.time >= _lastUsedTime[action] + action.cooldown;
        }

        public void RecordActionUse(AIActionData action)
        {
            if (action == null) return;
            ActiveAction = action; // Ustawiamy aktywną akcję dla hitboxów
            if (_lastUsedTime.ContainsKey(action)) _lastUsedTime[action] = Time.time;
            else _lastUsedTime.Add(action, Time.time);
        }

        // --- ZARZĄDZANIE HITBOXAMI ---
        public void OpenHitbox(int id)
        {
            if (_hitboxes == null) return;
            foreach (var hb in _hitboxes)
                if (hb != null && hb.hitboxID == id) hb.OpenDamageWindow();
        }

        public void CloseHitbox(int id)
        {
            if (_hitboxes == null) return;
            foreach (var hb in _hitboxes)
                if (hb != null && hb.hitboxID == id) hb.CloseDamageWindow();
        }

        public void CloseAllHitboxes()
        {
            if (_hitboxes == null) return;
            foreach (var hb in _hitboxes)
                if (hb != null) hb.CloseDamageWindow();
        }

        private void HandlePoiseRegen()
        {
            if (_poiseRegenTimer > 0)
            {
                _poiseRegenTimer -= Time.deltaTime;
            }
            else if (currentPoise > 0)
            {
                currentPoise = Mathf.MoveTowards(currentPoise, 0, archetype.poiseRegenRate * Time.deltaTime);
            }
        }

        public void ChangeState(AIState newState)
        {
            if (newState == null) return;
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        public void SendAnimationSignal(string signal)
        {
            _currentState?.OnAnimationSignal(signal);
        }

        // --- IDamageable AAA Implementation ---
        public void OnDamagedByPlayer(float healthDamage, float poiseDamage, Vector3 hitSource)
        {
            // Dodajemy obrażenia postury (rośnie od 0 do Max)
            currentPoise += poiseDamage;
            _poiseRegenTimer = archetype.poiseResetDelay;

            if (_currentState is State_Passive)
            {
                ChangeState(new State_Combat(this));
            }

            if (currentPoise >= archetype.maxPoise)
            {
                currentPoise = archetype.maxPoise;
                ForceInterrupt(); // Postawa przełamana!
            }
        }

        public void ForceInterrupt()
        {
            CloseAllHitboxes(); // Awaryjne zamknięcie przy staggerze
            ChangeState(new State_Stagger(this));
        }

        public void MoveTo(Vector3 position)
        {
            if (agent != null && agent.isOnNavMesh)
                agent.SetDestination(position);
        }

        // ================================================================
        // ROOT MOTION (The AAA Movement)
        // ================================================================
        
        // Puste zdarzenie animacji dla starszych animacji
        public void CheckDistractionDash() {}
        public void ResetAttack() {}
        
        public void OpenDamageByID(int id) => OpenHitbox(id);
        public void CloseDamageByID(int id) => CloseHitbox(id);

        private void OnAnimatorMove()
        {
            if (anim == null || Time.deltaTime <= 0) return;

            // W AAA pozycji pilnuje animacja (deltaPosition)
            Vector3 nextPos = transform.position + anim.deltaPosition;

            // Ale NavMesh jest "granicą" (zabezpieczenie)
            if (agent != null && agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(nextPos, out hit, 1f, NavMesh.AllAreas))
                {
                    nextPos = hit.position;
                }
                
                // Synchronizujemy agenta z nową pozycją
                agent.nextPosition = nextPos;
            }

            transform.position = nextPos;
            
            // AAA: Płynniejsza rotacja podczas ruchu (jeśli nie jesteśmy w akcji, która blokuje rotację)
            if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
            }
            else
            {
                transform.rotation *= anim.deltaRotation;
            }
        }
    }
}
