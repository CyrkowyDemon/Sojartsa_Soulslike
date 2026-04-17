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
                CurrencyManager.Instance.AddCurrency(entry.soulsAmount);
            }
        }
    }

    // TĘ FUNKCJĘ MUSISZ WYWOŁAĆ Z KLATKI ANIMACJI W UNITY
    public void UnlockInteraction()
    {
        _isAnimating = false;
    }
}