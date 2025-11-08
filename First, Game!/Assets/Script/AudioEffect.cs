using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioEffect : MonoBehaviour
{
    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    public void Play()
    {
        if (!source) return;
        source.Stop();  // Reset in case it was still playing
        source.Play();
    }
}
