using UnityEngine;

namespace SojartsaAI.v3
{
    /// <summary>
    /// STAN AKCJI (Atak, Unik, Buff).
    /// To jest stan czysto animacyjny. Czeka na sygnały z Animatora (AIActionSMB).
    /// </summary>
    public class State_Action : AIState
    {
        private AIActionData _data;
        private bool _isActionComplete;
        private bool _canCancel;

        public State_Action(AIBrain brain, AIActionData data) : base(brain) 
        { 
            _data = data;
        }

        public override void Enter()
        {
            base.Enter();
            _isActionComplete = false;
            _canCancel = false;

            // Zatrzymujemy agenta, żeby Root Motion mógł przejąć kontrolę
            brain.agent.isStopped = true;

            // Odpalamy animację
            if (_data != null)
                brain.anim.SetTrigger(_data.animationTrigger);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            // --- AAA: Dynamic Tracking (The Hunting) ---
            AnimatorStateInfo stateInfo = brain.anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime < _data.trackingCutoff && brain.target != null)
            {
                Vector3 dir = (brain.target.position - brain.transform.position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, targetRot, Time.deltaTime * 10f * _data.trackingIntensity);
                }
            }

            // Jeśli animacja wysłała sygnał końca - wracamy do Combat
            if (_isActionComplete)
            {
                brain.ChangeState(new State_Combat(brain));
            }
        }

        public override void OnAnimationSignal(string signal)
        {
            if (signal == "ActionEnd")
            {
                _isActionComplete = true;
            }
            if (signal == "CanCancel")
            {
                _canCancel = true;
            }
        }

        public override void Exit()
        {
            // Zawsze uwalniamy żeton ataku po zakończeniu akcji
            if (GlobalCombatDirector.Instance != null)
                GlobalCombatDirector.Instance.ReleaseAttackToken(brain);

            // --- SYSTEM COMBO (AAA Extension) ---
            // Jeśli akcja ma Follow-up (Combo) i nie została przerwana (np. przez Stagger)
            if (_isActionComplete && _data != null && _data.followUpAction != null)
            {
                // Ważne: Sprawdzamy czy mamy żeton na kolejny cios
                if (GlobalCombatDirector.Instance != null && GlobalCombatDirector.Instance.RequestAttackToken(brain))
                {
                    // Używamy opóźnienia, żeby zmiana stanu nastąpiła "płynnie" po aktualnej klatce
                    brain.ChangeState(new State_Action(brain, _data.followUpAction));
                }
            }
        }
    }

    /// <summary>
    /// STAN STAGGERA (Hit Reaction).
    /// Również sterowany animacją.
    /// </summary>
    public class State_Stagger : AIState
    {
        private bool _isFinished;

        public State_Stagger(AIBrain brain) : base(brain) { }

        public override void Enter()
        {
            base.Enter();
            _isFinished = false;
            brain.agent.isStopped = true;
            
            // Losujemy animację hita
            brain.anim.SetTrigger("HitReaction");
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            if (_isFinished) brain.ChangeState(new State_Combat(brain));
            
            // Bezpiecznik czasowy (0.8s) jeśli animacja nie wyśle sygnału
            if (stateTimer > 0.8f) _isFinished = true;
        }

        public override void OnAnimationSignal(string signal)
        {
            if (signal == "ActionEnd") _isFinished = true;
        }
    }
}
