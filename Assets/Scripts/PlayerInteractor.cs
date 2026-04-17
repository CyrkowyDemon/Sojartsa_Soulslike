using UnityEngine;
using System.Collections.Generic;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Ustawienia Detekcji")]
    [SerializeField] private float detectionRadius = 2.0f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private InputReader inputReader;

    [Header("Ustawienia Widoczności (Dot Product)")]
    [Tooltip("Im bliżej 1.0, tym prościej musisz patrzeć na obiekt (0.7 to klasyczny Soulsowy kąt ok. 45 stopni)")]
    [Range(0, 1)]
    [SerializeField] private float facingThreshold = 0.5f;

    private readonly Collider[] _overlapResults = new Collider[10];
    private IInteractable currentInteractable;

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.InteractEvent += HandleInteraction;
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.InteractEvent -= HandleInteraction;
        }
    }

    private void Update()
    {
        FindClosestInteractable();
    }

    private void FindClosestInteractable()
    {
        // 1. Zbieramy collidery (NonAlloc - brak śmieci w pamięci!)
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, _overlapResults, interactableLayer);
        
        IInteractable closest = null;
        float minSqrDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapResults[i];
            
            // 2. Czy obiekt ma nasz interfejs?
            IInteractable interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract()) continue;

            // 3. Sprawdźmy, czy patrzymy w jego stronę (Souls Style!)
            Vector3 toObj = col.transform.position - transform.position;
            float sqrDist = toObj.sqrMagnitude;
            
            float dot = Vector3.Dot(transform.forward, toObj.normalized);
            if (dot < facingThreshold) continue;

            // 4. Wybieramy najbliższy
            if (sqrDist < minSqrDistance)
            {
                minSqrDistance = sqrDist;
                closest = interactable;
            }
        }

        currentInteractable = closest;
    }

    private void HandleInteraction()
    {
        // PRO FIX: Jeśli trwa dialog, zlewamy system patrzenia na NPC i od razu skipujemy!
        if (DialogueManager.Instance != null && DialogueManager.Instance.dialoguePanel != null && DialogueManager.Instance.dialoguePanel.activeInHierarchy)
        {
            DialogueManager.Instance.DisplayNextNode();
            return;
        }

        // Kliknąłeś E! Jeśli coś mamy pod nosem, to gadamy z tym.
        if (currentInteractable != null)
        {
            Debug.Log($"[PlayerInteractor] Interakcja z: {currentInteractable.GetType().Name}");
            currentInteractable.Interact(this.transform);
        }
    }

    // Widok w edytorze, żebyś wiedział jak duży jest zasięg
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    // Publiczna metoda dla UI, żeby wiedziało co napisać
    public IInteractable GetCurrentInteractable() => currentInteractable;
}
