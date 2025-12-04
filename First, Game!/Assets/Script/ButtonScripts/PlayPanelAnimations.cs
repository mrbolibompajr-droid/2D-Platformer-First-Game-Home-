using UnityEngine;

[System.Serializable]
public class PanelAnimation
{
    [Tooltip("Animator for this panel")]
    public Animator animator;

    [Tooltip("Exact animation state/clip name to play")]
    public string stateName;

    [Tooltip("Layer of the Animator (default 0)")]
    public int layer = 0;
}

public class PlayPanelAnimations : MonoBehaviour
{
    [Header("Assign your panels and their states")]
    public PanelAnimation[] panelAnimations;

    [Header("Input Settings")]
    [Tooltip("Key to play all animations")]
    public KeyCode playKey = KeyCode.E;

    void Update()
    {
        if (Input.GetKeyDown(playKey))
        {
            PlayAllAnimations();
        }
    }

    void PlayAllAnimations()
    {
        foreach (PanelAnimation panel in panelAnimations)
        {
            if (panel.animator != null && !string.IsNullOrEmpty(panel.stateName))
            {
                // Play the specified state immediately from the beginning
                panel.animator.Play(panel.stateName, panel.layer, 0f);
            }
        }
    }
}
