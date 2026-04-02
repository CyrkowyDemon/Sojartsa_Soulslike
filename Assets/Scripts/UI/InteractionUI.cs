using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    [Header("Referencje UI")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;

    [Header("Skąd czerpiemy dane?")]
    [SerializeField] private PlayerInteractor playerInteractor;

    private void Update()
    {
        // 1. Sprawdzamy co ma pod ręką nasz Gracz
        if (playerInteractor != null)
        {
            IInteractable interactable = playerInteractor.GetCurrentInteractable();
            
            // 2. Jeśli coś mamy pod ręką, pokazujemy napis
            if (interactable != null)
            {
                promptText.text = interactable.GetInteractText();
                promptPanel.SetActive(true);
            }
            else
            {
                // 3. Jeśli odejdziemy - Panel znika
                promptPanel.SetActive(false);
            }
        }
    }
}
