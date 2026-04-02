using UnityEngine;
using UnityEngine.Events;

public class ChestController : MonoBehaviour, IInteractable
{
    [Header("Skrzynia: Stan i Animacja")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool isOpen = false;
    [SerializeField] private string openAnimationParam = "IsOpen";

    [SerializeField] private bool canBeClosed = true;
    [SerializeField] private float animationDuration = 1.5f; // Tyle trwa Twoja animacja
    private bool isAnimating = false;

    [Header("Skrzynia: Punkt Interakcji (Styl FromSoft)")]
    [Tooltip("Miejsce, gdzie gracz ma ustać, żeby animacja wyglądała idealnie.")]
    public Transform interactionPoint;

    [Header("Wydarzenia (Loot, Dźwięk, VFX)")]
    public UnityEvent OnChestOpened;
    public UnityEvent OnChestClosed;

    // --- Implementacja IInteractable ---

    public void Interact(Transform interactor)
    {
        if (isAnimating) return; // BLOKADA: Nie spamuj "E"!

        if (!isOpen)
        {
            OpenChest();
        }
        else if (canBeClosed)
        {
            CloseChest();
        }
    }

    public string GetInteractText()
    {
        if (isAnimating) return ""; // Podczas animacji nie ma tekstu
        if (!isOpen) return "Otwórz skrzynię";
        return canBeClosed ? "Zamknij skrzynię" : "";
    }

    public bool CanInteract()
    {
        if (isAnimating) return false; // Nie pozwól na interakcję w trakcie pracy
        if (!isOpen) return true;
        return canBeClosed;
    }

    // --- Logika Skrzyni ---

    private void OpenChest()
    {
        isAnimating = true; // Rozpoczynamy pracę
        isOpen = true;
        
        // 1. Odpalamy animację w Unity Animatorze (o ile ma on przypisany Controller!)
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(openAnimationParam, true);
        }

        // 2. Wywołujemy zdarzenia (np. wylosowanie itemu, dźwięk otwierania)
        Debug.Log("[ChestController] Skrzynia otwarta! Wywołuję UnityEvents.");
        OnChestOpened?.Invoke();

        // 3. Po czasie animacji ściągamy blokadę
        Invoke(nameof(ResetAnimationFlag), animationDuration);
    }

    private void CloseChest()
    {
        isAnimating = true; // Rozpoczynamy pracę
        isOpen = false;

        // 1. Odpalamy animację zamykania
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(openAnimationParam, false);
        }

        // 2. Wywołujemy zdarzenia (np. dźwięk zatrzaśnięcia)
        Debug.Log("[ChestController] Skrzynia zamknięta!");
        OnChestClosed?.Invoke();

        // 3. Po czasie animacji ściągamy blokadę
        Invoke(nameof(ResetAnimationFlag), animationDuration);
    }

    private void ResetAnimationFlag()
    {
        isAnimating = false; // System znowu gotowy
    }
}
