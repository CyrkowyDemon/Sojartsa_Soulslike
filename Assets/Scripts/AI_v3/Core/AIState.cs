using UnityEngine;
using System;

namespace SojartsaAI.v3
{
    /// <summary>
    /// Baza dla wszystkich stanów AI (v3 AAA Edition).
    /// Każdy stan jest hermetyczny i zarządza własną logiką.
    /// </summary>
    public abstract class AIState
    {
        protected AIBrain brain;
        protected float stateTimer;

        public AIState(AIBrain brain)
        {
            this.brain = brain;
        }

        public virtual void Enter() { stateTimer = 0f; }
        public virtual void LogicUpdate() { stateTimer += Time.deltaTime; }
        public virtual void PhysicsUpdate() { }
        public virtual void Exit() { }

        // Sygnały z Animacji (z StateMachineBehaviours)
        public virtual void OnAnimationSignal(string signal) { }
    }
}
