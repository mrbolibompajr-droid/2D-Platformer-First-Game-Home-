using UnityEngine;
using System.Collections.Generic;

public class AudioPooler : MonoBehaviour
{
    [System.Serializable]
    public class AudioPoolItem
    {
        public string name;
        public GameObject prefab;
        public int size = 5; // number of pre-instantiated objects
    }

    public List<AudioPoolItem> audioItems;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (var item in audioItems)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            for (int i = 0; i < item.size; i++)
            {
                GameObject obj = Instantiate(item.prefab);
                obj.SetActive(false);
                obj.transform.parent = transform; // optional: keep hierarchy clean
                objectPool.Enqueue(obj);
            }
            poolDictionary.Add(item.name, objectPool);
        }
    }

    public GameObject SpawnFromPool(string name, Vector3 position)
    {
        if (!poolDictionary.ContainsKey(name))
        {
            Debug.LogWarning($"AudioPooler: No pool exists for {name}");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[name].Dequeue();

        // Safety check in case prefab was accidentally destroyed
        if (objectToSpawn == null)
        {
            Debug.LogWarning($"AudioPooler: Pooled object for {name} is missing.");
            return null;
        }

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;

        // Play audio
        AudioEffect effect = objectToSpawn.GetComponent<AudioEffect>();
        if (effect != null) effect.Play();

        // Return to pool at the end of clip
        AudioSource src = objectToSpawn.GetComponent<AudioSource>();
        if (src != null)
            StartCoroutine(DisableAfterTime(objectToSpawn, src.clip.length));

        poolDictionary[name].Enqueue(objectToSpawn);
        return objectToSpawn;
    }

    private System.Collections.IEnumerator DisableAfterTime(GameObject obj, float time)
    {
        yield return new WaitForSeconds(time);
        if (obj != null) obj.SetActive(false);
    }
}
