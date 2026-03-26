using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class KeybindsAI : MonoBehaviour
{
    [Header("Panele")]
    public GameObject settingsPanel;   // Panel, z którego przychodzimy
    public GameObject keybindsPanel;  // Panel, który odpalamy

    [Header("Nawigacja Padem")]
    public GameObject firstKeybindButton;     // Przycisk do podświetlenia w Keybindach
    public GameObject keybindsButtonInSettings; // Przycisk w Ustawieniach, do którego wracamy

    // Wywoływane z przycisku "Sterowanie" w Ustawieniach
    public void OpenKeybinds()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (keybindsPanel != null) keybindsPanel.SetActive(true);
        
        SelectButton(firstKeybindButton);
    }

    // Wywoływane z przycisku "Wstecz" w Keybindach
    public void CloseKeybinds()
    {
        if (keybindsPanel != null) keybindsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        
        SelectButton(keybindsButtonInSettings);
    }

    // Pomocnicza funkcja do obsługi podświetlania przycisków (pad/klawiatura)
    public void SelectButton(GameObject button)
    {
        if (button != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(SelectRoutine(button));
        }
    }

    private IEnumerator SelectRoutine(GameObject button)
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return null; 
        EventSystem.current.SetSelectedGameObject(button);
    }
}