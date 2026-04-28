using UnityEngine;
using System.Collections.Generic;

namespace SojartsaAI.v3
{
    [CreateAssetMenu(fileName = "New AI Archetype", menuName = "AAA SOJARTSA AI/Archetype")]
    public class AIArchetype : ScriptableObject
    {
        [Header("Percepcja")]
        public float lookRange = 20f;
        public float fieldOfView = 120f;
        public LayerMask obstacleMask;

        [Header("Osobowość")]
        [Tooltip("Minimalny czas namysłu między akcjami")]
        public float thinkDelay = 1.0f;
        public float preferredCombatDistance = 3.5f;

        [Header("Statystyki Postury (AAA)")]
        public float maxPoise = 100f;
        public float poiseRegenRate = 10f;
        public float poiseResetDelay = 3.0f;
        
        [Header("System Reakcji (Deterministyczny)")]
        public List<AIReactionData> reactions;

        [Header("Dostępne Akcje")]
        public List<AIActionData> actions;
    }
}
