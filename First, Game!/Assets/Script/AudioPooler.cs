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

        // Set volume from prefab data
        AudioSource source = objectToSpawn.GetComponent<AudioSource>();
        if (source != null)
        {
            AudioPoolItem item = audioItems.Find(x => x.name == name);
            if (item != null)
                source.volume = item.volume;
        }

        poolDictionary[name].Enqueue(objectToSpawn);
        return objectToSpawn;
    }
}
