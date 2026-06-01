using UnityEngine;

namespace SojartsaAI.v3
{
    public class State_Passive : AIState
    {
        public State_Passive(AIBrain brain) : base(brain) { }

        private float _nextIdleTime;

        public override void Enter()
        {
            base.Enter();
            brain.agent.isStopped = true;
            brain.anim.SetFloat("ForwardSpeed", 0f);
            _nextIdleTime = Time.time + Random.Range(3f, 10f);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            
            // Percepcja
            brain.Sensory.Tick();

            // Losowy dźwięk Idle
            if (Time.time > _nextIdleTime)
            {
                if (brain.SFX != null) brain.SFX.PlayIdleGrowl();
                _nextIdleTime = Time.time + Random.Range(5f, 15f);
            }
            
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
            brain.anim.SetFloat("ForwardSpeed", 1f * brain.movementSpeedMultiplier);

            // Obracanie wroga w stronę ścieżki lub bezpośrednio na cel (brak stucznej ścieżki = patrz na gracza)
            if (brain.agent != null && brain.agent.isOnNavMesh && brain.agent.hasPath)
            {
                Vector3 moveDir = brain.agent.desiredVelocity;
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
                    brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, targetRot, Time.deltaTime * 8f);
                }
            }
            else if (brain.target != null)
            {
                Vector3 dir = (brain.target.position - brain.transform.position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
                }
            }

            // Jeśli stracimy gracza z oczu na zbyt długo - wróć do pasywnego
            if (!brain.Sensory.IsPlayerVisible && stateTimer > 5f)
            {
                brain.ChangeState(new State_Passive(brain));
            }
        }
    }
}
