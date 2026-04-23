using UnityEngine;

namespace AI
{
    public class AIState_Idle : AIBaseState
    {
        public AIState_Idle(AIStateMachine machine, EnemyBase owner) : base(machine, owner) { }

        public override void Enter()
        {
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
                owner.Agent.isStopped = true;
            
            if (owner.Animator != null)
                owner.Animator.SetFloat("ForwardSpeed", 0f, 0.1f, Time.deltaTime);
        }

        public override void LogicUpdate()
        {
            if (owner.Target == null) return;

            float sqrDist = (owner.Target.position - owner.transform.position).sqrMagnitude;
            
            // Jeśli wróg zobaczy gracza, przechodzi w tryb walki i pościgu
            if (owner.CheckCanSeePlayer(sqrDist))
            {
                // Tutaj przełączymy na Chase (klasa powstanie zaraz)
                machine.ChangeState(new AIState_Chase(machine, owner));
            }
        }

        public override void PhysicsUpdate() { }

        public override void Exit() { }
    }
}
