using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Globalny rejestr postępów. 
/// Pamięta, które flagi (WorldFlagSO) zostały już aktywowane.
/// </summary>
public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance { get; private set; }

    [SerializeField] private List<WorldFlagSO> activeFlags = new List<WorldFlagSO>();
    private HashSet<WorldFlagSO> _flagSet = new HashSet<WorldFlagSO>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Konwertujemy listę na hashset dla błyskawicznego sprawdzania (0.001ms)
            foreach (var flag in activeFlags) _flagSet.Add(flag);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddFlag(WorldFlagSO flag)
    {
        if (flag == null) return;
        if (!_flagSet.Contains(flag))
        {
            _flagSet.Add(flag);
            activeFlags.Add(flag); // Synchronizujemy z listą dla podglądu w Inspektorze
            Debug.Log($"<color=green>[WORLD] Aktywowano flagę: {flag.name}</color>");
        }
    }

    public bool HasFlag(WorldFlagSO flag)
    {
        if (flag == null) return true; // Jeśli warunek jest pusty, to znaczy że zawsze spełniony
        return _flagSet.Contains(flag);
    }
}
