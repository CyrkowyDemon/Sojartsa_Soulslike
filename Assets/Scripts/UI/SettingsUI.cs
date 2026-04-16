using UnityEngine;
using UnityEngine.UI;
using TMPro; // Wymagane dla TextMeshPro!

public class SettingsUI : MonoBehaviour
{
    [Header("UI Elementy - Suwaki")]
    [SerializeField] private Slider mouseSlider;
    [SerializeField] private Slider gamepadSlider;
    [SerializeField] private Slider vibrationSlider;

    [Header("UI Elementy - Przyciski (Sekiro Style)")]
    [SerializeField] private Button invertXButton;
    [SerializeField] private TextMeshProUGUI invertXText; // Tekst na przycisku
    [SerializeField] private Button invertYButton;
    [SerializeField] private TextMeshProUGUI invertYText; // Tekst na przycisku

    private bool _currentInvertX;
    private bool _currentInvertY;

    private bool _isUpdating = false;
    private bool _isInitialized = false;

    private void Start()
    {
        // WYMUSZAMY twarde załadowanie rzetelnych wartości z dysku
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.LoadSettings();
        }

        UpdateSliders();
        _isInitialized = true; // Flaga: od tego momentu pozwalamy użytkownikowi na zmiany
    }

    private void OnEnable()
    {
        if (_isInitialized) UpdateSliders();
    }

    public void UpdateSliders()
    {
        if (SettingsManager.Instance == null) return;

        _isUpdating = true; // BLOKADA eventów

        // --- Aktualizacja Suwaków ---
        if (mouseSlider != null)
        {
            mouseSlider.minValue = 1f;
            mouseSlider.maxValue = 5f;
            mouseSlider.value = SettingsManager.Instance.mouseSensitivity;
        }

        if (gamepadSlider != null)
        {
            gamepadSlider.minValue = 0.1f;
            gamepadSlider.maxValue = 10f;
            gamepadSlider.value = SettingsManager.Instance.gamepadSensitivity;
        }

        if (vibrationSlider != null)
        {
            vibrationSlider.minValue = 0f;
            vibrationSlider.maxValue = 1f;
            vibrationSlider.value = SettingsManager.Instance.vibrationLevel;
        }

        // --- Aktualizacja Przycisków (Sekiro Style) ---
        _currentInvertX = SettingsManager.Instance.invertX;
        _currentInvertY = SettingsManager.Instance.invertY;
        UpdateInvertButtonVisuals(); // Funkcja zmieniająca napis na przycisku
            
        _isUpdating = false; // ODBLOKOWANIE
    }

    // Funkcja odświeżająca same teksty
    private void UpdateInvertButtonVisuals()
    {
        if (invertXText != null) 
            invertXText.text = _currentInvertX ? "Inverted" : "Normal";
            
        if (invertYText != null) 
            invertYText.text = _currentInvertY ? "Inverted" : "Normal";
    }

    // --- EVENTY DLA SUWAKÓW ---
    public void OnMouseSensitivityChanged(float newValue)
    {
        if (!_isInitialized || _isUpdating) return;
        SaveAll();
    }

    public void OnGamepadSensitivityChanged(float newValue)
    {
        if (!_isInitialized || _isUpdating) return;
        SaveAll();
    }

    public void OnVibrationChanged(float newValue)
    {
        if (!_isInitialized || _isUpdating) return;
        SaveAll();
    }

    // --- EVENTY DLA PRZYCISKÓW (Zamiast Toggle) ---
    public void ToggleInvertX()
    {
        if (!_isInitialized || _isUpdating) return;
        _currentInvertX = !_currentInvertX; // Odwracamy (jak było false, to teraz true)
        UpdateInvertButtonVisuals(); // Od razu zmieniamy napis
        SaveAll(); // Zapisujemy
    }

    public void ToggleInvertY()
    {
        if (!_isInitialized || _isUpdating) return;
        _currentInvertY = !_currentInvertY;
        UpdateInvertButtonVisuals();
        SaveAll();
    }

    private void SaveAll()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings(
                mouseSlider ? mouseSlider.value : SettingsManager.Instance.mouseSensitivity,
                gamepadSlider ? gamepadSlider.value : SettingsManager.Instance.gamepadSensitivity,
                _currentInvertX,
                _currentInvertY,
                vibrationSlider ? vibrationSlider.value : SettingsManager.Instance.vibrationLevel
            );
        }
    }
}