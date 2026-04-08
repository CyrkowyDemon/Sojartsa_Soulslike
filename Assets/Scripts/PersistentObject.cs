using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    private static PersistentObject instance;

    private void Awake()
    {
        // Simple Singleton pattern for persistence
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // Don't allow duplicates
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
