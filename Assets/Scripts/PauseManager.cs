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
    public GameObject keybindsPanel; // NOWE: PauseManager musi wiedziec o tym panelu!
    public GameObject firstSettingsButton; 
    public GameObject settingsButtonInPause;

    [Header("Kamera - Zabezpieczenie")]
    [Tooltip("Przeciągnij tu obiekt kamery z komponentem Cinemachine Input Axis Controller")]
    public CinemachineInputAxisController cameraInputController;

    private CinemachineBrain _brain;
    private bool isPaused = false;
    
    // Zmienna do ignorowania podwójnych sygnałów z ESC w tej samej klatce
    private int _lastToggleFrame = -1; 

    private void Awake()
    {
        Debug.Log($"<color=cyan>[PAUSE] PauseManager budzi się na obiekcie: {gameObject.name} (InstanceID: {gameObject.GetInstanceID()}). Rodzic: {transform.parent?.name}</color>");
    }

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
        // Resetujemy stan przy każdej załadowanej scenie
        ResetUI();
    }

    void Start()
    {
        ResetUI();
        _brain = Camera.main?.GetComponent<CinemachineBrain>();
    }

    private void ResetUI()
    {
        isPaused = false;
        
        // Wyłączamy wszystkie panele na start, żeby nic nie straszyło na ekranie
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (keybindsPanel != null) keybindsPanel.SetActive(false);

        // Odblokowujemy kursor i input (nie ruszamy czasu, zgodnie z Twoją prośbą)
        if (inputReader != null) inputReader.UnlockAllInput();
    }

    public void TogglePause()
    {
        // Magiczne rozwiązanie problemu z ESC: jeśli w tej samej klatce już to wywołaliśmy, zignoruj!
        if (Time.frameCount == _lastToggleFrame) return;
        _lastToggleFrame = Time.frameCount;

        isPaused = !isPaused;
        
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(isPaused);
            if (isPaused)
            {
                if (pauseMainPanel != null) pauseMainPanel.SetActive(true);
                if (settingsPanel != null) settingsPanel.SetActive(false);
            }
        }

        if (isPaused)
        {
            Time.timeScale = 1f; 
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            if (inputReader != null) inputReader.SetPauseMenuState(); 
            
            // WYŁĄCZAMY KOMPONENT KAMERY
            if (cameraInputController != null) cameraInputController.enabled = false;

            SelectButton(firstSelectedButton);
            if (_brain != null) _brain.enabled = true;
        }
        else
        {
            Time.timeScale = 1f; 
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            if (inputReader != null) inputReader.UnlockAllInput(); 
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            // WŁĄCZAMY KAMERĘ Z POWROTEM
            if (cameraInputController != null) cameraInputController.enabled = true;

            // Zamykamy WSZYSTKIE panele przy wyjsciu z pauzy
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (keybindsPanel != null) keybindsPanel.SetActive(false); // NOWE
            if (_brain != null) _brain.enabled = true;
        }
    }

    public void OpenSettings()
    {
        if (pauseMainPanel != null) pauseMainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);

        if (inputReader != null) inputReader.SetSettingsMenuState();
        SelectButton(firstSettingsButton);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseMainPanel != null) pauseMainPanel.SetActive(true);

        if (inputReader != null) inputReader.SetPauseMenuState();
        SelectButton(settingsButtonInPause);
    }

    private void HandleCancel()
    {
        if (!isPaused) return;

        // Hierarchia Cancel: Keybinds -> Settings -> Pauza
        if (keybindsPanel != null && keybindsPanel.activeSelf)
        {
            // Zamykamy keybindy, wracamy do ustawień
            keybindsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
            SelectButton(firstSettingsButton);
            return;
        }

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
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