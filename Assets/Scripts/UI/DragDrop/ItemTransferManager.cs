using UnityEngine;
using Sojartsa.Inventory;
using Sojartsa.UI.DragDrop;
using System.Collections.Generic;

/// <summary>
/// CENTRALNY DYSPOZYTOR TRANSFERÓW.
/// 
/// Jedyny skrypt w grze, który zna WSZYSTKIE systemy (Inventory, Barter, Currency).
/// Elementy UI (sloty, oferty) nie muszą o sobie wzajemnie wiedzieć.
/// 
/// Przepływ:
///   1. InventoryDragHandler pyta źródło o ItemPayload (co przenosisz?)
///   2. InventoryDragHandler pyta cel o ItemPayload (kim jesteś?)
///   3. InventoryDragHandler woła ItemTransferManager.Execute(source, target)
///   4. Ten skrypt decyduje co zrobić (swap, kupno, sprzedaż, transfer na stół)
/// </summary>
public class ItemTransferManager : MonoBehaviour
{
    public static ItemTransferManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Główna funkcja transferu. Dostaje info o źródle i celu, podejmuje decyzję.
    /// Zwraca true jeśli transfer się powiódł.
    /// </summary>
    public bool Execute(ItemPayload source, ItemPayload target)
    {
        if (source == null || source.IsEmpty) return false;

        // Identyczny typ źródła i celu = wewnętrzny swap
        // (np. Inventory → Inventory, albo Bag → Bag)
        if (IsSameContainer(source.Source, target.Source))
        {
            return HandleInternalSwap(source, target);
        }

        // Inventory/Bag ↔ Equipment (zakładanie i zdejmowanie sprzętu)
        if (IsInventoryType(source.Source) && target.Source == ItemPayload.SourceType.Equipment)
        {
            return HandleEquipItem(source, target);
        }
        if (source.Source == ItemPayload.SourceType.Equipment && IsInventoryType(target.Source))
        {
            return HandleEquipItem(source, target);
        }

        // Inventory/Bag → BarterTable (kładzenie na stół)
        if (IsInventoryType(source.Source) && target.Source == ItemPayload.SourceType.BarterTable)
        {
            return HandleInventoryToTable(source, target);
        }

        // BarterTable → Inventory/Bag (ściąganie ze stołu)
        if (source.Source == ItemPayload.SourceType.BarterTable && IsInventoryType(target.Source))
        {
            return HandleTableToInventory(source, target);
        }

        // BarterTable ↔ BarterTable (przestawianie na stole)
        if (source.Source == ItemPayload.SourceType.BarterTable && target.Source == ItemPayload.SourceType.BarterTable)
        {
            return HandleTableSwap(source, target);
        }

        // ShopOffer → Inventory (kupowanie oferty)
        if (source.Source == ItemPayload.SourceType.ShopOffer && IsInventoryType(target.Source))
        {
            return HandlePurchase(source);
        }

        Debug.LogWarning($"[TRANSFER] Nieobsługiwana kombinacja: {source.Source} → {target.Source}");
        return false;
    }

    // ============================================================
    //  HANDLERY – każdy obsługuje jedną konkretną akcję
    // ============================================================

    private bool HandleInternalSwap(ItemPayload source, ItemPayload target)
    {
        InventoryController inv = InventoryController.Instance;
        if (inv == null) return false;

        List<InventorySlot> list = GetSlotList(source.Source);
        if (list == null) return false;

        inv.SwapSlots(list, source.SlotIndex, target.SlotIndex);
        return true;
    }

    private bool HandleEquipItem(ItemPayload source, ItemPayload target)
    {
        InventoryController inv = InventoryController.Instance;
        if (inv == null) return false;

        List<InventorySlot> listA = GetSlotList(source.Source);
        List<InventorySlot> listB = GetSlotList(target.Source);
        if (listA == null || listB == null) return false;

        inv.SwapSlotsBetweenLists(listA, source.SlotIndex, listB, target.SlotIndex);
        return true;
    }

    private bool HandleInventoryToTable(ItemPayload source, ItemPayload target)
    {
        InventoryController inv = InventoryController.Instance;
        BarterTradingController barter = BarterTradingController.Instance;
        if (inv == null || barter == null) return false;

        List<InventorySlot> sourceList = GetSlotList(source.Source);
        if (sourceList == null) return false;

        barter.AddItemToTable(sourceList, source.SlotIndex, target.SlotIndex);
        return true;
    }

    private bool HandleTableToInventory(ItemPayload source, ItemPayload target)
    {
        InventoryController inv = InventoryController.Instance;
        BarterTradingController barter = BarterTradingController.Instance;
        if (inv == null || barter == null) return false;

        // Mordo, fix: Jeśli indeks to -1, to bierzemy przedmiot ze slotu WYNIKU, nie ze stołu!
        InventorySlot slotOnTable = (source.SlotIndex == -1) ? barter.sellResultSlot : barter.barterSlots[source.SlotIndex];
        
        if (slotOnTable.IsEmpty) return false;

        List<InventorySlot> targetList = GetSlotList(target.Source);
        if (targetList == null || target.SlotIndex < 0 || target.SlotIndex >= targetList.Count) return false;

        InventorySlot targetSlot = targetList[target.SlotIndex];

        if (!targetSlot.IsEmpty && targetSlot.item.itemID == slotOnTable.item.itemID && targetSlot.item.isStackable)
        {
            int spaceLeft = targetSlot.item.maxStackSize - targetSlot.amount;
            if (spaceLeft > 0)
            {
                int amountToMove = Mathf.Min(spaceLeft, slotOnTable.amount);
                targetSlot.amount += amountToMove;
                slotOnTable.amount -= amountToMove;

                if (slotOnTable.amount <= 0) slotOnTable.Clear();
            }
            else
            {
                // Swap
                ItemData tempItem = targetSlot.item;
                int tempAmount = targetSlot.amount;
                targetSlot.item = slotOnTable.item;
                targetSlot.amount = slotOnTable.amount;
                slotOnTable.item = tempItem;
                slotOnTable.amount = tempAmount;
            }
        }
        else if (targetSlot.IsEmpty)
        {
            targetSlot.item = slotOnTable.item;
            targetSlot.amount = slotOnTable.amount;
            slotOnTable.Clear();
        }
        else
        {
            // Swap
            ItemData tempItem = targetSlot.item;
            int tempAmount = targetSlot.amount;
            targetSlot.item = slotOnTable.item;
            targetSlot.amount = slotOnTable.amount;
            slotOnTable.item = tempItem;
            slotOnTable.amount = tempAmount;
        }

        barter.RefreshSellValue();
        inv.TriggerInventoryUpdate();
        return true;
    }

    private bool HandleTableSwap(ItemPayload source, ItemPayload target)
    {
        BarterTradingController barter = BarterTradingController.Instance;
        if (barter == null) return false;

        // Mordo, nie pozwalamy na zamianę miejscami (Swap) ze slotem wyniku (-1)
        if (source.SlotIndex < 0 || target.SlotIndex < 0) return false;
        if (source.SlotIndex >= barter.barterSlots.Count || target.SlotIndex >= barter.barterSlots.Count) return false;

        InventorySlot slotA = barter.barterSlots[source.SlotIndex];
        InventorySlot slotB = barter.barterSlots[target.SlotIndex];

        if (!slotA.IsEmpty && !slotB.IsEmpty && slotA.item.itemID == slotB.item.itemID && slotA.item.isStackable)
        {
            int spaceLeft = slotB.item.maxStackSize - slotB.amount;
            if (spaceLeft > 0)
            {
                int amountToMove = Mathf.Min(spaceLeft, slotA.amount);
                slotB.amount += amountToMove;
                slotA.amount -= amountToMove;

                if (slotA.amount <= 0) slotA.Clear();
            }
            else
            {
                // Swap
                ItemData tempItem = slotA.item;
                int tempAmount = slotA.amount;
                slotA.item = slotB.item;
                slotA.amount = slotB.amount;
                slotB.item = tempItem;
                slotB.amount = tempAmount;
            }
        }
        else
        {
            ItemData tempItem = slotA.item;
            int tempAmount = slotA.amount;

            slotA.item = slotB.item;
            slotA.amount = slotB.amount;

            slotB.item = tempItem;
            slotB.amount = tempAmount;
        }

        barter.RefreshSellValue();
        return true;
    }

    private bool HandlePurchase(ItemPayload source)
    {
        BarterTradingController barter = BarterTradingController.Instance;
        if (barter == null || source.Offer == null) return false;

        barter.ExecutePurchase(source.Offer);
        return true;
    }

    // ============================================================
    //  SPLIT STACK (Dzielenie kupek)
    // ============================================================

    /// <summary>
    /// Dzieli stack na dwie połowy. Połowa zostaje w źródle, 
    /// druga połowa trafia na pierwszy wolny slot w tym samym kontenerze.
    /// </summary>
    public void SplitStack(ItemPayload source)
    {
        if (source == null || source.IsEmpty || source.Amount <= 1) return;

        System.Collections.Generic.List<InventorySlot> list = GetSlotList(source.Source);
        if (list == null) return;

        InventorySlot sourceSlot = list[source.SlotIndex];
        if (sourceSlot.IsEmpty || sourceSlot.amount <= 1) return;

        int halfAmount = sourceSlot.amount / 2;
        int remaining = sourceSlot.amount - halfAmount;

        // Szukamy pierwszego wolnego slotu w tej samej liście
        for (int i = 0; i < list.Count; i++)
        {
            if (i == source.SlotIndex) continue;
            if (list[i].IsEmpty)
            {
                list[i].item = sourceSlot.item;
                list[i].amount = halfAmount;
                sourceSlot.amount = remaining;

                // Odświeżamy UI
                InventoryController inv = InventoryController.Instance;
                if (inv != null) inv.TriggerInventoryUpdate();
                
                // Jeśli to stół barterowy, odśwież wartość
                if (source.Source == ItemPayload.SourceType.BarterTable)
                {
                    BarterTradingController barter = BarterTradingController.Instance;
                    if (barter != null) barter.RefreshSellValue();
                }

                Debug.Log($"[SPLIT] Podzielono stack: {remaining} + {halfAmount}");
                return;
            }
        }

        Debug.Log("[SPLIT] Brak wolnego miejsca na podzielony stack!");
    }

    // ============================================================
    //  POMOCNICZE
    // ============================================================

    private List<InventorySlot> GetSlotList(ItemPayload.SourceType type)
    {
        InventoryController inv = InventoryController.Instance;
        if (inv == null) return null;

        switch (type)
        {
            case ItemPayload.SourceType.Inventory: return inv.inventorySlots;
            case ItemPayload.SourceType.Bag: return inv.bagSlots;
            case ItemPayload.SourceType.Equipment: return inv.equipmentSlots;
            case ItemPayload.SourceType.BarterTable:
                return BarterTradingController.Instance?.barterSlots;
            default: return null;
        }
    }

    private bool IsInventoryType(ItemPayload.SourceType type)
    {
        return type == ItemPayload.SourceType.Inventory || type == ItemPayload.SourceType.Bag;
    }

    private bool IsSameContainer(ItemPayload.SourceType a, ItemPayload.SourceType b)
    {
        return a == b && a != ItemPayload.SourceType.ShopOffer;
    }
}
