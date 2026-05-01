using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewTradeShop", menuName = "Sojartsa/Shop/Trade Shop Data")]
public class TradeShopData : ScriptableObject
{
    public string shopName = "Handlarz";
    
    [Header("Oferty Sprzedaży")]
    public List<TradeOfferData> offers = new List<TradeOfferData>();

    [Header("Skup (Sprzedaż Gracza)")]
    [Range(0.1f, 2.0f)]
    public float sellRate = 0.5f; // Ile % wartości bazowej płaci NPC (np. 0.5 = 50%)
}
