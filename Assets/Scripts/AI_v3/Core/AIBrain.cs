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

        // Sensory (Osobny moduł)
        public AISensory Sensory { get; private set; }

        // Taktyka (Zarządzana przez Director)
        public int CombatSlotIndex { get; set; } = -1;
        public bool HasAttackToken { get; set; } = false;

        private void Awake()
        {
            if (anim == null) anim = GetComponentInChildren<Animator>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            
            Sensory = new AISensory(transform, target, archetype, playerInput);
            currentPoise = archetype != null ? archetype.maxPoise : 100f;
        }

        private void Start()
        {
            ChangeState(new State_Passive(this));
        }

        private void Update()
        {
            _currentState?.LogicUpdate();
            HandlePoiseRegen();
            
            gameObject.name = $"Enemy [{_currentState?.GetType().Name}] (Poise: {currentPoise:F0})";
        }

        private void OnDestroy()
        {
            Sensory?.Cleanup();
        }

        private void HandlePoiseRegen()
        {
            if (_poiseRegenTimer > 0)
            {
                _poiseRegenTimer -= Time.deltaTime;
            }
            else if (currentPoise < archetype.maxPoise)
            {
                currentPoise = Mathf.MoveTowards(currentPoise, archetype.maxPoise, archetype.poiseRegenRate * Time.deltaTime);
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
            currentPoise -= poiseDamage;
            _poiseRegenTimer = archetype.poiseResetDelay;

            if (currentPoise <= 0)
            {
                currentPoise = 0;
                ForceInterrupt(); // Postawa przełamana!
            }
            else
            {
                // Small stagger or hyper armor?
                // Tu można dodać animację drgnięcia "SmallHit"
            }
        }

        public void ForceInterrupt()
        {
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
            transform.rotation *= anim.deltaRotation;
        }
    }
}
