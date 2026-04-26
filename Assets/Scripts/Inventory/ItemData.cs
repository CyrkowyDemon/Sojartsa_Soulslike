using UnityEngine;

public enum ItemType { Weapon, Consumable, KeyItem, Armor, Enchant, Other }

public abstract class ItemData : ScriptableObject
{
    [Header("Identyfikacja")]
    public string itemID; // To będzie klucz do JSON-a (np. "sword_short_01")
    public string itemName;
    public ItemType type;
    public Sprite icon;

    [Header("Opis i Lore (Klucze Lokalizacji)")]
    [TextArea(3, 10)]
    public string description;
    public string extraStats; // NOWOŚĆ: Efekty specjalne (klucz)

    [Header("Bonusy do Statystyk (Opcjonalne)")]
    public int bonusVitality = 0;
    public int bonusAttackPower = 0;

    [Header("Ustawienia Ekwipunku")]
    public bool isStackable = false;
    public int maxStackSize = 1;
    public int buyValue = 100;
    public int sellValue = 50;

    public string GetLocalizedName()
    {
        if (Sojartsa.Localization.LocalizationManager.Instance != null)
            return Sojartsa.Localization.LocalizationManager.Instance.GetText(itemName);
        return itemName;
    }

    public string GetLocalizedDescription()
    {
        if (Sojartsa.Localization.LocalizationManager.Instance != null)
            return Sojartsa.Localization.LocalizationManager.Instance.GetText(description);
        return description;
    }
}
