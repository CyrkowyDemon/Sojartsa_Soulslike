using UnityEngine;
using TMPro;

namespace Sojartsa.Inventory.UI
{
    /// <summary>
    /// Wyświetla aktualne statystyki gracza w panelu ekwipunku.
    /// </summary>
    public class StatsWindowUI : MonoBehaviour
    {
        [Header("Pola Tekstowe (TMP)")]
        [SerializeField] private TextMeshProUGUI vitalityText;
        [SerializeField] private TextMeshProUGUI attackPowerText;
        [SerializeField] private TextMeshProUGUI totalDamageText;

        private void OnEnable()
        {
            UpdateStats();
        }

        /// <summary>
        /// Odświeża wartości tekstowe na podstawie PlayerStats i EquipmentManager.
        /// </summary>
        public void UpdateStats()
        {
            if (PlayerStats.Instance != null)
            {
                if (vitalityText != null) vitalityText.text = "Vitality: " + PlayerStats.Instance.vitality;
                if (attackPowerText != null) attackPowerText.text = "Attack Power: " + PlayerStats.Instance.attackPower;
            }

            if (EquipmentManager.Instance != null)
            {
                if (totalDamageText != null) totalDamageText.text = "Total Damage: " + EquipmentManager.Instance.GetCurrentAttackDamage();
            }
        }
    }
}
