using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DialogSystem.Runtime.Models.Nodes
{
    /// <summary>
    /// Presents a line of text and a list of selectable answers (choices).
    /// </summary>
    [CreateAssetMenu(fileName = "ChoiceNode", menuName = "Dialog System/Choice Node")]
    public class ChoiceNode : BaseNode
    {
        #region -------- Data --------
        [TextArea(2, 5)]
        public string text;

        [Header("Choices")]
        public List<Choice> choices = new();
        #endregion
    }

    [System.Serializable]
    public class Choice
    {
        [Tooltip("Text shown for this answer.")]
        public string answerText;

        [Tooltip("GUID of the next node when this answer is picked.")]
        public string nextNodeGUID;

        [Tooltip("Optional UnityEvent fired when this choice is selected.")]
        public UnityEvent onSelected = new UnityEvent();
    }
}
