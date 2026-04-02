using UnityEngine;

public interface IInteractable
{
    // Co się dzieje jak klikniesz [E]?
    // Przekazujemy interactor (postać gracza), żeby skrzynia wiedziała gdzie stoi gracz
    void Interact(Transform interactor);

    // Tekst, który pokaże się w UI (np. "Open Chest", "Talk", "Pull Lever")
    string GetInteractText();

    // Sprawdzamy, czy w ogóle można teraz kliknąć (np. skrzynia już otwarta -> stop)
    bool CanInteract();
}
