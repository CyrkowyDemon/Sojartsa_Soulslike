using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple Object Pooler - Styl FromSoftware (oszczędność pamięci i procesora).
/// Zamiast Instantiate/Destroy, używamy tego do VFX i świateł trafień.
/// </summary>
public class SimplePool : MonoBehaviour
{
    private static Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!_pools.ContainsKey(prefab))
        {
            _pools[prefab] = new Queue<GameObject>();
        }

        GameObject instance;

        if (_pools[prefab].Count > 0)
        {
            instance = _pools[prefab].Dequeue();
            if (instance == null)
            {
                instance = GameObject.Instantiate(prefab);
            }
        }
        else
        {
            instance = GameObject.Instantiate(prefab);
        }

        instance.transform.position = position;
        instance.transform.rotation = rotation;
        instance.SetActive(true);

        return instance;
    }

    /// <summary>
    /// Automatycznie chowa obiekt do puli po upływie czasu.
    /// </summary>
    public static void Despawn(GameObject instance, GameObject prefab, float delay = 0f)
    {
        if (instance == null || prefab == null) return;

        // Jeśli opóźnienie > 0, odpalamy Coroutine na instancji poola
        if (delay > 0)
        {
            // Szukamy instancji poola na scenie, żeby móc odpalić Coroutine
            SimplePool pool = FindFirstObjectByType<SimplePool>();
            if (pool == null)
            {
                // Automatycznie tworzymy Managera jeśli go nie ma na scenie
                GameObject poolGo = new GameObject("[SYSTEM] SimplePool Manager");
                pool = poolGo.AddComponent<SimplePool>();
            }
            
            pool.StartCoroutine(pool.DespawnCoroutine(instance, prefab, delay));
        }
        else
        {
            DoDespawn(instance, prefab);
        }
    }

    private System.Collections.IEnumerator DespawnCoroutine(GameObject instance, GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        DoDespawn(instance, prefab);
    }

    private static void DoDespawn(GameObject instance, GameObject prefab)
    {
        if (instance == null) return;
        instance.SetActive(false);
        
        if (!_pools.ContainsKey(prefab))
        {
            _pools[prefab] = new Queue<GameObject>();
        }

        if (!_pools[prefab].Contains(instance))
        {
            _pools[prefab].Enqueue(instance);
        }
    }
}

