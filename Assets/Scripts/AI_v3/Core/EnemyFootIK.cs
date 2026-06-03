using UnityEngine;

namespace SojartsaAI.v3
{
    /// <summary>
    /// Foot IK dla przeciwników (FromSoftware Style).
    /// NIE wymaga paczki Animation Rigging ani RigBuilder.
    /// Używa wbudowanego w Unity systemu OnAnimatorIK.
    /// 
    /// SETUP:
    /// 1. Dodaj ten skrypt do obiektu z Animatorem wroga.
    /// 2. W Animator Controller, w Base Layer zaznacz checkbox "IK Pass".
    /// 3. Ustaw groundLayer (np. "Default" lub "Environment").
    /// 4. Skrypt resztę zrobi sam.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EnemyFootIK : MonoBehaviour
    {
        [Header("IK Settings")]
        [SerializeField] private bool useFootIK = true;
        [SerializeField] private LayerMask groundLayer;
        [Tooltip("Odległość od podłoża na jakiej chcemy utrzymać stopę (kostka)")]
        [SerializeField] private float footOffset = 0.06f;
        [Tooltip("Jak płynnie miednica opada żeby wyrównać obie stopy (większa = wolniej)")]
        [SerializeField] private float pelvisSpeed = 5f;
        [Tooltip("Jak szybko IK dopasowuje wagi (1 = natychmiast, 20 = wolno)")]
        [SerializeField] private float ikWeightSpeed = 8f;
        [Tooltip("Promień Raycastu startującego nad stopą (wyżej = więcej miejsca na wykrycie)")]
        [SerializeField] private float rayOriginHeight = 1.0f;
        [Tooltip("Długość promienia wykrywającego ziemię")]
        [SerializeField] private float rayDistance = 2.0f;
        [Tooltip("Jak wysoko podniesiona stopa w animacji całkowicie wyłącza IK (np. 0.15 = 15cm)")]
        [SerializeField] private float footLiftThreshold = 0.15f;
        [Tooltip("Wysokość kostki nad ziemią")]
        [SerializeField] private float footAnkleHeight = 0.08f;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        // Wewnętrzny stan IK
        private Animator _animator;
        private float _leftFootWeight;
        private float _rightFootWeight;
        private Vector3 _leftFootPosition;
        private Vector3 _rightFootPosition;
        private Quaternion _leftFootRotation;
        private Quaternion _rightFootRotation;
        private float _pelvisOffset;
        private float _lastPelvisOffset;

        private RaycastHit _leftHit;
        private RaycastHit _rightHit;
        private bool _leftHasHit;
        private bool _rightHasHit;

        private const int ACTIONS_LAYER = 2;
        private int _nothingStateHash;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            if (_animator != null)
            {
                _nothingStateHash = Animator.StringToHash("Nothing");
            }
        }

        // OnAnimatorIK jest wywoływany przez Unity automatycznie gdy "IK Pass" jest zaznaczony w Animator Controller
        private void OnAnimatorIK(int layerIndex)
        {
            if (!useFootIK || _animator == null) return;

            // === FROMSOFTWARE FIX: Wygaszamy IK podczas ataku/akcji ===
            bool isActionsPlaying = false;
            if (_animator.layerCount > ACTIONS_LAYER)
            {
                AnimatorStateInfo actionState = _animator.GetCurrentAnimatorStateInfo(ACTIONS_LAYER);
                isActionsPlaying = actionState.shortNameHash != _nothingStateHash || _animator.IsInTransition(ACTIONS_LAYER);
            }

            float targetGlobalWeight = isActionsPlaying ? 0f : 1f;

            // === RAYCASTY NA STOPY ===
            _leftHasHit = SampleGround(AvatarIKGoal.LeftFoot, out _leftHit);
            _rightHasHit = SampleGround(AvatarIKGoal.RightFoot, out _rightHit);

            // Wyliczamy wagi stóp dynamicznie (zależnie od tego, jak wysoko animacja unosi stopy)
            float leftTargetWeight = 0f;
            float rightTargetWeight = 0f;

            if (_leftHasHit)
            {
                float leftDist = (_animator.GetIKPosition(AvatarIKGoal.LeftFoot).y - _leftHit.point.y) - footAnkleHeight;
                leftTargetWeight = Mathf.Pow(Mathf.Clamp01(1f - (Mathf.Max(0f, leftDist) / footLiftThreshold)), 3) * targetGlobalWeight;
            }

            if (_rightHasHit)
            {
                float rightDist = (_animator.GetIKPosition(AvatarIKGoal.RightFoot).y - _rightHit.point.y) - footAnkleHeight;
                rightTargetWeight = Mathf.Pow(Mathf.Clamp01(1f - (Mathf.Max(0f, rightDist) / footLiftThreshold)), 3) * targetGlobalWeight;
            }

            _leftFootWeight = Mathf.Lerp(_leftFootWeight, leftTargetWeight, Time.deltaTime * ikWeightSpeed);
            _rightFootWeight = Mathf.Lerp(_rightFootWeight, rightTargetWeight, Time.deltaTime * ikWeightSpeed);

            // Ustawiamy wagi do animatora
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, _leftFootWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, _leftFootWeight);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, _rightFootWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, _rightFootWeight);

            if (_leftFootWeight < 0.01f && _rightFootWeight < 0.01f) return;

            // === WYRÓWNANIE MIEDNICY ===
            AdjustPelvis();

            // === USTAWIAMY POZYCJE IK STÓP ===
            if (_leftHasHit)
            {
                Vector3 footPos = _leftHit.point + Vector3.up * footOffset;
                _animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPos);
                _animator.SetIKRotation(AvatarIKGoal.LeftFoot, GetFootRotation(_leftHit.normal));
            }

            if (_rightHasHit)
            {
                Vector3 footPos = _rightHit.point + Vector3.up * footOffset;
                _animator.SetIKPosition(AvatarIKGoal.RightFoot, footPos);
                _animator.SetIKRotation(AvatarIKGoal.RightFoot, GetFootRotation(_rightHit.normal));
            }
        }

        private bool SampleGround(AvatarIKGoal foot, out RaycastHit hit)
        {
            // Pobieramy aktualną pozycję stopy z animatora (nie IK, tylko surowa animacja)
            Vector3 footPos = _animator.GetIKPosition(foot);
            Vector3 origin = footPos + Vector3.up * rayOriginHeight;

            bool hasHit = Physics.Raycast(origin, Vector3.down, out hit, rayOriginHeight + rayDistance, groundLayer);

            if (showDebugGizmos && hasHit)
            {
                Debug.DrawLine(origin, hit.point, foot == AvatarIKGoal.LeftFoot ? Color.blue : Color.green);
                Debug.DrawRay(hit.point, hit.normal * 0.3f, Color.yellow);
            }

            return hasHit;
        }

        private void AdjustPelvis()
        {
            if (!_leftHasHit && !_rightHasHit) return;

            // Liczymy ile miednica musi się opuścić żeby obie stopy dotykały ziemi
            float leftOffset = _leftHasHit ? (_leftHit.point.y - _animator.GetIKPosition(AvatarIKGoal.LeftFoot).y) : 0f;
            float rightOffset = _rightHasHit ? (_rightHit.point.y - _animator.GetIKPosition(AvatarIKGoal.RightFoot).y) : 0f;

            // Bierzemy ten gorszy przypadek (niżej idąca stopa "ciągnie" miednicę)
            float targetPelvisOffset = Mathf.Min(leftOffset, rightOffset);
            targetPelvisOffset = Mathf.Clamp(targetPelvisOffset, -0.5f, 0f); // Max 50cm opuszczenia

            _pelvisOffset = Mathf.Lerp(_lastPelvisOffset, targetPelvisOffset, Time.deltaTime * pelvisSpeed);
            _lastPelvisOffset = _pelvisOffset;

            // Przesuwamy miednicę
            Vector3 pelvisPos = _animator.bodyPosition;
            pelvisPos.y += _pelvisOffset;
            _animator.bodyPosition = pelvisPos;
        }

        private Quaternion GetFootRotation(Vector3 groundNormal)
        {
            // Dopasowujemy rotację stopy do nachylenia terenu
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = transform.forward;
            return Quaternion.LookRotation(forward, groundNormal);
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || !Application.isPlaying || _animator == null) return;

            if (_leftHasHit)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_leftHit.point, 0.05f);
            }
            if (_rightHasHit)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_rightHit.point, 0.05f);
            }
        }
    }
}
