// EditorTools/View/Elements/Nodes/EndNodeView.cs
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogSystem.EditorTools.View.Elements.Nodes
{
    /// <summary>
    /// Non-deletable end node with a single input port.
    /// Header and border use a red accent; body uses a soft tint.
    /// </summary>
    public class EndNodeView : Node
    {
        public string GUID;
        public Port inputPort;

        public EndNodeView(string guid)
        {
            GUID = guid;
            title = "End";

            // Lock down basic capabilities
            capabilities &= ~Capabilities.Deletable;
            capabilities &= ~Capabilities.Renamable;
            capabilities &= ~Capabilities.Resizable;
            capabilities &= ~Capabilities.Collapsible;

            // Input port on the header (left side)
            inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            inputPort.portName = "In";
            titleContainer.Insert(0, inputPort);

            // Typography
            MakeLabelBlackAndBold(titleContainer.Q<Label>());
            MakeLabelBlackAndBold(inputPort.Q<Label>());

            // Colors
            var accent = new Color32(0xE7, 0x4C, 0x3C, 0xFF);              // #E74C3C
            var nodeBg = new Color(231 / 255f, 76 / 255f, 60 / 255f, 0.25f);
            var sc = new StyleColor(accent);

            titleContainer.style.backgroundColor = sc;
            style.backgroundColor = nodeBg;
            mainContainer.style.backgroundColor = nodeBg;

            style.borderLeftWidth = 2;
            style.borderRightWidth = 2;
            style.borderTopWidth = 2;
            style.borderBottomWidth = 2;

            style.borderLeftColor = sc;
            style.borderRightColor = sc;
            style.borderTopColor = sc;
            style.borderBottomColor = sc;

            RefreshExpandedState();
            RefreshPorts();
        }

        private static void MakeLabelBlackAndBold(Label lbl)
        {
            if (lbl == null) return;
            lbl.style.color = Color.black;
#if UNITY_2021_3_OR_NEWER
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
#endif
        }
    }
}
