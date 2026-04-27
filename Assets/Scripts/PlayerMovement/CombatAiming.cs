using UnityEngine;

[DefaultExecutionOrder(20000)]
public class CombatAiming : MonoBehaviour
{
    [Header("Kości (od dołu do góry)")]
    public Transform spine0; 
    public Transform spine1;
    public Transform spine2;
    
    [Header("Ustawienia")]
    public float maxAngle = 90f; // TESTOWE 90 STOPNI!
    public float smoothSpeed = 15f;
    public int animatorLayer = 1; // Indeks 1 = Warstwa 2 w Animatorze

    private Animator _animator;
    private Camera _cam;
    private float _currentPitch;

    void Start()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null) _animator = GetComponentInParent<Animator>();
        
        _cam = Camera.main;
        
        Debug.Log("<color=cyan>[Aiming] SKRYPT ODPALONY. Czekam na animacje z tagiem 'Attack' na warstwie " + (animatorLayer + 1) + "</color>");
    }

    void LateUpdate()
    {
        if (spine0 == null || _cam == null || _animator == null) return;

        float targetPitch = 0f;

        // Sprawdzamy stan Animatora na konkretnej warstwie
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(animatorLayer);
        
        // Jeśli animacja ma tag "Attack", odpalamy celowanie
        if (stateInfo.IsTag("Attack"))
        {
            float pitch = _cam.transform.eulerAngles.x;
            if (pitch > 180) pitch -= 360;
            targetPitch = Mathf.Clamp(pitch, -maxAngle, maxAngle);
            
            // Log dla testu (możesz wyłączyć jak zacznie działać)
            // Debug.Log("[Aiming] WYKRYTO TAG ATTACK! Wyginam o: " + targetPitch);
        }

        _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.deltaTime * smoothSpeed);

        if (Mathf.Abs(_currentPitch) > 0.1f)
        {
            float segmentAngle = _currentPitch / 3f;
            RotateBone(spine0, segmentAngle);
            RotateBone(spine1, segmentAngle);
            RotateBone(spine2, segmentAngle);
        }
    }

    private void RotateBone(Transform bone, float angle)
    {
        if (bone == null) return;
        bone.RotateAround(bone.position, transform.right, angle);
    }
}
