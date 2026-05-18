using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mordo, to jest Twoja wielka księga wszystkich przedmiotów w grze.
/// Musisz tu wrzucić każdy ItemData, który chcesz, żeby gra potrafiła zapisać i wczytać.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Sojartsa/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Tooltip("Przeciągnij tutaj wszystkie swoje assety ItemData (Monety, Bronie, Enchanty itp.)")]
    public List<ItemData> allItems = new List<ItemData>();

    /// <summary>
    /// Znajduje asset przedmiotu na podstawie jego tekstowego ID.
    /// </summary>
    public ItemData GetItemByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (var item in allItems)
        {
            if (item != null && item.itemID == id)
            {
                return item;
            }
        }

        Debug.LogWarning($"[ITEM DATABASE] Nie znaleziono przedmiotu o ID: {id}. Sprawdź czy jest dodany do bazy!");
        return null;
    }
}
