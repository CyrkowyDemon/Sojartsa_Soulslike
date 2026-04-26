using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Dynamiczne menu Hub dla NPC. Generuje przyciski na podstawie tego, co NPC potrafi.
/// Działa jak menu przy Ognisku lub u kowala w Soulsach.
/// </summary>
public class NPCMenuUI : MonoBehaviour
{
    public static NPCMenuUI Instance { get; private set; }

    [Header("Referencje UI")]
    public GameObject menuPanel; // Całe okienko (tło)
    public Transform buttonsParent; // Miejsce, gdzie pojawiają się przyciski (Vertical Layout Group)
    public GameObject buttonPrefab; // Prefab przycisku

    [Header("Referencje Pomocnicze")]
    public TMP_Text npcNameText; // Opcjonalnie: Nagłówek z imieniem NPC

    private PeacefulNPC _currentNPC;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (menuPanel != null) menuPanel.SetActive(false);
    }

    /// <summary>
    /// Otwiera Główne Menu dla danego NPC.
    /// </summary>
    public void OpenMainMenu(PeacefulNPC npc)
    {
        Debug.Log("[NPCMenuUI] 1. OpenMainMenu zostało wywołane!");
        _currentNPC = npc;
        if (npcNameText != null) npcNameText.text = npc.name; // Zastąp potem zmienną npcName jeśli dodasz
        
        if (menuPanel != null) 
        {
            menuPanel.SetActive(true);
            Debug.Log("[NPCMenuUI] 2. Włączyłem MenuPanel!");
        }
        else Debug.LogError("[NPCMenuUI] Nie przypisałeś MenuPanel w inspektorze!");

        ClearButtons();

        // 1. Opcja: Porozmawiaj (Dodajemy tylko jeśli NPC ma w ogóle jakieś opcjonalne dialogi)
        List<DialogueConversation> availableTalks = npc.GetAvailableTalkConversations();
        if (availableTalks.Count > 0)
        {
            CreateButton("Porozmawiaj", () => OpenTalkMenu(availableTalks));
        }

        // 2. Opcja: Sklep (TODO: Dodamy warunek gdy NPC dostanie ShopData)
        // CreateButton("Sklep", () => OpenShop());

        // 3. Opcja: Odejdź (Zawsze na samym dole)
        Debug.Log("[NPCMenuUI] 3. Tworzę przycisk Odejdź...");
        CreateButton("Odejdź", CloseMenu);

        SelectFirstButton();
        Debug.Log("[NPCMenuUI] 4. Zakończono generowanie menu!");
    }

    /// <summary>
    /// Zmienia widok na listę dostępnych tematów rozmów.
    /// </summary>
    private void OpenTalkMenu(List<DialogueConversation> talks)
    {
        ClearButtons();

        // Generujemy przycisk dla każdego dostępnego tematu
        foreach (var conv in talks)
        {
            // Możesz w przyszłości dodać "Topic Name" do DialogueConversation. Na razie użyjemy nazwy pliku.
            string topicName = conv.name; 
            CreateButton(topicName, () => StartOptionalDialogue(conv));
        }

        // Zawsze dodajemy przycisk Wstecz
        CreateButton("Wstecz", () => OpenMainMenu(_currentNPC));

        SelectFirstButton();
    }

    private void StartOptionalDialogue(DialogueConversation conv)
    {
        menuPanel.SetActive(false); // Chowamy menu na czas rozmowy
        
        // Zmieniamy stan NPC z Menu na Rozmowę, podając mu konkretny dialog
        _currentNPC.SwitchState(_currentNPC.DialogueState);
        
        // UWAGA: Musimy podać DialogueManagerowi ten konkretny plik, bo to opcja z menu!
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartConversation(conv, _currentNPC);
        }
    }

    public void CloseMenu()
    {
        menuPanel.SetActive(false);
        if (_currentNPC != null)
        {
            _currentNPC.SwitchState(_currentNPC.IdleState);
            _currentNPC = null;
        }
    }

    // ====================================================================
    // FUNKCJE POMOCNICZE DO GENEROWANIA PRZYCISKÓW
    // ====================================================================

    private void CreateButton(string text, UnityEngine.Events.UnityAction onClickAction)
    {
        Debug.Log($"[NPCMenuUI] Próba stworzenia przycisku: {text}");
        if (buttonPrefab == null || buttonsParent == null)
        {
            Debug.LogError($"[NPCMenuUI] BŁĄD! Nie przypisałeś ButtonPrefab albo ButtonsParent w Inspektorze! Nie mogę stworzyć guzika: {text}");
            return;
        }

        GameObject btnObj = Instantiate(buttonPrefab, buttonsParent);
        Debug.Log($"[NPCMenuUI] Stworzono obiekt przycisku: {btnObj.name} wewnątrz {buttonsParent.name}");
        
        // Zmieniamy tekst (szukamy TextMeshPro w dziecku)
        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        if (btnText != null) btnText.text = text;

        // Podpinamy funkcję
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(onClickAction);
        }
    }

    private void ClearButtons()
    {
        foreach (Transform child in buttonsParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void SelectFirstButton()
    {
        // Auto-fokus dla padów/klawiatury
        if (buttonsParent.childCount > 0 && UnityEngine.EventSystems.EventSystem.current != null)
        {
            // Oczekujemy klatkę, żeby przyciski zdążyły się w pełni stworzyć
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(buttonsParent.GetChild(0).gameObject);
        }
    }
}
