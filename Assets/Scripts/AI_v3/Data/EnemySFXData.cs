using UnityEngine;
using FMODUnity;

namespace SojartsaAI.v3
{
    [CreateAssetMenu(fileName = "NewEnemySFXData", menuName = "Sojartsa/Enemy/SFX Data")]
    public class EnemySFXData : ScriptableObject
    {
        [Header("Vocal Sounds")]
        public EventReference attackVoice;
        public EventReference hitVoice;
        public EventReference deathVoice;
        public EventReference idleGrowl;

        [Header("Movement")]
        public EventReference footstepSound;
        public EventReference jumpSound;
        public EventReference landSound;

        [Header("Special")]
        public EventReference poiseBreakSound; // Ten legendarny "Ding!" z Sekiro/Souls
    }
}
