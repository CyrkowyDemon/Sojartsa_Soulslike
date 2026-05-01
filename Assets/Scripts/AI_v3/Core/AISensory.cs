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
        public Transform Player => _player;

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
            // AAA: Autodetekcja gracza jeśli nie został przypisany w Inspektorze
            if (_player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) 
                {
                    _player = p.transform;
                    _playerAnim = _player.GetComponentInChildren<Animator>();
                }
                else return; // Wciąż nie ma gracza w scenie
            }

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
            RaycastHit hit;
            Vector3 origin = _self.position + Vector3.up * 1f;
            
            int mask = LayerMask.GetMask("Environment", "Default");
            if (mask == 0) mask = ~0; 

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
            if (_player == null || _config == null) return false;
            if (SqrDistance > _config.lookRange * _config.lookRange) return false;

            // AAA: Dynamiczne dopasowanie wysokości wzroku (pół metra nad pivotem dla małych jednostek jak pszczoły)
            float selfHeight = 1.0f; 
            float targetHeight = 1.0f;

            Vector3 eyePos = _self.position + Vector3.up * selfHeight;
            Vector3 headPos = _player.position + Vector3.up * targetHeight;
            Vector3 chestPos = _player.position + Vector3.up * (targetHeight * 0.5f);

            LayerMask mask = _config.obstacleMask;
            if (mask.value == 0) mask = LayerMask.GetMask("Default", "Environment");

            RaycastHit hit;
            // Sprawdzamy Head i Chest
            bool headClear = !Physics.Linecast(eyePos, headPos, out hit, mask) || hit.transform.root == _player.root;
            bool chestClear = !Physics.Linecast(eyePos, chestPos, out hit, mask) || hit.transform.root == _player.root;

            // AAA: Dodatkowe sprawdzenie kąta FOV (tylko w pasywnym wykrywaniu)
            float angle = Vector3.Angle(_self.forward, (_player.position - _self.position).normalized);
            if (angle > _config.fieldOfView * 0.5f) return false;

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
