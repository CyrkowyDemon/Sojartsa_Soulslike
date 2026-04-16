using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Slider hpSlider;

    [Header("Waluta")]
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private float tickSpeed = 0.05f; // Szybkość przeskoku (opcjonalne)

    private int _displayedCurrency = 0;
    private Coroutine _tickCoroutine;

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
        if (hpSlider != null)
            hpSlider.value = (float)currentHP / maxHP;
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
