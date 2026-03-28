using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

/// <summary>
/// Profesjonalny skaler czułości dla Cinemachine 3.0.
/// Wykorzystuje delegata ReadControlValueOverride do modyfikowania sygnału Look w czasie rzeczywistym.
/// </summary>
[RequireComponent(typeof(CinemachineInputAxisController))]
public class CinemachineInputScaler : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private TargetHandler targetHandler; // NOWE: Referencja do skryptu namierzania
    private CinemachineInputAxisController _axisController;

    private void Awake()
    {
        _axisController = GetComponent<CinemachineInputAxisController>();
        
        // --- NOWOŚĆ: Automatyczne szukanie TargetHandlera ---
        if (targetHandler == null)
        {
            targetHandler = FindFirstObjectByType<TargetHandler>();
        }

        // --- NOWOŚĆ: Wymuszenie, żeby kamera zawsze za nami latała (Zero teleportacji!) ---
        if (TryGetComponent<CinemachineCamera>(out var cam))
        {
            cam.StandbyUpdate = CinemachineCamera.StandbyUpdateMode.Always;
            Debug.Log($"<color=green>[CAMERA] Wymuszono 'Standby Update' na Always dla {gameObject.name}!</color>");
        }

        if (_axisController == null) return;

        // Poprawna sygnatura dla tej wersji Cinemachine 3.0
        _axisController.ReadControlValueOverride = (InputAction action, IInputAxisOwner.AxisDescriptor.Hints hint, Object context, CinemachineInputAxisController.Reader.ControlValueReader defaultReader) => 
        {
            // 1. Odczytujemy surową wartość
            float value = defaultReader(action, hint, context, null);
            
            // 2. Sprawdzamy czy to oś Look (Nazwa akcji zawiera "Look")
            if (action != null && action.name.Contains("Look"))
            {
                // --- NOWOŚĆ: Jeśli masz namierzony cel, nie obracaj kamery swobodnej! ---
                if (targetHandler != null && targetHandler.IsLockedOn)
                {
                    return 0f; // Ignoruj ruch myszki/gałki dla obrotu kamery
                }

                if (SettingsManager.Instance == null || inputReader == null) return value;

                // 3. Pobieramy czułość dla aktywnego urządzenia
                float sensitivity = (inputReader.LastUsedDevice == InputReader.InputDeviceType.Mouse) 
                    ? SettingsManager.Instance.mouseSensitivity 
                    : SettingsManager.Instance.gamepadSensitivity;

                // 4. Obsługa inwersji osi
                float invX = SettingsManager.Instance.invertX ? -1f : 1f;
                float invY = SettingsManager.Instance.invertY ? -1f : 1f;

                // Sprawdzamy czy to oś X czy Y (AxisDescriptor hints pomagają)
                if (hint == IInputAxisOwner.AxisDescriptor.Hints.X)
                    return value * sensitivity * invX;
                if (hint == IInputAxisOwner.AxisDescriptor.Hints.Y)
                    return value * sensitivity * invY;

                return value * sensitivity;
            }

            return value;
        };
        
        Debug.Log("<color=cyan>[CAMERA] CinemachineInputScaler zainicjalizowany (Globalnie - 4 args)!</color>");
    }
}
