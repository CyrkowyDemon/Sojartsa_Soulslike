using UnityEngine;

[DefaultExecutionOrder(20000)]
public class VerticalAimTest : MonoBehaviour
{
    public Transform spineBone;
    [Range(0, 1)] public float weight = 1f;
    public float maxAngle = 45f;

    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        if (spineBone == null) Debug.LogError("PRZYPISZ SPINE W INSPEKTORZE!");
    }

    void LateUpdate()
    {
        if (spineBone == null || _cam == null) return;

        // Obliczamy o ile kamera patrzy w górę/dół
        float pitch = _cam.transform.eulerAngles.x;
        if (pitch > 180) pitch -= 360;

        // Chcemy, żeby kręgosłup wygiął się o ten kąt (lub jego część)
        float targetAngle = Mathf.Clamp(-pitch, -maxAngle, maxAngle) * weight;

        // Wymuszamy rotację wokół osi "prawo" postaci w przestrzeni świata
        // To jest trudniejsze do nadpisania dla Animatora niż localRotation
        spineBone.RotateAround(spineBone.position, transform.right, targetAngle);
    }
}
