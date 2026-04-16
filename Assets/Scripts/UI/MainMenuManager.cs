using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    public enum LevelID 
    {
        MainMenu = 0,
        Tutorial = 1,
        Pojezierza = 2,
    }

    [Header("Ustawienia Poziomu")]
    public LevelID levelToLoad; 

    [Header("Input & Sterowanie")]
    [SerializeField] private InputReader inputReader;
    public GameObject firstSelectedButton; 

    [Header("Panele UI")]
    public GameObject mainMenuPanel; 
    public GameObject settingsPanel;

    [Header("Nawigacja dla Pada")]
    public GameObject firstSettingsButton; 
    public GameObject settingsButtonInMenu; 

    private void OnEnable()
    {
        if (inputReader != null)
            inputReader.CancelEvent += HandleCancel;
    }

    private void OnDisable()
    {
        if (inputReader != null)
            inputReader.CancelEvent -= HandleCancel;
    }

    private void HandleCancel()
    {
        // ESC w Settings → wróć do Main Menu
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
        }
    }

    private void Start()
    {
        SelectButton(firstSelectedButton);
    }

    // --- KLUCZOWA FUNKCJA DLA PRZYCISKÓW ---
    // W Inspektorze w Button -> OnClick wybierz: MainMenuManager -> LoadSpecificLevel
    // I wpisz w okienku numer sceny (np. 1, 2, 3)
    public void LoadSpecificLevel(int sceneIndex)
    {
        if (inputReader != null) 
        {
            // Odkomentuj to, jak naprawisz błędy w InputReaderze:
            // inputReader.EnableGameplay();
        }
        
        Time.timeScale = 1f; 
        SceneManager.LoadScene(sceneIndex);
    }

    // Standardowy start z poziomu wybranego w "levelToLoad" (np. dla przycisku "Graj Dalej")
    public void StartGame()
    {
        LoadSpecificLevel((int)levelToLoad);
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        SelectButton(firstSettingsButton);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        SelectButton(settingsButtonInMenu);
    }

    public void QuitGame()
    {
        Debug.Log("Wychodzę z gry!");
        Application.Quit();
    }

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
