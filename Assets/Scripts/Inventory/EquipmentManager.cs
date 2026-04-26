using UnityEngine;
using Sojartsa.Inventory;

/// <summary>
/// Zarządza tym, co gracz ma aktualnie "na sobie" (w rękach i w slocie użytkowym).
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [Header("Aktualnie Wyposażone")]
    public WeaponData currentMainHand;
    public OtherItemData currentOffHand;
    public ConsumableData currentConsumable;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnEquipmentChanged += RefreshFromSlots;
        }
    }

    private void OnDisable()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnEquipmentChanged -= RefreshFromSlots;
        }
    }

    [Header("Wizualia (Sockety)")]
    public Transform mainHandSocket;
    public Transform offHandSocket; // NOWOŚĆ: Socket dla lewej ręki
    private GameObject _currentWeaponModel;
    private GameObject _currentOffHandModel; // Model tarczy/pochodni

    /// <summary>
    /// Pobiera dane bezpośrednio ze slotów InventoryController i aktualizuje wizualia oraz statystyki.
    /// </summary>
    public void RefreshFromSlots()
    {
        InventoryController inv = InventoryController.Instance;
        if (inv == null || inv.equipmentSlots.Count < 3) return;

        // 1. Aktualizacja Main Hand (Broń)
        WeaponData newWeapon = inv.equipmentSlots[0].item as WeaponData;
        if (newWeapon != currentMainHand)
        {
            UpdateMainHandVisuals(newWeapon);
        }

        // 2. Aktualizacja Off Hand (Tarcza/Pochodnia)
        OtherItemData newOffHand = inv.equipmentSlots[1].item as OtherItemData;
        if (newOffHand != currentOffHand)
        {
            UpdateOffHandVisuals(newOffHand);
        }

        // 3. Aktualizacja Consumable
        currentConsumable = inv.equipmentSlots[2].item as ConsumableData;

        Debug.Log("<color=green>[EQUIP] Statystyki i wizualia zsynchronizowane.</color>");
    }

    private void UpdateMainHandVisuals(WeaponData weapon)
    {
        if (_currentWeaponModel != null) Destroy(_currentWeaponModel);
        currentMainHand = weapon;

        if (weapon != null && weapon.weaponPrefab != null && mainHandSocket != null)
        {
            _currentWeaponModel = Instantiate(weapon.weaponPrefab, mainHandSocket);
            _currentWeaponModel.transform.localPosition = Vector3.zero;
            _currentWeaponModel.transform.localRotation = Quaternion.identity;
        }
    }

    private void UpdateOffHandVisuals(OtherItemData other)
    {
        if (_currentOffHandModel != null) Destroy(_currentOffHandModel);
        currentOffHand = other;

        // Zakładamy, że OtherItemData też może mieć prefab (np. model tarczy)
        // Jeśli nie ma, po prostu ignorujemy
        // Tutaj można dodać pole .itemPrefab do OtherItemData w przyszłości
    }

    /// <summary>
    /// Pobiera całkowite obrażenia gracza (Baza * 3).
    /// </summary>
    public int GetCurrentAttackDamage()
    {
        if (PlayerStats.Instance != null)
        {
            return PlayerStats.Instance.GetTotalDamage();
        }

        return 5; // Failsafe: Pięść
    }
}
