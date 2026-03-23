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
    public GameObject firstSettingsButton; 
    public GameObject settingsButtonInPause;

    private CinemachineBrain _brain;
    private bool isPaused = false;

    private void OnEnable()
    {
        if (inputReader != null) inputReader.MainMenuEvent += TogglePause;
    }

    private void OnDisable()
    {
        if (inputReader != null) inputReader.MainMenuEvent -= TogglePause;
    }

    void Start()
    {
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        _brain = Camera.main?.GetComponent<CinemachineBrain>();
    }

    public void TogglePause()
    {
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
            // FromSoftware style: Time does NOT stop.
            Time.timeScale = 1f; 
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            inputReader.EnableUI(); 
            SelectButton(firstSelectedButton);

            // FromSoftware style: Camera still tracks in background, we just don't feed it new input.
            if (_brain != null) _brain.enabled = true;
        }
        else
        {
            Time.timeScale = 1f; 
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            inputReader.EnableGameplay(); 
            EventSystem.current.SetSelectedGameObject(null);

            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (_brain != null) _brain.enabled = true;
        }
    }

    public void OpenSettings()
    {
        if (pauseMainPanel != null) pauseMainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);

        SelectButton(firstSettingsButton);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseMainPanel != null) pauseMainPanel.SetActive(true);

        SelectButton(settingsButtonInPause);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(0); 
    }

    // --- SYSTEM BEZPIECZNEGO ZAZNACZANIA ---
    public void SelectButton(GameObject button)
    {
        if (button != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(SelectRoutine(button));
        }
    }

    private IEnumerator SelectRoutine(GameObject button)
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return null; 
        EventSystem.current.SetSelectedGameObject(button);
    }
}