using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI Elementy")]
    [SerializeField] private Slider mouseSlider;
    [SerializeField] private Slider gamepadSlider;

    private bool _isUpdating = false;
    private bool _isInitialized = false;

    private void Start()
    {
        // WYMUSZAMY twarde załadowanie rzetelnych wartości z dysku (PlayerPrefs) przy każdym uruchomieniu sceny,
        // żeby pominąć 100% potencjalnych błędów pamięci/Singleto'ów
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.LoadSettings();
        }

        UpdateSliders();
        _isInitialized = true; // Flaga: od tego momentu pozwalamy użytkownikowi na zmiany
    }

    private void OnEnable()
    {
        UpdateSliders();
    }

    public void UpdateSliders()
    {
        if (SettingsManager.Instance == null) return;

        _isUpdating = true; // BLOKADA: Blokujemy wywoływanie eventów przy aktualizacji

        if (mouseSlider != null)
        {
            mouseSlider.minValue = 0.1f;
            mouseSlider.maxValue = 10f;
            mouseSlider.value = SettingsManager.Instance.mouseSensitivity;
        }
        else
        {
            Debug.LogError("[UI] BRAK mouseSlider w Inspektorze!");
        }

        if (gamepadSlider != null)
        {
            gamepadSlider.minValue = 0.1f;
            gamepadSlider.maxValue = 10f;
            gamepadSlider.value = SettingsManager.Instance.gamepadSensitivity;
        }
        else
        {
            Debug.LogError("[UI] BRAK gamepadSlider w Inspektorze!");
        }
            
        // DIAGNOSTYKA
        Debug.Log($"[UI] Wymuszono synchronizację suwaków. Odczyt z bazy to Mysz: {SettingsManager.Instance.mouseSensitivity}, Pad: {SettingsManager.Instance.gamepadSensitivity}");
            
        _isUpdating = false; // ODBLOKOWANIE
    }

    // Funkcja podpięta pod suwak myszki (OnValueChanged)
    public void OnMouseSensitivityChanged(float newValue)
    {
        if (!_isInitialized || _isUpdating) return; // Chronimy przed eventami ze startu silnika Unity!

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings(newValue, SettingsManager.Instance.gamepadSensitivity);
        }
    }

    // Funkcja podpięta pod suwak pada (OnValueChanged)
    public void OnGamepadSensitivityChanged(float newValue)
    {
        if (!_isInitialized || _isUpdating) return; // Chronimy przed eventami ze startu silnika Unity!

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings(SettingsManager.Instance.mouseSensitivity, newValue);
        }
    }
}
