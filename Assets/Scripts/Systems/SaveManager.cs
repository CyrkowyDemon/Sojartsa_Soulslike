using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System;
using Sojartsa.Inventory;

/// <summary>
/// Mordo, to jest mózg całego systemu zapisu. 
/// Obsługuje zapisywanie i wczytywanie wszystkiego przez JSONa.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Ustawienia Automatyczne")]
    public bool autoSaveOnExit = true;
    public float autoSaveInterval = 60f; // Domyślnie co minutę jako backup
    private float _autoSaveTimer;

    [Header("Konfiguracja")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private string saveFileExtension = ".json";
    
    private SaveData _currentSaveData;
    public SaveData CurrentSaveData => _currentSaveData;

    public int CurrentSlotIndex { get; private set; } = -1;

    private bool _isWaitingForScene = false;
    private float _sessionStartTime;
    private string _currentCharacterName = "Nieznany Wędrowiec";

    public void SetCharacterName(string newName) => _currentCharacterName = newName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_isWaitingForScene)
        {
            _isWaitingForScene = false;
            ApplyLoadedData();
        }
    }

    private void Update()
    {
        // Prosty timer dla backupu auto-zapisu
        if (SceneManager.GetActiveScene().name != "UI") // Nie zapisuj automatycznie w Main Menu
        {
            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= autoSaveInterval)
            {
                _autoSaveTimer = 0;
                SaveGame(0);
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnExit && SceneManager.GetActiveScene().name != "UI")
            SaveGame(0);
    }

    public string FormatTime(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours + (t.Days * 24), t.Minutes, t.Seconds);
    }

    public void SaveGame(int slotIndex)
    {
        _currentSaveData = (_currentSaveData == null) ? new SaveData() : _currentSaveData;
        _currentSaveData.lastSaveTime = DateTime.Now;
        _currentSaveData.lastScene = SceneManager.GetActiveScene().name;
        _currentSaveData.locationDisplayName = SceneManager.GetActiveScene().name;
        _currentSaveData.characterName = _currentCharacterName;

        // Czas gry
        float sessionTime = Time.time - _sessionStartTime;
        _currentSaveData.playTimeSeconds += sessionTime;
        _sessionStartTime = Time.time;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 pos = player.transform.position;
            Vector3 rot = player.transform.eulerAngles;
            _currentSaveData.playerPosition = new float[] { pos.x, pos.y, pos.z };
            _currentSaveData.playerRotation = new float[] { rot.x, rot.y, rot.z };
        }

        if (RespawnManager.Instance != null)
        {
            _currentSaveData.currentCheckpointID = RespawnManager.Instance.CheckpointID;
            _currentSaveData.lastScene = RespawnManager.Instance.CheckpointScene;
            
            _currentSaveData.deathDropAmount = RespawnManager.Instance.CurrentDropAmount;
            Vector3 dropPos = RespawnManager.Instance.CurrentDropPos;
            _currentSaveData.deathDropPos = new float[] { dropPos.x, dropPos.y, dropPos.z };
            _currentSaveData.deathDropScene = RespawnManager.Instance.CurrentDropScene;
        }

        if (string.IsNullOrEmpty(_currentSaveData.lastScene))
        {
            _currentSaveData.lastScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        if (InventoryController.Instance != null)
        {
            _currentSaveData.inventoryItems = PackInventory(InventoryController.Instance.inventorySlots);
            _currentSaveData.bagItems = PackInventory(InventoryController.Instance.bagSlots);
            _currentSaveData.equipmentItems = PackInventory(InventoryController.Instance.equipmentSlots);
        }

        _currentSaveData.openedChestIDs = new List<string>(_openedChestIDs);
        _currentSaveData.worldFlags = new Dictionary<string, bool>(_worldFlags);
        _currentSaveData.dialogueProgress = new Dictionary<string, int>(_dialogueProgress);

        string json = JsonConvert.SerializeObject(_currentSaveData, Formatting.Indented);
        string path = GetSavePath(slotIndex);
        File.WriteAllText(path, json);

        Debug.Log($"<color=cyan>[SAVE] Gra zapisana w slocie {slotIndex}!</color>");
    }

    public void DeleteGame(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"<color=red>[SAVE] Zapis usunięto ze slotu {slotIndex}</color>");
        }
    }

    public void SaveCurrentGame()
    {
        if (CurrentSlotIndex >= 0)
        {
            SaveGame(CurrentSlotIndex);
        }
        else
        {
            Debug.LogWarning("[SAVE] Brak ustawionego slotu! Zapisuję do domyślnego 0.");
            CurrentSlotIndex = 0;
            SaveGame(0);
        }
    }

    private List<string> _openedChestIDs = new List<string>();
    private Dictionary<string, bool> _worldFlags = new Dictionary<string, bool>();
    private Dictionary<string, int> _dialogueProgress = new Dictionary<string, int>();

    public void MarkChestAsOpened(string id)
    {
        if (!string.IsNullOrEmpty(id) && !_openedChestIDs.Contains(id))
            _openedChestIDs.Add(id);
    }

    public bool IsChestOpened(string id) => _openedChestIDs.Contains(id);

    public void SetWorldFlag(string key, bool value) { if (!string.IsNullOrEmpty(key)) _worldFlags[key] = value; }
    public bool GetWorldFlag(string key) => _worldFlags.ContainsKey(key) ? _worldFlags[key] : false;

    public void SetDialogueProgress(string npcID, int stage) { if (!string.IsNullOrEmpty(npcID)) _dialogueProgress[npcID] = stage; }
    public int GetDialogueProgress(string npcID) => _dialogueProgress.ContainsKey(npcID) ? _dialogueProgress[npcID] : 0;

    public bool HasSave(int slotIndex)
    {
        return File.Exists(GetSavePath(slotIndex));
    }

    public SaveData GetSaveMetadata(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SAVE] Błąd odczytu metadanych slotu {slotIndex}: {e.Message}");
            return null;
        }
    }

    public int GetMostRecentSaveSlot()
    {
        int mostRecentSlot = -1;
        DateTime mostRecentTime = DateTime.MinValue;

        for (int i = 0; i < 10; i++)
        {
            SaveData meta = GetSaveMetadata(i);
            if (meta != null && meta.lastSaveTime > mostRecentTime)
            {
                mostRecentTime = meta.lastSaveTime;
                mostRecentSlot = i;
            }
        }
        return mostRecentSlot;
    }

    public void StartNewGame(string charName, string startScene)
    {
        // Szukamy wolnego slotu (0-9)
        CurrentSlotIndex = 0;
        for (int i = 0; i < 10; i++)
        {
            if (!HasSave(i))
            {
                CurrentSlotIndex = i;
                break;
            }
        }

        _currentCharacterName = charName;
        _sessionStartTime = Time.time;
        _currentSaveData = new SaveData(); // Czyścimy stare dane
        _currentSaveData.characterName = charName;
        _currentSaveData.lastSaveTime = DateTime.Now;
        
        if (Sojartsa.UI.LoadingScreenManager.Instance != null)
        {
            Sojartsa.UI.LoadingScreenManager.Instance.LoadScene(startScene);
        }
        else
        {
            SceneManager.LoadScene(startScene);
        }
    }

    public void LoadGame(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (!File.Exists(path)) return;

        CurrentSlotIndex = slotIndex;
        string json = File.ReadAllText(path);
        _currentSaveData = JsonConvert.DeserializeObject<SaveData>(json);

        // Ratunek dla starych/uszkodzonych zapisów z pustą nazwą sceny
        if (string.IsNullOrEmpty(_currentSaveData.lastScene))
        {
            _currentSaveData.lastScene = "Pojezierza";
            Debug.LogWarning("[SAVE] Plik zapisu miał pustą scenę! Ustawiono domyślnie 'Pojezierza'.");
        }

        _isWaitingForScene = true;

        if (Sojartsa.UI.LoadingScreenManager.Instance != null)
        {
            Sojartsa.UI.LoadingScreenManager.Instance.LoadScene(_currentSaveData.lastScene);
        }
        else
        {
            SceneManager.LoadScene(_currentSaveData.lastScene);
        }
    }

    public void ApplyLoadedData()
    {
        if (_currentSaveData == null) return;

        _sessionStartTime = Time.time;
        _currentCharacterName = _currentSaveData.characterName;

        if (InventoryController.Instance != null && itemDatabase != null)
        {
            UnpackInventory(_currentSaveData.inventoryItems, InventoryController.Instance.inventorySlots);
            UnpackInventory(_currentSaveData.bagItems, InventoryController.Instance.bagSlots);
            UnpackInventory(_currentSaveData.equipmentItems, InventoryController.Instance.equipmentSlots);
            
            InventoryController.Instance.TriggerInventoryUpdate();
            InventoryController.Instance.TriggerEquipmentUpdate();
        }

        if (RespawnManager.Instance != null)
        {
            RespawnManager.Instance.SetCheckpoint(_currentSaveData.lastScene, _currentSaveData.currentCheckpointID);
            
            Vector3 loadedDropPos = Vector3.zero;
            if (_currentSaveData.deathDropPos != null && _currentSaveData.deathDropPos.Length >= 3)
                loadedDropPos = new Vector3(_currentSaveData.deathDropPos[0], _currentSaveData.deathDropPos[1], _currentSaveData.deathDropPos[2]);
                
            RespawnManager.Instance.LoadDeathDropData(_currentSaveData.deathDropAmount, loadedDropPos, _currentSaveData.deathDropScene);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && _currentSaveData.playerPosition != null && _currentSaveData.playerPosition.Length >= 3)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            player.transform.position = new Vector3(_currentSaveData.playerPosition[0], _currentSaveData.playerPosition[1], _currentSaveData.playerPosition[2]);
            if (_currentSaveData.playerRotation != null && _currentSaveData.playerRotation.Length >= 3)
            {
                player.transform.eulerAngles = new Vector3(_currentSaveData.playerRotation[0], _currentSaveData.playerRotation[1], _currentSaveData.playerRotation[2]);
            }
            
            if (cc != null) cc.enabled = true;
        }

        _openedChestIDs = new List<string>(_currentSaveData.openedChestIDs);
        _worldFlags = new Dictionary<string, bool>(_currentSaveData.worldFlags);
        _dialogueProgress = new Dictionary<string, int>(_currentSaveData.dialogueProgress);

        ChestController[] allChests = UnityEngine.Object.FindObjectsByType<ChestController>(FindObjectsSortMode.None);
        foreach (var chest in allChests)
        {
            if (!string.IsNullOrEmpty(chest.uniqueID) && _openedChestIDs.Contains(chest.uniqueID))
                chest.ForceOpen();
        }

        Debug.Log("<color=lime>[SAVE] Dane wczytane pomyślnie!</color>");
    }

    private List<InventorySaveEntry> PackInventory(List<InventorySlot> slots)
    {
        List<InventorySaveEntry> packed = new List<InventorySaveEntry>();
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty) packed.Add(new InventorySaveEntry(slot.item.itemID, slot.amount));
            else packed.Add(new InventorySaveEntry("", 0));
        }
        return packed;
    }

    private void UnpackInventory(List<InventorySaveEntry> data, List<InventorySlot> targetSlots)
    {
        for (int i = 0; i < targetSlots.Count; i++)
        {
            if (i >= data.Count) break;
            if (!string.IsNullOrEmpty(data[i].itemID))
            {
                targetSlots[i].item = itemDatabase.GetItemByID(data[i].itemID);
                targetSlots[i].amount = data[i].amount;
            }
            else targetSlots[i].Clear();
        }
    }

    private string GetSavePath(int slotIndex) => Path.Combine(Application.persistentDataPath, $"save_slot_{slotIndex}{saveFileExtension}");
}
