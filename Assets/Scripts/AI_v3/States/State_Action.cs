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
        private float _randomCancelTime; // Wylosowany moment cancela (-1 = nieaktywny)

        public State_Action(AIBrain brain, AIActionData data) : base(brain) 
        { 
            _data = data;
        }

        public override void Enter()
        {
            base.Enter();
            _isActionComplete = false;
            _canCancel = false;
            _randomCancelTime = -1f;

            // Zatrzymujemy agenta, żeby Root Motion mógł przejąć kontrolę
            brain.agent.isStopped = true;

            // Jeśli tryb losowego okna jest włączony, losujemy moment cancela już teraz
            if (_data != null && _data.cancelIntoAction != null && _data.useRandomCancelWindow)
            {
                _randomCancelTime = Random.Range(_data.cancelWindowMin, _data.cancelWindowMax);
            }

            // Odpalamy animację
            if (_data != null)
                brain.anim.SetTrigger(_data.animationTrigger);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (_data == null) return;

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

            // --- SYSTEM CANCELOWANIA (Cancel Window) ---
            if (_data != null && _data.cancelIntoAction != null)
            {
                bool shouldCancel = false;

                if (_data.useRandomCancelWindow)
                {
                    // Tryb LOSOWY: cancel w wylosowanym momencie z zakresu [min, max]
                    if (_randomCancelTime >= 0f && stateInfo.normalizedTime >= _randomCancelTime)
                    {
                        shouldCancel = true;
                        _randomCancelTime = -1f; // Zabezpieczenie przed podwójnym odpaleniem
                    }
                }
                else
                {
                    // Tryb STANDARDOWY: cancel gdy animator wyśle sygnał CanCancel
                    if (_canCancel)
                    {
                        shouldCancel = true;
                    }
                }

                if (shouldCancel)
                {
                    brain.RecordActionUse(_data.cancelIntoAction);
                    brain.ChangeState(new State_Action(brain, _data.cancelIntoAction));
                    return;
                }
            }

            // Bezpiecznik czasowy na wypadek zgubionego sygnału ActionEnd w animacji (np. brak eventu w Unity)
            if (stateTimer > 4.0f)
            {
                _isActionComplete = true;
            }

            // Jeśli animacja wysłała sygnał końca - decydujemy co dalej
            if (_isActionComplete)
            {
                // --- BRANCHING COMBO (AAA Extension) ---
                if (_data != null && _data.randomFollowUps != null && _data.randomFollowUps.Count > 0)
                {
                    foreach (var branch in _data.randomFollowUps)
                    {
                        if (branch.followUpAction != null && Random.Range(0, 100) < branch.chance)
                        {
                            if (GlobalCombatDirector.Instance != null && GlobalCombatDirector.Instance.RequestAttackToken(brain))
                            {
                                brain.RecordActionUse(branch.followUpAction);
                                brain.ChangeState(new State_Action(brain, branch.followUpAction));
                                return;
                            }
                        }
                    }
                }

                // --- SYSTEM COMBO (AAA Extension) ---
                if (_data != null && _data.followUpAction != null)
                {
                    // Sprawdzamy czy mamy żeton na kolejny cios
                    if (GlobalCombatDirector.Instance != null && GlobalCombatDirector.Instance.RequestAttackToken(brain))
                    {
                        brain.RecordActionUse(_data.followUpAction);
                        brain.ChangeState(new State_Action(brain, _data.followUpAction));
                        return;
                    }
                }

                // Jeśli nie ma combo lub brak żetonu - wracamy do krążenia
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
        }
    }

    /// <summary>
    /// STAN STAGGERA (Hit Reaction).
    /// Również sterowany animacją.
    /// </summary>
    public class State_Stagger : AIState
    {
        private bool _isFinished;
        private float _maxDuration;

        public State_Stagger(AIBrain brain) : base(brain) { }

        public override void Enter()
        {
            base.Enter();
            _isFinished = false;
            brain.agent.isStopped = true;
            
            // Wyznaczamy maksymalny czas trwania staggera
            if (brain.staggerDurationOverride > 0f)
            {
                _maxDuration = brain.staggerDurationOverride;
            }
            else if (brain.archetype != null)
            {
                _maxDuration = brain.archetype.staggerDuration;
            }
            else
            {
                _maxDuration = 0.8f;
            }
            
            // Losujemy animację hita
            brain.anim.SetTrigger("HitReaction");
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            if (_isFinished) brain.ChangeState(new State_Combat(brain));
            
            // Bezpiecznik czasowy jeśli animacja nie wyśle sygnału
            if (stateTimer > _maxDuration) _isFinished = true;
        }

        public override void OnAnimationSignal(string signal)
        {
            if (signal == "ActionEnd") _isFinished = true;
        }
    }
}
