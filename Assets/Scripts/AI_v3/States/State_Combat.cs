using UnityEngine;
using System.Collections.Generic;

namespace SojartsaAI.v3
{
    /// <summary>
    /// STAN WALKI (The Tactical Dance).
    /// Tu dzieje się osaczanie, reakcje na leczenie i decyzje o ataku.
    /// </summary>
    public class State_Combat : AIState
    {
        private float _decisionTimer;
        private float _movementDecisionTimer;
        private float _currentStrafeOffset;
        private float _targetStrafeOffset;

        public State_Combat(AIBrain brain) : base(brain) { }

        public override void Enter()
        {
            base.Enter();
            if (GlobalCombatDirector.Instance != null)
                GlobalCombatDirector.Instance.RegisterEnemy(brain);
                
            _decisionTimer = Random.Range(brain.archetype.minThinkDelay, brain.archetype.maxThinkDelay);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            brain.Sensory.Tick();

            // ================================================================
            // 1. REAKCJE (The AAA Intelligence - Weights & Priority)
            // ================================================================
            AIReactionData bestReaction = null;
            float totalWeight = 0;
            int highestPriority = -1;

            if (brain.archetype.reactions != null)
            {
                foreach (var reaction in brain.archetype.reactions)
                {
                    // AAA: Sprawdzamy Cooldown reakcji
                    if (!brain.IsActionReady(reaction.actionToPerform)) continue;

                    bool conditionMet = false;
                    if (reaction.condition == AIReactionCondition.OnPlayerHeal && brain.Sensory.IsPlayerHealing()) conditionMet = true;
                    
                    if (reaction.condition == AIReactionCondition.OnPlayerAttack && 
                        (brain.Sensory.IsPlayerAttacking() || brain.Sensory.IsPlayerPressingAttack)) conditionMet = true;
                        
                    if (reaction.condition == AIReactionCondition.OnPlayerDistanceClose && brain.Sensory.Distance < 2f) conditionMet = true;

                    if (conditionMet)
                    {
                        if (reaction.priority > highestPriority)
                        {
                            highestPriority = reaction.priority;
                            bestReaction = reaction;
                            totalWeight = reaction.weight;
                        }
                        else if (reaction.priority == highestPriority)
                        {
                            totalWeight += reaction.weight;
                            if (Random.Range(0, totalWeight) <= reaction.weight)
                            {
                                bestReaction = reaction;
                            }
                        }
                    }
                }
            }

            if (bestReaction != null && bestReaction.actionToPerform != null)
            {
                float d = brain.Sensory.Distance;
                if (d >= bestReaction.actionToPerform.minDistance && d <= bestReaction.actionToPerform.maxDistance)
                {
                    brain.RecordActionUse(bestReaction.actionToPerform);
                    brain.ChangeState(new State_Action(brain, bestReaction.actionToPerform));
                    return;
                }
            }

            // 2. RUCH TAKTYCZNY (Flankowanie i Strafing - AAA Movement Decisions)
            if (GlobalCombatDirector.Instance != null)
            {
                // Aktualizujemy decyzję o kierunku krążenia
                _movementDecisionTimer -= Time.deltaTime;
                if (_movementDecisionTimer <= 0f)
                {
                    // Losujemy nowy offset: -30 stopni (lewo), 0 (centrum), 30 (prawo)
                    float[] choices = { -30f, 0f, 30f };
                    _targetStrafeOffset = choices[Random.Range(0, choices.Length)];
                    _movementDecisionTimer = Random.Range(2f, 4f); // Jak długo trzymać ten kierunek
                }

                // Płynne przechodzenie do nowego offsetu (Lerp)
                _currentStrafeOffset = Mathf.Lerp(_currentStrafeOffset, _targetStrafeOffset, Time.deltaTime * 2f);

                Vector3 slotPos = GlobalCombatDirector.Instance.GetSlotPosition(brain, brain.target, _currentStrafeOffset);
                brain.MoveTo(slotPos);
                
                // AAA: Obliczamy wektor ruchu względem wroga, żeby obsłużyć Strafing (RightSpeed)
                Vector3 moveDir = (slotPos - brain.transform.position);
                float distToSlot = moveDir.magnitude;
                
                if (distToSlot > 0.3f) // Nieco mniejszy próg dla płynności
                {
                    moveDir.Normalize();
                    // Mapujemy ruch na osie Forward/Right wroga
                    float forwardMove = Vector3.Dot(brain.transform.forward, moveDir);
                    float rightMove = Vector3.Dot(brain.transform.right, moveDir);

                    brain.anim.SetFloat("ForwardSpeed", forwardMove * brain.movementSpeedMultiplier, 0.1f, Time.deltaTime);
                    brain.anim.SetFloat("SidewaysSpeed", rightMove * brain.movementSpeedMultiplier, 0.1f, Time.deltaTime);
                }
                else
                {
                    brain.anim.SetFloat("ForwardSpeed", 0, 0.2f, Time.deltaTime);
                    brain.anim.SetFloat("SidewaysSpeed", 0, 0.2f, Time.deltaTime);
                }
            }

            // 3. DECYZJA O ATAKU
            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer <= 0f)
            {
                if (TryAttack())
                {
                    _decisionTimer = Random.Range(brain.archetype.minThinkDelay, brain.archetype.maxThinkDelay);
                }
            }

            // Patrzymy na gracza
            RotateTowardsTarget();
        }

        private bool TryAttack(bool force = false)
        {
            if (GlobalCombatDirector.Instance != null)
            {
                if (!GlobalCombatDirector.Instance.RequestAttackToken(brain)) return false;
            }

            AIActionData action = SelectAction(AIActionType.Attack);
            if (action != null)
            {
                brain.RecordActionUse(action);
                brain.ChangeState(new State_Action(brain, action));
                return true;
            }

            return false;
        }

        private AIActionData SelectAction(AIActionType type)
        {
            List<AIActionData> valid = new List<AIActionData>();
            float totalWeight = 0;

            foreach (var action in brain.archetype.actions)
            {
                if (action.type != type) continue;
                if (!brain.IsActionReady(action)) continue; // AAA: Sprawdzenie Cooldownu
                
                if (brain.Sensory.Distance < action.minDistance || brain.Sensory.Distance > action.maxDistance) continue;
                
                // Kierunkowość (Relaxed: Z tyłu preferujemy backstaby, ale zwykłe ataki też dopuszczalne jeśli brak innych)
                if (action.isBehindOnly && brain.Sensory.Side != AISensory.PlayerSide.Behind) continue;

                // --- AAA: Environmental Awareness ---
                if (action.checkEnvironment)
                {
                    Vector3 worldDir = brain.transform.TransformDirection(action.environmentCheckDir);
                    if (brain.Sensory.IsPathBlocked(worldDir, action.checkDistance)) continue;
                }

                valid.Add(action);
                totalWeight += action.weight;
            }

            if (valid.Count == 0) return null;

            float roll = Random.Range(0, totalWeight);
            float sum = 0;
            foreach (var action in valid)
            {
                sum += action.weight;
                if (roll <= sum) return action;
            }
            return valid[0];
        }

        private void RotateTowardsTarget()
        {
            Vector3 dir = (brain.target.position - brain.transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
            }
        }

        public override void Exit()
        {
            // Nie wyrejestrowujemy się z Director, bo wciąż jesteśmy w walce!
        }
    }
}
