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

    [Header("Dialogi (ScriptableObjects)")]
    [Tooltip("Lista rozmów. Gra wybierze z góry na dół pierwszą rZECZ, której wymóg jest spełniony. Zostaw puste pole flagi na samym dole jako domyślny dialog!")]
    public List<DialogueConversation> conversations; 

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

    // Interfejs IInteractable (Co się dzieje po kliknięciu [E])
    public void Interact(Transform interactor)
    {
        if (_currentState == IdleState)
        {
            SwitchState(DialogueState);
        }
        else if (_currentState == DialogueState)
        {
            // Gdy jesteśmy w rozmowie, kliknięcie E przesuwa tekst do przodu.
            if (DialogueManager.Instance != null)
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

    // Profesjonalne szukanie odpowiedniego tekstu
    public DialogueConversation GetCurrentConversation()
    {
        foreach (var conv in conversations)
        {
            // Czy spełniamy WSZYSTKIE wymagania?
            bool hasAllRequired = true;
            foreach (var flag in conv.requiredFlags)
            {
                if (!WorldStateManager.Instance.HasFlag(flag))
                {
                    hasAllRequired = false;
                    break;
                }
            }
            
            // Czy NIE jesteśmy wykluczeni (przez którąkolwiek flagę)?
            bool anyExcluded = false;
            foreach (var flag in conv.excludeFlags)
            {
                if (WorldStateManager.Instance.HasFlag(flag))
                {
                    anyExcluded = true;
                    break;
                }
            }

            if (hasAllRequired && !anyExcluded)
            {
                return conv;
            }
        }
        Debug.LogWarning($"[NPC] {name} chce coś powiedzieć, ale nie znalazł odpowiedniego pliku dialogu (może wszystkie jednorazówki już wystrzelały?)!");
        return null; 
    }
}
