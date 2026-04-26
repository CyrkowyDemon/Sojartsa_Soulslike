using UnityEngine;
using TMPro;
using Sojartsa.Inventory;

namespace Sojartsa.Inventory.UI
{
    /// <summary>
    /// Zarządza wyświetlaniem głównych statystyk gracza (Vitality, Attack Power).
    /// Pokazuje bazowe wartości oraz bonusy z wybranego przedmiotu.
    /// </summary>
    public class PlayerStatsWindowUI : MonoBehaviour
    {
        public static PlayerStatsWindowUI Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI statsText;
        
        private ItemData _currentlyPreviewedItem;

        private void Awake()
        {
            // Celowo puste - usunęliśmy Destroy(gameObject), 
            // żeby kopie okna w innych panelach nie kasowały się nawzajem!
        }

        private void OnEnable()
        {
            // Trik "Przekazania Korony":
            // Kiedy ten panel (Inventory lub Enchant) się włącza, to JEGO okno statystyk zostaje "Królem".
            Instance = this;

            // Przy każdym włączeniu okna (np. otwarcie pauzy/ekwipunku) odświeżamy staty
            UpdateStats();

            // Słuchamy zmian w ekwipunku
            if (InventoryController.Instance != null)
            {
                InventoryController.Instance.OnEquipmentChanged -= UpdateStats; // Zabezpieczenie przed dublowaniem
                InventoryController.Instance.OnEquipmentChanged += UpdateStats;
            }
        }

        private void OnDisable()
        {
            if (InventoryController.Instance != null)
                InventoryController.Instance.OnEquipmentChanged -= UpdateStats;
        }

        /// <summary>
        /// Wywołuje podgląd statystyk dla przedmiotu, na który najechaliśmy/kliknęliśmy.
        /// </summary>
        public void PreviewItemBonus(ItemData item)
        {
            // _currentlyPreviewedItem = item; // Na razie wyłączone, skupiamy się na TOTAL
            UpdateStats();
        }

        public void UpdateStats()
        {
            if (PlayerStats.Instance == null || statsText == null) return;

            // Pobieramy sumę (Baza + Ekwipunek)
            int totalVit = PlayerStats.Instance.GetTotalVitality();
            int totalAtk = PlayerStats.Instance.GetTotalAttackPower();

            if (playerNameText != null)
                playerNameText.text = PlayerStats.Instance.playerName;

            string vitString = $"Vitality            {totalVit}";
            string atkString = $"AttackPower  {totalAtk}";

            statsText.text = $"{vitString}\n{atkString}";
        }
    }
}
