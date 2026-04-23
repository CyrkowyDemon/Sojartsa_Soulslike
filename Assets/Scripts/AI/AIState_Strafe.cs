using UnityEngine;

namespace AI
{
    public class AIState_Strafe : AIBaseState
    {
        private float _strafeDir = 1f;
        private float _nextDirChangeTime;
        private float _minAttackDist = 2.5f;
        private float _maxAttackDist = 7f;
        private float _nextAttackCheckTime;

        public AIState_Strafe(AIStateMachine machine, EnemyBase owner) : base(machine, owner) { }

        public override void Enter()
        {
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.updatePosition = true;
                owner.Agent.updateRotation = false; // Sami kontrolujemy obrót w stronę gracza
                owner.Agent.isStopped = false;
            }

            SoulsAI soulsAI = owner as SoulsAI;
            float minDur = 1.5f;
            float maxDur = 4f;

            if (soulsAI != null && soulsAI.BehaviorConfig != null)
            {
                _maxAttackDist = soulsAI.BehaviorConfig.stopDistance * 2f;
                _minAttackDist = soulsAI.BehaviorConfig.stopDistance * 0.7f;
                minDur = soulsAI.BehaviorConfig.minStrafeDuration;
                maxDur = soulsAI.BehaviorConfig.maxStrafeDuration;
            }

            _strafeDir = Random.value > 0.5f ? 1f : -1f;
            _nextDirChangeTime = Time.time + Random.Range(minDur, maxDur);
            
            // OPÓŹNIENIE ATAKU PO WEJŚCIU W STRAFE: Zmusi go do pokrążenia chwilę, zamiast od razu bić.
            float aggression = (soulsAI != null && soulsAI.BehaviorConfig != null) ? soulsAI.BehaviorConfig.aggressionLevel : 1.5f;
            _nextAttackCheckTime = Time.time + Random.Range(aggression * 0.5f, aggression * 1.5f);
        }

        public override void LogicUpdate()
        {
            if (owner.Target == null)
            {
                machine.ChangeState(new AIState_Idle(machine, owner));
                return;
            }

            float dist = Vector3.Distance(owner.transform.position, owner.Target.position);

            // Jeśli gracz ucieknie za daleko, wracamy do Chase
            if (dist > _maxAttackDist)
            {
                machine.ChangeState(new AIState_Chase(machine, owner));
                return;
            }

            // --- LOGIKA WYBORU ATAKU ---
            SoulsAI soulsAI = owner as SoulsAI;
            if (soulsAI != null)
            {
                float aggression = soulsAI.BehaviorConfig != null ? soulsAI.BehaviorConfig.aggressionLevel : 1f;

                if (Time.time > _nextAttackCheckTime)
                {
                    _nextAttackCheckTime = Time.time + aggression;
                    
                    if (!soulsAI.IsAttackOnCooldown)
                    {
                        var attack = soulsAI.GetNextAttack(dist);
                        if (attack != null)
                        {
                            machine.ChangeState(new AIState_Attack(machine, owner, attack));
                            return;
                        }
                    }
                }
            }

            // Losowa zmiana kierunku krążenia
            if (Time.time > _nextDirChangeTime)
            {
                _strafeDir *= -1;
                _nextDirChangeTime = Time.time + Random.Range(2f, 5f);
            }

            // --- RUCH KRĄŻĄCY ---
            MoveToStrafePosition(dist);

            // Obrót w stronę gracza
            LookAtTarget();

            // Animacja krążenia
            SyncAnimation();
        }

        private void MoveToStrafePosition(float dist)
        {
            if (owner.Agent == null || !owner.Agent.isOnNavMesh) return;

            Vector3 directionToPlayer = (owner.Target.position - owner.transform.position).normalized;
            Vector3 sideways = Vector3.Cross(Vector3.up, directionToPlayer) * _strafeDir;
            
            // Punkt docelowy to wektor wypadkowy: trochę w bok, trochę w tył/przód zależnie od dystansu
            float forwardWeight = 0f;
            if (dist < _minAttackDist) forwardWeight = -0.5f; // Cofaj się
            else if (dist > (_maxAttackDist + _minAttackDist) / 2f) forwardWeight = 0.3f; // Przybliż się lekko

            Vector3 moveTarget = owner.transform.position + sideways + (directionToPlayer * forwardWeight);

            // Próbujemy znaleźć bezpieczny punkt na NavMeshu blisko celu
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(moveTarget, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            {
                owner.Agent.SetDestination(hit.position);
            }
        }

        private void LookAtTarget()
        {
            Vector3 dir = (owner.Target.position - owner.transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                float rotSpeed = 10f;
                SoulsAI soulsAI = owner as SoulsAI;
                if (soulsAI != null && soulsAI.BehaviorConfig != null)
                    rotSpeed = soulsAI.BehaviorConfig.rotationSpeed;

                owner.transform.rotation = Quaternion.Slerp(
                    owner.transform.rotation, 
                    Quaternion.LookRotation(dir), 
                    Time.deltaTime * rotSpeed
                );
            }
        }

        private void SyncAnimation()
        {
            if (owner.Animator == null || owner.Agent == null) return;

            // Pobieramy prędkość agenta i rzutujemy ją na lokalne osie, żeby animator wiedział czy idziemy w bok czy przód
            Vector3 localVel = owner.transform.InverseTransformDirection(owner.Agent.velocity);
            
            owner.Animator.SetFloat("SidewaysSpeed", localVel.x, 0.1f, Time.deltaTime);
            owner.Animator.SetFloat("ForwardSpeed", localVel.z, 0.1f, Time.deltaTime);
        }

        public override void PhysicsUpdate() { }

        public override void Exit() 
        {
            // Nie ruszamy updateRotation, od teraz zawsze sami to kontrolujemy.
        }
    }
}
