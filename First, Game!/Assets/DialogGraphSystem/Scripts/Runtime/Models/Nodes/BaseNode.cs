using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView; // Capabilities
#endif

namespace DialogSystem.Runtime.Models.Nodes
{
    /// <summary>
    /// Base class for all node ScriptableObjects in the dialog graph.
    /// Stores identity (GUID), kind, and editor layout position.
    /// </summary>
    public abstract class BaseNode : ScriptableObject
    {
        #region -------- Identity --------
        [Header("Identity")]
        [SerializeField] private string GUID;
        protected NodeKind nodeKind = NodeKind.None;
        #endregion

        #region -------- Editor / Capabilities (Editor-only) --------
#if UNITY_EDITOR
        protected Capabilities capabilities =
            Capabilities.Deletable | Capabilities.Movable | Capabilities.Copiable;

        public Capabilities GetCapabilities() => capabilities;
        public void SetCapabilities(Capabilities newCapabilities) => capabilities = newCapabilities;
#endif
        #endregion

        #region -------- Editor / Layout --------
        [Header("Editor / Layout")]
        [Tooltip("Editor-only: position of this node in the graph view.")]
        [SerializeField] private Vector2 nodePosition;
        #endregion

        #region -------- API --------
        public string GetGuid() => GUID;

        public void SetGuid(string guid = null)
        {
            GUID = string.IsNullOrEmpty(guid) ? Guid.NewGuid().ToString("N") : guid;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public NodeKind GetNodeKind() => nodeKind;

        public Vector2 GetPosition() => nodePosition;

        public void SetPosition(Vector2 newPosition)
        {
            if (nodePosition == newPosition) return;
            nodePosition = newPosition;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        #endregion

        #region -------- Validation --------
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(GUID))
                GUID = Guid.NewGuid().ToString("N");
        }
        #endregion
    }
}
