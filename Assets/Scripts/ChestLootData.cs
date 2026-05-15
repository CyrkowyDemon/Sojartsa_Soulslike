using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "NewChestLoot", menuName = "Combat/Chest Loot Data")]
public class ChestLootData : ScriptableObject
{
    public List<LootEntry> entries = new List<LootEntry>();
}

[Serializable]
public struct LootEntry
{
    [Tooltip("Przy którym otwarciu ta nagroda ma wpaść? (Dla pierwszego otwarcia wpisz 1)")]
    public int requiredOpenCount;
    
    [Header("Nagroda Pieniężna (Skrót)")]
    [Tooltip("Ile waluty (np. monet) gracz dostaje. Automatycznie używa przedmiotu zdefiniowanego w CurrencyManager.")]
    public int currencyAmount;

    [Header("Nagroda Przedmiotowa")]
    [Tooltip("Konkretny przedmiot, który gracz dostaje.")]
    public ItemData itemReward;
    
    [Tooltip("Ilość powyższego przedmiotu.")]
    public int itemAmount;
    
    [Header("Ustawienia")]
    [Tooltip("Jeśli zaznaczone, nagroda wpadnie tylko raz (nawet jeśli skrzynia zostanie zamknięta i otwarta ponownie).")]
    public bool giveOnlyOnce;
}
