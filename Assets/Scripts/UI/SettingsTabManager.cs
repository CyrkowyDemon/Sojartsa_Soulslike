using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; 

public class SettingsTabManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputReader inputReader;

    [Header("Structure (Panele)")]
    [SerializeField] private List<CanvasGroup> categoryPanels; 
    [SerializeField] private TextMeshProUGUI categoryTitleText;
    
    [Header("Wygląd zakładek (Górne przyciski)")]
    // Zamiast zwykłej listy obrazków, mamy listę naszych konfiguracji (TabConfig)
    public List<TabConfig> tabs; 

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

    // Kursor myszki WCHODZI na zakładkę - od razu ją wybieramy (świeci się!)
    public void OnTabEnter(int newIndex)
    {
        if (_index != newIndex)
        {
            _index = newIndex;
            UpdateTabs();
        }
    }

    private void UpdateTabs()
    {
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

            if (isActive && !isMouseActive) 
            {
                FocusFirst(categoryPanels[i].gameObject);
            }
        }

        // 2. PRZYCISKI GÓRNE (Każdy ma swoje unikalne grafiki)
        if (tabs != null)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].tabImage == null) continue;
                
                // Jeśli i == _index, to znaczy, że to ta zakładka jest włączona.
                // Dajemy jej aktywny sprite. Reszta dostaje nieaktywny.
                tabs[i].tabImage.sprite = (i == _index) ? tabs[i].activeSprite : tabs[i].inactiveSprite;
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

// Dodajemy małą "paczkę" danych, żeby każda zakładka miała SWOJE WŁASNE grafiki
[System.Serializable]
public class TabConfig
{
    public Image tabImage;        // Komponent Image danej zakładki
    public Sprite activeSprite;   // Obrazek, gdy świeci (jest wybrana/najechana)
    public Sprite inactiveSprite; // Obrazek, gdy jest zgaszona
}