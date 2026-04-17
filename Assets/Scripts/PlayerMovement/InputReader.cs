using UnityEngine;
using UnityEngine.InputSystem;
using System;

[CreateAssetMenu(fileName = "InputReader", menuName = "Combat/Input Reader")]
public class InputReader : ScriptableObject, PlayerInputActions.IPlayerActions, PlayerInputActions.IUIActions
{
    public event Action AttackEvent;
    public event Action HeavyAttackEvent;
    public event Action DodgeEvent;
    public event Action TargetEvent;
    public event Action MainMenuEvent;
    public event Action CancelEvent;
    public event Action TabPrevEvent;
    public event Action TabNextEvent;
    public event Action<Vector2> SwitchTargetEvent;
    public event Action InteractEvent;

    public enum InputDeviceType { Mouse, Gamepad }
    public InputDeviceType LastUsedDevice { get; private set; } = InputDeviceType.Mouse;

    public Vector2 MovementValue { get; private set; }
    public Vector2 LookValue { get; private set; }
    
    public bool IsMovementLocked = false;
    public bool IsCameraLocked = false;

    private PlayerInputActions _controls;
    public PlayerInputActions Controls 
    { 
        get 
        {
            if (_controls == null) LoadRebinds();
            return _controls;
        }
    }
    private float _lastSwitchTime;
    [SerializeField] private float switchCooldown = 0.3f; 
    
    private float mouseSensitivity = 1.0f;
    private float gamepadSensitivity = 3.0f;

    private void OnEnable()
    {
        _lastSwitchTime = -100f; 
        
        RefreshSettings();

        if (SettingsManager.Instance != null)
        {
            // Zabezpieczenie przed podwójną subskrypcją w ScriptableObject
            SettingsManager.Instance.OnSettingsUpdated -= RefreshSettings; 
            SettingsManager.Instance.OnSettingsUpdated += RefreshSettings;
        }
        
        UnlockAllInput();

        // LoadRebinds teraz zajmuje się WSZYSTKIM: tworzeniem instancji, wczytywaniem i włączaniem
        LoadRebinds();
    }

    public void LoadRebinds()
    {
        // 1. TWARDY RESET: Zamiast bawić się w Disable/Enable, całkowicie zabijamy stary system
        if (_controls != null)
        {
            _controls.Disable();
            _controls.Dispose(); // Czyścimy śmieci z pamięci RAM (zapobiega memory leakom!)
        }

        // 2. Tworzymy całkowicie ŚWIEŻĄ instancję z domyślnymi klawiszami
        _controls = new PlayerInputActions();
        _controls.Player.SetCallbacks(this);
        _controls.UI.SetCallbacks(this);

        // 3. Wczytujemy zapisane klawisze z PlayerPrefs
        string rebinds = PlayerPrefs.GetString("rebinds", string.Empty);
        if (!string.IsNullOrEmpty(rebinds))
        {
            // Nakładamy JSON-a na zupełnie nowy, czysty obiekt. Unity nie ma prawa tego zignorować.
            _controls.asset.LoadBindingOverridesFromJson(rebinds);
        }

        // 4. Odpalamy zaktualizowany system
        _controls.Player.Enable();
        _controls.UI.Enable();
    }

    public void SaveRebinds()
    {
        if (_controls == null) return;
        
        string rebindsData = _controls.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebindsData);
        PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsUpdated -= RefreshSettings;
        }

        // Sprzątanie pamięci przy wyłączaniu gry
        if (_controls != null)
        {
            _controls.Disable();
            // _controls.Dispose(); // Wywalamy to, bo w Edytorze Unity gryzie się z systemem Destroy
            _controls = null;
        }
    }

    // --- TWOJE EVENTY Z GRY ---
    
    public void OnMove(InputAction.CallbackContext context)
    {
        if (IsMovementLocked)
        {
            MovementValue = Vector2.zero;
            return;
        }
        MovementValue = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context) 
    { 
        if (IsCameraLocked)
        {
            LookValue = Vector2.zero;
            return;
        }

        Vector2 rawValue = context.ReadValue<Vector2>();
        bool isMouse = (context.control.device is Mouse);
        LastUsedDevice = isMouse ? InputDeviceType.Mouse : InputDeviceType.Gamepad;

        LookValue = rawValue;

        if (Mathf.Abs(rawValue.x) < 0.4f) return;

        bool isHorizontal = Mathf.Abs(rawValue.x) > Mathf.Abs(rawValue.y);
        float timeSinceLastSwitch = Time.time - _lastSwitchTime;
        bool cooldownPassed = timeSinceLastSwitch > switchCooldown;

        if (isHorizontal)
        {
            if (cooldownPassed)
            {
                Vector2 cleanDirection = new Vector2(Mathf.Sign(rawValue.x), 0);
                SwitchTargetEvent?.Invoke(cleanDirection); 
                _lastSwitchTime = Time.time;          
            }
        }
    }

    public void OnAttack(InputAction.CallbackContext context) { if (context.performed && !IsMovementLocked) AttackEvent?.Invoke(); }
    public void OnHeavyAttack(InputAction.CallbackContext context) { if (context.performed && !IsMovementLocked) HeavyAttackEvent?.Invoke(); }
    public void OnDodge(InputAction.CallbackContext context) { if (context.performed && !IsMovementLocked) DodgeEvent?.Invoke(); }
    public void OnTarget(InputAction.CallbackContext context) { if (context.performed && !IsMovementLocked) TargetEvent?.Invoke(); }
    
    public void OnMainMenu(InputAction.CallbackContext context) { if (context.performed) MainMenuEvent?.Invoke(); }

    public void OnNavigate(InputAction.CallbackContext context) { }
    public void OnSubmit(InputAction.CallbackContext context) { }
    public void OnCancel(InputAction.CallbackContext context) { if (context.performed) CancelEvent?.Invoke(); }
    public void OnTabPrev(InputAction.CallbackContext context) { if (context.performed) TabPrevEvent?.Invoke(); }
    public void OnTabNext(InputAction.CallbackContext context) { if (context.performed) TabNextEvent?.Invoke(); }
    public void OnInteraction(InputAction.CallbackContext context) { if (context.performed) InteractEvent?.Invoke(); }
    public void OnNewaction(InputAction.CallbackContext context) { }

    public void RefreshSettings()
    {
        if (SettingsManager.Instance != null)
        {
            mouseSensitivity = SettingsManager.Instance.mouseSensitivity;
            gamepadSensitivity = SettingsManager.Instance.gamepadSensitivity;
        }
    }

    public void SetPauseMenuState()
    {
        IsCameraLocked = true;
        IsMovementLocked = true;
    }

    public void SetSettingsMenuState()
    {
        IsCameraLocked = true;
        IsMovementLocked = true;
    }

    public void SetDialogueState()
    {
        IsCameraLocked = true;
        IsMovementLocked = true;
    }

    public void UnlockAllInput()
    {
        IsCameraLocked = false;
        IsMovementLocked = false;
    }
}