using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

using DialogSystem.Runtime.Models;
using DialogSystem.EditorTools.View.Elements.Nodes;
using DialogSystem.Runtime.Models.Nodes;
using DialogSystem.Runtime.Utils;

namespace DialogSystem.EditorTools.View
{
    public class DialogGraphView : GraphView
    {
        #region ---------------- Fields ----------------
        private MiniMap miniMap;
        private const float MiniW = 200f;
        private const float MiniH = 140f;
        private const float MiniMargin = 10f;
        private static readonly Vector2 kDefaultNodeSize = new Vector2(200, 120);

        public string GraphId { get; set; } = "NewDialogGraph";
        #endregion

        #region ---------------- Ctor ----------------
        public DialogGraphView()
        {
            name = "Dialog Graph";
            GraphId = name;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // MiniMap: bottom-right
            miniMap = new MiniMap { anchored = true };
            Add(miniMap);
            this.RegisterCallback<GeometryChangedEvent>(_ => RepositionMiniMap());
            RepositionMiniMap();

            // Context menu: create nodes
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.InsertAction(0, "Create Dialog Node", action =>
                {
                    Vector2 mouse = action.eventInfo?.mousePosition ?? Vector2.zero;
                    Vector2 pos = contentViewContainer.WorldToLocal(mouse);
                    _ = CreateDialogNode("New Node", false, pos.x, pos.y);
                });

                evt.menu.InsertAction(1, "Create Choice Node", action =>
                {
                    Vector2 mouse = action.eventInfo?.mousePosition ?? Vector2.zero;
                    Vector2 pos = contentViewContainer.WorldToLocal(mouse);
                    _ = CreateChoiceNode("Choice", false, pos.x, pos.y);
                });

                evt.menu.InsertAction(2, "Create Action Node", action =>
                {
                    Vector2 mouse = action.eventInfo?.mousePosition ?? Vector2.zero;
                    Vector2 pos = contentViewContainer.WorldToLocal(mouse);
                    _ = CreateActionNode("Action", false, pos.x, pos.y);
                });
            }));

            graphViewChanged = OnGraphViewChanged;
            EnsureStartEndNodes();
        }
        #endregion

        #region ---------------- MiniMap ----------------
        private void RepositionMiniMap()
        {
            var w = layout.width;
            var h = layout.height;
            miniMap.SetPosition(new Rect(
                Mathf.Max(MiniMargin, w - MiniW - MiniMargin),
                Mathf.Max(MiniMargin, h - MiniH - MiniMargin),
                MiniW, MiniH));
        }
        #endregion

        #region ---------------- Node Create ----------------
        public DialogNodeView CreateDialogNode(string nodeName, bool autoPosition = false, float xPos = 0, float yPos = 0)
        {
            var node = new DialogNodeView(nodeName, this);

            Vector2 size = new Vector2(220, 170);
            Vector2 position = new Vector2(xPos, yPos);

            if (autoPosition)
            {
                Vector2 viewCenter = contentViewContainer.WorldToLocal(this.layout.center);
                position = viewCenter - (size * 0.5f);
            }

            node.SetPosition(new Rect(position, size));
            AddElement(node);
            return node;
        }

        public ChoiceNodeView CreateChoiceNode(string nodeName, bool autoPosition = false, float xPos = 0, float yPos = 0)
        {
            var node = new ChoiceNodeView(nodeName, this);

            Vector2 size = new Vector2(260, 220);
            Vector2 position = new Vector2(xPos, yPos);

            if (autoPosition)
            {
                Vector2 viewCenter = contentViewContainer.WorldToLocal(this.layout.center);
                position = viewCenter - (size * 0.5f);
            }

            node.SetPosition(new Rect(position, size));
            node.LoadNodeData(null); // start with one empty row
            AddElement(node);
            return node;
        }

        public ActionNodeView CreateActionNode(string nodeName, bool autoPosition = false, float xPos = 0, float yPos = 0)
        {
            // temp data (graph asset sub-asset is created on SaveGraph)
            var data = ScriptableObject.CreateInstance<ActionNode>();
            data.SetGuid();
            data.SetPosition(new Vector2(xPos, yPos));

            var view = new ActionNodeView(data.GetGuid());
            view.Initialize(data, new Vector2(xPos, yPos), "Action");
            view.LoadNodeData("", "", false, 0f);

            // keep data position synced while dragging
            view.OnChanged += _ =>
            {
                if (view.Data == null) return;

                Undo.RecordObject(view.Data, "Move Node");
                view.Data.SetPosition(view.GetPosition().position);
                EditorUtility.SetDirty(view.Data);

                var asset = LoadGraphAsset(GraphId);
                if (asset != null) EditorUtility.SetDirty(asset);
            };

            var size = new Vector2(240, 170);
            var pos = new Vector2(xPos, yPos);

            if (autoPosition)
            {
                Vector2 viewCenter = contentViewContainer.WorldToLocal(this.layout.center);
                pos = viewCenter - (size * 0.5f);
            }

            view.SetPosition(new Rect(pos, size));
            AddElement(view);
            return view;
        }

        private StartNodeView GetOrCreateStartView(string guid, Vector2 pos)
        {
            var existing = nodes.ToList().OfType<StartNodeView>().FirstOrDefault();
            if (existing != null)
            {
                if (!string.IsNullOrEmpty(guid)) existing.GUID = guid;
                var r = existing.GetPosition();
                var size = (r.width <= 0f || r.height <= 0f) ? kDefaultNodeSize : new Vector2(r.width, r.height);
                existing.SetPosition(new Rect(pos, size));
                return existing;
            }

            var view = new StartNodeView(string.IsNullOrEmpty(guid) ? System.Guid.NewGuid().ToString("N") : guid);
            AddElement(view);
            view.SetPosition(new Rect(pos, kDefaultNodeSize));
            return view;
        }

        private EndNodeView GetOrCreateEndView(string guid, Vector2 pos)
        {
            var existing = nodes.ToList().OfType<EndNodeView>().FirstOrDefault();
            if (existing != null)
            {
                if (!string.IsNullOrEmpty(guid)) existing.GUID = guid;
                var r = existing.GetPosition();
                var size = (r.width <= 0f || r.height <= 0f) ? kDefaultNodeSize : new Vector2(r.width, r.height);
                existing.SetPosition(new Rect(pos, size));
                return existing;
            }

            var view = new EndNodeView(string.IsNullOrEmpty(guid) ? System.Guid.NewGuid().ToString("N") : guid);
            AddElement(view);
            view.SetPosition(new Rect(pos, kDefaultNodeSize));
            return view;
        }

        public void EnsureStartEndNodes()
        {
            bool hasStart = nodes.ToList().Any(n => n is StartNodeView);
            bool hasEnd = nodes.ToList().Any(n => n is EndNodeView);

            if (!hasStart)
            {
                var start = GetOrCreateStartView("Start", new Vector2(-320f, 80f));
                start.capabilities &= ~Capabilities.Deletable;
            }
            if (!hasEnd)
            {
                var end = GetOrCreateEndView("End", new Vector2(720f, 80f));
                end.capabilities &= ~Capabilities.Deletable;
            }
        }
        #endregion

        #region ---------------- Graph Change Handling ----------------
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            // Never allow deleting Start/End views
            if (change.elementsToRemove != null && change.elementsToRemove.Count > 0)
            {
                change.elementsToRemove = change.elementsToRemove
                    .Where(e => e is not StartNodeView && e is not EndNodeView)
                    .ToList();
            }

            // Deletions (nodes or edges)
            if (change.elementsToRemove != null && change.elementsToRemove.Count > 0)
            {
                var asset = LoadGraphAsset(GraphId);
                if (asset != null)
                {
                    foreach (var element in change.elementsToRemove)
                    {
                        if (element is Edge edge)
                        {
                            HandleDeleteEdge(asset, edge);
                            continue;
                        }

                        if (element is DialogNodeView || element is ChoiceNodeView || element is ActionNodeView)
                        {
                            HandleDeleteNode(asset, element);
                            continue;
                        }
                    }

                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssets();
                }
            }

            // Edge create → add/update link + set choice nextNodeGUID
            if (change.edgesToCreate != null && change.edgesToCreate.Count > 0)
            {
                var asset = LoadGraphAsset(GraphId);
                if (asset != null)
                {
                    foreach (var e in change.edgesToCreate)
                    {
                        var fromGuid = ExtractGuidFromView(e.output?.node as Node);
                        var toGuid = ExtractGuidFromView(e.input?.node as Node);
                        if (string.IsNullOrEmpty(fromGuid) || string.IsNullOrEmpty(toGuid)) continue;

                        int portIdx = GetOutputPortIndex(e.output);

                        // Unique per (fromGuid, portIdx)
                        asset.links.RemoveAll(l => l.fromGuid == fromGuid && l.fromPortIndex == portIdx);
                        asset.links.Add(new GraphLink { fromGuid = fromGuid, toGuid = toGuid, fromPortIndex = portIdx });

                        // If from is a ChoiceNode, update the saved next
                        var cSo = asset.choiceNodes?.FirstOrDefault(c => c != null && c.GetGuid() == fromGuid);
                        if (cSo != null && cSo.choices != null && portIdx >= 0 && portIdx < cSo.choices.Count)
                        {
                            cSo.choices[portIdx].nextNodeGUID = toGuid;
                            EditorUtility.SetDirty(cSo);
                        }
                    }

                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssets();
                }

                // Update UI mapping in the view too
                foreach (var e in change.edgesToCreate)
                {
                    if (e.output?.node is ChoiceNodeView chv)
                    {
                        var toGuid = ExtractGuidFromView(e.input?.node as Node);
                        chv.SetNextForPort((Port)e.output, toGuid);
                    }
                }
            }

            // Moves handled in node views (OnChanged hooks)
            return change;
        }
        #endregion

        #region ---------------- Port Rules ----------------
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(port =>
                startPort != port &&
                startPort.node != port.node &&
                startPort.direction != port.direction).ToList();
        }
        #endregion

        #region ---------------- Utils ----------------
        public bool IsGraphEmptyForSave()
        {
            var anyNodes = this.nodes != null && this.nodes.ToList().Count > 0;
            var anyEdges = this.edges != null && this.edges.ToList().Count > 0;
            return !(anyNodes || anyEdges);
        }

        public void ClearGraph()
        {
            graphElements.ToList().ForEach(RemoveElement);
            EnsureStartEndNodes();
        }

        private static string ExtractGuidFromView(Node nodeView)
        {
            if (nodeView is EndNodeView ev) return ev.GUID;
            if (nodeView is StartNodeView sv) return sv.GUID;
            if (nodeView is DialogNodeView dv) return dv.GUID;
            if (nodeView is ChoiceNodeView cv) return cv.GUID;
            if (nodeView is ActionNodeView av) return av.GUID;
            return string.Empty;
        }

        private static int GetOutputPortIndex(Port output)
        {
            if (output?.node is DialogNodeView) return 0;
            if (output?.node is ChoiceNodeView chv)
                return chv.GetPortIndex(output);
            if (output?.node is StartNodeView) return 0;
            if (output?.node is ActionNodeView) return 0;
            return 0;
        }

        private static string CombineAssetPath(string folder, string fileWithExt)
            => $"{folder.TrimEnd('/')}/{fileWithExt.TrimStart('/')}";

        private DialogGraph LoadGraphAsset(string graphId)
        {
            var path = CombineAssetPath(TextResources.CONVERSATION_FOLDER, $"{graphId}.asset");
            return AssetDatabase.LoadAssetAtPath<DialogGraph>(path);
        }

        private void HandleDeleteEdge(DialogGraph asset, Edge edge)
        {
            var fromGuid = ExtractGuidFromView(edge.output?.node as Node);
            var toGuid = ExtractGuidFromView(edge.input?.node as Node);
            if (string.IsNullOrEmpty(fromGuid) || string.IsNullOrEmpty(toGuid)) return;

            int portIdx = GetOutputPortIndex(edge.output);

            // Remove the link record
            asset.links.RemoveAll(l => l.fromGuid == fromGuid && l.toGuid == toGuid && l.fromPortIndex == portIdx);

            // If it's a ChoiceNode -> clear its saved next on that port
            var cSo = asset.choiceNodes?.FirstOrDefault(c => c != null && c.GetGuid() == fromGuid);
            if (cSo != null && cSo.choices != null && portIdx >= 0 && portIdx < cSo.choices.Count)
            {
                cSo.choices[portIdx].nextNodeGUID = null;
                EditorUtility.SetDirty(cSo);
            }
        }

        private void HandleDeleteNode(DialogGraph asset, GraphElement element)
        {
            string guid = null;

            if (element is DialogNodeView dView) guid = dView.GUID;
            else if (element is ChoiceNodeView cView) guid = cView.GUID;
            else if (element is ActionNodeView aView) guid = aView.GUID;
            else return;

            if (string.IsNullOrEmpty(guid)) return;

            // 1) remove links to/from this node
            asset.links.RemoveAll(l => l.fromGuid == guid || l.toGuid == guid);

            // 2) purge references from choice nodes (their nextNodeGUID may point to this)
            if (asset.choiceNodes != null)
            {
                foreach (var ch in asset.choiceNodes)
                {
                    if (ch?.choices == null) continue;
                    foreach (var choice in ch.choices)
                        if (choice != null && choice.nextNodeGUID == guid)
                            choice.nextNodeGUID = null;
                }
            }

            // 3) remove the node sub-asset + list entry
            var path = AssetDatabase.GetAssetPath(asset);
            var all = AssetDatabase.LoadAllAssetsAtPath(path);

            // Dialog
            var dSo = asset.nodes?.FirstOrDefault(n => n != null && n.GetGuid() == guid);
            if (dSo != null)
            {
                asset.nodes.Remove(dSo);
                var match = all.FirstOrDefault(x => x == dSo);
                if (match != null) AssetDatabase.RemoveObjectFromAsset(match);
                UnityEngine.Object.DestroyImmediate(dSo, true);
            }

            // Choice
            var chSo = asset.choiceNodes?.FirstOrDefault(n => n != null && n.GetGuid() == guid);
            if (chSo != null)
            {
                asset.choiceNodes.Remove(chSo);
                var match = all.FirstOrDefault(x => x == chSo);
                if (match != null) AssetDatabase.RemoveObjectFromAsset(match);
                UnityEngine.Object.DestroyImmediate(chSo, true);
            }

            // Action
            var aSo = asset.actionNodes?.FirstOrDefault(n => n != null && n.GetGuid() == guid);
            if (aSo != null)
            {
                asset.actionNodes.Remove(aSo);
                var match = all.FirstOrDefault(x => x == aSo);
                if (match != null) AssetDatabase.RemoveObjectFromAsset(match);
                UnityEngine.Object.DestroyImmediate(aSo, true);
            }
        }
        #endregion

        #region ---------------- Duplicate Node Functionality ----------------
        public void DuplicateSelectedNodes()
        {
            var dialogNodeOriginals = selection.OfType<DialogNodeView>().ToList();
            var choiceNodeOriginals = selection.OfType<ChoiceNodeView>().ToList();
            var actionNodeOriginals = selection.OfType<ActionNodeView>().ToList();

            if (dialogNodeOriginals.Count == 0 && choiceNodeOriginals.Count == 0 && actionNodeOriginals.Count == 0) return;

            var existingEdges = this.edges.ToList().OfType<Edge>().ToList();

            var mapDialogNodes = new Dictionary<DialogNodeView, DialogNodeView>();
            var mapChoiceNodes = new Dictionary<ChoiceNodeView, ChoiceNodeView>();
            var mapActionNodes = new Dictionary<ActionNodeView, ActionNodeView>();

            // clone dialog nodes
            foreach (var src in dialogNodeOriginals)
            {
                var srcRect = src.GetPosition();
                var pos = srcRect.position + new Vector2(40f, 40f);

                var clone = CreateDialogNode(src.NodeTitle, false, pos.x, pos.y);
                clone.LoadNodeData(
                    src.SpeakerName,
                    src.QuestionText,
                    src.NodeTitle,
                    src.PortraitSprite,
                    src.DialogueAudio,
                    src.DisplayTimeSeconds
                );
                clone.SetPosition(new Rect(pos, srcRect.size));
                mapDialogNodes[src] = clone;
            }

            // clone choice nodes
            foreach (var src in choiceNodeOriginals)
            {
                var srcRect = src.GetPosition();
                var pos = srcRect.position + new Vector2(40f, 40f);

                var clone = CreateChoiceNode("Choice", false, pos.x, pos.y);
                clone.LoadNodeData(null);
                clone.LoadAnswers(src.answers.Select(a => new Choice { answerText = a }).ToList());
                clone.SetPosition(new Rect(pos, srcRect.size));
                mapChoiceNodes[src] = clone;
            }

            // clone action nodes
            foreach (var src in actionNodeOriginals)
            {
                var srcRect = src.GetPosition();
                var pos = srcRect.position + new Vector2(40f, 40f);

                var clone = CreateActionNode("Action", false, pos.x, pos.y);
                clone.LoadNodeData(src.ActionId, src.PayloadJson, src.WaitForCompletion, src.WaitSeconds);
                clone.SetPosition(new Rect(pos, srcRect.size));
                mapActionNodes[src] = clone;
            }

            // re-create edges between cloned nodes when both endpoints were selected
            foreach (var e in existingEdges)
            {
                var from = e.output?.node as Node;
                var to = e.input?.node as Node;

                Node fromClone = null, toClone = null;

                if (from is DialogNodeView fd && mapDialogNodes.TryGetValue(fd, out var fdc)) fromClone = fdc;
                else if (from is ChoiceNodeView fc && mapChoiceNodes.TryGetValue(fc, out var fcc)) fromClone = fcc;

                if (to is DialogNodeView td && mapDialogNodes.TryGetValue(td, out var tdc)) toClone = tdc;
                else if (to is ChoiceNodeView tc && mapChoiceNodes.TryGetValue(tc, out var tcc)) toClone = tcc;

                if (fromClone == null || toClone == null) continue;

                Port outPort = null, inPort = null;

                if (fromClone is DialogNodeView fdv) outPort = fdv.outputPort;
                else if (fromClone is ChoiceNodeView fcv)
                {
                    int idx = ((ChoiceNodeView)e.output.node).GetPortIndex((Port)e.output);
                    if (idx >= 0 && idx < fcv.outputPorts.Count)
                        outPort = fcv.outputPorts[idx];
                }

                if (toClone is DialogNodeView tdv) inPort = tdv.inputPort;
                else if (toClone is ChoiceNodeView tcv) inPort = tcv.inputPort;

                if (outPort != null && inPort != null)
                {
                    var newEdge = outPort.ConnectTo(inPort);
                    if (newEdge != null) AddElement(newEdge);
                }
            }

            // update selection to clones
            ClearSelection();
            foreach (var kv in mapDialogNodes) AddToSelection(kv.Value);
            foreach (var kv in mapChoiceNodes) AddToSelection(kv.Value);
        }
        #endregion

        #region ---------------- Save / Load ----------------
        public void SaveGraph(string fileName)
        {
            string path = CombineAssetPath(TextResources.CONVERSATION_FOLDER, $"{fileName}.asset");
            DialogGraph asset = AssetDatabase.LoadAssetAtPath<DialogGraph>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DialogGraph>();
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                foreach (var node in asset.nodes)
                    if (node != null) UnityEngine.Object.DestroyImmediate(node, true);

                var subs = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var c in subs.OfType<ChoiceNode>().ToList())
                    UnityEngine.Object.DestroyImmediate(c, true);
                foreach (var a in subs.OfType<ActionNode>().ToList())
                    UnityEngine.Object.DestroyImmediate(a, true);

                if (asset.actionNodes == null) asset.actionNodes = new List<ActionNode>();
                if (asset.choiceNodes == null) asset.choiceNodes = new List<ChoiceNode>();
                if (asset.links == null) asset.links = new List<GraphLink>();

                asset.links.Clear();
                asset.nodes.Clear();
                asset.actionNodes.Clear();
                asset.choiceNodes.Clear();
            }

            var startView = nodes.ToList().OfType<StartNodeView>().FirstOrDefault();
            var endView = nodes.ToList().OfType<EndNodeView>().FirstOrDefault();

            if (startView != null)
            {
                asset.startGuid = startView.GUID;
                asset.startPosition = startView.GetPosition().position;
                asset.startInitialized = true;
            }
            else
            {
                asset.startGuid = string.IsNullOrEmpty(asset.startGuid) ? Guid.NewGuid().ToString("N") : asset.startGuid;
                asset.startPosition = asset.startInitialized ? new Vector2(-320f, 80f) : asset.startPosition;
            }

            if (endView != null)
            {
                asset.endGuid = endView.GUID;
                asset.endPosition = endView.GetPosition().position;
                asset.endInitialized = true;
            }
            else
            {
                asset.endGuid = string.IsNullOrEmpty(asset.endGuid) ? Guid.NewGuid().ToString("N") : asset.endGuid;
                asset.endPosition = asset.startInitialized ? new Vector2(720f, 80f) : asset.endPosition;
            }
            EditorUtility.SetDirty(asset);

            var edges = this.edges.ToList().OfType<Edge>().ToList();
            var dialogViews = this.nodes.ToList().OfType<DialogNodeView>().ToList();
            var choiceViews = this.nodes.ToList().OfType<ChoiceNodeView>().ToList();
            var actionViews = this.nodes.ToList().OfType<ActionNodeView>().ToList();

            // Dialog nodes
            var dialogMap = new Dictionary<string, DialogNode>();
            foreach (var view in dialogViews)
            {
                var dNode = ScriptableObject.CreateInstance<DialogNode>();
                dNode.SetGuid(view.GUID);
                dNode.name = "Node_" + view.NodeTitle;
                dNode.speakerName = view.SpeakerName;
                dNode.questionText = view.QuestionText;
                dNode.speakerPortrait = view.PortraitSprite;
                dNode.dialogAudio = view.DialogueAudio;
                dNode.displayTime = view.DisplayTimeSeconds;
                dNode.SetPosition(view.GetPosition().position);

                asset.nodes.Add(dNode);
                dialogMap[view.GUID] = dNode;
                AssetDatabase.AddObjectToAsset(dNode, asset);
            }

            // Choice nodes
            var choiceMap = new Dictionary<string, ChoiceNode>();
            foreach (var view in choiceViews)
            {
                var chNode = ScriptableObject.CreateInstance<ChoiceNode>();
                chNode.SetGuid(view.GUID);
                chNode.name = "ChoiceNode";
                chNode.SetPosition(view.GetPosition().position);

                chNode.choices = new List<Choice>();
                foreach (var a in view.answers)
                    chNode.choices.Add(new Choice { answerText = a, nextNodeGUID = null });

                asset.choiceNodes.Add(chNode);
                choiceMap[view.GUID] = chNode;
                AssetDatabase.AddObjectToAsset(chNode, asset);
            }

            // Action nodes
            var actionMap = new Dictionary<string, ActionNode>();
            foreach (var view in actionViews)
            {
                var aNode = ScriptableObject.CreateInstance<ActionNode>();
                aNode.SetGuid(view.GUID);
                aNode.name = "ActionNode";
                aNode.actionId = view.ActionId;
                aNode.payloadJson = view.PayloadJson;
                aNode.waitForCompletion = view.WaitForCompletion;
                aNode.waitSeconds = view.WaitSeconds;
                aNode.SetPosition(view.GetPosition().position);

                asset.actionNodes.Add(aNode);
                actionMap[view.GUID] = aNode;
                AssetDatabase.AddObjectToAsset(aNode, asset);
            }

            // Links + wire choice nexts
            foreach (var e in edges)
            {
                var fromGuid = ExtractGuidFromView(e.output?.node as Node);
                var toGuid = ExtractGuidFromView(e.input?.node as Node);
                if (string.IsNullOrEmpty(fromGuid) || string.IsNullOrEmpty(toGuid)) continue;

                int portIdx = GetOutputPortIndex(e.output);
                asset.links.Add(new GraphLink { fromGuid = fromGuid, toGuid = toGuid, fromPortIndex = portIdx });

                if (choiceMap.TryGetValue(fromGuid, out var cSo))
                {
                    if (cSo.choices != null && portIdx >= 0 && portIdx < cSo.choices.Count)
                        cSo.choices[portIdx].nextNodeGUID = toGuid;
                }
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public void LoadGraph(string fileName)
        {
            GraphId = fileName;
            string path = CombineAssetPath(TextResources.CONVERSATION_FOLDER, $"{fileName}.asset");
            var asset = AssetDatabase.LoadAssetAtPath<DialogGraph>(path);

            if (asset == null)
            {
                Debug.LogError("DialogGraph asset not found: " + path);
                return;
            }

            ClearGraph();
            var viewLookup = new Dictionary<string, Node>();

            // Start node
            if (!asset.startInitialized)
            {
                asset.startPosition = new Vector2(-320f, 80f);
                asset.startGuid = System.Guid.NewGuid().ToString("N");
                asset.startInitialized = true;
            }

            var startView = GetOrCreateStartView(asset.startGuid, asset.startPosition);
            asset.startGuid = startView.GUID;
            viewLookup[startView.GUID] = startView;

            // End node
            if (!asset.endInitialized)
            {
                asset.endPosition = new Vector2(720f, 80f);
                asset.endGuid = System.Guid.NewGuid().ToString("N");
                asset.endInitialized = true;
            }

            var endView = GetOrCreateEndView(asset.endGuid, asset.endPosition);
            asset.endGuid = endView.GUID;
            viewLookup[endView.GUID] = endView;

            // Dialog nodes
            foreach (var dNode in asset.nodes)
            {
                if (dNode == null) continue;

                var view = CreateDialogNode(dNode.name.Replace("Node_", ""), false, dNode.GetPosition().x, dNode.GetPosition().y);
                view.GUID = dNode.GetGuid();
                view.LoadNodeData(dNode.speakerName, dNode.questionText, view.NodeTitle, dNode.speakerPortrait, dNode.dialogAudio, dNode.displayTime);
                view.SetPosition(new Rect(dNode.GetPosition(), new Vector2(200, 150)));
                viewLookup[dNode.GetGuid()] = view;
            }

            // Choice nodes
            foreach (var chNode in asset.choiceNodes)
            {
                if (chNode == null) continue;

                var view = CreateChoiceNode("Choice", false, chNode.GetPosition().x, chNode.GetPosition().y);
                view.GUID = chNode.GetGuid();
                view.LoadNodeData(chNode.choices);
                viewLookup[chNode.GetGuid()] = view;
            }

            // Action nodes
            foreach (var aNode in asset.actionNodes)
            {
                if (aNode == null) continue;

                var view = CreateActionNode("Action", false, aNode.GetPosition().x, aNode.GetPosition().y);
                view.GUID = aNode.GetGuid();
                view.LoadNodeData(aNode.actionId, aNode.payloadJson, aNode.waitForCompletion, aNode.waitSeconds);
                view.SetPosition(new Rect(aNode.GetPosition(), new Vector2(320, 240)));
                viewLookup[aNode.GetGuid()] = view;
            }

            // Rebuild edges
            foreach (var link in asset.links)
            {
                if (!viewLookup.TryGetValue(link.fromGuid, out var fromView)) continue;
                if (!viewLookup.TryGetValue(link.toGuid, out var toView)) continue;

                Port outPort = null, inPort = null;

                if (fromView is DialogNodeView fdv) outPort = fdv.outputPort;
                else if (fromView is ChoiceNodeView fcv)
                {
                    if (link.fromPortIndex >= 0 && link.fromPortIndex < fcv.outputPorts.Count)
                        outPort = fcv.outputPorts[link.fromPortIndex];
                }
                else if (fromView is StartNodeView fsv) outPort = fsv.outputPort;
                else if (fromView is ActionNodeView fav) outPort = fav.outputPort;

                if (toView is DialogNodeView tdv) inPort = tdv.inputPort;
                else if (toView is ChoiceNodeView tcv) inPort = tcv.inputPort;
                else if (toView is EndNodeView tev) inPort = tev.inputPort;
                else if (toView is ActionNodeView tav) inPort = tav.inputPort;

                if (outPort != null && inPort != null)
                {
                    var edge = outPort.ConnectTo(inPort);
                    if (edge != null) AddElement(edge);
                }
            }

            EditorUtility.SetDirty(asset);
        }
        #endregion
    }
}