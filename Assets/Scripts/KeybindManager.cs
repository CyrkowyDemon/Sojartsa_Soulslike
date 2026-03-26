using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class KeybindManager : MonoBehaviour
{
    // Ta klasa (struktura) tworzy ładne okienko dla każdego przycisku w Inspektorze
    [System.Serializable]
    public class KeybindSlot
    {
        public string slotName; // Np. "Atak" (tylko dla Ciebie, żebyś wiedział co to)
        public InputActionReference actionToRebind;
        public Button buttonToClick;
        public TMP_Text buttonText;
        public int bindingIndex = 0; // 0 dla klawiatury, 1 dla pada (zazwyczaj)
        public bool excludeMouse = true;
    }

    [Header("Lista wszystkich klawiszy")]
    public List<KeybindSlot> keybinds = new List<KeybindSlot>();

    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;

    private void Start()
    {
        // Na start gry, menedżer sam podpina wszystkie przyciski i odświeża napisy
        foreach (var slot in keybinds)
        {
            UpdateBindingText(slot);
            
            // Magia: automatycznie podpinamy kliknięcie przycisku pod funkcję zmiany klawisza!
            slot.buttonToClick.onClick.AddListener(() => StartRebinding(slot));
        }
    }

    public void StartRebinding(KeybindSlot slot)
    {
        // Jeśli już coś bindujemy, ignoruj
        if (_rebindingOperation != null) return;

        // Wyłączamy akcję i zmieniamy tekst
        slot.actionToRebind.action.Disable();
        slot.buttonText.text = "Wciśnij klawisz...";
        slot.buttonToClick.interactable = false;

        var rebind = slot.actionToRebind.action.PerformInteractiveRebinding(slot.bindingIndex);
        if (slot.excludeMouse) rebind.WithControlsExcluding("Mouse");

        _rebindingOperation = rebind
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => RebindComplete(slot))
            .OnCancel(operation => RebindComplete(slot))
            .Start();
    }

    private void RebindComplete(KeybindSlot slot)
    {
        _rebindingOperation.Dispose();
        _rebindingOperation = null;

        slot.actionToRebind.action.Enable();
        slot.buttonToClick.interactable = true;
        
        UpdateBindingText(slot);

        // Zapisujemy nowe ustawienia do pamięci gry
        string rebindsData = slot.actionToRebind.action.actionMap.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebindsData);
        PlayerPrefs.Save();
    }

    private void UpdateBindingText(KeybindSlot slot)
    {
        if (slot.actionToRebind != null && slot.buttonText != null)
        {
            slot.buttonText.text = slot.actionToRebind.action.GetBindingDisplayString(slot.bindingIndex);
        }
    }

    // BONUS: Funkcja do resetowania wszystkich klawiszy (możesz ją podpiąć pod osobny przycisk)
    public void ResetAllBindings()
    {
        foreach (var slot in keybinds)
        {
            slot.actionToRebind.action.RemoveAllBindingOverrides();
            UpdateBindingText(slot);
        }
        
        PlayerPrefs.DeleteKey("rebinds");
        PlayerPrefs.Save();
    }
}