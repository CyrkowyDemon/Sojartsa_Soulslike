using UnityEngine;
using TMPro;
using Sojartsa.Inventory;
using Sojartsa.Localization;

namespace Sojartsa.Inventory.UI
{
    /// <summary>
    /// Zarządza wyświetlaniem szczegółowych informacji o przedmiocie w UI.
    /// </summary>
    public class ItemDetailsUI : MonoBehaviour
    {
        public static ItemDetailsUI Instance { get; private set; }

        [Header("Elementy UI (TextMeshe)")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI loreText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI priceText;

        [Header("Ustawienia")]
        [SerializeField] private GameObject contentPanel; // Panel, który możemy ukryć gdy nic nie jest wybrane

        private void Awake()
        {
            // Puste! Nie niszczymy klonów w innych panelach.
        }

        private void OnEnable()
        {
            // Trik "Przekazania Korony"
            Instance = this;
            ResetDetails(); // Zawsze czyścimy opisy po otwarciu panelu
        }

        /// <summary>
        /// Wyświetla informacje o konkretnym przedmiocie.
        /// </summary>
        public void ShowItemDetails(ItemData item)
        {
            if (item == null)
            {
                ResetDetails();
                return;
            }

            // 1. Tytuł
            if (titleText != null) 
                titleText.text = item.GetLocalizedName();

            // 2. Typ + Lore (Połączone)
            if (loreText != null) 
            {
                string typeName = item.type.ToString();
                string lore = item.GetLocalizedDescription();
                loreText.text = $"{typeName}\n\n{lore}";
            }

            // 3. Statystyki (DMG + Efekty Specjalne)
            if (statsText != null)
                statsText.text = GenerateStatsString(item);

            // 4. Cena (Format: <kupno> | <skup>)
            if (priceText != null)
            {
                priceText.text = $"{item.buyValue} | {item.sellValue}";
            }
        }

        public void ResetDetails()
        {
            if (titleText != null) titleText.text = "---";
            if (loreText != null) loreText.text = "---\n\n---";
            if (statsText != null) statsText.text = "---";
            if (priceText != null) priceText.text = "0 | 0";
        }

        private string GenerateStatsString(ItemData item)
        {
            // Wyświetlamy TYLKO to, co gracz sam wpisze w extraStats (przez lokalizację)
            if (!string.IsNullOrEmpty(item.extraStats) && LocalizationManager.Instance != null)
            {
                return LocalizationManager.Instance.GetText(item.extraStats);
            }

            return "---";
        }
    }
}
