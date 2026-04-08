using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public string identifier;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position, transform.forward * 1.0f);
    }
}
