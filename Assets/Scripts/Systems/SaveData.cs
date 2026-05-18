using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Mordo, to jest kontener na Twoje dane. Wszystko co tu wpiszesz, trafi do pliku .json.
/// </summary>
[Serializable]
public class SaveData
{
    [Header("Informacje o Sesji")]
    public string saveName = "Nowa Gra";
    public string characterName = "Nieznany Wędrowiec";
    public float playTimeSeconds;
    public string locationDisplayName = "Początek drogi";
    public string lastScene;
    public DateTime lastSaveTime;

    [Header("Gracz")]
    public float[] playerPosition = new float[3];
    public float[] playerRotation = new float[3];
    public string currentCheckpointID;

    [Header("Ekwipunek")]
    public List<InventorySaveEntry> inventoryItems = new List<InventorySaveEntry>();
    public List<InventorySaveEntry> bagItems = new List<InventorySaveEntry>();
    public List<InventorySaveEntry> equipmentItems = new List<InventorySaveEntry>();

    [Header("Świat i Postęp")]
    public List<string> openedChestIDs = new List<string>();
    public Dictionary<string, bool> worldFlags = new Dictionary<string, bool>();
    public Dictionary<string, int> dialogueProgress = new Dictionary<string, int>();
    
    [Header("Death Drop")]
    public int deathDropAmount;
    public float[] deathDropPos = new float[3];
    public string deathDropScene;
}

[Serializable]
public class InventorySaveEntry
{
    public string itemID;
    public int amount;

    public InventorySaveEntry(string id, int count)
    {
        itemID = id;
        amount = count;
    }
}
