using UnityEngine;

namespace SojartsaAI.v3
{
    /// <summary>
    /// Moduł percepcji (Oczy i Uszy AI). 
    /// Odpowiada za czytanie stanu gracza i otoczenia.
    /// </summary>
    public class AISensory
    {
        public enum PlayerSide { Front, Behind, Left, Right }

        private Transform _self;
        private Transform _player;
        private AIArchetype _config;

        // Cache'owane wartości (aktualizowane co Tick)
        public float Distance { get; private set; }
        public float SqrDistance { get; private set; }
        public float AngleToPlayer { get; private set; }
        public PlayerSide Side { get; private set; }
        public bool IsPlayerVisible { get; private set; }

        // --- AAA INTELLIGENCE (Roll Catching) ---
        public float TimeSinceLastPlayerDodge { get; private set; } = 99f;
        public bool IsPlayerDodging { get; private set; }
        
        // --- INPUT READING (The "Cheating" AI) ---
        public bool IsPlayerPressingAttack { get; private set; }
        public bool IsPlayerPressingDodge { get; private set; }

        private Animator _playerAnim;
        private InputReader _playerInput;

        public AISensory(Transform self, Transform player, AIArchetype config, InputReader playerInput = null)
        {
            _self = self;
            _player = player;
            _config = config;
            _playerInput = playerInput;
            
            if (_player != null) 
            {
                _playerAnim = _player.GetComponentInChildren<Animator>();
            }

            if (_playerInput != null)
            {
                _playerInput.AttackEvent += HandleAttackInput;
                _playerInput.DodgeEvent += HandleDodgeInput;
            }
        }

        private void HandleAttackInput() => IsPlayerPressingAttack = true;
        private void HandleDodgeInput() => IsPlayerPressingDodge = true;

        public void Cleanup()
        {
            if (_playerInput != null)
            {
                _playerInput.AttackEvent -= HandleAttackInput;
                _playerInput.DodgeEvent -= HandleDodgeInput;
            }
        }

        public void Tick()
        {
            if (_player == null) return;

            // Resetujemy flagi inputu na początku klatki (bo zdarzenia są asynchroniczne)
            // W Soulsach reakcja na input jest błyskawiczna
            
            Vector3 toPlayer = _player.position - _self.position;
            SqrDistance = toPlayer.sqrMagnitude;
            Distance = Mathf.Sqrt(SqrDistance);

            // Obliczamy kąt i stronę
            Vector3 forward = _self.forward;
            AngleToPlayer = Vector3.SignedAngle(forward, toPlayer.normalized, Vector3.up);

            UpdateSide(toPlayer.normalized);
            IsPlayerVisible = CheckLineOfSight();

            // Śledzenie uników
            IsPlayerDodging = CheckPlayerTag("Dodge");
            if (IsPlayerDodging) TimeSinceLastPlayerDodge = 0;
            else TimeSinceLastPlayerDodge += Time.deltaTime;

            // Czyszczenie flag inputu na koniec klatki
            IsPlayerPressingAttack = false;
            IsPlayerPressingDodge = false;
        }

        public bool IsPathBlocked(Vector3 direction, float distance)
        {
            // AAA: Spatial Awareness - Sprawdzamy czy w danym kierunku jest ściana/przepaść
            // Używamy SphereCast, żeby symulować szerokość ciała AI
            RaycastHit hit;
            Vector3 origin = _self.position + Vector3.up * 1f;
            
            // Maskujemy warstwę Environment
            int mask = LayerMask.GetMask("Environment", "Default");
            if (mask == 0) mask = ~0; // Fallback

            return Physics.SphereCast(origin, 0.5f, direction, out hit, distance, mask);
        }

        private void UpdateSide(Vector3 dirToPlayer)
        {
            float dotForward = Vector3.Dot(_self.forward, dirToPlayer);
            float dotRight = Vector3.Dot(_self.right, dirToPlayer);

            if (dotForward > 0.5f) Side = PlayerSide.Front;
            else if (dotForward < -0.5f) Side = PlayerSide.Behind;
            else Side = dotRight > 0 ? PlayerSide.Right : PlayerSide.Left;
        }

        private bool CheckLineOfSight()
        {
            if (_config == null || SqrDistance > _config.lookRange * _config.lookRange) return false;

            // Multi-point LoS (Head/Chest)
            Vector3 eyePos = _self.position + Vector3.up * 1.6f;
            Vector3 headPos = _player.position + Vector3.up * 1.6f;
            Vector3 chestPos = _player.position + Vector3.up * 1.0f;

            // Jeśli maska jest pusta (Nothing), wymuszamy sensowny fallback (Default + Environment)
            LayerMask mask = _config.obstacleMask;
            if (mask.value == 0) mask = LayerMask.GetMask("Default", "Environment", "Obstacles");

            // Sprawdzamy czy cokolwiek blokuje linię wzroku (poza samym sobą)
            RaycastHit hit;
            bool headClear = !Physics.Linecast(eyePos, headPos, out hit, mask) || hit.transform.root == _player.root;
            bool chestClear = !Physics.Linecast(eyePos, chestPos, out hit, mask) || hit.transform.root == _player.root;

            return headClear || chestClear;
        }

        // Czytanie "Intencji" gracza (Input Reading)
        public bool IsPlayerHealing() => CheckPlayerTag("Healing");
        public bool IsPlayerAttacking() => CheckPlayerTag("Attack");

        private bool CheckPlayerTag(string tag)
        {
            if (_playerAnim == null) return false;
            return _playerAnim.GetCurrentAnimatorStateInfo(0).IsTag(tag) || 
                   (_playerAnim.IsInTransition(0) && _playerAnim.GetNextAnimatorStateInfo(0).IsTag(tag));
        }
    }
}
