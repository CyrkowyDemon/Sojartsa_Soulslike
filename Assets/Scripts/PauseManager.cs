using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Unity.Cinemachine;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [Header("UI Elementy")]
    public GameObject pauseCanvas;
    
    [Tooltip("Przycisk, który podświetli się pierwszy po włączeniu pauzy")]
    public GameObject firstSelectedButton; 

    [Header("Input")]
    [SerializeField] private InputReader inputReader;

    [Header("Panele")]
    public GameObject pauseMainPanel; 
    public GameObject settingsPanel;
    public GameObject keybindsPanel;
    public GameObject inventoryPanel;
    public GameObject enchantPanel;

    [Header("Podświetlenia (Focus)")]
    public GameObject firstSettingsButton; 
    public GameObject settingsButtonInPause;
    public GameObject firstInventoryButton;
    public GameObject inventoryButtonInPause;
    public GameObject firstEnchantButton;
    public GameObject enchantButtonInPause;

    [Header("Kamera - Zabezpieczenie")]
    [Tooltip("Przeciągnij tu obiekt kamery z komponentem Cinemachine Input Axis Controller")]
    public CinemachineInputAxisController cameraInputController;

    private CinemachineBrain _brain;
    private bool isPaused = false;
    private int _lastToggleFrame = -1; 

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.MainMenuEvent += TogglePause;
            inputReader.CancelEvent += HandleCancel;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.MainMenuEvent -= TogglePause;
            inputReader.CancelEvent -= HandleCancel;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetUI();
    }

    private void Start()
    {
        _brain = Camera.main?.GetComponent<CinemachineBrain>();
        ResetUI();
    }

    private void ResetUI()
    {
        Debug.Log("[PauseManager] ResetUI wywołane!");
        isPaused = false;
        SetPanelState(pauseCanvas, false);
        CloseAllPanels();
        if (inputReader != null) inputReader.UnlockAllInput();
    }

    /// <summary>
    /// Wyłącza wszystkie panele menu (Settings, Inventory, Keybinds itp.)
    /// </summary>
    private void CloseAllPanels()
    {
        SetPanelState(pauseMainPanel, false);
        SetPanelState(settingsPanel, false);
        SetPanelState(keybindsPanel, false);
        SetPanelState(inventoryPanel, false);
        SetPanelState(enchantPanel, false);
    }

    private void SetPanelState(GameObject panel, bool isActive)
    {
        if (panel == null) return;
        Debug.Log($"[PauseManager] SetPanelState dla {panel.name} na {isActive}. Klatka: {Time.frameCount}");

        UIFadeHelper fader = panel.GetComponent<UIFadeHelper>();
        if (fader != null)
        {
            if (isActive) fader.FadeIn();
            else fader.FadeOut();
        }
        else
        {
            Debug.Log($"[PauseManager] {panel.name} nie ma Fadera - robię SetActive({isActive})");
            panel.SetActive(isActive);
        }
    }

    public void TogglePause()
    {
        Debug.Log($"[PauseManager] TogglePause wywołane! Obecny stan isPaused: {isPaused}");
        if (!isPaused)
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.dialoguePanel != null && DialogueManager.Instance.dialoguePanel.activeInHierarchy) 
            {
                Debug.Log("[PauseManager] Blokada: DialoguePanel jest aktywny!");
                return;
            }
            if (NPCMenuUI.Instance != null && NPCMenuUI.Instance.menuPanel != null && NPCMenuUI.Instance.menuPanel.activeInHierarchy) 
            {
                Debug.Log("[PauseManager] Blokada: NPCMenuUI jest aktywny!");
                return;
            }
        }

        if (Time.frameCount == _lastToggleFrame) 
        {
            Debug.Log($"[PauseManager] BLOKADA KLATKI: Próba wywołania w tej samej klatce ({Time.frameCount}). Ignoruję.");
            return;
        }
        _lastToggleFrame = Time.frameCount;

        isPaused = !isPaused;
        Debug.Log($"[PauseManager] PRZEŁĄCZAM STAN NA: {isPaused}");
        
        if (pauseCanvas != null)
        {
            if (isPaused)
            {
                Debug.Log("[PauseManager] Wykonuję: WŁĄCZANIE (SetPanelState True)");
                SetPanelState(pauseCanvas, true);
                CloseAllPanels();
                SetPanelState(pauseMainPanel, true);
            }
            else
            {
                Debug.Log("[PauseManager] Wykonuję: WYŁĄCZANIE (SetPanelState False)");
                SetPanelState(pauseCanvas, false);
            }
        }

        if (isPaused)
        {
            Time.timeScale = 1f; 
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (inputReader != null) inputReader.SetPauseMenuState(); 
            if (cameraInputController != null) cameraInputController.enabled = false;
            SelectButton(firstSelectedButton);
        }
        else
        {
            Debug.Log("[PauseManager] Przywracam stan gry (InputReader Unlock)");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (inputReader != null) inputReader.UnlockAllInput(); 
            if (cameraInputController != null) cameraInputController.enabled = true;
        }
    }

    public void OpenSettings()
    {
        CloseAllPanels();
        SetPanelState(settingsPanel, true);
        if (inputReader != null) inputReader.SetSettingsMenuState();
        SelectButton(firstSettingsButton);
    }

    public void CloseSettings()
    {
        CloseAllPanels();
        SetPanelState(pauseMainPanel, true);
        if (inputReader != null) inputReader.SetPauseMenuState();
        SelectButton(settingsButtonInPause);
    }

    public void OpenInventory()
    {
        CloseAllPanels();
        SetPanelState(inventoryPanel, true);
        if (inputReader != null) inputReader.SetPauseMenuState();
        SelectButton(firstInventoryButton);
    }

    public void CloseInventory()
    {
        CloseAllPanels();
        SetPanelState(pauseMainPanel, true);
        if (inputReader != null) inputReader.SetPauseMenuState();
        SelectButton(inventoryButtonInPause);
    }

    public void OpenEnchant()
    {
        CloseAllPanels();
        SetPanelState(enchantPanel, true);
        if (inputReader != null) inputReader.SetPauseMenuState();
        SelectButton(firstEnchantButton);
    }

    public void CloseEnchant()
    {
        CloseAllPanels();
        SetPanelState(pauseMainPanel, true);
        if (inputReader != null) inputReader.SetPauseMenuState();
        SelectButton(enchantButtonInPause);
    }

    private void HandleCancel()
    {
        if (!isPaused) return;

        // Hierarchia Cancel: Keybinds -> Settings/Inventory -> Pauza
        if (keybindsPanel != null && keybindsPanel.activeSelf)
        {
            SetPanelState(keybindsPanel, false);
            SetPanelState(settingsPanel, true);
            SelectButton(firstSettingsButton);
            return;
        }

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
        }
        else if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            CloseInventory();
        }
        else if (enchantPanel != null && enchantPanel.activeSelf)
        {
            CloseEnchant();
        }
        else
        {
            TogglePause();
        }
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; 
        if (inputReader != null) inputReader.UnlockAllInput(); 
        SceneManager.LoadScene(0); 
    }

   public void SelectButton(GameObject button)
{
    // 1. Sprawdzamy, czy obiekt jest aktywny, żeby nie wywalić błędu
    if (button == null || !gameObject.activeInHierarchy) return;

    // 2. Odpalamy Coroutine, żeby dać Unity jedną klatkę przerwy
    StartCoroutine(SelectRoutine(button));
}

private IEnumerator SelectRoutine(GameObject button)
{
    // Czyścimy zaznaczenie całkowicie
    if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    
    // Czekamy do końca klatki - to pozwala myszce "odświeżyć" swój stan
    yield return new WaitForEndOfFrame();

    // Sprawdzamy: jeśli gracz poruszył myszką, EventSystem sam coś "scoveruje". 
    // Jeśli nic nie jest podświetlone myszką, wtedy ustawiamy focus dla pada.
    if (EventSystem.current != null)
    {
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(button);
        }
    }
}
}