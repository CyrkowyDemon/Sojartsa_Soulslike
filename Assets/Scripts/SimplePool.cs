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
            if (instance == null) // Na wypadek, gdyby obiekt został zniszczony w scenie
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

    public static void Despawn(GameObject instance, GameObject prefab)
    {
        if (instance == null || prefab == null) return;

        instance.SetActive(false);
        
        if (!_pools.ContainsKey(prefab))
        {
            _pools[prefab] = new Queue<GameObject>();
        }

        _pools[prefab].Enqueue(instance);
    }
}
