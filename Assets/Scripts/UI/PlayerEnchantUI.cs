using UnityEngine;
using UnityEngine.UI;

public class PlayerEnchantUI : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField] private PlayerEnchantController enchantController;
    [SerializeField] private Image papyrusBackground;
    [SerializeField] private Image orbIcon;
    
    [Header("Ustawienia Wizualne")]
    [SerializeField] private Material uiMaterialBase;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private float pulseSpeed = 2f;
    
    private Material _papyrusInstance;
    private Material _orbInstance;

    private void Start()
    {
        if (enchantController == null)
            enchantController = Object.FindFirstObjectByType<PlayerEnchantController>();

        // Tworzymy unikalne kopie materiału, żeby każdy obrazek mógł mieć inny stopień szarości
        if (uiMaterialBase != null)
        {
            _papyrusInstance = new Material(uiMaterialBase);
            _orbInstance = new Material(uiMaterialBase);
            
            papyrusBackground.material = _papyrusInstance;
            orbIcon.material = _orbInstance;
        }
    }

    private void Update()
    {
        if (enchantController == null) return;

        EnchantData currentData = enchantController.GetCurrentEnchant();
        bool hasWeapon = enchantController.HasWeapon();
        bool isActive = enchantController.IsActive();
        float cooldown = enchantController.GetCooldownNormalized();
        float duration = enchantController.GetDurationNormalized();

        // 1. Ikonka Orba (Włączamy/Wyłączamy cały obiekt)
        if (currentData != null)
        {
            if (!orbIcon.gameObject.activeSelf) orbIcon.gameObject.SetActive(true);
            orbIcon.sprite = currentData.icon;
        }
        else
        {
            if (orbIcon.gameObject.activeSelf) orbIcon.gameObject.SetActive(false);
        }

        // 2. Sterowanie Shaderem (Grayscale)
        if (_papyrusInstance != null && _orbInstance != null)
        {
            float papyrusTargetGray = (hasWeapon && currentData != null) ? 0f : 1f;
            _papyrusInstance.SetFloat("_GrayscaleAmount", Mathf.Lerp(_papyrusInstance.GetFloat("_GrayscaleAmount"), papyrusTargetGray, Time.deltaTime * 5f));

            float orbTargetGray = (hasWeapon && (cooldown >= 0.99f || isActive)) ? 0f : 1f;
            _orbInstance.SetFloat("_GrayscaleAmount", Mathf.Lerp(_orbInstance.GetFloat("_GrayscaleAmount"), orbTargetGray, Time.deltaTime * 5f));
        }

        // 3. Efekt Butelki i Pulsowanie
        bool isReady = hasWeapon && currentData != null && cooldown >= 0.99f && !isActive;

        if (isActive)
        {
            orbIcon.fillAmount = duration;
            float pulse = (Mathf.Sin(Time.time * pulseSpeed * 2) + 1f) / 2f;
            orbIcon.color = Color.Lerp(normalColor, Color.red, pulse); 
        }
        else
        {
            // WAŻNE: Tu ustawiamy wypełnienie cooldownu
            orbIcon.fillAmount = cooldown;
            orbIcon.color = normalColor; 

            if (isReady)
            {
                float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
                orbIcon.transform.localScale = Vector3.one * (1f + pulse * 0.1f);
            }
            else
            {
                orbIcon.transform.localScale = Vector3.one;
            }
        }
    }
}
