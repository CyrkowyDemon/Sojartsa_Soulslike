using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Skrypt na przyciski wyboru w UI.
/// </summary>
public class DialogueChoiceButton : MonoBehaviour
{
    [SerializeField] private TMP_Text choiceText;
    private DialogueConversation _targetConversation;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void Setup(DialogueChoice choice)
    {
        if (choiceText != null)
        {
            choiceText.text = choice.choiceText;
        }
        else
        {
            Debug.LogError($"[UI] Przycisk {gameObject.name} nie ma przypiętego komponentu Text (TMP) w Inspektorze! Sprawdź prefab!");
        }

        _targetConversation = choice.nextConversation;
        
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        // Poinformuj menedżera, że gracz wybrał tę opcję
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.SelectChoice(_targetConversation);
        }
    }
}
