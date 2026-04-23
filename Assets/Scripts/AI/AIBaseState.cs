using UnityEngine;

namespace AI
{
    public abstract class AIBaseState
    {
        protected AIStateMachine machine;
        protected EnemyBase owner;

        public AIBaseState(AIStateMachine machine, EnemyBase owner)
        {
            this.machine = machine;
            this.owner = owner;
        }

        public abstract void Enter();
        public abstract void LogicUpdate();
        public abstract void PhysicsUpdate();
        public abstract void Exit();
    }
}
