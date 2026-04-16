using UnityEngine;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Sterowanie")]
    [Range(0.1f, 10f)] public float mouseSensitivity = 1.0f;
    [Range(0.1f, 10f)] public float gamepadSensitivity = 3.0f;
    public bool invertX = false;
    public bool invertY = false;
    [Range(0f, 1f)] public float vibrationLevel = 1.0f;

    [Header("Dźwięk i UI")]
    public bool showSubtitles = true;
    public bool showHUD = true;
    [Range(0f, 1f)] public float musicVolume = 1.0f;
    [Range(0f, 1f)] public float soundsVolume = 1.0f;
    [Range(0f, 1f)] public float voiceVolume = 1.0f;

    [Header("Grafika")]
    public int qualityIndex = 2; 
    public int screenModeIndex = 0; 
    public int resolutionIndex = 0;
    public bool showBlood = true;

    [Header("Opcje Gry")]
    public bool autoLock = true;
    public bool autoAim = true;

    public event Action OnSettingsUpdated;

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // === FROMSOFTWARE STANDARD: Blokada 60 FPS ===
        // VSync musi być wyłączony, żeby targetFrameRate był jedynym szefem!
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Debug.Log($"<color=lime>[FPS] Blokada aktywna! Target: {Application.targetFrameRate} FPS | vSync: {QualitySettings.vSyncCount}</color>");
        // ==============================================

        LoadSettings();
    }

    // Zapis dla Sterowania
    public void SaveSettings(float mouse, float gamepad, bool invX, bool invY, float vib)
    {
        mouseSensitivity = mouse;
        gamepadSensitivity = gamepad;
        invertX = invX;
        invertY = invY;
        vibrationLevel = vib;

        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
        PlayerPrefs.SetFloat("GamepadSensitivity", gamepadSensitivity);
        PlayerPrefs.SetInt("InvertX", invertX ? 1 : 0);
        PlayerPrefs.SetInt("InvertY", invertY ? 1 : 0);
        PlayerPrefs.SetFloat("VibrationLevel", vibrationLevel);
        PlayerPrefs.Save();
        OnSettingsUpdated?.Invoke();
    }
        public void SaveGameSettings(bool useLock, bool aim)
    {
        autoLock = useLock;
        autoAim = aim;

        PlayerPrefs.SetInt("AutoLock", autoLock ? 1 : 0);
        PlayerPrefs.SetInt("AutoAim", autoAim ? 1 : 0);
        PlayerPrefs.Save();
        
        OnSettingsUpdated?.Invoke();
    }
    // Zapis dla Audio i UI
    public void SaveAudioAndUISettings(bool subtitles, bool hud, float music, float sounds, float voice)
    {
        showSubtitles = subtitles;
        showHUD = hud;
        musicVolume = music;
        soundsVolume = sounds;
        voiceVolume = voice;

        PlayerPrefs.SetInt("ShowSubtitles", showSubtitles ? 1 : 0);
        PlayerPrefs.SetInt("ShowHUD", showHUD ? 1 : 0);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SoundsVolume", soundsVolume);
        PlayerPrefs.SetFloat("VoiceVolume", voiceVolume);
        PlayerPrefs.Save();
        OnSettingsUpdated?.Invoke();
    }

    // Zapis dla Grafiki
    public void SaveGraphicsSettings(int quality, int screenMode, int resIndex, bool blood)
    {
        qualityIndex = quality;
        screenModeIndex = screenMode;
        resolutionIndex = resIndex;
        showBlood = blood;

        PlayerPrefs.SetInt("QualityIndex", qualityIndex);
        PlayerPrefs.SetInt("ScreenModeIndex", screenModeIndex);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.SetInt("ShowBlood", showBlood ? 1 : 0);
        PlayerPrefs.Save();

        ApplyGraphics();
        OnSettingsUpdated?.Invoke();
    }

    public void ApplyGraphics()
    {
        // Upewniamy się, że blokada FPS nie zostanie przypadkowo wyłączona przez Unity
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        QualitySettings.SetQualityLevel(qualityIndex);

        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        if (screenModeIndex == 1) mode = FullScreenMode.FullScreenWindow;
        if (screenModeIndex == 2) mode = FullScreenMode.Windowed;

        Resolution[] resolutions = Screen.resolutions;
        if (resolutions.Length > 0)
        {
            // Zabezpieczenie przed wyjściem poza zakres, jeśli monitor się zmienił
            int index = Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1);
            Resolution res = resolutions[index];
            // Zawsze wybieramy najwyższe możliwe odświeżanie dla tej rozdzielczości
            Screen.SetResolution(res.width, res.height, mode);
        }
    }

    public void LoadSettings()
    {
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
        gamepadSensitivity = PlayerPrefs.GetFloat("GamepadSensitivity", 3.0f);
        invertX = PlayerPrefs.GetInt("InvertX", 0) == 1;
        invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;
        vibrationLevel = PlayerPrefs.GetFloat("VibrationLevel", 1.0f);

        showSubtitles = PlayerPrefs.GetInt("ShowSubtitles", 1) == 1;
        showHUD = PlayerPrefs.GetInt("ShowHUD", 1) == 1;
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        soundsVolume = PlayerPrefs.GetFloat("SoundsVolume", 1.0f);
        voiceVolume = PlayerPrefs.GetFloat("VoiceVolume", 1.0f);

        qualityIndex = PlayerPrefs.GetInt("QualityIndex", 2);
        screenModeIndex = PlayerPrefs.GetInt("ScreenModeIndex", 0);
        resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", Screen.resolutions.Length - 1);
        showBlood = PlayerPrefs.GetInt("ShowBlood", 1) == 1;

        autoLock = PlayerPrefs.GetInt("AutoLock", 1) == 1;
        autoAim = PlayerPrefs.GetInt("AutoAim", 1) == 1;

        ApplyGraphics();
        OnSettingsUpdated?.Invoke();
    }
}