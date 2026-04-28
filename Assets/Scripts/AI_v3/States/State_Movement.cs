using UnityEngine;

namespace SojartsaAI.v3
{
    public class State_Passive : AIState
    {
        public State_Passive(AIBrain brain) : base(brain) { }

        public override void Enter()
        {
            base.Enter();
            brain.agent.isStopped = true;
            brain.anim.SetFloat("ForwardSpeed", 0f);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            
            // Percepcja
            brain.Sensory.Tick();
            
            if (brain.Sensory.IsPlayerVisible)
            {
                brain.ChangeState(new State_Chase(brain));
            }
        }
    }

    public class State_Chase : AIState
    {
        public State_Chase(AIBrain brain) : base(brain) { }

        public override void Enter()
        {
            base.Enter();
            brain.agent.isStopped = false;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            brain.Sensory.Tick();

            float dist = brain.Sensory.Distance;
            float combatDist = brain.archetype.preferredCombatDistance;

            // Jeśli jesteśmy blisko, przechodzimy do taktyki (Combat)
            if (dist <= combatDist * 1.2f)
            {
                brain.ChangeState(new State_Combat(brain));
                return;
            }

            // Podążanie
            brain.MoveTo(brain.target.position);
            brain.anim.SetFloat("ForwardSpeed", 1f);

            // Jeśli stracimy gracza z oczu na zbyt długo - wróć do pasywnego
            if (!brain.Sensory.IsPlayerVisible && stateTimer > 5f)
            {
                brain.ChangeState(new State_Passive(brain));
            }
        }
    }
}
