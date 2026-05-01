namespace Sojartsa.UI.DragDrop
{
    /// <summary>
    /// Uniwersalny "paczka" opisujący CO jest przenoszone i SKĄD.
    /// Żaden element UI nie musi wiedzieć, kim jest nadawca – wystarczy ta paczka.
    /// </summary>
    public class ItemPayload
    {
        /// <summary>Typ źródła – skąd przyszedł przedmiot.</summary>
        public enum SourceType
        {
            Inventory,      // Z głównego plecaka gracza
            Bag,            // Z torby enchantów
            Equipment,      // Z slotu ekwipunku (broń/tarcza/zużywalne/enchant)
            BarterTable,    // Ze stołu barterowego (3x3)
            ShopOffer       // Z oferty sklepu (BarterDisplaySlot)
        }

        public SourceType Source;
        public ItemData Item;
        public int Amount;
        public int SlotIndex;           // Indeks w liście źródłowej (-1 jeśli nie dotyczy)
        public TradeOfferData Offer;    // Tylko dla SourceType.ShopOffer

        public bool IsEmpty => Item == null || Amount <= 0;
    }
}
