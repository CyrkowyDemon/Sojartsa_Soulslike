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
        // 1. Zbieramy wszystkie collidery w zasięgu (Zamiast Raycastu, używamy Overlapa - to milsze dla gracza)
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, interactableLayer);
        
        IInteractable closest = null;
        float minDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            // 2. Czy obiekt ma nasz interfejs?
            IInteractable interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract()) continue;

            // 3. Sprawdźmy, czy patrzymy w jego stronę (Souls Style!)
            Vector3 directionToObj = (col.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, directionToObj);

            if (dot < facingThreshold) continue;

            // 4. Wybieramy najbliższy, jeśli jest ich kilka
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = interactable;
            }
        }

        currentInteractable = closest;
    }

    private void HandleInteraction()
    {
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
