using System;
using System.Linq;
using UnityEditor;                         // Undo, AssetDatabase, EditorUtility
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using DialogSystem.Runtime.Models;
using DialogSystem.Runtime.Models.Nodes;
using DialogSystem.Runtime.Utils;         // TextResources

namespace DialogSystem.EditorTools.View.Elements.Nodes
{
    /// <summary>
    /// GraphView UI for a <see cref="DialogNode"/>.
    /// - 1 input, 1 output.
    /// - Edits title/speaker/portrait/text/audio/displayTime.
    /// - Persists changes to the backing ScriptableObject (via GUID) with Undo support.
    /// </summary>
    public class DialogNodeView : BaseNodeView<DialogNode>
    {
        #region Layout
        private const float NodeWidth = 400f;
        private const float PortHolderWidth = 28f;
        #endregion

        #region Data
        public string GUID { get; set; }
        public string SpeakerName;
        public string QuestionText;
        public string NodeTitle;
        public Sprite PortraitSprite;
        public AudioClip DialogueAudio;
        public float DisplayTimeSeconds;
        #endregion

        #region Graph / UI
        public DialogGraphView graphView;

        private VisualElement header;
        private Image avatar;
        private Label titleLabel;
        private VisualElement portraitPreview;

        private TextField titleField;
        private TextField speakerField;
        private ObjectField spriteField;
        private TextField questionField;
        private FloatField displayTimeField;
        private ObjectField audioField;

        public Port inputPort;
        public Port outputPort;

        private static StyleSheet s_uss;
        #endregion

        #region Asset helpers
        private static string CombineAssetPath(string folder, string fileWithExt)
            => $"{folder.TrimEnd('/')}/{fileWithExt.TrimStart('/')}";

        private DialogGraph GetAssetSafe()
        {
            if (graphView == null || string.IsNullOrEmpty(graphView.GraphId)) return null;
            var path = CombineAssetPath(TextResources.CONVERSATION_FOLDER, $"{graphView.GraphId}.asset");
            return AssetDatabase.LoadAssetAtPath<DialogGraph>(path);
        }

        private DialogNode FindSoNode(DialogGraph asset)
        {
            if (asset == null || string.IsNullOrEmpty(GUID)) return null;
            // Use view GUID (Data may be null for freshly created views)
            return asset.nodes.FirstOrDefault(n => n != null && n.GetGuid() == GUID);
        }

        private void WithAssetNode(string undoLabel, Action<DialogGraph, DialogNode> act)
        {
            var asset = GetAssetSafe(); if (asset == null) return;
            var soNode = FindSoNode(asset); if (soNode == null) return;

            Undo.RecordObject(soNode, undoLabel);
            act(asset, soNode);
            EditorUtility.SetDirty(soNode);
            EditorUtility.SetDirty(asset);
        }
        #endregion

        #region API (setters)
        public void SetPortraitSprite(Sprite sprite)
        {
            PortraitSprite = sprite;
            spriteField.SetValueWithoutNotify(sprite);
            UpdatePortraitPreview();
            UpdateAvatarVisual();
        }

        public void SetSpeakerName(string name)
        {
            SpeakerName = name;
            speakerField.SetValueWithoutNotify(name);
            UpdatePortraitPreview();
            UpdateAvatarVisual();
        }
        #endregion

        #region Ctor
        public DialogNodeView(string nodeTitle, DialogGraphView graph)
        {
            graphView = graph;
            NodeTitle = nodeTitle;
            title = nodeTitle;
            GUID = Guid.NewGuid().ToString("N");

            if (s_uss == null) s_uss = Resources.Load<StyleSheet>("USS/NodeViewUSS");
            if (s_uss != null && !styleSheets.Contains(s_uss)) styleSheets.Add(s_uss);

            AddToClassList("dlg-node");
            AddToClassList("type-dialogue");

            style.width = NodeWidth;

            BuildHeader();
            BuildBody();
            BuildPorts();

            RefreshExpandedState();
            RefreshPorts();

            // Context menu: Duplicate selection (delegates to GraphView)
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Duplicate", _ =>
                {
                    if (!selected)
                    {
                        graphView?.ClearSelection();
                        graphView?.AddToSelection(this);
                    }
                    graphView?.DuplicateSelectedNodes();
                });
            }));
        }
        #endregion

        #region Header
        private void BuildHeader()
        {
            titleContainer?.AddToClassList("action-header");

            // Assign to field (avoid shadowing)
            titleLabel = titleContainer?.Q<Label>();
            if (titleLabel != null)
            {
                titleLabel.text = NodeTitle;
                titleLabel.style.color = Color.white;
#if UNITY_2021_3_OR_NEWER
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
#endif
            }

            header = new VisualElement { name = "header" };

            var headRow = new VisualElement();
            headRow.style.flexDirection = FlexDirection.Row;
            headRow.style.alignItems = Align.Center;

            avatar = new Image { name = "avatar", scaleMode = ScaleMode.ScaleToFit };
            headRow.Add(avatar);

            header.Add(headRow);
            titleContainer.Add(header);

            UpdateAvatarVisual();
        }

        private void UpdateAvatarVisual()
        {
            if (avatar == null) return;

            if (PortraitSprite != null)
            {
                avatar.image = PortraitSprite.texture;
                avatar.style.display = DisplayStyle.Flex;
            }
            else
            {
                avatar.image = null;
                avatar.style.display = DisplayStyle.None;
            }
        }
        #endregion

        #region Body
        private void BuildBody()
        {
            titleField = new TextField("Node Title") { value = NodeTitle };
            titleField.RegisterValueChangedCallback(e =>
            {
                NodeTitle = e.newValue;
                title = e.newValue;
                if (titleLabel != null) titleLabel.text = e.newValue;

                WithAssetNode("Edit Node Title", (_, soNode) =>
                {
                    soNode.name = "Node_" + (string.IsNullOrWhiteSpace(NodeTitle) ? "Untitled" : NodeTitle.Trim());
                });
            });

            speakerField = new TextField("Speaker") { value = "" };
            speakerField.RegisterValueChangedCallback(e =>
            {
                SpeakerName = e.newValue;
                WithAssetNode("Edit Speaker", (_, soNode) => soNode.speakerName = SpeakerName);
            });

            spriteField = new ObjectField("Portrait") { objectType = typeof(Sprite), allowSceneObjects = false };
            spriteField.RegisterValueChangedCallback(e =>
            {
                PortraitSprite = e.newValue as Sprite;
                UpdatePortraitPreview();
                UpdateAvatarVisual();
                WithAssetNode("Change Portrait", (_, soNode) => soNode.speakerPortrait = PortraitSprite);
            });

            // Visual preview beside the dialog text
            portraitPreview = new VisualElement { name = "portrait-preview" };
            portraitPreview.style.width = 64;
            portraitPreview.style.height = 64;
            portraitPreview.style.marginRight = 6;
            portraitPreview.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);

            questionField = new TextField("Dialog") { multiline = true };
            questionField.name = "Dialog";
            questionField.style.minHeight = 60;
            questionField.style.maxWidth = NodeWidth - 20;
            questionField.style.whiteSpace = WhiteSpace.Normal;
            questionField.RegisterValueChangedCallback(e =>
            {
                QuestionText = e.newValue;
                WithAssetNode("Edit Dialogue Text", (_, soNode) => soNode.questionText = QuestionText);
            });

            displayTimeField = new FloatField("Display Time (sec)") { value = 0f };
            displayTimeField.RegisterValueChangedCallback(e =>
            {
                DisplayTimeSeconds = e.newValue;
                WithAssetNode("Edit Display Time", (_, soNode) => soNode.displayTime = DisplayTimeSeconds);
            });

            audioField = new ObjectField("Audio Clip") { objectType = typeof(AudioClip), allowSceneObjects = false };
            audioField.RegisterValueChangedCallback(e =>
            {
                DialogueAudio = e.newValue as AudioClip;
                WithAssetNode("Change Dialogue Audio", (_, soNode) => soNode.dialogAudio = DialogueAudio);
            });

            var dialogRow = new VisualElement { name = "dialogue-row" };
            dialogRow.style.flexDirection = FlexDirection.Row;
            dialogRow.style.alignItems = Align.FlexStart;

            dialogRow.Add(portraitPreview);
            dialogRow.Add(questionField);

            mainContainer.Add(titleField);
            mainContainer.Add(speakerField);
            mainContainer.Add(spriteField);
            mainContainer.Add(dialogRow);
            mainContainer.Add(displayTimeField);
            mainContainer.Add(audioField);

            UpdatePortraitPreview();
        }
        #endregion

        #region Ports
        private void BuildPorts()
        {
            inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            inputPort.portName = "In";
            inputContainer.Add(inputPort);

            AddOutputPort();
        }

        public void AddOutputPort()
        {
            outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            outputPort.portName = "Out";
            outputContainer.Add(outputPort);
        }

        public override void RebuildPorts()
        {
            inputContainer.Clear();
            outputContainer.Clear();
            BuildPorts();
            RefreshExpandedState();
            RefreshPorts();
        }
        #endregion

        #region Load / Visual
        public void LoadNodeData(
            string speaker, string question, string titleText, Sprite sprite,
            AudioClip audioClip, float displayTime)
        {
            SpeakerName = speaker;
            QuestionText = question;
            NodeTitle = titleText;
            PortraitSprite = sprite;
            DialogueAudio = audioClip;
            DisplayTimeSeconds = displayTime;

            if (titleLabel != null) titleLabel.text = titleText;
            title = titleText;

            speakerField.SetValueWithoutNotify(speaker);
            questionField.SetValueWithoutNotify(question);
            titleField.SetValueWithoutNotify(titleText);
            spriteField.SetValueWithoutNotify(sprite);
            audioField.SetValueWithoutNotify(audioClip);
            displayTimeField.SetValueWithoutNotify(displayTime);

            UpdatePortraitPreview();
            UpdateAvatarVisual();
        }

        private void UpdatePortraitPreview()
        {
            if (portraitPreview == null) return;

            if (PortraitSprite != null)
            {
                portraitPreview.style.backgroundImage = new StyleBackground(PortraitSprite);
                portraitPreview.style.backgroundColor = Color.clear;
            }
            else
            {
                portraitPreview.style.backgroundImage = null;
                portraitPreview.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);
            }
        }
        #endregion

        #region Persist position
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            WithAssetNode("Move Dialog Node", (_, soNode) => soNode.SetPosition(newPos.position));
        }
        #endregion
    }
}