using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inteligentna strefa, która automatycznie wywołuje dialog z NPC (np. Melina przy ognisku).
/// Wrzucasz ten skrypt na oddzielny, pusty obiekt z Colliderem ustawionym na "Is Trigger".
/// </summary>
public class NPCTriggerZone : MonoBehaviour
{
    [Header("Ustawienia")]
    [SerializeField] private PeacefulNPC targetNPC;
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Warunki Aktywacji (Listy)")]
    [Tooltip("Wszystkie te flagi MUSZĄ być w pamięci świata, żeby strefa zadziałała.")]
    public List<WorldFlagSO> requiredFlags = new List<WorldFlagSO>(); 
    [Tooltip("Jeśli KTÓRAKOLWIEK z tych flag jest w pamięci, strefa zostanie ZABLOKOWANA.")]
    public List<WorldFlagSO> excludeFlags = new List<WorldFlagSO>(); 
    
    private bool _hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Sprawdzamy czy to gracz (Tag musi być ustawiony na "Player")
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && _hasTriggered) return;

            // 1. Sprawdź czy spełniamy WSZYSTKIE wymagania (Logic AND)
            foreach (var flag in requiredFlags)
            {
                if (!WorldStateManager.Instance.HasFlag(flag)) return;
            }

            // 2. Sprawdź czy nie jesteśmy WYKLUCZENI (Logic OR - którakolwiek wystarczy by wyłączyć)
            foreach (var flag in excludeFlags)
            {
                if (WorldStateManager.Instance.HasFlag(flag)) return;
            }

            if (targetNPC != null)
            {
                Debug.Log($"[TriggerZone] Gracz {other.name} wchodzi w zasięg {targetNPC.name}.");
                
                // Wywołujemy interakcję u NPC (on sam dobierze odpowiedni dialog na liście)
                targetNPC.Interact(other.transform); 
                
                _hasTriggered = true;
            }
        }
    }
}
