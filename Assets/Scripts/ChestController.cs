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

    private HashSet<int> _claimedLootIndices = new HashSet<int>();
    private bool _isAnimating = false;
    private int _openCount = 0;

    [ContextMenu("DEBUG: Force Unlock Interaction")]
    public void ForceUnlock()
    {
        _isAnimating = false;
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
                
                // Dodajemy dusze (jeśli są)
                if (entry.soulsAmount > 0 && CurrencyManager.Instance != null)
                    CurrencyManager.Instance.AddCurrency(entry.soulsAmount);

                // NOWOŚĆ: Dodajemy przedmioty do EQ
                if (entry.itemReward != null && InventoryController.Instance != null)
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