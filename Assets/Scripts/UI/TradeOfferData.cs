using UnityEngine;

[CreateAssetMenu(fileName = "NewTradeOffer", menuName = "Sojartsa/Shop/Trade Offer")]
public class TradeOfferData : ScriptableObject
{
    [Header("Co Gracz Kupuje")]
    public ItemData resultItem;
    public int resultAmount = 1;

    [Header("Koszt 1 (Główny)")]
    public ItemData costItem1;
    public int costAmount1;

    [Header("Koszt 2 (Opcjonalny)")]
    public ItemData costItem2;
    public int costAmount2;

    [Header("Ustawienia")]
    [Range(0.1f, 5.0f)]
    public float priceMultiplier = 1.0f; // Marża handlarza

    public int GetFinalCost1() => Mathf.CeilToInt(costAmount1 * priceMultiplier);
    public int GetFinalCost2() => Mathf.CeilToInt(costAmount2 * priceMultiplier);
}
