using UnityEngine;

namespace SojartsaAI.v3
{
    /// <summary>
    /// Przekaźnik dla animacji przeciwnika. 
    /// Odbiera eventy z animacji i przekazuje je do EnemySFXManager na rodzicu.
    /// </summary>
    public class EnemyAnimationRelay : MonoBehaviour
    {
        private EnemySFXManager _sfxManager;

        private void Awake()
        {
            // Szukamy managera na tym samym obiekcie lub u rodziców
            _sfxManager = GetComponentInParent<EnemySFXManager>();
        }

        // --- FUNKCJE DLA ANIMATORA ---
        
        public void PlayAttackSound()
        {
            if (_sfxManager != null) _sfxManager.PlayAttackVoice();
        }

        public void PlayFootstepSound()
        {
            if (_sfxManager != null) _sfxManager.PlayFootstep();
        }
        
        // Możesz tu dodawać kolejne funkcje, których będziesz używał w Animacjach
    }
}
