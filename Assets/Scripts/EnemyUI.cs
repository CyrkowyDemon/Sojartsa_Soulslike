using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class EnemyUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider woundSlider;
    
    [Header("Settings")]
    [SerializeField] private float showDuration = 5f;
    [SerializeField] private float updateDistance = 25f; // Dystans, od którego pasek "żyje"

    private CanvasGroup _canvasGroup;
    private float _hideTimer;
    private Transform _mainCamera;
    private Transform _playerTransform; // Cache gracza
    private bool _isPersistent = false; // Czy pasek ma zostac na ekranie na stale (np. przy Broken)

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _mainCamera = Camera.main.transform;

        // Szukamy gracza raz, by nie robić tego co klatkę
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTransform = player.transform;
        
        if (_canvasGroup != null) _canvasGroup.alpha = 0;
    }

    void LateUpdate()
    {
        // OPTYMALIZACJA: Jeśli gracz jest za daleko, nie marnujemy mocy na obracanie pasków
        float sqrDist = (transform.position - _playerTransform.position).sqrMagnitude;
        if (_playerTransform == null || sqrDist > updateDistance * updateDistance)
        {
            return;
        }

        if (_mainCamera != null) transform.LookAt(transform.position + _mainCamera.forward);

        // Jesli nie jestesmy w trybie stalym, odliczamy czas do znikniecia
        if (!_isPersistent && _hideTimer > 0)
        {
            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0 && _canvasGroup != null) _canvasGroup.alpha = 0;
        }
    }

    public void UpdateStatus(int currentHP, int maxHP, int currentWounds, int maxWounds, bool isBroken)
    {
        if (_canvasGroup == null) return;

        _isPersistent = isBroken;
        _canvasGroup.alpha = 1;
        
        if (!_isPersistent) _hideTimer = showDuration;

        if (hpSlider != null) hpSlider.value = (float)currentHP / maxHP;
        if (woundSlider != null) woundSlider.value = (float)currentWounds / maxWounds;
    }
}
