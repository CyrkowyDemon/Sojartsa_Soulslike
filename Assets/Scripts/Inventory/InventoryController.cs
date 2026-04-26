using System.Collections.Generic;
using UnityEngine;
using Sojartsa.Inventory;

/// <summary>
/// Menedżer dwóch torb gracza: Inventory i Enchant Bag.
/// Odpowiada za logikę (dodawanie, usuwanie, zamiana slotów).
/// </summary>
public class InventoryController : MonoBehaviour
{
    public static InventoryController Instance { get; private set; }

    [Header("Ustawienia Wielkości")]
    [SerializeField] private int inventorySize = 27;
    [SerializeField] private int bagSize = 18;

    [Header("Dane Ekwipunku")]
    public List<InventorySlot> inventorySlots = new List<InventorySlot>();
    public List<InventorySlot> bagSlots = new List<InventorySlot>();
    public List<InventorySlot> equipmentSlots = new List<InventorySlot>();

    // --- SYSTEM ZDARZEŃ (EVENTS) ---
    public event System.Action OnInventoryChanged;
    public event System.Action OnEquipmentChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeInventory();
    }

    private void InitializeInventory()
    {
        // Przygotowujemy puste sloty (Minecraft-style grid musi mieć stałą liczbę "pudełek")
        for (int i = 0; i < inventorySize; i++)
        {
            inventorySlots.Add(new InventorySlot());
        }

        for (int i = 0; i < bagSize; i++)
        {
            bagSlots.Add(new InventorySlot());
        }

        // 4 sloty na sprzęt (Main Hand, Off Hand, Consumable, Enchant)
        // Kolejność: 0=Broń, 1=Offhand, 2=Użytkowe, 3=Enchant
        for (int i = 0; i < 4; i++)
        {
            equipmentSlots.Add(new InventorySlot());
        }
    }

    /// <summary>
    /// Próbuje dodać przedmiot do odpowiedniej torby.
    /// </summary>
    public bool AddItem(ItemData item, int amount = 1)
    {
        // 1. Decydujemy, do której torby ma trafić
        List<InventorySlot> targetBag = (item.type == ItemType.Enchant) ? bagSlots : inventorySlots;

        // 2. Jeśli przedmiot się stackuje, szukamy istniejącego stacka
        if (item.isStackable)
        {
            foreach (var slot in targetBag)
            {
                if (!slot.IsEmpty && slot.item.itemID == item.itemID && slot.amount < item.maxStackSize)
                {
                    slot.AddAmount(amount);
                    OnInventoryChanged?.Invoke(); // KRZYCZYMY!
                    return true;
                }
            }
        }

        // 3. Szukamy pierwszego wolnego miejsca
        foreach (var slot in targetBag)
        {
            if (slot.IsEmpty)
            {
                slot.item = item;
                slot.amount = amount;
                OnInventoryChanged?.Invoke(); // KRZYCZYMY!
                return true;
            }
        }

        Debug.LogWarning("Ekwipunek jest pełny!");
        return false;
    }

    /// <summary>
    /// Zamienia przedmioty miejscami (pod Drag & Drop).
    /// </summary>
    public void SwapSlots(List<InventorySlot> targetBag, int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= targetBag.Count || indexB < 0 || indexB >= targetBag.Count) return;

        InventorySlot temp = new InventorySlot(targetBag[indexA].item, targetBag[indexA].amount);
        
        targetBag[indexA].item = targetBag[indexB].item;
        targetBag[indexA].amount = targetBag[indexB].amount;

        targetBag[indexB].item = temp.item;
        targetBag[indexB].amount = temp.amount;

        // Powiadamiamy i odświeżamy
        if (targetBag == equipmentSlots) 
            TriggerEquipmentUpdate();
        else 
            OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Zamienia przedmioty między dwiema różnymi listami (np. Inventory <-> Equipment).
    /// </summary>
    public void SwapSlotsBetweenLists(List<InventorySlot> listA, int indexA, List<InventorySlot> listB, int indexB)
    {
        // Walidacja: Jeśli listB to ekwipunek, sprawdzamy czy przedmiot z listA tam pasuje
        if (listB == equipmentSlots && !IsValidForEquipmentSlot(listA[indexA].item, indexB)) return;
        if (listA == equipmentSlots && !IsValidForEquipmentSlot(listB[indexB].item, indexA)) return;

        InventorySlot temp = new InventorySlot(listA[indexA].item, listA[indexA].amount);
        
        listA[indexA].item = listB[indexB].item;
        listA[indexA].amount = listB[indexB].amount;

        listB[indexB].item = temp.item;
        listB[indexB].amount = temp.amount;

        // Powiadamiamy wszystkich o zmianach
        OnInventoryChanged?.Invoke();
        if (listA == equipmentSlots || listB == equipmentSlots) 
            TriggerEquipmentUpdate();
    }

    /// <summary>
    /// Centralna funkcja wyzwalająca WSZYSTKIE aktualizacje związane ze sprzętem.
    /// </summary>
    public void TriggerEquipmentUpdate()
    {
        OnEquipmentChanged?.Invoke();
        
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.RefreshFromSlots();

        if (Sojartsa.Inventory.UI.PlayerStatsWindowUI.Instance != null)
            Sojartsa.Inventory.UI.PlayerStatsWindowUI.Instance.UpdateStats();
    }

    private bool IsValidForEquipmentSlot(ItemData item, int slotIndex)
    {
        if (item == null) return true; // Puste miejsce zawsze ok
        
        switch (slotIndex)
        {
            case 0: return item.type == ItemType.Weapon;
            case 1: return item.type == ItemType.Other;
            case 2: return item.type == ItemType.Consumable;
            case 3: return item.type == ItemType.Enchant; // Tylko magiczne kule!
            default: return false;
        }
    }

    public void UpdateEquipmentVisuals()
    {
        if (EquipmentManager.Instance == null) return;
        EquipmentManager.Instance.RefreshFromSlots();
    }
}
