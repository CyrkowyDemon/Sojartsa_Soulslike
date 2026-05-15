using UnityEngine;

/// <summary>
/// Obiekt "Pozostałości" (Bloodstain), który gracz może podnieść po śmierci,
/// aby odzyskać straconą walutę.
/// </summary>
public class DeathDrop : MonoBehaviour, IInteractable
{
    private int _storedAmount;
    private string _interactText = "Odzyskaj monety";

    /// <summary>
    /// Inicjalizuje drop wartością.
    /// </summary>
    public void Setup(int amount)
    {
        _storedAmount = amount;
    }

    public void Interact(Transform interactor)
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(_storedAmount);
            Debug.Log($"<color=gold>[RECOVERY] Odzyskano {_storedAmount} monet!</color>");
            
            // Informujemy RespawnManager, że podnieśliśmy drop, żeby go wyczyścił z pamięci
            if (RespawnManager.Instance != null)
            {
                RespawnManager.Instance.ClearActiveDrop();
            }

            // Znikamy ze świata
            Destroy(gameObject);
        }
    }

    public string GetInteractText() => $"{_interactText} ({_storedAmount})";
    public bool CanInteract() => true;
}
