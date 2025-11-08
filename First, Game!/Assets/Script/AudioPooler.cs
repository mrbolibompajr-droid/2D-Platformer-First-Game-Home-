using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudioPoolItem
{
    public string name;              // Name used to spawn
    public GameObject prefab;        // Prefab to spawn
    public int size = 5;             // Number of pooled objects
    [Range(0f, 1f)]
    public float volume = 1f;        // Default volume for this sound
}

public class AudioPooler : MonoBehaviour
{
    public List<AudioPoolItem> audioItems;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (AudioPoolItem item in audioItems)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < item.size; i++)
            {
                GameObject obj = Instantiate(item.prefab);
                obj.name = item.prefab.name; // Ensure consistent name for pooling
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(item.name, objectPool);
        }
    }

    public GameObject SpawnFromPool(string name, Vector3 position)
    {
        if (!poolDictionary.ContainsKey(name))
        {
            Debug.LogWarning("No pool exists for " + name);
            return null;
        }

        GameObject objectToSpawn = poolDictionary[name].Dequeue();
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;

        AudioSource source = objectToSpawn.GetComponent<AudioSource>();
        if (source != null)
        {
            // Set prefab volume
            AudioPoolItem item = audioItems.Find(x => x.name == name);
            if (item != null) source.volume = item.volume;

            // Play sound
            source.Play();

            // Return to pool after sound finishes
            StartCoroutine(ReturnToPoolAfterTime(objectToSpawn, source.clip.length, name));
        }
        else
        {
            // If no AudioSource, immediately return to pool
            poolDictionary[name].Enqueue(objectToSpawn);
        }

        return objectToSpawn;
    }

    private IEnumerator ReturnToPoolAfterTime(GameObject obj, float time, string poolName)
    {
        yield return new WaitForSeconds(time);

        if (obj == null) yield break; // In case object was destroyed

        obj.SetActive(false);

        if (poolDictionary.ContainsKey(poolName))
        {
            poolDictionary[poolName].Enqueue(obj);
        }
    }
}
