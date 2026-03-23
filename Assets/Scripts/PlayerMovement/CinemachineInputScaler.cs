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
    private CinemachineInputAxisController _axisController;

    private void Start()
    {
        _axisController = GetComponent<CinemachineInputAxisController>();
        if (_axisController == null) return;

        // Poprawna sygnatura dla tej wersji Cinemachine 3.0
        _axisController.ReadControlValueOverride = (InputAction action, IInputAxisOwner.AxisDescriptor.Hints hint, Object context, CinemachineInputAxisController.Reader.ControlValueReader defaultReader) => 
        {
            // 1. Odczytujemy surową wartość używając domyślnego readera (żeby uniknąć nieskończonej pętli przekazujemy null jako ostatni parametr)
            float value = defaultReader(action, hint, context, null);
            
            // 2. Sprawdzamy czy to oś Look (Nazwa akcji zawiera "Look")
            if (action != null && action.name.Contains("Look"))
            {
                if (SettingsManager.Instance == null || inputReader == null) return value;

                // 3. Pobieramy czułość dla aktywnego urządzenia
                float sensitivity = (inputReader.LastUsedDevice == InputReader.InputDeviceType.Mouse) 
                    ? SettingsManager.Instance.mouseSensitivity 
                    : SettingsManager.Instance.gamepadSensitivity;

                return value * sensitivity;
            }

            return value;
        };
        
        Debug.Log("<color=cyan>[CAMERA] CinemachineInputScaler zainicjalizowany (Globalnie - 4 args)!</color>");
    }
}
