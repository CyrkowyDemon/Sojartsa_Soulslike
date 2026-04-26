using System;
using UnityEngine;

namespace Sojartsa.Inventory
{
    /// <summary>
    /// Klasa reprezentująca pojedynczy slot w ekwipunku (pudełko).
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int amount;

        public InventorySlot()
        {
            item = null;
            amount = 0;
        }

        public InventorySlot(ItemData newItem, int newAmount)
        {
            item = newItem;
            amount = newAmount;
        }

        public bool IsEmpty => item == null || amount <= 0;

        public void Clear()
        {
            item = null;
            amount = 0;
        }

        public void AddAmount(int value)
        {
            amount += value;
        }

        public void RemoveAmount(int value)
        {
            amount -= value;
            if (amount <= 0) Clear();
        }
    }
}
