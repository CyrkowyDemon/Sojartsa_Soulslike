using UnityEngine;

[CreateAssetMenu(fileName = "NewCurrencyItem", menuName = "Sojartsa/Inventory/Currency")]
public class CurrencyData : ItemData
{
    private void OnValidate()
    {
        type = ItemType.Currency;
        isStackable = true; // Waluta zawsze powinna się stackować!
    }
}
