using UnityEngine;

namespace AI
{
    public class AIState_Chase : AIBaseState
    {
        private float _stopDistance = 3.5f;

        public AIState_Chase(AIStateMachine machine, EnemyBase owner) : base(machine, owner) { }

        public override void Enter()
        {
            // Gdy przechodzimy w tryb pościgu, wróg oficjalnie jest "W Walce"
            owner.IsInCombat = true;

            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.isStopped = false;
                
                // Ustawiamy dystans zatrzymania
                SoulsAI soulsAI = owner as SoulsAI;
                if (soulsAI != null && soulsAI.BehaviorConfig != null)
                    _stopDistance = soulsAI.BehaviorConfig.stopDistance;
                else
                    _stopDistance = 3.5f; // Fallback
            }
        }

        public override void LogicUpdate()
        {
            if (owner.Target == null)
            {
                machine.ChangeState(new AIState_Idle(machine, owner));
                return;
            }

            float sqrDist = (owner.Target.position - owner.transform.position).sqrMagnitude;

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
            if (owner.Animator == null || owner.Agent == null) return;

            Vector3 velocity = owner.Agent.velocity;
            Vector3 localVel = owner.transform.InverseTransformDirection(velocity);
            
            owner.Animator.SetFloat("ForwardSpeed", localVel.z, 0.1f, Time.deltaTime);
            owner.Animator.SetFloat("SidewaysSpeed", localVel.x, 0.1f, Time.deltaTime);
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

        public override void Exit() { }
    }
}
