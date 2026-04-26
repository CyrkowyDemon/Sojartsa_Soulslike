using UnityEngine;

/// <summary>
/// Punkt kontrolny (Ognisko/Kapliczka), który służy jako miejsce odrodzenia
/// i odnawia statystyki gracza.
/// </summary>
public class Checkpoint : MonoBehaviour, IInteractable
{
    [Header("Ustawienia")]
    [SerializeField] private string checkpointID; // Musi pasować do ID SpawnPointa!
    [SerializeField] private string interactText = "Odpocznij przy kapliczce";
    
    [Header("Wizualia")]
    [SerializeField] private GameObject activeVFX;
    [SerializeField] private bool isActivated = false;

    private void Start()
    {
        if (activeVFX != null) activeVFX.SetActive(isActivated);
    }

    public void Interact(Transform interactor)
    {
        ActivateCheckpoint();
        
        // Odnawiamy życie gracza
        PlayerHealth ph = interactor.GetComponent<PlayerHealth>();
        if (ph != null) ph.Revive();

        Debug.Log($"<color=lime>[CHECKPOINT] Odpoczynek: {checkpointID}. Życie odnowione.</color>");
    }

    public void ActivateCheckpoint()
    {
        isActivated = true;
        if (activeVFX != null) activeVFX.SetActive(true);

        // Rejestrujemy punkt w WarpManagerze
        if (WarpManager.Instance != null)
        {
            // Przekazujemy obecną scenę i ID punktu jako "ostatni punkt"
            WarpManager.Instance.SetLastCheckpoint(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, 
                checkpointID
            );
        }
    }

    public string GetInteractText() => interactText;
    public bool CanInteract() => true; // Zawsze można odpocząć

    private void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(0.5f, 2f, 0.5f));
    }
}
