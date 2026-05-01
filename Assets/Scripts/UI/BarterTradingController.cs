using UnityEngine;
using System.Collections.Generic;
using Sojartsa.Inventory;

/// <summary>
/// Kontroler fizycznego handlu barterowego (stół 3x3).
/// Zarządza danymi na stole, logiką kupna/skupu, i powiadamia UI o zmianach.
/// </summary>
public class BarterTradingController : MonoBehaviour
{
    public static BarterTradingController Instance { get; private set; }

    [Header("Ustawienia")]
    [SerializeField] private ItemData primaryCurrencyItem; // Co NPC daje przy skupie (np. Miedziak)

    [Header("Sloty Barteru (Stół 3x3)")]
    public List<InventorySlot> barterSlots = new List<InventorySlot>();

    [Header("Slot Skupu (Wynik 1x1)")]
    public InventorySlot sellResultSlot = new InventorySlot();

    private TradeShopData _currentShop;

    // --- EVENT: Powiadamia BarterGridSlotUI i BarterUI o zmianach na stole ---
    public event System.Action OnBarterChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Inicjalizacja 9 slotów stołu
        barterSlots.Clear();
        for (int i = 0; i < 9; i++) barterSlots.Add(new InventorySlot());
    }

    public void OpenTrade(TradeShopData shop)
    {
        _currentShop = shop;
        Debug.Log($"[BARTER] Otwarto handel z: {shop.shopName}");
        NotifyBarterChanged();
    }

    public void CloseTrade()
    {
        if (InventoryController.Instance == null) return;

        // Zwracamy wszystko ze stołu do plecaka
        foreach (var slot in barterSlots)
        {
            if (!slot.IsEmpty)
            {
                InventoryController.Instance.AddItem(slot.item, slot.amount);
                slot.Clear();
            }
        }
        
        if (!sellResultSlot.IsEmpty)
        {
            InventoryController.Instance.AddItem(sellResultSlot.item, sellResultSlot.amount);
            sellResultSlot.Clear();
        }

        _currentShop = null;
        NotifyBarterChanged();
    }

    // ============================================================
    // LOGIKA KUPNA (BUY)
    // ============================================================

    public bool CanAfford(TradeOfferData offer)
    {
        if (offer == null) return false;

        bool hasCost1 = HasItemsOnTable(offer.costItem1, offer.GetFinalCost1());
        bool hasCost2 = true;

        if (offer.costItem2 != null)
        {
            hasCost2 = HasItemsOnTable(offer.costItem2, offer.GetFinalCost2());
        }

        return hasCost1 && hasCost2;
    }

    public void ExecutePurchase(TradeOfferData offer)
    {
        if (!CanAfford(offer)) return;

        RemoveFromTable(offer.costItem1, offer.GetFinalCost1());
        if (offer.costItem2 != null)
        {
            RemoveFromTable(offer.costItem2, offer.GetFinalCost2());
        }

        InventoryController.Instance.AddItem(offer.resultItem, offer.resultAmount);
        
        Debug.Log($"[BARTER] Zakupiono: {offer.resultItem.itemName}");
        RefreshSellValue();
        NotifyBarterChanged();
    }

    // ============================================================
    // LOGIKA SKUPU (SELL)
    // ============================================================

    public void RefreshSellValue()
    {
        if (_currentShop == null || primaryCurrencyItem == null) return;

        int totalValue = 0;
        foreach (var slot in barterSlots)
        {
            if (!slot.IsEmpty)
            {
                totalValue += Mathf.FloorToInt(slot.item.sellValue * slot.amount * _currentShop.sellRate);
            }
        }

        if (totalValue > 0)
        {
            sellResultSlot.item = primaryCurrencyItem;
            sellResultSlot.amount = totalValue;
        }
        else
        {
            sellResultSlot.Clear();
        }

        // Odświeżamy też tekst sprzedaży w BarterUI
        if (BarterUI.Instance != null) BarterUI.Instance.RefreshSellDisplay();
    }

    public void FinalizeSale()
    {
        if (!sellResultSlot.IsEmpty)
        {
            InventoryController.Instance.AddItem(sellResultSlot.item, sellResultSlot.amount);
        }

        foreach (var slot in barterSlots) slot.Clear();
        sellResultSlot.Clear();
        
        RefreshSellValue();
        NotifyBarterChanged();
        Debug.Log("[BARTER] Sprzedaż sfinalizowana.");
    }

    // ============================================================
    // TRANSFER (wywoływane przez ItemTransferManager)
    // ============================================================

    public void AddItemToTable(List<InventorySlot> sourceList, int sourceIndex, int tableIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= sourceList.Count) return;
        if (tableIndex < 0 || tableIndex >= barterSlots.Count) return;

        InventorySlot sourceSlot = sourceList[sourceIndex];
        InventorySlot targetSlot = barterSlots[tableIndex];

        if (sourceSlot.IsEmpty) return;

        // Merguj, przenieś lub zamień
        if (!targetSlot.IsEmpty && targetSlot.item.itemID == sourceSlot.item.itemID && targetSlot.item.isStackable)
        {
            int spaceLeft = targetSlot.item.maxStackSize - targetSlot.amount;
            if (spaceLeft > 0)
            {
                int amountToMove = Mathf.Min(spaceLeft, sourceSlot.amount);
                targetSlot.amount += amountToMove;
                sourceSlot.amount -= amountToMove;

                if (sourceSlot.amount <= 0) sourceSlot.Clear();
            }
            else
            {
                // Brak miejsca na merge -> swap
                SwapSlotsOnTable(sourceSlot, targetSlot);
            }
        }
        else if (targetSlot.IsEmpty)
        {
            targetSlot.item = sourceSlot.item;
            targetSlot.amount = sourceSlot.amount;
            sourceSlot.Clear();
        }
        else
        {
            // Swap
            SwapSlotsOnTable(sourceSlot, targetSlot);
        }

        RefreshSellValue();
        if (InventoryController.Instance != null)
            InventoryController.Instance.TriggerInventoryUpdate();
        NotifyBarterChanged();
    }

    private void SwapSlotsOnTable(InventorySlot slotA, InventorySlot slotB)
    {
        ItemData tempItem = slotA.item;
        int tempAmount = slotA.amount;
        slotA.item = slotB.item;
        slotA.amount = slotB.amount;
        slotB.item = tempItem;
        slotB.amount = tempAmount;
    }

    // ============================================================
    // POMOCNICZE
    // ============================================================

    private bool HasItemsOnTable(ItemData item, int amount)
    {
        int count = 0;
        foreach (var slot in barterSlots)
        {
            if (!slot.IsEmpty && slot.item.itemID == item.itemID)
                count += slot.amount;
        }
        return count >= amount;
    }

    private void RemoveFromTable(ItemData item, int amount)
    {
        int toRemove = amount;
        for (int i = 0; i < barterSlots.Count; i++)
        {
            if (toRemove <= 0) break;
            var slot = barterSlots[i];
            if (!slot.IsEmpty && slot.item.itemID == item.itemID)
            {
                if (slot.amount <= toRemove)
                {
                    toRemove -= slot.amount;
                    slot.Clear();
                }
                else
                {
                    slot.amount -= toRemove;
                    toRemove = 0;
                }
            }
        }
    }

    private void NotifyBarterChanged()
    {
        OnBarterChanged?.Invoke();
    }
}
