using UnityEngine;

/// <summary>
/// Mordo, to jest skrypt na Twoje drzwi (albo jakikolwiek trigger).
/// Wrzucasz to na obiekt z BoxColliderem (IsTrigger = true).
/// W Inspektorze wpisujesz nazwę sceny i ID punktu, gdzie gracz ma wylądować.
/// </summary>
public class WarpTrigger : MonoBehaviour
{
    [Header("Ustawienia Portalu")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnID;
    
    [Header("Pamięć Powrotna")]
    [Tooltip("Jeśli zaznaczone, drzwi zlekceważą pola powyżej i wrócą Cię tam skąd przyszedłeś.")]
    [SerializeField] private bool useReturnMemory = false;
    [Tooltip("Jeśli to NIE SĄ drzwi powrotne, możesz tu wpisać ID punktu, pod którym masz się pojawić, gdy kiedyś będziesz WRACAŁ przez te drzwi.")]
    [SerializeField] private string returnIDForDestination;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (WarpManager.Instance != null)
            {
                if (useReturnMemory)
                {
                    Debug.Log($"<color=cyan>[PORTAL] {gameObject.name}: Wracamy skąd przyszliśmy!</color>");
                    WarpManager.Instance.WarpBack();
                }
                else
                {
                    Debug.Log($"<color=cyan>[PORTAL] {gameObject.name}: Idziemy do {targetSceneName} (ReturnID: {returnIDForDestination})</color>");
                    WarpManager.Instance.Warp(targetSceneName, targetSpawnID, returnIDForDestination);
                }
            }
            else
            {
                Debug.LogError("<color=red>[PORTAL] Brak WarpManagera!</color>");
            }
        }
    }
}
