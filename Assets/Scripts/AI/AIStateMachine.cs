using UnityEngine;

namespace AI
{
    public class AIStateMachine : MonoBehaviour
    {
        public AIBaseState CurrentState { get; private set; }

        // Opcjonalnie: Referencja do kontrolera AI/Encji, 
        // którą stany będą manipulować (np. agent, animacje)
        // public AIController Owner { get; private set; }

        public void Initialize(AIBaseState initialState)
        {
            if (initialState == null) return;

            CurrentState = initialState;
            CurrentState.Enter();
        }

        public void ChangeState(AIBaseState newState)
        {
            // Sprawdzenie, czy nowy stan nie jest nullem, zapobiega crashom
            if (newState == null || CurrentState == newState) return;

            CurrentState?.Exit(); // Bezpieczne wyjście (null-conditional)
            CurrentState = newState;
            CurrentState.Enter();
        }

        private void Update()
        {
            // Wykonujemy Update tylko, jeśli stan istnieje
            CurrentState?.LogicUpdate();
        }

        private void FixedUpdate()
        {
            CurrentState?.PhysicsUpdate();
        }
    }
}
