using UnityEngine;

namespace SojartsaAI.v3
{
    /// <summary>
    /// KLUCZOWY ELEMENT AAA: StateMachineBehaviour.
    /// Ten skrypt umieszczasz na stanach (np. Atak, Unik) w Animatorze.
    /// Łączy klatki animacji bezpośrednio z logiką AI.
    /// </summary>
    public class AIActionSMB : StateMachineBehaviour
    {
        [Header("Sygnały")]
        public string onEnterSignal = "ActionStart";
        public string onExitSignal = "ActionEnd";

        [Header("AAA - Delayed Attack (The Margit Move)")]
        public bool isDelayedAttack = false;
        [Range(0, 1)] public float holdNormalizedTime = 0.4f;
        public float maxHoldDuration = 1.0f;

        [Header("Okna Obrażeń (Hitbox Window)")]
        public bool useHitboxWindow = true;
        public int hitboxID = 0;
        public float hitboxStartNormalized = 0.2f;
        public float hitboxEndNormalized = 0.6f;

        [Header("Okna Przerwania (Cancel Windows)")]
        public bool useCancelWindow;
        public float cancelStartNormalized = 0.7f;

        private AIBrain _brain;
        private bool _cancelSignalSent;
        private bool _hitboxOpened;
        private float _holdTimer;
        private bool _hasHeld;

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_brain == null) _brain = animator.GetComponentInParent<AIBrain>();
            
            _holdTimer = 0;
            _hasHeld = false;
            
            if (_brain != null)
            {
                _brain.SendAnimationSignal(onEnterSignal);

                // --- AUDIO: Odtwarzamy dźwięk telegrafowania (The Tell) ---
                if (_brain.ActiveAction != null)
                    _brain.PlayActionSound(_brain.ActiveAction.actionTellSound);
            }
                
            _cancelSignalSent = false;
        }

        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // --- AAA: Delayed Attack (Margit Move) ---
            if (isDelayedAttack && !_hasHeld && stateInfo.normalizedTime >= holdNormalizedTime)
            {
                animator.speed = 0;
                _holdTimer += Time.deltaTime;

                bool playerFinishedDodge = _brain != null && _brain.Sensory.TimeSinceLastPlayerDodge > 0.1f && !_brain.Sensory.IsPlayerDodging;

                if (playerFinishedDodge || _holdTimer >= maxHoldDuration)
                {
                    animator.speed = 1;
                    _hasHeld = true;
                }
            }

            // --- AAA: Hitbox Management ---
            if (useHitboxWindow && _brain != null)
            {
                float time = stateInfo.normalizedTime;
                if (!_hitboxOpened && time >= hitboxStartNormalized && time <= hitboxEndNormalized)
                {
                    _brain.OpenHitbox(hitboxID);
                    _hitboxOpened = true;
                }
                else if (_hitboxOpened && (time > hitboxEndNormalized || time < hitboxStartNormalized))
                {
                    _brain.CloseHitbox(hitboxID);
                    _hitboxOpened = false;
                }
            }

            if (useCancelWindow && !_cancelSignalSent && stateInfo.normalizedTime >= cancelStartNormalized)
            {
                if (_brain != null)
                {
                    _brain.SendAnimationSignal("CanCancel");
                    _cancelSignalSent = true;
                }
            }
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_brain != null)
            {
                _brain.CloseAllHitboxes(); // Fail-safe
                _brain.SendAnimationSignal(onExitSignal);
                _brain.ActiveAction = null; // Czyszczenie akcji po zakończeniu
            }
        }
    }
}
