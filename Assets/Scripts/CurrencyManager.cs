using UnityEngine;
using System;

/// <summary>
/// Mordo, to nasz "Bankier". Jeden obiekt w całej grze, który
/// pilnuje ile masz kasy. Działa jak FromSoftware - jeden centralny
/// punkt do zarządzania walutą.
/// 
/// UŻYCIE Z DOWOLNEGO SKRYPTU:
///   CurrencyManager.Instance.AddCurrency(100);
///   CurrencyManager.Instance.SpendCurrency(50);
///   int kasa = CurrencyManager.Instance.CurrentCurrency;
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    // Klucz zapisu w PlayerPrefs (do wymiany na SaveManager w przyszłości)
    private const string SAVE_KEY = "PlayerCurrency";

    private int _currentCurrency = 0;

    /// <summary>Aktualna ilość waluty (read-only z zewnątrz).</summary>
    public int CurrentCurrency => _currentCurrency;

    /// <summary>
    /// Event wywoływany za każdym razem gdy waluta się zmienia.
    /// HUD subskrybuje go i sam się aktualizuje - zero spamowania.
    /// </summary>
    public event Action<int> OnCurrencyChanged;

    private void Awake()
    {
        // Singleton: tylko jeden Bankier może istnieć
        if (Instance != null && Instance != this)
        {
            Debug.Log("<color=yellow>[CURRENCY] Klon Bankiera wykryty! Niszczę nadmiar.</color>");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // NIE robimy DontDestroyOnLoad - jesteśmy już dzieckiem PersistentRoot,
        // który sam jest DontDestroyOnLoad. Nie podwajamy zabezpieczenia.

        LoadCurrency();
    }

    /// <summary>
    /// Dodaje walutę graczowi. Wywołuj przy zbieraniu dusz/monet.
    /// </summary>
    public void AddCurrency(int amount)
    {
        if (amount <= 0) return;

        _currentCurrency += amount;
        Debug.Log($"<color=green>[CURRENCY] +{amount} | Razem: {_currentCurrency}</color>");

        SaveCurrency();
        OnCurrencyChanged?.Invoke(_currentCurrency);
    }

    /// <summary>
    /// Próbuje wydać walutę. Zwraca TRUE jeśli gracz miał wystarczająco,
    /// FALSE jeśli był za biedny.
    /// </summary>
    public bool SpendCurrency(int amount)
    {
        if (amount <= 0) return true;

        if (_currentCurrency < amount)
        {
            Debug.Log($"<color=red>[CURRENCY] Za mało kasy! Masz {_currentCurrency}, potrzebujesz {amount}.</color>");
            return false;
        }

        _currentCurrency -= amount;
        Debug.Log($"<color=orange>[CURRENCY] -{amount} | Razem: {_currentCurrency}</color>");

        SaveCurrency();
        OnCurrencyChanged?.Invoke(_currentCurrency);
        return true;
    }

    /// <summary>
    /// Zeruje portfel. Wywołuj przy zgonie gracza (strata dusz jak u FromSoft).
    /// </summary>
    public void ResetCurrency()
    {
        Debug.Log($"<color=red>[CURRENCY] ZGON! Tracisz {_currentCurrency} dusz!</color>");
        _currentCurrency = 0;

        SaveCurrency();
        OnCurrencyChanged?.Invoke(_currentCurrency);
    }

    // ============================================================
    // ZAPIS I ODCZYT (PlayerPrefs - placeholder do przyszłego SaveManagera)
    // ============================================================

    private void SaveCurrency()
    {
        PlayerPrefs.SetInt(SAVE_KEY, _currentCurrency);
        PlayerPrefs.Save();
    }

    private void LoadCurrency()
    {
        _currentCurrency = PlayerPrefs.GetInt(SAVE_KEY, 0);
        Debug.Log($"<color=cyan>[CURRENCY] Wczytano portfel: {_currentCurrency} dusz.</color>");

        // Informujemy UI od razu po załadowaniu
        OnCurrencyChanged?.Invoke(_currentCurrency);
    }
}
