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

    public enum InputDeviceType { Mouse, Gamepad }
    public InputDeviceType LastUsedDevice { get; private set; } = InputDeviceType.Mouse;

    public Vector2 MovementValue { get; private set; }
    public Vector2 LookValue { get; private set; }
    
    // --- NOWE: BLOKADY INPUTU DLA MENU ---
    public bool IsMovementLocked = false;
    public bool IsCameraLocked = false;

    private PlayerInputActions _controls;
    private float _lastSwitchTime;
    [SerializeField] private float switchCooldown = 0.3f; 
    
    private float mouseSensitivity = 1.0f;
    private float gamepadSensitivity = 3.0f;

    private void OnEnable()
    {
        _lastSwitchTime = -100f; 
        
        RefreshSettings();
        
        if (_controls == null)
        {
            _controls = new PlayerInputActions();
            _controls.Player.SetCallbacks(this);
            _controls.UI.SetCallbacks(this);
        }

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsUpdated += RefreshSettings;
        }
        
        // Zawsze na start włączamy obie mapy
        _controls.Player.Enable();
        _controls.UI.Enable();
        UnlockAllInput(); // Upewniamy się, że po starcie nic nie jest zablokowane

        // Wczytywanie zapisanych klawiszy z PlayerPrefs
        string rebinds = PlayerPrefs.GetString("rebinds", string.Empty);
        if (!string.IsNullOrEmpty(rebinds))
        {
            _controls.asset.LoadBindingOverridesFromJson(rebinds);
        }
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsUpdated -= RefreshSettings;
        }

        if (_controls != null)
        {
            _controls.Player.Disable();
            _controls.UI.Disable();
        }
    }

    // --- TWOJE EVENTY Z GRY ---
    
    public void OnMove(InputAction.CallbackContext context)
    {
        // BRAMKA: Jeśli zablokowane (Settings Menu), ustawiamy ruch na 0,0
        if (IsMovementLocked)
        {
            MovementValue = Vector2.zero;
            return;
        }
        
        MovementValue = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context) 
    { 
        // BRAMKA: Jeśli zablokowane (Pause lub Settings), ustawiamy kamerę na 0,0
        if (IsCameraLocked)
        {
            LookValue = Vector2.zero;
            return;
        }

        Vector2 rawValue = context.ReadValue<Vector2>();
        bool isMouse = (context.control.device is Mouse);
        LastUsedDevice = isMouse ? InputDeviceType.Mouse : InputDeviceType.Gamepad;

        float currentSensitivity = isMouse ? mouseSensitivity : gamepadSensitivity;
        LookValue = rawValue * currentSensitivity;

        if (Mathf.Abs(LookValue.x) < 0.4f) return;

        bool isHorizontal = Mathf.Abs(LookValue.x) > Mathf.Abs(LookValue.y);
        float timeSinceLastSwitch = Time.time - _lastSwitchTime;
        bool cooldownPassed = timeSinceLastSwitch > switchCooldown;

        if (isHorizontal)
        {
            if (cooldownPassed)
            {
                Vector2 cleanDirection = new Vector2(Mathf.Sign(LookValue.x), 0);
                SwitchTargetEvent?.Invoke(cleanDirection); 
                _lastSwitchTime = Time.time;          
            }
        }
    }

    // Dodajemy małe zabezpieczenie (bramkę) do ataku i uniku, żeby gracz nie atakował klikając w UI
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
    public void OnNewaction(InputAction.CallbackContext context) { }

    public void RefreshSettings()
    {
        if (SettingsManager.Instance != null)
        {
            mouseSensitivity = SettingsManager.Instance.mouseSensitivity;
            gamepadSensitivity = SettingsManager.Instance.gamepadSensitivity;
        }
    }

    // --- NOWE FUNKCJE DO KONTROLI STANU ---
    
    public void SetPauseMenuState()
    {
        IsCameraLocked = true;
        IsMovementLocked = false; // Gracz może chodzić w pauzie
    }

    public void SetSettingsMenuState()
    {
        IsCameraLocked = true;
        IsMovementLocked = true; // W opcjach nic nie robimy
    }

    public void UnlockAllInput()
    {
        IsCameraLocked = false;
        IsMovementLocked = false;
    }
}