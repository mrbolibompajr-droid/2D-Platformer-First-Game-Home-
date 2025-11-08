using UnityEngine;

namespace DialogSystem.Runtime.Models.Nodes
{
    /// <summary>
    /// Graph start marker. Points to the first playable node in the conversation.
    /// </summary>
    [CreateAssetMenu(fileName = "StartNode", menuName = "Dialog System/Start Node")]
    public class StartNode : BaseNode
    {
        [Tooltip("First node to jump to (GUID).")]
        public string nextNodeGUID;

        public StartNode()
        {
            nodeKind = NodeKind.Start;
        }
    }
}
