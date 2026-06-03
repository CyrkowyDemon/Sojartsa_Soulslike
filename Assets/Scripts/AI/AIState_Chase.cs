using UnityEngine;

namespace AI
{
    public class AIState_Chase : AIBaseState
    {
        private float _stopDistance = 3.5f;
        private float _originalSpeed = 1.5f;
        private float _lostTargetTimer = 0f;
        private const float TIME_TO_LOSE_TARGET = 4.0f; // Po 4 sekundach braku widoczności wróg wraca na pozycję
        private const float MAX_LEASH_DISTANCE = 30f;   // Max dystans od bazy (smycz)
        private float _leashCheckTimer = 0f;
        private const float LEASH_CHECK_INTERVAL = 0.5f; // Sprawdzamy smycz co 0.5s (nie co klatkę)

        public AIState_Chase(AIStateMachine machine, EnemyBase owner) : base(machine, owner) { }

        public override void Enter()
        {
            // Gdy przechodzimy w tryb pościgu, wróg oficjalnie jest "W Walce"
            owner.IsInCombat = true;

            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.isStopped = false;
                
                _originalSpeed = owner.Agent.speed;
                // Romero biegnie szybciej podczas pościgu (np. 3.8)
                owner.Agent.speed = 3.8f;

                // Ustawiamy dystans zatrzymania
                SoulsAI soulsAI = owner as SoulsAI;
                if (soulsAI != null && soulsAI.BehaviorConfig != null)
                    _stopDistance = soulsAI.BehaviorConfig.stopDistance;
                else
                    _stopDistance = 3.5f; // Fallback
            }
            _lostTargetTimer = 0f;
            if (owner.Animator != null)
            {
                owner.Animator.SetFloat("SidewaysSpeed", 0f);
                owner.Animator.SetFloat("ForwardSpeed", 1.0f);
            }
        }

        public override void LogicUpdate()
        {
            if (owner.Target == null)
            {
                machine.ChangeState(new AIState_Return(machine, owner));
                return;
            }

            float sqrDist = (owner.Target.position - owner.transform.position).sqrMagnitude;

            // --- SYSTEM SMYCZY (Leash) – optymalizowany, co 0.5s ---
            _leashCheckTimer += Time.deltaTime;
            if (_leashCheckTimer >= LEASH_CHECK_INTERVAL)
            {
                _leashCheckTimer = 0f;
                float sqrDistFromBase = (owner.transform.position - owner.StartPosition).sqrMagnitude;
                if (sqrDistFromBase > MAX_LEASH_DISTANCE * MAX_LEASH_DISTANCE)
                {
                    // Wróg za daleko od bazy – wraca bez względu na wszystko
                    machine.ChangeState(new AIState_Return(machine, owner));
                    return;
                }
            }

            // --- SYSTEM GUBIENIA GRACZA (Line of Sight) ---
            Vector3 rayStart = owner.transform.position + Vector3.up * 1.5f;
            Vector3 rayEnd = owner.Target.position + Vector3.up * 1.5f;
            bool hasLineOfSight = !Physics.Linecast(rayStart, rayEnd, LayerMask.GetMask("Default", "Environment", "Obstacles"));

            if (sqrDist > 25f * 25f || !hasLineOfSight)
            {
                _lostTargetTimer += Time.deltaTime;
                if (_lostTargetTimer >= TIME_TO_LOSE_TARGET)
                {
                    machine.ChangeState(new AIState_Return(machine, owner));
                    return;
                }
            }
            else
            {
                _lostTargetTimer = 0f;
            }

            // Aktualizujemy cel
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.SetDestination(owner.Target.position);
                
                // Jeśli jesteśmy na NavMeshu, ale agent "nie wie co zrobić", rusz go
                if (owner.Agent.isStopped) owner.Agent.isStopped = false;
            }

            // Sync animatora
            SyncAnimation();

            // SZYBKI OBRÓT DO GRACZA (Zawsze patrz, kogo gonisz!)
            LookAtTarget();

            // Przejście do ataku / okrążania
            if (sqrDist < _stopDistance * _stopDistance)
            {
                SoulsAI soulsAI = owner as SoulsAI;
                if (soulsAI != null)
                {
                    // Rzut kostką: Strafe czy Atak?
                    if (soulsAI.ShouldStrafe())
                    {
                        machine.ChangeState(new AIState_Strafe(machine, owner));
                        return;
                    }

                    if (!soulsAI.IsAttackOnCooldown)
                    {
                        var attack = soulsAI.GetNextAttack(Mathf.Sqrt(sqrDist));
                        if (attack != null)
                        {
                            machine.ChangeState(new AIState_Attack(machine, owner, attack));
                            return;
                        }
                    }
                    
                    // Jeśli nic nie wybraliśmy (cooldown) -> Strafe
                    machine.ChangeState(new AIState_Strafe(machine, owner));
                }
                else
                {
                    // Fallback
                    machine.ChangeState(new AIState_Strafe(machine, owner));
                }
            }
        }

        private void SyncAnimation()
        {
            if (owner.Animator == null) return;
            owner.Animator.SetFloat("ForwardSpeed", 1.0f, 0.1f, Time.deltaTime);
            owner.Animator.SetFloat("SidewaysSpeed", 0f, 0.1f, Time.deltaTime);
        }

        private void LookAtTarget()
        {
            if (owner.Target == null) return;
            Vector3 dir = (owner.Target.position - owner.transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                owner.transform.rotation = Quaternion.Slerp(
                    owner.transform.rotation, 
                    Quaternion.LookRotation(dir), 
                    Time.deltaTime * 12f // Szybki obrót podczas pościgu
                );
            }
        }

        public override void PhysicsUpdate() { }

        public override void Exit() 
        {
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.speed = _originalSpeed;
            }
        }
    }
}
