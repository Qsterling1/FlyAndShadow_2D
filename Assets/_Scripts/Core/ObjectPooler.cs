using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        [Tooltip("The prefab to pool.")]
        public GameObject prefab;
        [Tooltip("The initial number of objects to create in this pool.")]
        public int size;
    }

    public static ObjectPooler Instance;

    private void Awake()
    {
        Instance = this;
    }

    [Header("Pools to Create on Start")]
    public List<Pool> pools;

    // The dictionary now uses the prefab's unique Instance ID (an integer) as its key.
    private Dictionary<int, Queue<GameObject>> poolDictionary;
    
    // This second dictionary lets us find the original prefab from a cloned instance.
    private Dictionary<GameObject, int> instanceIdMap;

    void Start()
    {
        poolDictionary = new Dictionary<int, Queue<GameObject>>();
        instanceIdMap = new Dictionary<GameObject, int>();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null) continue;

            Queue<GameObject> objectPool = new Queue<GameObject>();
            int prefabId = pool.prefab.GetInstanceID();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                instanceIdMap.Add(obj, prefabId); // Link the clone to its original prefab's ID
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }
            
            if (!poolDictionary.ContainsKey(prefabId))
            {
                poolDictionary.Add(prefabId, objectPool);
            }
        }
    }

    /// <summary>
    /// Spawns an object from the pool associated with the given prefab.
    /// </summary>
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int prefabId = prefab.GetInstanceID();

        if (!poolDictionary.ContainsKey(prefabId))
        {
            Debug.LogWarning("Pool for prefab " + prefab.name + " doesn't exist.");
            return null;
        }

       // If the pool for this prefab is empty, create a new object to prevent errors.
if (poolDictionary[prefabId].Count == 0)
{
    Debug.LogWarning("Pool for " + prefab.name + " was empty. Expanding pool size.");
    GameObject obj = Instantiate(prefab);
    instanceIdMap.Add(obj, prefabId); // Link the new clone to its original prefab's ID
    obj.SetActive(false);
    poolDictionary[prefabId].Enqueue(obj);
}

GameObject objectToSpawn = poolDictionary[prefabId].Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
// Check if the object has a component that needs to be reset on spawn.
IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
if (pooledObj != null)
{
    pooledObj.OnObjectSpawn();
}

return objectToSpawn;
    }
    
    /// <summary>
    /// Returns an object to its pool.
    /// </summary>
    public void ReturnToPool(GameObject objectToReturn)
    {
        if (instanceIdMap.TryGetValue(objectToReturn, out int prefabId))
        {
            objectToReturn.SetActive(false);
            poolDictionary[prefabId].Enqueue(objectToReturn);
        }
        else
        {
            Debug.LogWarning("Tried to return an object to the pool that was not created by it: " + objectToReturn.name);
            Destroy(objectToReturn);
        }
    }
}