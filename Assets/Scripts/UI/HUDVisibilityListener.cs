using UnityEngine;

/// <summary>
/// Uniwersalny skrypt nasłuchujący zmian widoczności HUD.
/// Dodaj ten komponent do dowolnego obiektu UI (np. tła monet, okienka enchantu),
/// który ma być automatycznie ukrywany/pokazywany na podstawie opcji w grze.
/// </summary>
public class HUDVisibilityListener : MonoBehaviour
{
    [Tooltip("Opcjonalnie: CanvasGroup tego obiektu. Jeśli jest przypisany, będziemy zmieniać jego Alpha. Jeśli nie, po prostu włączymy/wyłączymy cały GameObject.")]
    [SerializeField] private CanvasGroup canvasGroup;

    private void OnEnable()
    {
        // Subskrybujemy się na zmiany w ustawieniach
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsUpdated += UpdateVisibility;
        }
        
        // Aktualizujemy widoczność przy aktywacji obiektu
        UpdateVisibility();
    }

    private void OnDisable()
    {
        // Odpinamy się, żeby nie zaśmiecać pamięci
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsUpdated -= UpdateVisibility;
        }
    }

    private void Start()
    {
        // Zabezpieczenie na starcie sceny
        UpdateVisibility();
    }

    public void UpdateVisibility()
    {
        if (SettingsManager.Instance == null) return;
        
        bool show = SettingsManager.Instance.showHUD;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = show ? 1f : 0f;
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;
        }
        else
        {
            gameObject.SetActive(show);
        }
    }
}
