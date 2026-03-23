using UnityEngine;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Czułość Sterowania")]
    [Range(0.1f, 10f)] public float mouseSensitivity = 1.0f;
    [Range(0.1f, 10f)] public float gamepadSensitivity = 3.0f;

    // Event informujący inne skrypty o zmianie ustawień
    public event Action OnSettingsUpdated;

    // Automatycznie tworzy Menedżera Ustawień przy starcie jakiejkolwiek sceny!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeOnLoad()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("SettingsManager_AutoSpawned");
            Instance = go.AddComponent<SettingsManager>();
            DontDestroyOnLoad(go);
            Instance.LoadSettings();
        }
    }

    private void Awake()
    {
        // Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Zapobiega podwójnemu załadowaniu, jeśli został już wywołany przez AutoSpawn
        if (PlayerPrefs.HasKey("MouseSensitivity")) 
        {
            LoadSettings();
        }
    }

    public void SaveSettings(float mouse, float gamepad)
    {
        mouseSensitivity = mouse;
        gamepadSensitivity = gamepad;

        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
        PlayerPrefs.SetFloat("GamepadSensitivity", gamepadSensitivity);
        PlayerPrefs.Save();

        OnSettingsUpdated?.Invoke();
        Debug.Log($"[SETTINGS] Zapisano: Mouse={mouseSensitivity}, Gamepad={gamepadSensitivity}");
    }

    public void LoadSettings()
    {
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
        gamepadSensitivity = PlayerPrefs.GetFloat("GamepadSensitivity", 3.0f);
        
        OnSettingsUpdated?.Invoke();
        Debug.Log($"[SETTINGS] Wczytano: Mouse={mouseSensitivity}, Gamepad={gamepadSensitivity}");
    }
}
