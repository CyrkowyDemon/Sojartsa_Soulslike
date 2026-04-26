using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mózg pokojowego bohatera NPC (np. Meliny czy Kowala).
/// System przeskakuje między stanami (Stoi -> Rozmawia). 
/// Dobiera dialog w zależności od flag (postępu gracza w grze).
/// </summary>
public class PeacefulNPC : MonoBehaviour, IInteractable
{
    private INPCState _currentState;

    public NPCIdleState IdleState = new NPCIdleState();
    public NPCDialogueState DialogueState = new NPCDialogueState();
    public NPCMenuState MenuState = new NPCMenuState(); // Nowy stan menu!

    [Header("Ważne Dialogi (Automatyczne/Priorytety)")]
    [Tooltip("Odpali się od razu po wciśnięciu E lub po wejściu w strefę (TriggerZone).")]
    public List<DialogueConversation> priorityConversations = new List<DialogueConversation>(); 

    [Header("Dialogi Opcjonalne (Pod menu 'Porozmawiaj')")]
    [Tooltip("Pojawią się tylko jako opcje do wyboru w menu NPC.")]
    public List<DialogueConversation> talkConversations = new List<DialogueConversation>();

    [Header("System Kamery")]
    [Tooltip("Punkt, na którym skupi się kamera gracza. Jeśli puste, użyje środka NPC.")]
    public Transform lockOnTransform;

    private void Start()
    {
        if (lockOnTransform == null) lockOnTransform = this.transform;
        SwitchState(IdleState);
    }

    private void Update()
    {
        _currentState?.UpdateState(this);
    }

    public void SwitchState(INPCState newState)
    {
        if (_currentState == newState) return;
        
        _currentState?.ExitState(this);
        _currentState = newState;
        _currentState?.EnterState(this);
    }

    // Interfejs IInteractable (Domyślne kliknięcie [E])
    public void Interact(Transform interactor)
    {
        InteractWithIntent(isAutomaticTrigger: false);
    }

    /// <summary>
    /// Główna logika rozmowy. Rozróżnia wciśnięcie E od strefy automatycznej.
    /// </summary>
    public void InteractWithIntent(bool isAutomaticTrigger)
    {
        if (_currentState == IdleState)
        {
            if (isAutomaticTrigger)
            {
                // To wywołanie ze strefy (NPCTriggerZone). Sprawdzamy czy jest ważna rozmowa.
                DialogueConversation priority = GetPriorityConversation();
                if (priority != null)
                {
                    SwitchState(DialogueState);
                }
            }
            else
            {
                // Gracz wcisnął E "z palca" - ZAWSZE otwieramy Hub Menu (Sklep, Porozmawiaj, Bywaj).
                Debug.Log($"[NPC] Otwieram Menu dla {name} (Porozmawiaj, Sklep, Bywaj)");
                SwitchState(MenuState); 
            }
        }
        else if (_currentState == DialogueState)
        {
            // Gdy jesteśmy w rozmowie, kliknięcie E przesuwa tekst do przodu.
            if (!isAutomaticTrigger && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.DisplayNextNode();
            }
        }
    }

    public string GetInteractText()
    {
        if (_currentState == IdleState) return "Talk";
        return ""; // Ukrywamy napis E gdy z nim gadamy
    }

    public bool CanInteract()
    {
        return true; 
    }

    // Szuka najważniejszej rozmowy (z góry na dół)
    public DialogueConversation GetPriorityConversation()
    {
        if (priorityConversations == null) return null;

        foreach (var conv in priorityConversations)
        {
            if (conv == null) continue;

            // Czy spełniamy WSZYSTKIE wymagania?
            bool hasAllRequired = true;
            if (conv.requiredFlags != null)
            {
                foreach (var flag in conv.requiredFlags)
                {
                    if (!WorldStateManager.Instance.HasFlag(flag))
                    {
                        hasAllRequired = false;
                        break;
                    }
                }
            }
            
            // Czy NIE jesteśmy wykluczeni (przez którąkolwiek flagę)?
            bool anyExcluded = false;
            if (conv.excludeFlags != null)
            {
                foreach (var flag in conv.excludeFlags)
                {
                    if (WorldStateManager.Instance.HasFlag(flag))
                    {
                        anyExcluded = true;
                        break;
                    }
                }
            }

            if (hasAllRequired && !anyExcluded)
            {
                return conv;
            }
        }
        Debug.Log($"[NPC] Brak priorytetowych dialogów dla {name}.");
        return null; 
    }

    // Pobiera wszystkie opcjonalne dialogi, które gracz odblokował (do wrzucenia w listę przycisków)
    public List<DialogueConversation> GetAvailableTalkConversations()
    {
        List<DialogueConversation> available = new List<DialogueConversation>();
        if (talkConversations == null) return available;
        
        foreach (var conv in talkConversations)
        {
            if (conv == null) continue;

            bool hasAllRequired = true;
            if (conv.requiredFlags != null)
            {
                foreach (var flag in conv.requiredFlags)
                {
                    if (!WorldStateManager.Instance.HasFlag(flag)) { hasAllRequired = false; break; }
                }
            }
            
            bool anyExcluded = false;
            if (conv.excludeFlags != null)
            {
                foreach (var flag in conv.excludeFlags)
                {
                    if (WorldStateManager.Instance.HasFlag(flag)) { anyExcluded = true; break; }
                }
            }

            if (hasAllRequired && !anyExcluded)
            {
                available.Add(conv);
            }
        }

        return available;
    }
}
