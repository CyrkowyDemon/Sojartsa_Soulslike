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

    // NOWE: Event do wysyłania kierunku, w którym gracz wychylił gałkę/mysz
    public event Action<Vector2> SwitchTargetEvent;

    public enum InputDeviceType { Mouse, Gamepad }
    public InputDeviceType LastUsedDevice { get; private set; } = InputDeviceType.Mouse;

    // --- TUTAJ JEST TYLKO JEDNO MovementValue ---
    public Vector2 MovementValue { get; private set; }

    private PlayerInputActions _controls;

    // DODANE: Tutaj będziemy trzymać pomnożony przez czułość obrót kamery
    public Vector2 LookValue { get; private set; }
    
    // NOWE: Zmienne do kontrolowania przełączania (żeby cel nie latał jak szalony)
    private float _lastSwitchTime;
    [SerializeField] private float switchCooldown = 0.3f; // 0.3 sekundy przerwy między przeskokami
    
    [Header("Czułość sterowania (zsynchronizowana z Menedżerem)")]
    private float mouseSensitivity = 1.0f;
    private float gamepadSensitivity = 3.0f;

    private void OnEnable()
    {
        _lastSwitchTime = -100f; // Resetujemy licznik przy starcie, żeby pierwszy klik zawsze działał
        
        RefreshSettings();
        
        if (_controls == null)
        {
            _controls = new PlayerInputActions();
            _controls.Player.SetCallbacks(this);
            _controls.UI.SetCallbacks(this);
        }

        // Subskrybujemy zmiany w czasie rzeczywistym
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsUpdated += RefreshSettings;
        }
        
        EnableGameplay();
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
        MovementValue = context.ReadValue<Vector2>();
    }

    // ZMODYFIKOWANE: Tutaj łapiemy flicki myszą lub padem
    public void OnLook(InputAction.CallbackContext context) 
    { 
        Vector2 rawValue = context.ReadValue<Vector2>();
        
        bool isMouse = (context.control.device is Mouse);
        LastUsedDevice = isMouse ? InputDeviceType.Mouse : InputDeviceType.Gamepad;

        float currentSensitivity = isMouse ? mouseSensitivity : gamepadSensitivity;
        
        // DODANE: Zapisujemy pomnożoną wartość! Twój skrypt kamery musi czytać inputReader.LookValue
        LookValue = rawValue * currentSensitivity;

        // Zmniejszamy próg do 0.4f (po uwzględnieniu czułości)
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

    public void OnAttack(InputAction.CallbackContext context) { if (context.performed) AttackEvent?.Invoke(); }
    public void OnHeavyAttack(InputAction.CallbackContext context) { if (context.performed) HeavyAttackEvent?.Invoke(); }
    public void OnDodge(InputAction.CallbackContext context) { if (context.performed) DodgeEvent?.Invoke(); }
    public void OnTarget(InputAction.CallbackContext context) { if (context.performed) TargetEvent?.Invoke(); }
    public void OnMainMenu(InputAction.CallbackContext context) { if (context.performed) MainMenuEvent?.Invoke(); }

    // Implementacja brakującego interfejsu UI
    public void OnNavigate(InputAction.CallbackContext context) { }
    public void OnSubmit(InputAction.CallbackContext context) { }
    public void OnCancel(InputAction.CallbackContext context) { }
    public void OnNewaction(InputAction.CallbackContext context) { }

    public void RefreshSettings()
    {
        if (SettingsManager.Instance != null)
        {
            mouseSensitivity = SettingsManager.Instance.mouseSensitivity;
            gamepadSensitivity = SettingsManager.Instance.gamepadSensitivity;
            Debug.Log($"[INPUT] Odświeżono sensy: M={mouseSensitivity}, G={gamepadSensitivity}");
        }
    }

    public void EnableUI()
    {
        _controls.Player.Disable(); 
        _controls.UI.Enable();      
        
        // Zabezpieczamy przycisk wyjścia z pauzy przed zablokowaniem
        _controls.Player.MainMenu.Enable(); 
    }

    public void EnableGameplay()
    {
        _controls.UI.Disable();     
        _controls.Player.Enable();  
    }
}