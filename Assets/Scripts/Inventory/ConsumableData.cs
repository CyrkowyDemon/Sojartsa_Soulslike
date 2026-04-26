using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Sojartsa/Inventory/Consumable")]
public class ConsumableData : ItemData
{
    [Header("Efekt Mikstury")]
    public int healAmount = 50;
    public float effectDuration = 0f; // 0 dla natychmiastowych

    private void OnValidate()
    {
        type = ItemType.Consumable;
        isStackable = true; // Mikstury zazwyczaj się stakują!
    }
}
