using UnityEngine;
using FMODUnity;

public class PlayerEnchantController : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private PlayerCombat playerCombat;
    
    [Header("Tymczasowe (do testów, potem z Inventory)")]
    [Tooltip("Póki co wrzuć tu swoją Magiczną Kulę, żeby przetestować kod bez Ekwipunku.")]
    [SerializeField] private EnchantData testEquippedEnchant;

    // Stan
    private EnchantData currentActiveEnchant;
    private float currentDurationTimer;
    private float currentCooldownTimer;
    private bool isEnchantActive;
    private WeaponEnchantVisuals _visuals;
    
    private void Awake()
    {
        if (playerCombat == null) playerCombat = GetComponentInParent<PlayerCombat>();
    }

    private void OnEnable()
    {
        if (inputReader != null)
            inputReader.UseEnchantEvent += HandleUseEnchant;
    }

    private void OnDisable()
    {
        if (inputReader != null)
            inputReader.UseEnchantEvent -= HandleUseEnchant;
    }

    private EnchantData _lastKnownEnchant;

    private void Update()
    {
        // 1. Detekcja zmiany Orba w slocie (Nawet pierwsze założenie odpala cooldown)
        EnchantData currentEquipped = GetEquippedEnchant();
        if (currentEquipped != _lastKnownEnchant)
        {
            // Jeśli założyliśmy nową kulę (niezależnie czy wcześniej coś było)
            if (currentEquipped != null)
            {
                Debug.Log("<color=orange>[ENCHANT] Założono nową kulę! Ładowanie...</color>");
                DeactivateEnchant();
                currentCooldownTimer = currentEquipped.cooldown;
            }
            else
            {
                DeactivateEnchant();
                currentCooldownTimer = 0;
            }
            _lastKnownEnchant = currentEquipped;
        }

        // 2. OPCJA A: Jeśli broń zniknie (Missing Reference), gasimy enchant
        if (isEnchantActive && _visuals == null)
        {
            Debug.Log("<color=red>[ENCHANT] Broń zniknęła! Efekt przerwany.</color>");
            DeactivateEnchant();
        }

        // 3. Obsługa czasu trwania
        if (isEnchantActive)
        {
            currentDurationTimer -= Time.deltaTime;
            if (currentDurationTimer <= 0) DeactivateEnchant();
        }
        else // 4. Obsługa cooldownu
        {
            if (currentCooldownTimer > 0) currentCooldownTimer -= Time.deltaTime;
        }
    }

    private void HandleUseEnchant()
    {
        // 1. Zabezpieczenia: Czy mamy broń? Czy skill się ładuje?
        if (_visuals == null)
        {
            Debug.Log("<color=yellow>[ENCHANT] Nie możesz użyć magii bez broni w ręku!</color>");
            return;
        }

        if (currentCooldownTimer > 0 || isEnchantActive)
        {
            Debug.Log("<color=red>Enchant niedostępny:</color> Czeka na Cooldown albo już trwa!");
            return;
        }

        EnchantData enchantToUse = GetEquippedEnchant();
        if (enchantToUse == null) return;

        currentActiveEnchant = enchantToUse;

        if (playerCombat != null && playerCombat.RequestEnchantAction())
        {
            // Cooldownu tu nie odpalamy! Odpalimy go w DeactivateEnchant().
            Debug.Log($"<color=lime>Rozpoczęto rzucanie Enchantu:</color> {enchantToUse.name}");
        }
        else
        {
            currentActiveEnchant = null;
        }
    }

    public void ActivateEnchantEffects()
    {
        if (currentActiveEnchant == null) return;

        isEnchantActive = true;
        currentDurationTimer = currentActiveEnchant.duration;

        if (_visuals != null)
        {
            _visuals.ActivateVisuals(currentActiveEnchant);
        }

        // DODANO: Dźwięk startowy (One Shot)
        if (!currentActiveEnchant.startSound.IsNull)
        {
            RuntimeManager.PlayOneShot(currentActiveEnchant.startSound, transform.position);
        }

        Debug.Log($"<color=orange>Ogień na mieczu!</color> Potrwa {currentDurationTimer} sekund.");
    }

    private void DeactivateEnchant()
    {
        bool wasActive = isEnchantActive;
        isEnchantActive = false;
        
        if (_visuals != null)
        {
            _visuals.DeactivateVisuals();
        }

        // Startujemy cooldown dopiero po zgaśnięciu efektu
        if (wasActive && currentActiveEnchant != null)
        {
            currentCooldownTimer = currentActiveEnchant.cooldown;
            Debug.Log($"<color=gray>Enchant zgasł.</color> Start cooldownu: {currentCooldownTimer}s.");
        }

        currentActiveEnchant = null;
    }

    private EnchantData GetEquippedEnchant()
    {
        // Pytamy InventoryController o slot nr 3 (tam są Enchanty)
        if (InventoryController.Instance != null && InventoryController.Instance.equipmentSlots.Count > 3)
        {
            var slot = InventoryController.Instance.equipmentSlots[3];
            return slot.item as EnchantData;
        }
        
        return testEquippedEnchant;
    }

    // Właściwości dla UI (zwracają wartości od 0 do 1)
    public float GetDurationNormalized() => (isEnchantActive && currentActiveEnchant != null && currentActiveEnchant.duration > 0) ? (currentDurationTimer / currentActiveEnchant.duration) : 0f;
    public float GetCooldownNormalized() => (_lastKnownEnchant != null && _lastKnownEnchant.cooldown > 0) ? (1f - (currentCooldownTimer / _lastKnownEnchant.cooldown)) : 0f;
    public bool IsActive() => isEnchantActive;
    public EnchantData GetCurrentEnchant() => GetEquippedEnchant(); 

    public bool HasWeapon()
    {
        // 1. Sprawdzamy czy fizycznie mamy miecz w ręku (bo wizualizacje muszą mieć na czym grać)
        if (_visuals == null) _visuals = GetComponentInChildren<WeaponEnchantVisuals>();
        if (_visuals != null) return true;

        // 2. Failsafe: Jeśli nie mamy modelu, może chociaż w Inventory jest coś w slocie 0?
        if (InventoryController.Instance != null && InventoryController.Instance.equipmentSlots.Count > 0)
        {
            return !InventoryController.Instance.equipmentSlots[0].IsEmpty;
        }

        return false;
    }

    public void SetActiveWeapon(WeaponEnchantVisuals visuals)
    {
        _visuals = visuals;
        if (visuals != null) Debug.Log($"<color=green>[ENCHANT] Zarejestrowano broń: {visuals.gameObject.name}</color>");
        if (isEnchantActive) DeactivateEnchant();
    }
}
