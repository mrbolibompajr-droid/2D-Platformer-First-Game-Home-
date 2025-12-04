using UnityEngine;
using System.Collections.Generic;

public class SyncAnimations : MonoBehaviour
{
    [System.Serializable]
    public class ClipEntry
    {
        public Animation animationComponent; // Must be an Animation component
        public AnimationClip clip;           // Raw .anim clip
    }

    [Header("Animation clips to play (press + to add more)")]
    public List<ClipEntry> clips = new List<ClipEntry>();

    [Header("Key Settings")]
    public KeyCode triggerKey = KeyCode.E;

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            foreach (var entry in clips)
            {
                if (entry.animationComponent != null && entry.clip != null)
                {
                    // Ensure the clip is added to the Animation component
                    if (!entry.animationComponent.GetClip(entry.clip.name))
                        entry.animationComponent.AddClip(entry.clip, entry.clip.name);

                    entry.animationComponent.Play(entry.clip.name);
                }
            }
        }
    }
}
