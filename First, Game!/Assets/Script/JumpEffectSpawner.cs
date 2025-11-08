using System.Collections.Generic;
using UnityEngine;

public class JumpEffectSpawner : MonoBehaviour
{
    public JumpEffect effectPrefab;
    public int poolSize = 5;

    private Queue<JumpEffect> pool = new Queue<JumpEffect>();

    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            JumpEffect effect = Instantiate(effectPrefab);
            effect.gameObject.SetActive(false);
            pool.Enqueue(effect);
        }
    }

    public void Spawn(Vector3 position)
    {
        JumpEffect effect;
        if (pool.Count > 0)
            effect = pool.Dequeue();
        else
            effect = Instantiate(effectPrefab);

        effect.transform.position = position;
        effect.gameObject.SetActive(true);
        effect.Play();

        pool.Enqueue(effect);
    }
}
