using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioUI : MonoBehaviour
{
    [Header("UI Elementy - Przyciski (Sekiro Style)")]
    [SerializeField] private Button subtitlesButton;
    [SerializeField] private TextMeshProUGUI subtitlesText;
    [SerializeField] private Button hudButton;
    [SerializeField] private TextMeshProUGUI hudText;

    [Header("UI Elementy - Suwaki Audio")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundsSlider;
    [SerializeField] private Slider voiceSlider;

    private bool _currentSubtitles;
    private bool _currentHUD;

    private bool _isUpdating = false;
    private bool _isInitialized = false;

    private void Start()
    {
        UpdateUIElements();
        _isInitialized = true; 
    }

    private void OnEnable()
    {
        if (_isInitialized) UpdateUIElements();
    }

    public void UpdateUIElements()
    {
        if (SettingsManager.Instance == null) return;

        _isUpdating = true; // Blokujemy eventy podczas odświeżania

        // Pobieramy aktualne stany z menedżera
        _currentSubtitles = SettingsManager.Instance.showSubtitles;
        _currentHUD = SettingsManager.Instance.showHUD;
        UpdateVisuals(); // Odświeża teksty na przyciskach

        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value = SettingsManager.Instance.musicVolume;
        }

        if (soundsSlider != null)
        {
            soundsSlider.minValue = 0f;
            soundsSlider.maxValue = 1f;
            soundsSlider.value = SettingsManager.Instance.soundsVolume;
        }

        if (voiceSlider != null)
        {
            voiceSlider.minValue = 0f;
            voiceSlider.maxValue = 1f;
            voiceSlider.value = SettingsManager.Instance.voiceVolume;
        }

        _isUpdating = false; // Odblokowujemy eventy
    }

    private void UpdateVisuals()
    {
        if (subtitlesText != null) 
            subtitlesText.text = _currentSubtitles ? "On" : "Off";
            
        if (hudText != null) 
            hudText.text = _currentHUD ? "On" : "Off";
    }

    // --- EVENTY DLA PRZYCISKÓW (Zamiast Toggle) ---
    public void ToggleSubtitles()
    {
        if (!_isInitialized || _isUpdating) return;
        _currentSubtitles = !_currentSubtitles; 
        UpdateVisuals(); 
        SaveAudioAndUI(); 
    }

    public void ToggleHUD()
    {
        if (!_isInitialized || _isUpdating) return;
        _currentHUD = !_currentHUD; 
        UpdateVisuals(); 
        SaveAudioAndUI(); 
    }

    // --- EVENTY DLA SUWAKÓW ---
    public void OnMusicVolumeChanged(float newValue)
    {
        if (!_isInitialized || _isUpdating) return;
        SaveAudioAndUI();
    }

    public void OnSoundsVolumeChanged(float newValue)
    {
        if (!_isInitialized || _isUpdating) return;
        SaveAudioAndUI();
    }

    public void OnVoiceVolumeChanged(float newValue)
    {
        if (!_isInitialized || _isUpdating) return;
        SaveAudioAndUI();
    }

    private void SaveAudioAndUI()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveAudioAndUISettings(
                _currentSubtitles,
                _currentHUD,
                musicSlider ? musicSlider.value : SettingsManager.Instance.musicVolume,
                soundsSlider ? soundsSlider.value : SettingsManager.Instance.soundsVolume,
                voiceSlider ? voiceSlider.value : SettingsManager.Instance.voiceVolume
            );
        }
    }
}