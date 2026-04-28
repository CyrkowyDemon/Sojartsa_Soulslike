using UnityEngine;

namespace SojartsaAI.v3
{
    public enum AIActionType { Attack, Dodge, Special, Buff }

    [CreateAssetMenu(fileName = "New AI Action", menuName = "AAA SOJARTSA AI/Action")]
    public class AIActionData : ScriptableObject
    {
        public string actionName;
        public AIActionType type;
        public string animationTrigger;

        [Header("Zasięg")]
        public float minDistance = 0f;
        public float maxDistance = 5f;

        [Header("Balans")]
        public float weight = 1.0f;
        public float cooldown = 2.0f;
        public float poiseDamage = 10f;
        
        [Header("AAA - Dynamic Tracking (The Hunting)")]
        [Tooltip("Jak silnie AI ma się obracać w stronę gracza w trakcie ataku (0-1)")]
        [Range(0, 1)] public float trackingIntensity = 0.5f;
        [Tooltip("Do którego momentu animacji AI ma śledzić gracza (np. 0.8 = do 80%)")]
        [Range(0, 1)] public float trackingCutoff = 0.5f;

        [Header("Kierunkowość")]
        public bool isBehindOnly = false;
        
        [Header("AAA - Environmental Awareness")]
        public bool checkEnvironment = false;
        public Vector3 environmentCheckDir = Vector3.back;
        public float checkDistance = 2f;
        
        [Header("Combo (AAA Extension)")]
        public AIActionData followUpAction;
    }
}
