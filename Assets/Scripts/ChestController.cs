using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class ChestController : MonoBehaviour, IInteractable
{
    [Header("Skrzynia: Stan")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool canBeClosed = true;

    [Header("Skrzynia: Konfiguracja")]
    public Transform interactionPoint;
    [SerializeField] private ChestLootData lootData;

    [Header("Wydarzenia")]
    public UnityEvent OnChestOpened;
    public UnityEvent OnChestClosed;
    
    [Header("Zapis Stanu")]
    [Tooltip("Wpisz unikalną nazwę, żeby gra zapamiętała otwarcie. Puste = reset po śmierci.")]
    public string uniqueID;

    private HashSet<int> _claimedLootIndices = new HashSet<int>();
    private bool _isAnimating = false;
    private int _openCount = 0;

    [ContextMenu("DEBUG: Force Unlock Interaction")]
    public void ForceUnlock()
    {
        _isAnimating = false;
    }

    /// <summary>
    /// Wymusza otwarcie skrzyni bez animacji (dla systemu zapisu).
    /// </summary>
    public void ForceOpen()
    {
        isOpen = true;
        _claimedLootIndices.Clear(); // Zakładamy, że skoro otwarta, to loot wzięty (lub nie)
        
        // Jeśli masz animatora, trzeba mu ustawić stan Open
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.Play("Open", 0, 1f); // 1f = koniec animacji

        OnChestOpened?.Invoke();
    }

    public void Interact(Transform interactor)
    {
        if (_isAnimating || (!canBeClosed && isOpen)) return;

        if (!isOpen) OpenChest();
        else CloseChest();
    }

    public string GetInteractText() => _isAnimating ? "" : (!isOpen ? "Otwórz skrzynię" : (canBeClosed ? "Zamknij skrzynię" : ""));
    public bool CanInteract() => !_isAnimating && (!isOpen || canBeClosed);

    private void OpenChest()
    {
        isOpen = true;
        _openCount++;
        _isAnimating = true;

        ProcessLoot();
        
        if (!string.IsNullOrEmpty(uniqueID) && SaveManager.Instance != null)
        {
            SaveManager.Instance.MarkChestAsOpened(uniqueID);
        }

        OnChestOpened?.Invoke(); // Krzyczy: "Zostałam otwarta!"
    }

    private void CloseChest()
    {
        isOpen = false;
        _isAnimating = true;

        OnChestClosed?.Invoke(); // Krzyczy: "Zostałam zamknięta!"
    }

    private void ProcessLoot()
    {
        if (lootData == null || CurrencyManager.Instance == null) return;

        for (int i = 0; i < lootData.entries.Count; i++)
        {
            var entry = lootData.entries[i];
            if (entry.requiredOpenCount == _openCount)
            {
                if (entry.giveOnlyOnce && !_claimedLootIndices.Add(i)) continue;
                
                // Dodajemy walutę (korzystając ze skrótu)
                if (entry.currencyAmount > 0 && CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.AddCurrency(entry.currencyAmount);
                    Debug.Log($"[CHEST] Dodano walutę: {entry.currencyAmount}");
                }
                
                // Dodajemy przedmioty do EQ
                if (entry.itemReward != null && InventoryController.Instance != null && entry.itemAmount > 0)
                {
                    InventoryController.Instance.AddItem(entry.itemReward, entry.itemAmount);
                    Debug.Log($"[CHEST] Dodano do EQ: {entry.itemReward.itemName} x{entry.itemAmount}");
                }
            }
        }
    }

    // TĘ FUNKCJĘ MUSISZ WYWOŁAĆ Z KLATKI ANIMACJI W UNITY
    public void UnlockInteraction()
    {
        _isAnimating = false;
    }
}