using UnityEngine;

[CreateAssetMenu(fileName = "NewOtherItem", menuName = "Sojartsa/Inventory/Other (Offhand)")]
public class OtherItemData : ItemData
{
    [Header("Ustawienia Offhand")]
    public string skillTrigger; // Nazwa skilla/akcji pod przyciskiem Offhand
    
    private void OnValidate()
    {
        type = ItemType.Other;
    }
}
