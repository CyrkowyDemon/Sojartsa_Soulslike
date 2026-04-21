using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider hpGhostSlider;
    [SerializeField] private float hpGhostLerpSpeed = 5f;
    [SerializeField] private float hpGhostDelay = 1f; // Jak długo pasek stoi w miejscu po ciosie

    [Header("Rany (Poise)")]
    [SerializeField] private Slider woundSlider;       // Pasek główny (natychmiastowy) - ciemny kolor
    [SerializeField] private Slider woundGhostSlider;  // Pasek ghost (płynny) - jaśniejszy, pod spodem
    [SerializeField] private float ghostLerpSpeed = 4f; // Szybkość opadania ghosta (niżej = wolniej = bardziej efektownie)

    [Header("Waluta")]
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private float tickSpeed = 0.05f; // Szybkość przeskoku (opcjonalne)

    private int _displayedCurrency = 0;
    private Coroutine _tickCoroutine;
    private float _ghostWoundTarget = 0f;
    private float _ghostHpTarget = 0f;
    private float _hpDamageTimer = 0f;    // Timer dla HP
    private float _woundIncreaseTimer = 0f; // Timer dla ran

    private void OnEnable()
    {
        // Subskrybujemy się na zmiany portfela - HUD sam się odświeży
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += UpdateCurrency;
    }

    private void OnDisable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= UpdateCurrency;
    }

    private void Update()
    {
        // Ghost bar ran - lerpujemy tylko jeśli upłynął czas opóźnienia (woundDecayDelay z PlayerHealth tu nie mieszamy, to czysto wizualne)
        if (woundGhostSlider != null && Time.time > _woundIncreaseTimer + 0.5f) // dla ran damy 0.5s opóźnienia
        {
            woundGhostSlider.value = Mathf.Lerp(woundGhostSlider.value, _ghostWoundTarget, ghostLerpSpeed * Time.deltaTime);
        }

        // Ghost bar życia - lerpujemy tylko po upływie delay
        if (hpGhostSlider != null && Time.time > _hpDamageTimer + hpGhostDelay)
        {
            hpGhostSlider.value = Mathf.Lerp(hpGhostSlider.value, _ghostHpTarget, hpGhostLerpSpeed * Time.deltaTime);
        }
    }

    private void Start()
    {
        // Odświeżamy wyświetlaną kwotę od razu przy starcie sceny
        if (CurrencyManager.Instance != null)
        {
            _displayedCurrency = CurrencyManager.Instance.CurrentCurrency;
            if (currencyText != null) currencyText.text = _displayedCurrency.ToString("N0");
        }
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        float normalizedValue = (float)currentHP / maxHP;

        if (hpSlider != null)
            hpSlider.value = normalizedValue;

        if (hpGhostSlider != null)
        {
            // Jeśli zdrowie SPADŁO, resetujemy timer opóźnienia
            if (normalizedValue < _ghostHpTarget)
            {
                _hpDamageTimer = Time.time;
            }
            // Jeśli leczymy (życie rośnie), ghost skacze od razu
            else if (normalizedValue > _ghostHpTarget)
            {
                hpGhostSlider.value = normalizedValue;
            }

            _ghostHpTarget = normalizedValue;
        }
    }

    public void UpdateWounds(float currentWounds, float maxWounds)
    {
        float normalizedValue = maxWounds > 0f ? currentWounds / maxWounds : 0f;

        if (woundSlider != null)
            woundSlider.value = normalizedValue;

        if (woundGhostSlider != null)
        {
            // Jeśli rany wzrosły, resetujemy timer opóźnienia
            if (normalizedValue > _ghostWoundTarget)
            {
                _woundIncreaseTimer = Time.time;
                woundGhostSlider.value = normalizedValue; // Przy ranach ghost rośnie natychmiast, ale spada powoli
            }
            
            _ghostWoundTarget = normalizedValue;
        }
    }

    public void UpdateCurrency(int targetAmount)
    {
        if (_tickCoroutine != null) StopCoroutine(_tickCoroutine);
        _tickCoroutine = StartCoroutine(TickCurrencyRoutine(targetAmount));
    }

    private System.Collections.IEnumerator TickCurrencyRoutine(int target)
    {
        // Jeśli różnica jest duża, idziemy większymi krokami, jeśli mała - po 1.
        while (_displayedCurrency != target)
        {
            int diff = target - _displayedCurrency;
            int step = Mathf.Max(1, Mathf.Abs(diff) / 10);
            
            if (diff > 0) _displayedCurrency += step;
            else _displayedCurrency -= step;

            // Zabezpieczenie przed "przeskoczeniem" celu
            if (Mathf.Abs(target - _displayedCurrency) < step) _displayedCurrency = target;

            if (currencyText != null) currencyText.text = _displayedCurrency.ToString("N0");

            // Im mniejsza różnica, tym krótsze czekanie (efekt zwalniania przy końcu)
            yield return new WaitForSeconds(0.01f);
        }
        _tickCoroutine = null;
    }
}
