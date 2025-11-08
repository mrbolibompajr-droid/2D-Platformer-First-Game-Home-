using DialogSystem.Runtime.Models.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogSystem.EditorTools.View.Elements.Nodes
{
    /// <summary>
    /// Editor view for <see cref="ActionNode"/> with input/output ports and editable fields.
    /// </summary>
    public class ActionNodeView : BaseNodeView<ActionNode>
    {
        public Port inputPort { get; private set; }
        public Port outputPort { get; private set; }

        private TextField actionIdField;
        private TextField payloadField;
        private Toggle waitToggle;
        private FloatField waitSecondsField;

        public string GUID { get; set; }

        public string ActionId => actionIdField?.value ?? string.Empty;
        public string PayloadJson => payloadField?.value ?? string.Empty;
        public bool WaitForCompletion => waitToggle != null && waitToggle.value;
        public float WaitSeconds => waitSecondsField != null ? waitSecondsField.value : 0f;

        public ActionNodeView(string guid)
        {
            GUID = guid;
            title = "Action";
            AddToClassList("dlg-node");
            AddToClassList("type-action");

            style.minWidth = 250f;
            style.minHeight = 150f;

            BuildHeader();
            BuildBody();
            RebuildPorts();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void BuildHeader()
        {
            titleContainer?.AddToClassList("action-header");

            var titleLabel = titleContainer?.Q<Label>();
            if (titleLabel != null)
            {
                titleLabel.style.color = Color.white;
#if UNITY_2021_3_OR_NEWER
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
#endif
            }
        }

        private void BuildBody()
        {
            var content = mainContainer;

            // --- Action & Payload ---
            var sectionAction = new VisualElement();
            sectionAction.AddToClassList("section");
            content.Add(sectionAction);

            var hdrAction = new Label("Action & Payload");
            hdrAction.AddToClassList("section-title");
            sectionAction.Add(hdrAction);

            actionIdField = new TextField("Action ID")
            {
                tooltip = "Identifier for your runtime action (must match your handler/binding)."
            };
            actionIdField.RegisterValueChangedCallback(_ =>
            {
                if (Data == null) return;
                Data.actionId = actionIdField.value;
                MarkDirty(Data);
            });
            sectionAction.Add(actionIdField);

            payloadField = new TextField("Payload")
            {
                multiline = true,
                tooltip = "Optional JSON payload consumed by your runtime action."
            };
            payloadField.AddToClassList("json-box");
            payloadField.RegisterValueChangedCallback(_ =>
            {
                if (Data == null) return;
                Data.payloadJson = payloadField.value;
                MarkDirty(Data);
            });
            sectionAction.Add(payloadField);

            // Separator
            var sep = new VisualElement();
            sep.AddToClassList("separator");
            content.Add(sep);

            // --- Flow Control ---
            var sectionFlow = new VisualElement();
            sectionFlow.AddToClassList("section");
            content.Add(sectionFlow);

            var hdrFlow = new Label("Flow Control");
            hdrFlow.AddToClassList("section-title");
            sectionFlow.Add(hdrFlow);

            waitToggle = new Toggle("Wait For Completion")
            {
                value = false,
                tooltip = "If enabled, the conversation waits until your action finishes."
            };
            waitToggle.RegisterValueChangedCallback(_ =>
            {
                if (Data == null) return;
                Data.waitForCompletion = waitToggle.value;
                MarkDirty(Data);
            });
            sectionFlow.Add(waitToggle);

            waitSecondsField = new FloatField("Delay (sec)")
            {
                value = 0f,
                tooltip = "Optional delay before continuing. Use 0 for none."
            };
            waitSecondsField.RegisterValueChangedCallback(_ =>
            {
                if (Data == null) return;
                Data.waitSeconds = waitSecondsField.value;
                MarkDirty(Data);
            });
            sectionFlow.Add(waitSecondsField);
        }

        public override void RebuildPorts()
        {
            inputContainer.Clear();
            outputContainer.Clear();

            inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            inputPort.portName = "In";
            inputContainer.Add(inputPort);

            outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            outputPort.portName = "Out";
            outputContainer.Add(outputPort);
        }

        /// <summary>Populate UI without sending change events.</summary>
        public void LoadNodeData(string actionId, string payload, bool waitForCompletion, float waitSeconds)
        {
            actionIdField?.SetValueWithoutNotify(actionId ?? string.Empty);
            payloadField?.SetValueWithoutNotify(payload ?? string.Empty);
            waitToggle?.SetValueWithoutNotify(waitForCompletion);
            waitSecondsField?.SetValueWithoutNotify(waitSeconds);
        }

        // NOTE: We intentionally do NOT override SetPosition here.
        // BaseNodeView raises OnChanged, and the GraphView subscribes to update the data asset.
    }
}