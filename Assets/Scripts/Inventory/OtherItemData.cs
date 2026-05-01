using UnityEngine;

[CreateAssetMenu(fileName = "NewOffhandItem", menuName = "Sojartsa/Inventory/Offhand")]
public class OtherItemData : ItemData
{
    [Header("Ustawienia Offhand")]
    public string skillTrigger; // Nazwa skilla/akcji pod przyciskiem Offhand
    
    private void OnValidate()
    {
        type = ItemType.Other;
    }
}
