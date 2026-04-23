using UnityEngine;

namespace AI
{
    public class AIState_Attack : AIBaseState
    {
        private EnemyAttackData _data;
        private float _stateTimer;
        private float _maxDuration = 2.5f;
        private bool _hasStartedAnimation = false;

        public AIState_Attack(AIStateMachine machine, EnemyBase owner, EnemyAttackData data) : base(machine, owner)
        {
            _data = data;
        }

        public override void Enter()
        {
            if (owner.Animator == null || _data == null)
            {
                machine.ChangeState(new AIState_Idle(machine, owner));
                return;
            }

            _stateTimer = 0f;
            _hasStartedAnimation = false;

            // Zatrzymujemy agenta
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                owner.Agent.isStopped = true;
                owner.Agent.updatePosition = false;
                owner.Agent.updateRotation = false;
            }

            // Włączamy Root Motion TYLKO na czas ataku
            if (owner.Animator != null)
            {
                owner.Animator.applyRootMotion = true;
                owner.Animator.SetTrigger(_data.animationTrigger);
            }
            
            if (_data.isJumpAttack && owner.Target != null)
            {
                Vector3 dir = (owner.Target.position - owner.transform.position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                    owner.transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        public override void LogicUpdate()
        {
            _stateTimer += Time.deltaTime;

            if (owner.Animator == null)
            {
                machine.ChangeState(new AIState_Idle(machine, owner));
                return;
            }

            // BEZPIECZNIK: Jeśli atak trwa za długo, wymuszamy wyjście
            if (_stateTimer > _maxDuration)
            {
                machine.ChangeState(new AIState_Strafe(machine, owner));
                return;
            }

            int layerIndex = 0; 
            AnimatorStateInfo stateInfo = owner.Animator.GetCurrentAnimatorStateInfo(layerIndex);

            if (stateInfo.IsTag("Attack"))
            {
                _hasStartedAnimation = true;
            }

            // OBRÓT W STRONĘ GRACZA (Tylko na początku ataku, żeby się nie kręcił jak bąk podczas ciosu)
            if (_stateTimer < 0.6f && owner.Target != null) // Śledzi gracza przez pierwsze pół sekundy po odpaleniu ataku
            {
                Vector3 dir = (owner.Target.position - owner.transform.position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
                }
            }

            // LOGIKA WYJŚCIA
            if (_hasStartedAnimation && !stateInfo.IsTag("Attack") && !owner.Animator.IsInTransition(layerIndex))
            {
                machine.ChangeState(new AIState_Strafe(machine, owner));
            }
        }

        public override void PhysicsUpdate() { }

        public override void Exit()
        {
            // Przywracamy normalny ruch i wyłączamy Root Motion
            if (owner.Agent != null && owner.Agent.isOnNavMesh)
            {
                // !!! KLUCZOWE: Synchronizujemy agenta do miejsca, gdzie Skończyła się animacja (np. koniec dasha)
                owner.Agent.nextPosition = owner.transform.position;
                
                owner.Agent.updatePosition = true;
                owner.Agent.updateRotation = true;
                owner.Agent.isStopped = false;
            }

            if (owner.Animator != null)
            {
                owner.Animator.applyRootMotion = false;
                // Czyścimy trigger, żeby nie odpalił się "z opóźnieniem" później
                if (_data != null)
                    owner.Animator.ResetTrigger(_data.animationTrigger);
            }

            // BEZPIECZNIK HITBOXÓW: Na wypadek, gdyby event z animacji się nie odpalił
            SoulsAI soulsAI = owner as SoulsAI;
            if (soulsAI != null)
            {
                soulsAI.CloseDamage();
            }
        }
    }
}
