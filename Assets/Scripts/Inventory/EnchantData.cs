using UnityEngine;

[CreateAssetMenu(fileName = "NewEnchant", menuName = "Sojartsa/Inventory/Enchant Orb")]
public class EnchantData : ItemData
{
    [Header("Ustawienia Magicznej Kuli")]
    public string skillID; // Identyfikator umiejętności, którą odblokowuje ta kula
    public int manaCost = 10;

    private void OnValidate()
    {
        type = ItemType.Enchant; 
        isStackable = false;     // Kule zazwyczaj są unikalne
    }
}
