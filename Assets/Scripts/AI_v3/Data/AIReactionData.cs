using UnityEngine;

namespace SojartsaAI.v3
{
    public enum AIReactionCondition { OnPlayerAttack, OnPlayerHeal, OnPlayerDistanceClose }

    [CreateAssetMenu(fileName = "New AI Reaction", menuName = "AAA SOJARTSA AI/Reaction")]
    public class AIReactionData : ScriptableObject
    {
        public AIReactionCondition condition;
        public AIActionData actionToPerform;
        
        [Range(0, 100)]
        public int priority = 50;
        
        public float weight = 1.0f; // Losowość wewnątrz tego samego priorytetu
        public float reactionDelay = 0.2f; // Realistyczny czas reakcji (nie frame-perfect)
    }
}
