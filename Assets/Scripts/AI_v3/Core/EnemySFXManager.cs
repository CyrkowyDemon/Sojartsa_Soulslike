using UnityEngine;
using FMODUnity;

namespace SojartsaAI.v3
{
    /// <summary>
    /// Zarządza odtwarzaniem dźwięków przeciwnika na podstawie przypisanych danych (EnemySFXData).
    /// </summary>
    public class EnemySFXManager : MonoBehaviour
    {
        [Header("Dane Audio")]
        public EnemySFXData sfxData;

        public bool HasData => sfxData != null;
        private float _lastIdleTime;
        private FMOD.Studio.EventInstance _idleInstance;
        private bool _isIdleLooping = false;

        private void Start()
        {
            // Jeśli dźwięk idle jest zapętlony (np. brzęczenie pszczoły), odpalamy go na starcie jako stałą instancję
            if (HasData && !sfxData.idleGrowl.IsNull)
            {
                _idleInstance = RuntimeManager.CreateInstance(sfxData.idleGrowl);
                RuntimeManager.AttachInstanceToGameObject(_idleInstance, transform);
                _idleInstance.start();
                _isIdleLooping = true;
            }
        }

        private void OnDisable()
        {
            StopLoopingSounds();
        }

        private void OnDestroy()
        {
            StopLoopingSounds();
        }

        public void StopLoopingSounds()
        {
            if (_idleInstance.isValid())
            {
                _idleInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _idleInstance.release();
                _isIdleLooping = false;
            }
        }

        public void PlayIdleGrowl()
        {
            // Odtwarzanie jednorazowego dźwięku (tylko jeśli nie działa stała pętla idle)
            if (HasData && !sfxData.idleGrowl.IsNull && !_isIdleLooping)
            {
                RuntimeManager.PlayOneShotAttached(sfxData.idleGrowl, gameObject);
            }
        }

        public void PlayAttackVoice()
        {
            Debug.Log($"<color=orange>[EnemySFX]</color> {gameObject.name}: Próba odtworzenia AttackVoice");
            if (HasData && !sfxData.attackVoice.IsNull)
                RuntimeManager.PlayOneShotAttached(sfxData.attackVoice, gameObject);
        }

        public void PlayHitVoice()
        {
            Debug.Log($"<color=orange>[EnemySFX]</color> {gameObject.name}: Próba odtworzenia HitVoice");
            if (HasData && !sfxData.hitVoice.IsNull)
                RuntimeManager.PlayOneShotAttached(sfxData.hitVoice, gameObject);
        }

        public void PlayDeathVoice()
        {
            Debug.Log($"<color=orange>[EnemySFX]</color> {gameObject.name}: Próba odtworzenia DeathVoice");
            if (HasData && !sfxData.deathVoice.IsNull)
                RuntimeManager.PlayOneShotAttached(sfxData.deathVoice, gameObject);
        }

        // Funkcja dla standardowych kroków (bez wykrywania podłoża)
        public void PlayFootstep()
        {
            Debug.Log($"<color=orange>[EnemySFX]</color> {gameObject.name}: Próba odtworzenia Footstep (Standard)");
            if (HasData && !sfxData.footstepSound.IsNull)
                RuntimeManager.PlayOneShot(sfxData.footstepSound, transform.position);
        }

        // Funkcja dla zaawansowanych kroków (z parametrem powierzchni)
        public void PlayFootstepWithSurface(Vector3 position, string surfaceLabel)
        {
            Debug.Log($"<color=orange>[EnemySFX]</color> {gameObject.name}: Próba odtworzenia Footstep (Surface: {surfaceLabel})");
            if (!HasData || sfxData.footstepSound.IsNull) return;

            FMOD.Studio.EventInstance instance = RuntimeManager.CreateInstance(sfxData.footstepSound);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            instance.setParameterByNameWithLabel("footsteps", surfaceLabel); 
            instance.start();
            instance.release();
        }

        public void PlayPoiseBreak()
        {
            Debug.Log($"<color=orange>[EnemySFX]</color> {gameObject.name}: Próba odtworzenia PoiseBreak (Ding!)");
            if (HasData && !sfxData.poiseBreakSound.IsNull)
                RuntimeManager.PlayOneShot(sfxData.poiseBreakSound, transform.position);
        }

        public void PlayActionSound(FMODUnity.EventReference sound)
        {
            if (SFX != null && !sound.IsNull) // Poprawka: tutaj powinien być RuntimeManager
            {
                RuntimeManager.PlayOneShotAttached(sound, gameObject);
            }
        }
        
        // Pomocnicza właściwość, bo używaliśmy jej w AIBrain
        public EnemySFXManager SFX => this;
    }
}
