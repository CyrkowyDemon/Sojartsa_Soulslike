using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // Kluczowe dla obsługi EventSystemu

public class MainMenuManager : MonoBehaviour
{
    // Lista poziomów do wyboru w Inspektorze
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
    [Tooltip("Przycisk zaznaczony na samym starcie (dla pada/klawiatury)")]
    public GameObject firstSelectedButton; 

    [Header("Panele UI (Prefaby/Obiekty)")]
    public GameObject mainMenuPanel; 
    public GameObject settingsPanel;

    [Header("Nawigacja dla Pada")]
    [Tooltip("Pierwszy suwak/przycisk w menu ustawień")]
    public GameObject firstSettingsButton; 
    [Tooltip("Przycisk 'Ustawienia' w głównym menu (do niego wrócimy po zamknięciu)")]
    public GameObject settingsButtonInMenu; 

    private void Start()
    {
        // 1. Włączamy mapę sterowania UI
        if (inputReader != null) inputReader.EnableUI();

        // 2. Bezpiecznie zaznaczamy pierwszy przycisk
        SelectButton(firstSelectedButton);
    }

    public void StartGame()
    {
        // Przywracamy sterowanie graczem przed zmianą sceny
        if (inputReader != null) inputReader.EnableGameplay();
        
        Time.timeScale = 1f; 
        SceneManager.LoadScene((int)levelToLoad);
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);

        // Przełączamy focus pada na menu ustawień
        SelectButton(firstSettingsButton);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        // Wracamy focusem na przycisk, który otworzył ustawienia
        SelectButton(settingsButtonInMenu);
    }

    public void QuitGame()
    {
        Debug.Log("Wychodzę z gry!");
        Application.Quit();
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
        // Wyczyszczenie obecnego zaznaczenia zapobiega błędom "Ghost Highlight"
        EventSystem.current.SetSelectedGameObject(null);
        
        // Czekamy jedną klatkę, aż UI przeliczy swoją strukturę po SetActive(true)
        yield return null; 
        
        EventSystem.current.SetSelectedGameObject(button);
    }
}