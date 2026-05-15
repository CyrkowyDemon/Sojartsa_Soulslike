using UnityEngine;
using System;

/// <summary>
/// Mordo, to jest nowy "Bankier" w wersji ITEMOWEJ.
/// Już nie trzyma kasy w pamięci, tylko patrzy ile masz monet w plecaku.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Ustawienia Waluty")]
    [SerializeField] private ItemData currencyItem; // Przeciągnij tu asset monety!

    /// <summary>ID przedmiotu wyciągnięte prosto z assetu (nie musisz wpisywać dwa razy!)</summary>
    public string CurrencyItemID => (currencyItem != null) ? currencyItem.itemID : "gold_coin";

    /// <summary>Aktualna ilość waluty pobrana prosto z Inventory.</summary>
    public int CurrentCurrency 
    {
        get 
        {
            if (InventoryController.Instance != null && currencyItem != null)
                return InventoryController.Instance.GetTotalItemCount(CurrencyItemID);
            return 0;
        }
    }

    /// <summary>
    /// Event wywoływany za każdym razem gdy zmieni się stan posiadania monet.
    /// HUD subskrybuje to i odświeża licznik.
    /// </summary>
    public event Action<int> OnCurrencyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Podpinamy się pod zmiany w ekwipunku, żeby UI kasy odświeżało się "samo"
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
        
        // Pierwsze odświeżenie po starcie
        HandleInventoryChanged();
    }

    private void OnDestroy()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        OnCurrencyChanged?.Invoke(CurrentCurrency);
    }

    /// <summary>
    /// Dodaje fizyczne monety do ekwipunku gracza.
    /// </summary>
    public void AddCurrency(int amount)
    {
        if (amount <= 0 || currencyItem == null) return;

        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.AddItem(currencyItem, amount);
            // OnInventoryChanged wywoła HandleInventoryChanged automatycznie!
        }
    }

    /// <summary>
    /// Sprawdza czy stać nas na zakupy i usuwa monety z plecaka.
    /// </summary>
    public bool SpendCurrency(int amount)
    {
        if (amount <= 0) return true;

        if (CurrentCurrency < amount)
        {
            Debug.Log($"<color=red>[CURRENCY] Za mało monet! Masz {CurrentCurrency}, potrzebujesz {amount}.</color>");
            return false;
        }

        if (InventoryController.Instance != null)
        {
            bool success = InventoryController.Instance.RemoveItem(CurrencyItemID, amount);
            return success;
        }

        return false;
    }

    /// <summary>
    /// Czyści wszystkie monety z plecaka (np. przy zgonie).
    /// </summary>
    public void ResetCurrency()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.RemoveItem(CurrencyItemID, CurrentCurrency);
        }
    }
}
