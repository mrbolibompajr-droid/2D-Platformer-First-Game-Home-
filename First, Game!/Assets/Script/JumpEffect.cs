using System.Collections;
using UnityEngine;

public class JumpEffect : MonoBehaviour
{
    [Header("Sprites / Frames")]
    public Sprite[] frames;
    public float frameRate = 0.1f;

    private SpriteRenderer sr;
    private int currentFrame;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Auto-detect frames if none assigned
        if (frames.Length == 0)
        {
            SpriteRenderer[] children = GetComponentsInChildren<SpriteRenderer>();
            frames = new Sprite[children.Length];
            for (int i = 0; i < children.Length; i++)
                frames[i] = children[i].sprite;
        }
    }

    public void Play()
    {
        currentFrame = 0;
        sr.sprite = frames[currentFrame];
        gameObject.SetActive(true);
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        while (currentFrame < frames.Length)
        {
            sr.sprite = frames[currentFrame];
            currentFrame++;
            yield return new WaitForSeconds(frameRate);
        }
        gameObject.SetActive(false);
    }
}
