using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public struct LootEntry
{
    [Tooltip("Przy którym otwarciu ta nagroda ma wpaść? (np. 1 dla pierwszego razu)")]
    public int requiredOpenCount;
    
    [Tooltip("Ile dusz gracz dostaje.")]
    public int soulsAmount;
    
    [Tooltip("Jeśli zaznaczone, nagroda wpadnie tylko raz, nawet jak gracz zamknie i otworzy skrzynię ponownie.")]
    public bool giveOnlyOnce;
}

[CreateAssetMenu(fileName = "NewChestLoot", menuName = "Combat/Chest Loot Data")]
public class ChestLootData : ScriptableObject
{
    public List<LootEntry> entries = new List<LootEntry>();
}
