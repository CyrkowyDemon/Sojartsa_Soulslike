using UnityEngine;

[RequireComponent(typeof(Light))]
public class HitLightSelfDestruct : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 0.1f;    // Jak długo światło żyje
    [SerializeField] private float fadeSpeed = 15f;    // Jak szybko gaśnie

    private Light _light;
    private float _initialIntensity;
    private float _timer;

    void Awake()
    {
        _light = GetComponent<Light>();
        _initialIntensity = _light.intensity;
        _timer = duration;
    }

    void Update()
    {
        _timer -= Time.deltaTime;

        // Płynne wygaszanie intensywności
        _light.intensity = Mathf.Lerp(_light.intensity, 0, Time.deltaTime * fadeSpeed);

        // Usuwamy obiekt, gdy czas minie lub światło zgaśnie prawie do zera
        if (_timer <= 0 || _light.intensity < 0.01f)
        {
            Destroy(gameObject);
        }
    }
}
