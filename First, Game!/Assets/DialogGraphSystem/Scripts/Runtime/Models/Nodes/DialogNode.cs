using UnityEngine;

namespace DialogSystem.Runtime.Models.Nodes
{
    /// <summary>
    /// Standard dialog line with optional speaker, portrait, audio, and display time.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogNode", menuName = "Dialog System/Dialog Node")]
    public class DialogNode : BaseNode
    {
        #region -------- Speaker --------
        [Header("Speaker")]
        [Tooltip("Name shown as the speaker of this line.")]
        [SerializeField] public string speakerName;

        [Tooltip("Portrait/avatar shown for the speaker.")]
        [SerializeField] public Sprite speakerPortrait;
        #endregion

        #region -------- Content --------
        [Header("Content")]
        [TextArea(2, 5)]
        [Tooltip("The main text shown to the player.")]
        [SerializeField] public string questionText;
        #endregion

        #region -------- Audio --------
        [Header("Audio")]
        [Tooltip("Optional voice-over or SFX for this node.")]
        [SerializeField] public AudioClip dialogAudio;
        #endregion

        #region -------- Flow --------
        [Tooltip("Seconds to show this node before auto-advancing. Use 0 to wait for input.")]
        [Min(0f)] public float displayTime = 0f;
        #endregion

        public DialogNode()
        {
            nodeKind = NodeKind.Dialog;
        }
    }
}
