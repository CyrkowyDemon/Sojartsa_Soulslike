using UnityEngine;

namespace AI
{
    public class AIState_Stagger : AIBaseState
    {
        private float _staggerDuration = 0.8f;
        private float _timer;

        public AIState_Stagger(AIStateMachine machine, EnemyBase owner) : base(machine, owner) { }

        public override void Enter()
        {
            _timer = _staggerDuration;

            // Zatrzymujemy ruch
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
                owner.Agent.isStopped = true;

            // Ustawiamy prędkość animatora na 0, żeby nie płynął w miejscu
            if (owner.Animator != null)
            {
                owner.Animator.SetFloat("ForwardSpeed", 0f);
                owner.Animator.SetFloat("SidewaysSpeed", 0f);
            }
        }

        public override void LogicUpdate()
        {
            _timer -= Time.deltaTime;

            if (_timer <= 0)
            {
                // Powrót do walki - decydujemy czy Chase czy Strafe (Soulsy zazwyczaj wracają do Strafe by dać graczowi oddech)
                float sqrDist = (owner.Target.position - owner.transform.position).sqrMagnitude;
                if (sqrDist < 5f * 5f)
                {
                    machine.ChangeState(new AIState_Strafe(machine, owner));
                }
                else
                {
                    machine.ChangeState(new AIState_Chase(machine, owner));
                }
            }
        }

        public override void PhysicsUpdate() { }

        public override void Exit() { }
    }
}
