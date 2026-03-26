using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GraphicUI : MonoBehaviour
{
    [Header("Przyciski (Sekiro Style)")]
    [SerializeField] private TextMeshProUGUI qualityText;
    [SerializeField] private TextMeshProUGUI screenModeText;
    [SerializeField] private TextMeshProUGUI resolutionText;
    [SerializeField] private TextMeshProUGUI bloodText;

    private int _currentQuality;
    private int _currentScreenMode;
    private int _currentResIndex;
    private bool _currentBlood;

    private Resolution[] _resolutions;
    private string[] _qualityNames = { "Low", "Medium", "High", "Ultra" };
    private string[] _screenModeNames = { "Fullscreen", "Borderless", "Windowed" };

    private bool _isUpdating = false;
    private bool _isInitialized = false;

    private void Awake()
    {
        // Wczytujemy rozdzielczości jako pierwsze w Awake, żeby OnEnable ich nie szukało na próżno
        _resolutions = Screen.resolutions;
    }

    private void Start()
    {
        _isInitialized = true; 
        UpdateUIElements();
    }

    private void OnEnable()
    {
        if (_isInitialized) UpdateUIElements();
    }

    public void UpdateUIElements()
    {
        if (SettingsManager.Instance == null) return;
        _isUpdating = true;

        // Na wszelki wypadek, jeśli Awake jeszcze nie pykło (rzadkie w Unity, ale bezpieczne)
        if (_resolutions == null || _resolutions.Length == 0) _resolutions = Screen.resolutions;

        _currentQuality = SettingsManager.Instance.qualityIndex;
        _currentScreenMode = SettingsManager.Instance.screenModeIndex;
        _currentResIndex = SettingsManager.Instance.resolutionIndex;
        _currentBlood = SettingsManager.Instance.showBlood;

        UpdateVisuals();
        _isUpdating = false;
    }

    private void UpdateVisuals()
    {
        if (qualityText) qualityText.text = _qualityNames[_currentQuality];
        if (screenModeText) screenModeText.text = _screenModeNames[_currentScreenMode];
        if (bloodText) bloodText.text = _currentBlood ? "On" : "Off";
        
        if (resolutionText && _resolutions != null && _resolutions.Length > 0)
        {
            Resolution res = _resolutions[Mathf.Clamp(_currentResIndex, 0, _resolutions.Length - 1)];
            resolutionText.text = $"{res.width} x {res.height} @ {res.refreshRateRatio.value:F0}Hz";
        }
    }

    // --- FUNKCJE DLA PRZYCISKÓW (Podepnij pod OnClick) ---

    public void CycleQuality()
    {
        _currentQuality = (_currentQuality + 1) % _qualityNames.Length;
        SaveAndApply();
    }

    public void CycleScreenMode()
    {
        _currentScreenMode = (_currentScreenMode + 1) % _screenModeNames.Length;
        SaveAndApply();
    }

    public void CycleResolution(int direction) // direction: 1 dla następnej, -1 dla poprzedniej
    {
        _currentResIndex += direction;
        if (_currentResIndex >= _resolutions.Length) _currentResIndex = 0;
        if (_currentResIndex < 0) _currentResIndex = _resolutions.Length - 1;
        SaveAndApply();
    }

    public void ToggleBlood()
    {
        _currentBlood = !_currentBlood;
        SaveAndApply();
    }

    private void SaveAndApply()
    {
        if (_isUpdating) return;
        UpdateVisuals();
        SettingsManager.Instance.SaveGraphicsSettings(_currentQuality, _currentScreenMode, _currentResIndex, _currentBlood);
    }
}