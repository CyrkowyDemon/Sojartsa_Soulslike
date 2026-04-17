using UnityEngine;
using TMPro;
using System.Collections;
using FMODUnity; 
using FMOD.Studio; 

/// <summary>
/// Menedżer wyświetlający dialogi. Singleton, do którego każdy NPC może wysłać swój "skrypt".
/// Posiada system pisania litera po literze z możliwością "przeklinikania" dla niecierpliwych.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Referencje UI")]
    public GameObject dialoguePanel; // Czarny pasek z dialogiem
    public TMP_Text dialogueText; // Komponent z tekstem

    [Header("Wybory UI")]
    public GameObject choicePanel; // Panel trzymający przyciski
    public Transform choiceParent; // Rodzic dla przycisków (np. Vertical Layout Group)
    public GameObject choiceButtonPrefab; // Prefab przycisku DialogueChoiceButton

    [Header("Ustawienia")]
    [Tooltip("Czas przerwy między kolejnymi literkami - mniejszy = szybciej")]
    public float typingSpeed = 0.03f;
    [Tooltip("Czy dialogi mają się same przełączać?")]
    public bool useAutoAdvance = true;
    [Tooltip("Ile sekund ma wisieć tekst po zakończeniu pisania, zanim sam zniknie/zmieni się? (Używane jako bufor bezpieczeństwa)")]
    public float waitTimeBuffer = 0.5f;
    [Tooltip("Prędkość czytania automatu (znaków na sekundę). Im więcej, tym szybciej dialogi znikają.")]
    public float charsPerSecond = 15f;

    // Dane aktualnej rozmowy
    private DialogueConversation _currentConversation;
    private PeacefulNPC _currentNPC;
    private int _currentNodeIndex = 0;
    
    // Status
    private bool _isTyping = false;
    private bool _isShowingChoices = false;
    private bool _isEnding = false;
    private Coroutine _typingCoroutine;
    private Coroutine _autoAdvanceCoroutine; // NOWE: Odliczanie do następnej kwestii
    
    private EventInstance _voiceInstance; // NASZA INSTANCJA FMOD

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (choicePanel != null) choicePanel.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // PRO FIX: Absolutna gwarancja, że spacja, enter i pad przewiną dialog niezależnie od tego,
        // czy system interakcji widzi NPC, czy nie.
        if (dialoguePanel != null && dialoguePanel.activeInHierarchy)
        {
            bool skipPressed = false;
            
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame || 
                    UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    skipPressed = true;
                }
            }
            
            if (UnityEngine.InputSystem.Gamepad.current != null)
            {
                if (UnityEngine.InputSystem.Gamepad.current.buttonSouth.wasPressedThisFrame || // A na Xbox
                    UnityEngine.InputSystem.Gamepad.current.buttonEast.wasPressedThisFrame)  // B na Xbox
                {
                    skipPressed = true;
                }
            }
            
            if (skipPressed)
            {
                DisplayNextNode();
            }
        }
    }

    public void StartConversation(DialogueConversation conv, PeacefulNPC npc)
    {
        if (conv == null)
        {
            Debug.LogWarning("[Dialogue] Pusto! NPC wraca do stania.");
            npc.SwitchState(npc.IdleState);
            return;
        }

        _currentConversation = conv;
        _currentNPC = npc;
        _currentNodeIndex = 0;
        _isShowingChoices = false;
        Debug.Log($"[Dialogue] StartConversation: {conv.name}");

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (choicePanel != null) choicePanel.SetActive(false); // Chowamy wybory na start
        
        DisplayNextNode(); // Rozpoczęcie natychmiastowe
    }

    public void EndConversation()
    {
        if (_isEnding) return; // BEZPIECZNIK PĘTLI
        _isEnding = true;
        Debug.Log("[Dialogue] EndConversation called.");

        _currentConversation = null;
        _isShowingChoices = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        
        // Zatrzymujemy głos FMOD natychmiastowo przy zakończeniu
        if (_voiceInstance.isValid())
        {
            _voiceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _voiceInstance.release();
        }

        // Kasujemy wszelkie odliczania, żeby dialog nie "wyskoczył" sam po zamknięciu!
        if (_autoAdvanceCoroutine != null) StopCoroutine(_autoAdvanceCoroutine);

        // Poinformuj NPC, że uciąłeś pogawędkę. Zamknie to lock kamery i uwolni gracza.
        if (_currentNPC != null)
        {
            PeacefulNPC temp = _currentNPC;
            _currentNPC = null;
            temp.SwitchState(temp.IdleState);
        }

        _isEnding = false;
    }

    public void DisplayNextNode()
    {
        if (_isShowingChoices) return; // BLOKADA: Nie przeklikuj dialogu, gdy trzeba wybrać opcję!

        // TWARDY SKIP: zatrzymujemy pisanie, ew. autoodliczanie i wyłączamy audio natychmiast
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        if (_autoAdvanceCoroutine != null) StopCoroutine(_autoAdvanceCoroutine);
        _isTyping = false;

        // Natychmiastowe zatrzymanie wokalu z poprzedniego zdania, żeby się nie nakładał
        if (_voiceInstance.isValid())
        {
            _voiceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }

        // Czy są jeszcze jakieś zdania do powiedzenia w tej paczce?
        if (_currentNodeIndex < _currentConversation.nodes.Count)
        {
            DialogueNode node = _currentConversation.nodes[_currentNodeIndex];
            _typingCoroutine = StartCoroutine(TypeSentence(node));
            
            _currentNodeIndex++;
        }
        else // Kwestie się skończyły - sprawdzamy WYBORY
        {
            if (_currentConversation.choices != null && _currentConversation.choices.Count > 0)
            {
                ShowChoices();
            }
            else
            {
                HandleFollowUp();
            }
        }
    }

    private void HandleFollowUp()
    {
        // Opcja dodania list flag/pieczątek NA KONIEC rozmowy
        if (_currentConversation.flagsToSetOnEnd != null)
        {
            foreach (var flag in _currentConversation.flagsToSetOnEnd)
            {
                WorldStateManager.Instance.AddFlag(flag);
            }
        }

        // Opcja automatycznego włączenia kolejnego dialogu (łańcuch)
        if (_currentConversation.nextConversation != null)
        {
            StartConversation(_currentConversation.nextConversation, _currentNPC);
        }
        else
        {
            EndConversation();
        }
    }

    private IEnumerator TypeSentence(DialogueNode node)
    {
        // Zagraj dubbing przez FMOD!
        if (!node.voiceoverEvent.IsNull)
        {
            if (_voiceInstance.isValid())
            {
                _voiceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                _voiceInstance.release();
            }

            _voiceInstance = RuntimeManager.CreateInstance(node.voiceoverEvent);
            // Ustawiamy dźwięk na NPC (efekt 3D)
            if (_currentNPC != null)
            {
                _voiceInstance.set3DAttributes(RuntimeUtils.To3DAttributes(_currentNPC.gameObject));
            }
            _voiceInstance.start();
        }

        dialogueText.text = "";
        _isTyping = true;
        
        // Pętla wyrzuca po jednej literze na ekran - stary trick narracyjny
        foreach (char letter in node.text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        _isTyping = false;

        // Jeśli mamy włączone automatyczne przewijanie (Auto-Advance)
        if (useAutoAdvance)
        {
            DialogueNode nextNode = _currentConversation.nodes[_currentNodeIndex - 1];
            float waitTime = CalculateWaitTime(nextNode);
            _autoAdvanceCoroutine = StartCoroutine(WaitAndNextNode(waitTime));
        }
    }

    private float CalculateWaitTime(DialogueNode node)
    {
        // 1. Czy wpisałeś czas ręcznie?
        if (node.useManualTime) return node.manualWaitTime;

        // 2. Czy masz nagrane Audio w FMOD? (Pobieramy długość z FMOD-a)
        if (!node.voiceoverEvent.IsNull)
        {
            EventDescription desc = RuntimeManager.GetEventDescription(node.voiceoverEvent);
            if (desc.isValid())
            {
                int lengthMs;
                desc.getLength(out lengthMs);
                return (lengthMs / 1000f) + waitTimeBuffer;
            }
        }

        // 3. Automat: Liczymy na podstawie długości tekstu
        float calculatedTime = node.text.Length / charsPerSecond;
        
        // Zwracamy wyliczony czas + bufor, żeby gracz nie sapał że za szybko
        return calculatedTime + waitTimeBuffer;
    }

    private IEnumerator WaitAndNextNode(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        DisplayNextNode();
    }



    private void ShowChoices()
    {
        if (choicePanel == null || choiceButtonPrefab == null) return;
        if (_isShowingChoices) return; // Zabezpieczenie przed podwójnym odpaleniem
        
        _isShowingChoices = true;
        Debug.Log($"[Dialogue] ShowChoices: {_currentConversation.choices.Count} opcji.");

        // Czyścimy stare przyciski
        foreach (Transform child in choiceParent)
        {
            Destroy(child.gameObject);
        }

        // Ukrywamy główny tekst rozmowy na czas podejmowania wyborów
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        choicePanel.SetActive(true);

        // Tworzymy nowe przyciski na podstawie listy choices z assetu
        foreach (var choice in _currentConversation.choices)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceParent);
            DialogueChoiceButton btnScript = btnObj.GetComponent<DialogueChoiceButton>();
            if (btnScript != null)
            {
                btnScript.Setup(choice);
            }
        }

        // AUTO-FOKUS: Wymuś zaznaczenie pierwszego przycisku (dla pada i klawiatury)
        if (choiceParent.childCount > 0 && UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(choiceParent.GetChild(0).gameObject);
        }
    }

    public void SelectChoice(DialogueConversation nextConv)
    {
        Debug.Log($"[Dialogue] Gracz wybrał opcję. Przechodzę do: {(nextConv != null ? nextConv.name : "KONIEC")}");
        
        // TWARDE CZYSZCZENIE: zanim zrobimy cokolwiek innego, usuwamy stare śmieci
        _isShowingChoices = false;
        if (choicePanel != null) choicePanel.SetActive(false);
        
        foreach (Transform child in choiceParent)
        {
            Destroy(child.gameObject);
        }
        
        // Odpalamy rozmowę, którą wskazuje wybrany przycisk
        StartConversation(nextConv, _currentNPC);
    }
}
