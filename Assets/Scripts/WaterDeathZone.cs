using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterDeathZone : MonoBehaviour
{
    public float gracePeriod = 1.5f;
    private float _entryTime = 0f;
    private bool _inWater = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _inWater = true;
            _entryTime = Time.time;
            Debug.Log("<color=blue>[WATER] Gracz wpadł do wody! Odliczanie: 1.5s!</color>");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (_inWater && other.CompareTag("Player"))
        {
            float timeInWater = Time.time - _entryTime;
            if (timeInWater >= gracePeriod)
            {
                PlayerHealth health = other.GetComponent<PlayerHealth>();
                if (health != null && !health.IsDead)
                {
                    Debug.Log("<color=red>[WATER] KONIEC CZASU! Gracz utonął.</color>");
                    health.TakeDamage(9999);
                    _inWater = false; // Zapobiegamy spamowaniu po śmierci
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _inWater = false;
            Debug.Log("<color=green>[WATER] Gracz uciekł z wody!</color>");
        }
    }
}
