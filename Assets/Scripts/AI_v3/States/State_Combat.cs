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

        public State_Combat(AIBrain brain) : base(brain) { }

        public override void Enter()
        {
            base.Enter();
            if (GlobalCombatDirector.Instance != null)
                GlobalCombatDirector.Instance.RegisterEnemy(brain);
                
            _decisionTimer = brain.archetype.thinkDelay;
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
                // Sprawdzamy czy akcja reakcji jest w zasięgu
                float d = brain.Sensory.Distance;
                if (d >= bestReaction.actionToPerform.minDistance && d <= bestReaction.actionToPerform.maxDistance)
                {
                    brain.ChangeState(new State_Action(brain, bestReaction.actionToPerform));
                    return;
                }
            }

            // 2. RUCH TAKTYCZNY (Flankowanie za pomocą Slotów)
            if (GlobalCombatDirector.Instance != null)
            {
                Vector3 slotPos = GlobalCombatDirector.Instance.GetSlotPosition(brain, brain.target);
                brain.MoveTo(slotPos);
                
                // Animacja (krążenie w boki)
                float distToSlot = Vector3.Distance(brain.transform.position, slotPos);
                brain.anim.SetFloat("ForwardSpeed", distToSlot > 0.5f ? 0.5f : 0f);
            }

            // 3. DECYZJA O ATAKU
            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer <= 0f)
            {
                if (TryAttack())
                {
                    _decisionTimer = brain.archetype.thinkDelay;
                }
            }

            // Patrzymy na gracza
            RotateTowardsTarget();
        }

        private bool TryAttack(bool force = false)
        {
            // Pytamy Dyrektora o żeton (Token)
            if (GlobalCombatDirector.Instance != null)
            {
                if (!GlobalCombatDirector.Instance.RequestAttackToken(brain)) return false;
            }

            // Wybieramy akcję
            AIActionData action = SelectAction(AIActionType.Attack);
            if (action != null)
            {
                brain.ChangeState(new State_Action(brain, action));
                return true;
            }

            return false;
        }

        private AIActionData SelectAction(AIActionType type)
        {
            // Ważone losowanie akcji z Archetypu
            List<AIActionData> valid = new List<AIActionData>();
            float totalWeight = 0;

            foreach (var action in brain.archetype.actions)
            {
                if (action.type != type) continue;
                if (brain.Sensory.Distance < action.minDistance || brain.Sensory.Distance > action.maxDistance) continue;
                
                // Kierunkowość
                if (action.isBehindOnly && brain.Sensory.Side != AISensory.PlayerSide.Behind) continue;
                if (!action.isBehindOnly && brain.Sensory.Side == AISensory.PlayerSide.Behind) continue;

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
