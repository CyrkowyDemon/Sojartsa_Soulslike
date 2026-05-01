using UnityEngine;

/// <summary>
/// System obrotu "KROPKA" (Wytyczne użytkownika):
/// 0° - 30°  : Tylko GŁOWA śledzi wroga
/// 30° - 60° : GŁOWA na 30° + TUŁÓW dokłada resztę
/// > 60°     : SNAP - ciało obraca się, góra się zeruje
/// NOWOŚĆ: CameraPoint obraca się razem z głową (1:1).
/// </summary>
public class PlayerLookController : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField] private TargetHandler targetHandler;
    [SerializeField] private Transform cameraPoint; // Punkt śledzony przez kamerę

    [Header("Kości")]
    [SerializeField] private Transform headBone;
    [SerializeField] private Transform spineBone;

    [Header("Progi (Wytyczne)")]
    [SerializeField] private float headLimit = 25f;
    [SerializeField] private float spineLimit = 25f; // Łącznie 50
    [SerializeField] private float snapThreshold = 50f;
    [SerializeField] private float maxPitchAngle = 25f;

    [Header("Płynność")]
    [SerializeField] private float lookSpeed = 10f;

    private float _headYaw;
    private float _spineYaw;
    private float _pitch;

    public bool IsSnapping { get; private set; }

    /// <summary>
    /// Metoda sprawdzająca kąt natychmiastowo dla PlayerMovement (brak opóźnienia 1 klatki).
    /// </summary>
    public bool CheckSnapThreshold()
    {
        bool hasTarget = targetHandler != null && targetHandler.IsLockedOn && targetHandler.CurrentTarget != null;
        if (!hasTarget) return false;

        Vector3 toTarget = targetHandler.CurrentTarget.position - transform.position;
        Vector3 toTargetFlat = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float horizAngle = Vector3.SignedAngle(transform.forward, toTargetFlat.normalized, Vector3.up);
        return Mathf.Abs(horizAngle) >= snapThreshold;
    }

    private void Update()
    {
        bool hasTarget = targetHandler != null && targetHandler.IsLockedOn && targetHandler.CurrentTarget != null;

        if (!hasTarget)
        {
            UpdateNeutralLerp();
            return;
        }

        // --- Obliczamy kąty do celu (W UPDATE!) ---
        Vector3 toTarget = targetHandler.CurrentTarget.position - transform.position;
        Vector3 toTargetFlat = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float horizAngle = Vector3.SignedAngle(transform.forward, toTargetFlat.normalized, Vector3.up);
        float absAngle = Mathf.Abs(horizAngle);

        // --- TYMCZASOWY LOG DEBUG DO ANALIZY ---
        if (Time.frameCount % 20 == 0)
            Debug.Log($"<color=pink>[ANGLE DEBUG] Kąt wroga od przodu gracza: {absAngle:F2} | Próg Snapa: {snapThreshold}</color>");

        // --- Zarządzanie Stanem SNAP (Histereza) ---
        if (absAngle >= snapThreshold)
        {
            IsSnapping = true;
        }
        else if (IsSnapping && absAngle < 5f)
        {
            IsSnapping = false;
        }

        float targetHead;
        float targetSpine;

        if (absAngle <= headLimit)
        {
            targetHead = horizAngle;
            targetSpine = 0f;
        }
        else
        {
            targetHead = headLimit * Mathf.Sign(horizAngle);
            float leftover = horizAngle - targetHead;
            targetSpine = Mathf.Clamp(leftover, -spineLimit, spineLimit);
        }

        _headYaw = Mathf.Lerp(_headYaw, targetHead, lookSpeed * Time.deltaTime);
        _spineYaw = Mathf.Lerp(_spineYaw, targetSpine, lookSpeed * Time.deltaTime);

        _pitch = Mathf.Lerp(_pitch, Mathf.Clamp(Mathf.Atan2(toTarget.y, toTargetFlat.magnitude) * Mathf.Rad2Deg, -maxPitchAngle, maxPitchAngle), lookSpeed * Time.deltaTime);
    }

    private void LateUpdate()
    {
        ApplyBoneRotations();
    }

    private void UpdateNeutralLerp()
    {
        _headYaw = Mathf.Lerp(_headYaw, 0f, lookSpeed * Time.deltaTime);
        _spineYaw = Mathf.Lerp(_spineYaw, 0f, lookSpeed * Time.deltaTime);
        _pitch = Mathf.Lerp(_pitch, 0f, lookSpeed * Time.deltaTime);
        IsSnapping = false;
    }

    private void ApplyBoneRotations()
    {
        if (headBone != null)
        {
            headBone.rotation = Quaternion.AngleAxis(_headYaw, Vector3.up) * headBone.rotation;
            headBone.rotation = Quaternion.AngleAxis(-_pitch * 0.6f, headBone.right) * headBone.rotation;
        }
        if (spineBone != null)
        {
            spineBone.rotation = Quaternion.AngleAxis(_spineYaw, Vector3.up) * spineBone.rotation;
            spineBone.rotation = Quaternion.AngleAxis(-_pitch * 0.4f, spineBone.right) * spineBone.rotation;
        }

        if (cameraPoint != null)
        {
            bool hasTarget = targetHandler != null && targetHandler.IsLockedOn && targetHandler.CurrentTarget != null;
            if (hasTarget)
            {
                Vector3 toTarget = targetHandler.CurrentTarget.position - transform.position;
                toTarget.y = 0;
                
                if (toTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetWorldRot = Quaternion.LookRotation(toTarget.normalized);
                    cameraPoint.rotation = Quaternion.Slerp(cameraPoint.rotation, targetWorldRot, lookSpeed * 0.5f * Time.deltaTime);

                    float maxAllowedRotation = headLimit + spineLimit;
                    float currentLocalY = cameraPoint.localEulerAngles.y;
                    if (currentLocalY > 180f) currentLocalY -= 360f;
                    
                    float clampedY = Mathf.Clamp(currentLocalY, -maxAllowedRotation, maxAllowedRotation);
                    cameraPoint.localRotation = Quaternion.Euler(0, clampedY, 0);
                }
            }
            else
            {
                // Sekiro Style: Powrót do pozycji tylko gdy gracz się porusza
                bool isMoving = false;
                
                // Próbujemy pobrać prędkość (Rigidbody lub CharacterController)
                if (TryGetComponent<CharacterController>(out var cc)) isMoving = cc.velocity.sqrMagnitude > 0.1f;
                else if (TryGetComponent<Rigidbody>(out var rb)) isMoving = rb.linearVelocity.sqrMagnitude > 0.1f;

                if (isMoving)
                {
                    // Powrót do bycia na wprost pleców - drastycznie zwolniony powrót (0.2f)
                    cameraPoint.localRotation = Quaternion.Slerp(cameraPoint.localRotation, Quaternion.identity, lookSpeed * 0.2f * Time.deltaTime);
                }
            }
        }
    }
}
