using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterDeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                Debug.Log("<color=blue>[WATER] Gracz wpadł do wody! Natychmiastowa śmierć.</color>");
                
                // Bezlitosna śmierć w stylu Souls: Woda ignoruje i-frames z dasha!
                health.DisableIFrames(); 
                health.TakeDamage(9999);
            }
        }
    }
}
