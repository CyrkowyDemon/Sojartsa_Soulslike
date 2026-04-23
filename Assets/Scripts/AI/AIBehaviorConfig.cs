using UnityEngine;

namespace AI
{
    [CreateAssetMenu(fileName = "NewAIBehavior", menuName = "AI/Behavior Config")]
    public class AIBehaviorConfig : ScriptableObject
    {
        [Header("Poruszanie się")]
        [Tooltip("Odległość z jakiej wróg zaczyna walkę / okrążanie")]
        public float stopDistance = 3.5f;
        
        [Tooltip("Szybkość obracania się w stronę gracza")]
        public float rotationSpeed = 8f;

        [Header("Decyzje (Szanse Procentowe)")]
        [Range(0, 100)]
        [Tooltip("Szansa (%), że wróg zacznie Cię okrążać zamiast od razu atakować")]
        public float strafeChance = 40f;
        
        [Range(0.1f, 5f)]
        [Tooltip("Jak często AI sprawdza czy może zaatakować (agresja)")]
        public float aggressionLevel = 1f;

        [Header("Okrążanie (Strafe)")]
        [Tooltip("Minimalny czas krążenia")]
        public float minStrafeDuration = 1.5f;
        [Tooltip("Maksymalny czas krążenia")]
        public float maxStrafeDuration = 4f;

        [Header("Bezpieczeństwo")]
        [Tooltip("Maksymalny czas trwania ataku (zabezpieczenie przed zacięciem)")]
        public float maxAttackDuration = 3f;
    }
}
