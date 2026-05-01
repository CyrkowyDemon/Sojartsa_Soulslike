using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewShopData", menuName = "Sojartsa/Shop/Shop Data")]
public class ShopData : ScriptableObject
{
    [System.Serializable]
    public class ShopEntry
    {
        public ItemData item;
        public int priceOverride = -1; // Jeśli -1, używa buyValue z ItemData
        public int stockCount = -1;    // Jeśli -1, towar jest nieskończony
        
        public int GetPrice() 
        {
            return priceOverride > 0 ? priceOverride : (item != null ? item.buyValue : 0);
        }
    }

    public string shopName = "Sklep";
    public List<ShopEntry> itemsForSale = new List<ShopEntry>();
}
