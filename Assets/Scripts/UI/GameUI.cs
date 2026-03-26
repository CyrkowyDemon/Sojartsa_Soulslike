using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("UI Elementy - Teksty")]
    [SerializeField] private TextMeshProUGUI autoLockText;
    [SerializeField] private TextMeshProUGUI autoAimText;

    private bool _currentAutoLock;
    private bool _currentAutoAim;
    private bool _isInitialized = false;

    private void Start()
    {
        _isInitialized = true;
        UpdateUI();
    }

    private void OnEnable()
    {
        if (_isInitialized) UpdateUI();
    }

    public void UpdateUI()
    {
        if (SettingsManager.Instance == null) return;
        
        _currentAutoLock = SettingsManager.Instance.autoLock;
        _currentAutoAim = SettingsManager.Instance.autoAim;
        
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Ustawiamy On/Off
        if (autoLockText) autoLockText.text = _currentAutoLock ? "On" : "Off";
        if (autoAimText) autoAimText.text = _currentAutoAim ? "On" : "Off";
    }

    // FUNKCJE DLA PRZYCISKÓW
    public void ToggleAutoLock()
    {
        _currentAutoLock = !_currentAutoLock;
        Save();
    }

    public void ToggleAutoAim()
    {
        _currentAutoAim = !_currentAutoAim;
        Save();
    }

    private void Save()
    {
        UpdateVisuals();
        SettingsManager.Instance.SaveGameSettings(_currentAutoLock, _currentAutoAim);
    }
}