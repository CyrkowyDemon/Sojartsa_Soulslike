using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Sojartsa.UI;

/// <summary>
/// Menedżer Głównego Menu. Zajmuje się nawigacją między głównymi panelami.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Ustawienia Poziomu Startowego")]
    public int firstLevelBuildIndex = 2; 

    [Header("UI: Główne Przyciski")]
    public GameObject continueButton;
    public GameObject firstSelectedButton; 

    [Header("UI: Panele")]
    public GameObject mainMenuPanel; 
    public GameObject settingsPanel;
    public GameObject loadGamePanel;
    public GameObject characterNamePanel;

    [Header("UI: Nowa Gra")]
    public TMP_InputField nameInputField;

    [Header("Input")]
    [SerializeField] private InputReader inputReader;

    private void OnEnable()
    {
        if (inputReader != null) inputReader.CancelEvent += HandleCancel;
    }

    private void OnDisable()
    {
        if (inputReader != null) inputReader.CancelEvent -= HandleCancel;
    }

    private void Start()
    {
        // Wymuszamy porządek na starcie
        BackToMainMenu();
        RefreshMenuState();
    }

    public void RefreshMenuState()
    {
        if (continueButton != null)
        {
            bool hasSave = SaveManager.Instance != null && SaveManager.Instance.GetMostRecentSaveSlot() >= 0;
            continueButton.SetActive(hasSave);
        }
    }

    public void OnClick_Continue()
    {
        if (SaveManager.Instance == null) return;

        int slot = SaveManager.Instance.GetMostRecentSaveSlot();
        if (slot < 0) return;

        SaveData meta = SaveManager.Instance.GetSaveMetadata(slot);
        string charName = meta != null ? meta.characterName : "Nieznany Wędrowiec";

        ConfirmationDialog.Instance.Show(
            "KONTYNUUJ",
            $"Wczytać postać \"{charName}\"?",
            () => SaveManager.Instance.LoadGame(slot)
        );
    }

    public void OnClick_NewGame()
    {
        Debug.Log("[MainMenu] Kliknięto 'Nowa Gra'");
        if (characterNamePanel != null)
        {
            SwitchPanel(characterNamePanel);
            if (nameInputField != null) SelectButton(nameInputField.gameObject);
        }
        else
        {
            // Jeśli nie ma panelu imienia, startujemy od razu
            OnClick_StartJourney();
        }
    }

    public void OnClick_OpenLoadPanel()
    {
        SwitchPanel(loadGamePanel);
    }

    public void OnClick_OpenSettings()
    {
        SwitchPanel(settingsPanel);
    }

    public void OnClick_StartJourney()
    {
        string charName = (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text)) 
                          ? nameInputField.text : "Wędrowiec";

        // Pobieramy nazwę sceny z indeksu dla systemu zapisu
        string scenePath = SceneUtility.GetScenePathByBuildIndex(firstLevelBuildIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

        Debug.Log($"[MainMenu] Próba startu nowej gry. Imię: {charName}, Scena: {sceneName} (Index: {firstLevelBuildIndex})");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.StartNewGame(charName, sceneName);
        }
        else
        {
            if (Sojartsa.UI.LoadingScreenManager.Instance != null)
            {
                Sojartsa.UI.LoadingScreenManager.Instance.LoadScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(firstLevelBuildIndex);
            }
        }
    }

    public void SwitchPanel(GameObject targetPanel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (loadGamePanel != null) loadGamePanel.SetActive(false);
        if (characterNamePanel != null) characterNamePanel.SetActive(false);

        if (targetPanel != null) targetPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        SwitchPanel(mainMenuPanel);
        SelectButton(firstSelectedButton);
        RefreshMenuState(); // Odświeżamy widoczność przycisku Continue
    }

    private void HandleCancel()
    {
        // Jeśli jesteśmy w jakimkolwiek podmenu, wracamy do głównego
        if (!mainMenuPanel.activeSelf)
        {
            BackToMainMenu();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void SelectButton(GameObject button)
    {
        if (button != null && gameObject.activeInHierarchy)
            StartCoroutine(SelectRoutine(button));
    }

    private IEnumerator SelectRoutine(GameObject button)
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return null; 
        EventSystem.current.SetSelectedGameObject(button);
    }
}
