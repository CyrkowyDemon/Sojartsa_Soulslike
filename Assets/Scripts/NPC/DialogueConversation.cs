using System.Collections.Generic;
using UnityEngine;
using FMODUnity; // NOWY NAMESPACE!

/// <summary>
/// Cała rozmowa składająca się z wielu kwestii.
/// </summary>
[CreateAssetMenu(fileName = "NewConversation", menuName = "Sojartsa/NPC/Conversation")]
public class DialogueConversation : ScriptableObject
{
    [Header("Ustawienia Zachowania")]
    public bool locksPlayer = true; 

    [Header("Warunki Aktywacji (Listy)")]
    [Tooltip("Wszystkie te flagi MUSZĄ być w pamięci świata, żeby dialog się odpalił.")]
    public List<WorldFlagSO> requiredFlags = new List<WorldFlagSO>(); 
    [Tooltip("Jeśli KTÓRAKOLWIEK z tych flag jest w pamięci, ten dialog zostanie POMINIĘTY.")]
    public List<WorldFlagSO> excludeFlags = new List<WorldFlagSO>(); 

    [Header("Kwestie NPC (Główna mowa)")]
    public List<DialogueNode> nodes = new List<DialogueNode>();

    [Header("Wybory Gracza (Na końcu mowy)")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [Header("Logika po zakończeniu (Jeśli brak wyborów)")]
    public DialogueConversation nextConversation; 
    [Tooltip("Lista pieczątek, które zostaną przybite w pamięci świata po zakończeniu tej rozmowy.")]
    public List<WorldFlagSO> flagsToSetOnEnd = new List<WorldFlagSO>(); 
}

/// <summary>
/// Pojedyncza kwestia dialogowa.
/// </summary>
[System.Serializable]
public class DialogueNode
{
    [TextArea(3, 10)]
    public string text; // Co NPC mówi
    public EventReference voiceoverEvent; // NOWY TYP: FMOD Event zamiast AudioClip!

    [Header("Timing (Opcjonalnie)")]
    public bool useManualTime = false;
    public float manualWaitTime = 2f; 
}

/// <summary>
/// Opcja wyboru dla gracza.
/// </summary>
[System.Serializable]
public class DialogueChoice
{
    public string choiceText; // Co gracz widzi na przycisku
    public DialogueConversation nextConversation; // Gdzie nas to prowadzi
}
