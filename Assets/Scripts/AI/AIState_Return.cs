using UnityEngine;
using UnityEngine.AI;

namespace AI
{
    public class AIState_Return : AIBaseState
    {
        private float _arrivalThreshold = 1.5f;

        public AIState_Return(AIStateMachine machine, EnemyBase owner) : base(machine, owner) { }

        public override void Enter()
        {
            owner.IsInCombat = false;
            
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.isStopped = false;
                // Ustawiamy mniejszą prędkość dla powrotu (np. spacer/patrol)
                owner.Agent.speed = 1.5f; 
                owner.Agent.SetDestination(owner.StartPosition);
            }
            
            Debug.Log($"[AI] {owner.gameObject.name}: Zgubiłem gracza. Wracam na pozycję startową.");
            if (owner.Animator != null)
            {
                owner.Animator.SetFloat("SidewaysSpeed", 0f);
                owner.Animator.SetFloat("ForwardSpeed", 0.5f);
            }
        }

        public override void LogicUpdate()
        {
            // 1. Sprawdzamy, czy w trakcie powrotu znów nie zobaczyliśmy gracza
            float sqrDistToPlayer = (owner.Target.position - owner.transform.position).sqrMagnitude;
            if (owner.CheckCanSeePlayer(sqrDistToPlayer))
            {
                machine.ChangeState(new AIState_Chase(machine, owner));
                return;
            }

            // 2. Jeśli dotarliśmy na miejsce, regenerujemy HP i przechodzimy do Idle
            float sqrDistToStart = (owner.StartPosition - owner.transform.position).sqrMagnitude;
            if (sqrDistToStart < _arrivalThreshold * _arrivalThreshold)
            {
                // Pełna regeneracja HP i ran po powrocie do bazy
                EnemyHealth health = owner.GetComponent<EnemyHealth>();
                if (health != null) health.FullHeal();

                machine.ChangeState(new AIState_Idle(machine, owner));
                return;
            }

            // 3. Ciągle idziemy do celu
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.SetDestination(owner.StartPosition);
                if (owner.Agent.isStopped) owner.Agent.isStopped = false;
            }

            SyncAnimation();
            LookAtDirection();
        }

        private void SyncAnimation()
        {
            if (owner.Animator == null) return;
            owner.Animator.SetFloat("ForwardSpeed", 0.5f, 0.1f, Time.deltaTime);
            owner.Animator.SetFloat("SidewaysSpeed", 0f, 0.1f, Time.deltaTime);
        }

        private void LookAtDirection()
        {
            if (owner.Agent == null || owner.Agent.velocity.sqrMagnitude < 0.1f) return;
            Vector3 dir = owner.Agent.velocity.normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                owner.transform.rotation = Quaternion.Slerp(
                    owner.transform.rotation, 
                    Quaternion.LookRotation(dir), 
                    Time.deltaTime * 6f
                );
            }
        }

        public override void PhysicsUpdate() { }

        public override void Exit()
        {
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.isStopped = true;
            }
        }
    }
}
