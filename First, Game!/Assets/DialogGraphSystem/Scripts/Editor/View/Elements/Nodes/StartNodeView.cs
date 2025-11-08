using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogSystem.EditorTools.View.Elements.Nodes
{
    /// <summary>
    /// Non-deletable start node with a single output port.
    /// Header and border use a green accent; body uses a soft tint.
    /// </summary>
    public class StartNodeView : Node
    {
        public string GUID;
        public Port outputPort;

        public StartNodeView(string guid)
        {
            GUID = guid;
            title = "Start";

            // Lock down basic capabilities
            capabilities &= ~Capabilities.Deletable;
            capabilities &= ~Capabilities.Renamable;
            capabilities &= ~Capabilities.Resizable;
            capabilities &= ~Capabilities.Collapsible;

            // Output port on the header (right side)
            outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            outputPort.portName = "Out";
            titleContainer.Add(outputPort);

            // Typography
            MakeLabelBlackAndBold(titleContainer.Q<Label>());
            MakeLabelBlackAndBold(outputPort.Q<Label>());

            // Colors
            var accent = new Color32(0x2E, 0xCC, 0x71, 0xFF);              // #2ECC71
            var nodeBg = new Color(46 / 255f, 204 / 255f, 113 / 255f, 0.25f);
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
