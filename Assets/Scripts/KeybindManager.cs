using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class KeybindManager : MonoBehaviour
{
    [Header("Referencje - Zabezpieczenie Zmiany w Locie")]
    [Tooltip("Przeciągnij tutaj plik InputReader z The Player, żeby od razu ogarnął zmianę klawisza!")]
    public InputReader inputReader;

    [System.Serializable]
    public class KeybindSlot
    {
        public string slotName; 
        public InputActionReference actionToRebind;
        public Button buttonToClick;
        public TMP_Text buttonText;
        public int bindingIndex = 0; 
        public bool excludeMouse = true;
        [Tooltip("Zaznacz to, jeśli ten slot przypisuje KLAWISZ NA PADZIE!")]
        public bool isGamepadSlot = false; 
    }

    [Header("Lista wszystkich klawiszy")]
    public List<KeybindSlot> keybinds = new List<KeybindSlot>();

    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;

    private void OnEnable()
    {
        if (keybinds.Count > 0 && keybinds[0].buttonToClick != null)
        {
            StartCoroutine(FocusFirstSlotRoutine());
        }
    }

    private System.Collections.IEnumerator FocusFirstSlotRoutine()
    {
        yield return new WaitForEndOfFrame();
        if (UnityEngine.EventSystems.EventSystem.current != null && keybinds[0].buttonToClick.gameObject.activeInHierarchy)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(keybinds[0].buttonToClick.gameObject);
        }
    }

    private System.Collections.IEnumerator ReselectButtonRoutine(GameObject go)
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            yield return new WaitForEndOfFrame();
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(go);
        }
    }

    // [NAPRAWIONE] Bierzemy "żywą" akcję w 100% niezawodnie po GUID (ID), a nie po ścieżkach ze stringami!
    private InputAction GetLiveAction(KeybindSlot slot)
    {
        if (inputReader != null && inputReader.Controls != null && slot.actionToRebind != null)
        {
            // Szukamy po unikalnym ID. To nigdy nie zawodzi (nie straszne mu spacje czy zmiany nazw)
            var liveAction = inputReader.Controls.asset.FindAction(slot.actionToRebind.action.id.ToString());

            if (liveAction != null) return liveAction;

            Debug.LogError($"[KeybindManager] TOTALNY BŁĄD! Nie znaleziono żywej akcji dla '{slot.slotName}'. " +
                           $"Sprawdź, czy InputReader załadował Controls!");
        }
        
        // Zwracanie fallbacku to to, co psuło Ci grę (modyfikowało twardy asset w projekcie)
        // Zostawiamy jako ostateczną ostateczność, żeby gra nie wywaliła NullReference, ale nie powinno to już nigdy wystąpić.
        return slot.actionToRebind?.action;
    }

    private void Start()
    {
        if (inputReader != null && inputReader.Controls == null)
        {
            inputReader.LoadRebinds();
        }

        foreach (var slot in keybinds)
        {
            UpdateBindingText(slot);
            slot.buttonToClick.onClick.AddListener(() => StartRebinding(slot));
        }
    }

    private void OnDestroy()
    {
        _rebindingOperation?.Dispose();
    }

    public void StartRebinding(KeybindSlot slot)
    {
        if (_rebindingOperation != null) return;

        var liveAction = GetLiveAction(slot);

        if (slot.bindingIndex < 0 || slot.bindingIndex >= liveAction.bindings.Count)
        {
            Debug.LogError($"[KeybindManager] Błędny bindingIndex ({slot.bindingIndex}) dla slotu '{slot.slotName}'!");
            return;
        }

        // [NAPRAWIONE] Wyłączamy CAŁĄ MAPĘ AKCJI, a nie pojedynczą akcję.
        // Wyłączanie pojedynczej akcji (liveAction.Disable()) robiło desync w Unity.
        liveAction.actionMap?.Disable();
        
        slot.buttonText.text = "Wciśnij klawisz...";
        slot.buttonToClick.interactable = false;

        var rebind = liveAction.PerformInteractiveRebinding(slot.bindingIndex);

        if (slot.isGamepadSlot)
        {
            rebind.WithControlsExcluding("<Keyboard>")
                  .WithControlsExcluding("<Mouse>");
        }
        else
        {
            rebind.WithControlsExcluding("<Gamepad>");
            if (slot.excludeMouse) rebind.WithControlsExcluding("<Mouse>");
            rebind.WithCancelingThrough("<Keyboard>/escape");
        }

        _rebindingOperation = rebind
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => RebindComplete(slot, liveAction))
            .OnCancel(operation => RebindComplete(slot, liveAction))
            .Start();
    }

    private void RebindComplete(KeybindSlot slot, InputAction liveAction)
    {
        _rebindingOperation.Dispose();
        _rebindingOperation = null;

        // [NAPRAWIONE] Włączamy całą mapę z powrotem.
        liveAction.actionMap?.Enable();
        
        slot.buttonToClick.interactable = true;
        
        UpdateBindingText(slot);
        StartCoroutine(ReselectButtonRoutine(slot.buttonToClick.gameObject));

        if (inputReader != null)
        {
            inputReader.SaveRebinds();
        }
    }

    private void UpdateBindingText(KeybindSlot slot)
    {
        if (slot.actionToRebind != null && slot.buttonText != null)
        {
            var liveAction = GetLiveAction(slot);
            slot.buttonText.text = liveAction.GetBindingDisplayString(slot.bindingIndex);
        }
    }

    public void ResetAllBindings()
    {
        if (inputReader != null && inputReader.Controls != null)
        {
            inputReader.Controls.asset.RemoveAllBindingOverrides();
            inputReader.SaveRebinds();
            
            foreach (var slot in keybinds)
            {
                UpdateBindingText(slot);
            }
        }
    }
}