using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // Dodaliśmy to!

public class SettingsTabManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputReader inputReader;

    [Header("Structure (Panele z Canvas Group)")]
    [SerializeField] private List<CanvasGroup> categoryPanels; 
    [SerializeField] private TextMeshProUGUI categoryTitleText;
    
    [Header("Górne Przyciski Zakładek")]
    [SerializeField] private List<CanvasGroup> topTabButtons; 

    [Header("Names")]
    [SerializeField] private List<string> categoryNames = new List<string> { "GAME", "INPUT", "GRAPHICS", "SOUND", "KEYS" };

    private int _index = 0; 

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.TabPrevEvent += Prev;
            inputReader.TabNextEvent += Next;
        }
        Invoke(nameof(UpdateTabs), 0.02f);
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.TabPrevEvent -= Prev;
            inputReader.TabNextEvent -= Next;
        }
        CancelInvoke();
    }

    public void Next() { ChangeTab((_index + 1) % categoryPanels.Count); }
    public void Prev() { ChangeTab((_index - 1 + categoryPanels.Count) % categoryPanels.Count); }
    public void GoToTab(int tabIndex) { ChangeTab(tabIndex); }

    private void ChangeTab(int newIndex)
    {
        if (categoryPanels == null || categoryPanels.Count == 0) return;
        _index = Mathf.Clamp(newIndex, 0, categoryPanels.Count - 1);
        UpdateTabs();
    }

    private void UpdateTabs()
    {
        // Sprawdzamy, czy lewy przycisk myszy jest wciśnięty (Nowy Input System)
        bool isMouseActive = false;
        if (Mouse.current != null)
        {
            isMouseActive = Mouse.current.leftButton.isPressed;
        }

        // 1. PANELE
        for (int i = 0; i < categoryPanels.Count; i++)
        {
            if (categoryPanels[i] == null) continue;

            bool isActive = (i == _index);
            categoryPanels[i].alpha = isActive ? 1f : 0f;
            categoryPanels[i].interactable = isActive;
            categoryPanels[i].blocksRaycasts = isActive;

            // Focusujemy tylko, gdy NIE trzymamy myszki (czyli nawigujemy padem/klawiaturą)
            if (isActive && !isMouseActive) 
            {
                FocusFirst(categoryPanels[i].gameObject);
            }
        }

        // 2. PRZYCISKI GÓRNE
        if (topTabButtons != null)
        {
            for (int i = 0; i < topTabButtons.Count; i++)
            {
                if (topTabButtons[i] == null) continue;
                topTabButtons[i].alpha = (i == _index) ? 1f : 0.4f;
                topTabButtons[i].interactable = true;
                topTabButtons[i].blocksRaycasts = true;
            }
        }

        if (categoryTitleText != null && _index < categoryNames.Count)
            categoryTitleText.text = categoryNames[_index];
    }

    private void FocusFirst(GameObject panel)
    {
        if (EventSystem.current == null) return;
        Selectable first = panel.GetComponentInChildren<Selectable>();
        if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }
}