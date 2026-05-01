using UnityEngine;

public enum EnchantType
{
    None,
    Fire,
    Blood,
    Magic,
    Poison,
    Rock,
    Lightning
}

[CreateAssetMenu(fileName = "NewEnchant", menuName = "Sojartsa/Inventory/Enchant Orb")]
public class EnchantData : ItemData
{
    [Header("Magia i Typ Enchantu")]
    public string skillID; // Identyfikator umiejętności, którą odblokowuje ta kula
    public int manaCost = 10;
    public EnchantType enchantType;

    [Header("Statystyki Enchantu (Czas)")]
    public float duration = 30f; // Ile sekund trwa efekt na broni (świecenie)
    public float cooldown = 45f; // Za ile sekund można użyć ponownie

    [Header("Grafika na Broni (Visuals)")]
    [Tooltip("Prefab cząsteczek, który pojawi się na mieczu (w Shape zaznacz 'Mesh')")]
    public GameObject weaponVFXPrefab; 
    
    [Tooltip("Jeśli chcesz podmienić cały Shader traila, wrzuć tu materiał. Jeśli nie - zostaw puste.")]
    public Material overrideTrailMaterial; 
    
    [Tooltip("Kolor domyślnego traila z efektem HDR (Glow)")]
    [ColorUsage(true, true)] 
    public Color trailColor = Color.white;

    private void OnValidate()
    {
        type = ItemType.Enchant; 
        isStackable = false;     // Kule zazwyczaj są unikalne
    }
}
