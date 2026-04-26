using UnityEngine;

/// <summary>
/// Mózg statystyk gracza (Souls-Style).
/// Trzyma bazowe wartości i oblicza statystyki pochodne (np. HP z Vitality).
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Główne Atrybuty")]
    public string playerName = "Zenek"; // Imię postaci (można zmieniać w inspektorze)
    public int vitality = 10;     // Wpływa na Max HP
    public int attackPower = 10;   // Wpływa na Obrażenia

    [Header("Mnożniki (Balans)")]
    [SerializeField] private int hpPerVitality = 10;
    [SerializeField] private float damageMultiplierPerPoint = 2f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Pobiera całkowite Vitality (Baza + Ekwipunek).
    /// </summary>
    public int GetTotalVitality()
    {
        int total = vitality;
        if (InventoryController.Instance != null)
        {
            foreach (var slot in InventoryController.Instance.equipmentSlots)
            {
                if (!slot.IsEmpty) total += slot.item.bonusVitality;
            }
        }
        return total;
    }

    /// <summary>
    /// Pobiera całkowitą Siłę Ataku (Baza + Ekwipunek).
    /// </summary>
    public int GetTotalAttackPower()
    {
        int total = attackPower;
        if (InventoryController.Instance != null)
        {
            foreach (var slot in InventoryController.Instance.equipmentSlots)
            {
                if (!slot.IsEmpty) total += slot.item.bonusAttackPower;
            }
        }
        return total;
    }

    /// <summary>
    /// Oblicza maksymalne życie na podstawie całkowitego Vitality.
    /// </summary>
    public int GetMaxHealth()
    {
        return GetTotalVitality() * hpPerVitality;
    }

    /// <summary>
    /// Oblicza finalne obrażenia na podstawie całkowitej siły.
    /// Wzór: Total Attack Power * 3
    /// </summary>
    public int GetTotalDamage()
    {
        return GetTotalAttackPower() * 3;
    }
}
