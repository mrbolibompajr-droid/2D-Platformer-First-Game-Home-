using UnityEngine;
using System.Collections.Generic;
using DialogSystem.Runtime.Models.Nodes;

namespace DialogSystem.Runtime.Models
{
    /// <summary>
    /// ScriptableObject that holds the full conversation graph:
    /// node collections (dialog/choice/action) and directed links between them.
    /// Start/End data is editor-facing for layout and entry/exit markers.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogGraph", menuName = "Dialog System/Dialog Graph")]
    public class DialogGraph : ScriptableObject
    {
        #region -------- Graph Data --------
        [Tooltip("Directed links connecting nodes by GUID.")]
        public List<GraphLink> links = new();

        [Header("Nodes")]
        [Tooltip("All Dialog nodes in this graph.")]
        public List<DialogNode> nodes = new();

        [Tooltip("All Choice nodes in this graph.")]
        public List<ChoiceNode> choiceNodes = new();

        [Tooltip("All Action nodes in this graph.")]
        public List<ActionNode> actionNodes = new();
        #endregion

        #region -------- Start/End (Editor) --------
        [Header("Start (Editor)")]
        [Tooltip("GUID of the Start marker; editor uses this to resolve the first playable node.")]
        public string startGuid;

        [Tooltip("Editor layout position of the Start marker.")]
        public Vector2 startPosition;

        [Tooltip("Editor bookkeeping flag for Start placement.")]
        public bool startInitialized = false;

        [Header("End (Editor)")]
        [Tooltip("GUID of the End marker.")]
        public string endGuid;

        [Tooltip("Editor layout position of the End marker.")]
        public Vector2 endPosition;

        [Tooltip("Editor bookkeeping flag for End placement.")]
        public bool endInitialized = false;
        #endregion
    }

    /// <summary>
    /// Directed edge between two nodes in the graph.
    /// </summary>
    [System.Serializable]
    public class GraphLink
    {
        [Tooltip("Source node GUID.")]
        public string fromGuid;

        [Tooltip("Destination node GUID.")]
        public string toGuid;

        [Tooltip("Output port index on the source node (0 for dialog/auto, choice index for choices).")]
        public int fromPortIndex;
    }
}
