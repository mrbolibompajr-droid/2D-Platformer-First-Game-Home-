using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using DialogSystem.Runtime.Models;
using DialogSystem.EditorTools.View;
using DialogSystem.EditorTools.View.Elements.Nodes;
using DialogSystem.Runtime.Utils;

namespace DialogSystem.EditorTools.Windows
{
    public class DialogGraphEditorWindow : EditorWindow
    {
        #region -------------------- MENU --------------------

        [MenuItem("Tools/Dialog/Dialog Graph")]
        public static void Open()
        {
            var window = GetWindow<DialogGraphEditorWindow>();
            window.titleContent = new GUIContent("Dialog Graph Editor");
        }

        #endregion

        #region -------------------- SETTINGS --------------------

        [SerializeField, Tooltip("Initial width of the Sidebar when shown.")]
        private float initialSidebarWidth = 340f;

        #endregion

        #region -------------------- STATE --------------------

        // Graph + layout
        private DialogGraphView graphView;
        private VisualElement toolbarWrapper;
        private TwoPaneSplitView split;          // graph (left) | sidebar (right)
        private VisualElement graphHost;         // container to keep GraphView sizing stable
        private VisualElement rightSidebarRoot;  // sidebar container
        private VisualElement soloHost;          // used when sidebar is collapsed

        // Toolbar widgets
        private PopupField<string> graphPopup;
        private Button addNodeBtn, loadBtn, saveBtn, clearBtn, toggleSidebarBtn;

        // Sidebar (separate class)
        private EditorSidbarWindow editorSidebar;

        // Runtime
        private string loadedGraphName;
        private bool sidebarVisible = true;
        private float sidebarWidthMemo = 440f;

        #endregion

        #region -------------------- UNITY --------------------

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            var ss = Resources.Load<StyleSheet>(TextResources.STYLE_PATH);
            if (ss != null) rootVisualElement.styleSheets.Add(ss);

            BuildUI();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            // Auto-save if there’s a loaded graph
            if (!string.IsNullOrEmpty(loadedGraphName) && graphView != null)
            {
                graphView.SaveGraph(loadedGraphName);
            }

            rootVisualElement.Clear();
        }

        private void OnUndoRedoPerformed()
        {
            if (!string.IsNullOrEmpty(loadedGraphName))
            {
                LoadGraph(loadedGraphName);
                Repaint();
            }
        }

        #endregion

        #region -------------------- UI BUILD --------------------

        private void BuildUI()
        {
            rootVisualElement.Clear();

            // Top toolbar
            toolbarWrapper = new VisualElement();
            toolbarWrapper.AddToClassList("dlg-toolbar");
            rootVisualElement.Add(toolbarWrapper);
            GenerateToolbar();

            CreateGraphAndSidebar();
            CreateSplit();

            sidebarWidthMemo = Mathf.Max(240f, initialSidebarWidth);
            UpdateSidebarToggleText();
        }

        private void CreateGraphAndSidebar()
        {
            // Graph
            graphView = new DialogGraphView { name = "Dialog Graph" };
            graphHost = new VisualElement();
            graphHost.AddToClassList("dlg-graph-host");
            graphHost.style.flexGrow = 1;
            graphHost.Add(graphView);
            graphView.StretchToParentSize();

            // Sidebar
            rightSidebarRoot = new VisualElement();
            rightSidebarRoot.AddToClassList("dlg-sidebar");

            editorSidebar = new EditorSidbarWindow(this);
            rightSidebarRoot.Add(editorSidebar);

            graphView?.EnsureStartEndNodes();
        }

        private void CreateSplit()
        {
            split = new TwoPaneSplitView(1, sidebarWidthMemo, TwoPaneSplitViewOrientation.Horizontal);
            split.AddToClassList("dlg-split");
            split.style.flexGrow = 1;

            split.Add(graphHost);
            split.Add(rightSidebarRoot);

            rootVisualElement.Add(split);
        }

        #endregion

        #region -------------------- TOOLBAR --------------------

        private void GenerateToolbar()
        {
            loadedGraphName = null;

            graphPopup = CreateGraphPopup();
            toolbarWrapper.Add(graphPopup);

            loadBtn = CreateButton("Load", TextResources.ICON_LOAD, "Load graph", OnClickLoad, "secondary");
            toolbarWrapper.Add(loadBtn);

            toolbarWrapper.Add(MakeSpacer());

            addNodeBtn = CreateButton("Add Node", TextResources.ICON_ADD, "Create a new node", OnClickAddNode, "success");
            toolbarWrapper.Add(addNodeBtn);

            saveBtn = CreateButton("Save", TextResources.ICON_SAVE, "Save graph", OnClickSave, "primary");
            toolbarWrapper.Add(saveBtn);

            clearBtn = CreateButton("Clear", TextResources.ICON_CLEAR, "Clear current graph", OnClickClear, "danger");
            toolbarWrapper.Add(clearBtn);

            toggleSidebarBtn = CreateButton("Hide Sidebar", TextResources.ICON_SIDEBAR, "Show/Hide Sidebar", ToggleSidebar, "secondary");
            toggleSidebarBtn.style.marginLeft = 10;
            toolbarWrapper.Add(toggleSidebarBtn);
        }

        private PopupField<string> CreateGraphPopup()
        {
            var graphNames = GetAllGraphAssetNamesFallback();
            var pop = new PopupField<string>("Dialogs:", graphNames, graphNames.Count > 0 ? 0 : -1)
            {
                style = { maxWidth = 250, marginRight = 0, marginLeft = 15, flexGrow = 1 },
                tooltip = "Select a graph to load"
            };
            pop.AddToClassList("dlg-popup");
            pop.AddToClassList("tight-label");

            var popLabel = pop.labelElement;
            popLabel.style.width = StyleKeyword.Auto;
            popLabel.style.minWidth = StyleKeyword.Auto;
            popLabel.style.flexShrink = 0;
            popLabel.style.marginRight = 6;

            return pop;
        }

        private VisualElement MakeSpacer()
        {
            var spacer = new VisualElement();
            spacer.AddToClassList("dlg-spacer");
            return spacer;
        }

        private Button CreateButton(string label, string iconPath, string tooltip, Action onClick, string styleClass = null, bool iconOnly = false)
        {
            var btn = new Button { tooltip = string.IsNullOrEmpty(tooltip) ? label : tooltip };
            btn.AddToClassList("dlg-btn");
            if (!string.IsNullOrEmpty(styleClass)) btn.AddToClassList(styleClass);
            if (onClick != null) btn.clicked += onClick;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexGrow = 1;

#if UNITY_EDITOR
            Texture2D tex = (!string.IsNullOrEmpty(iconPath)) ? AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath) : null;
            if (tex != null)
            {
                var img = new Image { image = tex, scaleMode = ScaleMode.ScaleToFit };
                img.style.width = 20;
                img.style.height = 20;
                img.style.marginRight = iconOnly ? 0 : 8;
                row.Add(img);
            }
#endif
            var lbl = new Label(iconOnly ? "" : label);
            lbl.style.flexShrink = 1;
            lbl.style.overflow = Overflow.Hidden;
#if UNITY_2021_3_OR_NEWER
            lbl.style.textOverflow = TextOverflow.Ellipsis;
#endif
            row.Add(lbl);

            btn.text = ""; // avoid default overlay
            btn.Add(row);

            btn.style.minHeight = 26;
            btn.style.paddingLeft = 8;
            btn.style.paddingRight = 10;

            return btn;
        }

        #endregion

        #region -------------------- TOOLBAR HANDLERS --------------------

        private void OnClickAddNode() => ShowCreateNodeMenu();

        private void OnClickLoad()
        {
            var selected = graphPopup != null ? graphPopup.value : null;
            if (string.IsNullOrEmpty(selected))
            {
                EditorUtility.DisplayDialog("No graph selected", "Please choose a graph from the popup.", "OK");
                return;
            }
            LoadGraph(selected);
        }

        private void OnClickSave() => OpenSavePopup();
        private void OnClickClear() => ClearLoadedGraph();

        #endregion

        #region -------------------- SAVE / LOAD / CLEAR --------------------

        private void OpenSavePopup()
        {
            var existing = GetAllGraphAssetNamesFallback();
            var suggestion = string.IsNullOrEmpty(loadedGraphName) ? "NewDialogGraph" : loadedGraphName;
            bool isEmpty = graphView != null && graphView.IsGraphEmptyForSave();

            SaveGraphPromptWindow.Open(
                currentName: suggestion,
                loadedGraphName: loadedGraphName,
                existingNames: existing,
                isGraphEmpty: isEmpty,
                onConfirm: finalName =>
                {
                    SaveAsset(finalName);

                    // Refresh popup choices / selection
                    var namesAfter = GetAllGraphAssetNamesFallback();
                    if (namesAfter.Contains(finalName))
                    {
                        var newPopup = CreateGraphPopup();
                        toolbarWrapper.Insert(0, newPopup);
                        toolbarWrapper.Remove(graphPopup);
                        graphPopup = newPopup;

                        var idx = namesAfter.IndexOf(finalName);
                        if (idx >= 0 && graphPopup.choices.Count > idx)
                            graphPopup.SetValueWithoutNotify(finalName);
                    }

                    loadedGraphName = finalName;
                }
            );
        }

        private void SaveAsset(string fileName) => graphView.SaveGraph(fileName);

        private void LoadGraph(string fileName)
        {
            graphView.LoadGraph(fileName);
            loadedGraphName = fileName;

            editorSidebar.RebuildFromGraph();
        }

        private void ClearLoadedGraph()
        {
            graphView.ClearGraph();
            loadedGraphName = null;
            editorSidebar.ClearAll();
        }

        private List<string> GetAllGraphAssetNamesFallback()
        {
            if (!Directory.Exists(TextResources.GRAPHS_FOLDER)) return new List<string>();

            var assetPaths = Directory.GetFiles(TextResources.GRAPHS_FOLDER, "*.asset", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta"))
                .Select(path => path.Replace('\\', '/'))
                .ToList();

            List<string> validNames = new();

            foreach (var path in assetPaths)
            {
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj is DialogGraph)
                    validNames.Add(Path.GetFileNameWithoutExtension(path));
            }

            return validNames.Distinct().OrderBy(n => n).ToList();
        }

        #endregion

        #region -------------------- SIDEBAR SHOW/HIDE --------------------

        public void ToggleSidebar()
        {
            if (sidebarVisible) CollapseSidebar();
            else ExpandSidebar();
        }

        private void CollapseSidebar()
        {
            if (!sidebarVisible) return;
            sidebarVisible = false;

            sidebarWidthMemo = Mathf.Max(240f, rightSidebarRoot.resolvedStyle.width);

            rootVisualElement.Remove(split);

            if (soloHost == null)
            {
                soloHost = new VisualElement();
                soloHost.AddToClassList("dlg-graph-host");
                soloHost.style.flexGrow = 1;
            }

            graphHost.RemoveFromHierarchy();
            soloHost.Add(graphHost);
            rootVisualElement.Add(soloHost);

            UpdateSidebarToggleText();
        }

        private void ExpandSidebar()
        {
            if (sidebarVisible) return;
            sidebarVisible = true;

            if (soloHost != null && soloHost.parent != null)
                rootVisualElement.Remove(soloHost);

            CreateSplit();
            UpdateSidebarToggleText();
        }

        private void UpdateSidebarToggleText()
        {
            if (toggleSidebarBtn == null) return;

            toggleSidebarBtn.text = string.Empty;
            var label = toggleSidebarBtn.Q<Label>();
            if (label != null)
                label.text = sidebarVisible ? "Hide Sidebar" : "Show Sidebar";
        }

        #endregion

        #region -------------------- HELPERS (exposed to sidebar) --------------------

        internal DialogGraphView GetGraphView() => graphView;

        internal IEnumerable<string> CollectSpeakersFromNodes()
        {
            if (graphView == null) yield break;

            foreach (var ge in graphView.nodes.ToList())
            {
                if (ge is not DialogNodeView dnv) continue;
                var speaker = dnv.SpeakerName;
                if (!string.IsNullOrWhiteSpace(speaker))
                    yield return speaker.Trim();
            }
        }

        internal Sprite FindFirstSpriteForSpeaker(string speaker)
        {
            if (graphView == null || string.IsNullOrWhiteSpace(speaker)) return null;
            var key = speaker.Trim();

            foreach (var ge in graphView.nodes.ToList())
            {
                if (ge is not DialogNodeView dnv) continue;
                var s = dnv.SpeakerName;
                if (string.Equals(s?.Trim(), key, StringComparison.Ordinal))
                {
                    var spr = dnv.PortraitSprite;
                    if (spr != null) return spr;
                }
            }
            return null;
        }

        internal (bool found, Color color) FindFirstNameColorForSpeaker(string speaker)
        {
            // Placeholder for future per-speaker color
            return (false, default);
        }

        internal void ApplySpritesToNodes(List<CharacterBinding> bindings)
        {
            if (graphView == null || bindings == null || bindings.Count == 0) return;

            foreach (var ge in graphView.nodes.ToList())
            {
                if (ge is not DialogNodeView dnv) continue;
                var speaker = dnv.SpeakerName?.Trim();
                if (string.IsNullOrEmpty(speaker)) continue;

                var bind = bindings.FirstOrDefault(b =>
                    !string.IsNullOrWhiteSpace(b.originalName) &&
                    b.originalName.Trim().Equals(speaker, StringComparison.Ordinal));

                if (bind == null) continue;

                var newName = bind.currentName?.Trim();
                if (!string.IsNullOrEmpty(newName) && !string.Equals(newName, speaker, StringComparison.Ordinal))
                    dnv.SetSpeakerName(newName);

                if (bind.sprite != null && bind.sprite != dnv.PortraitSprite)
                    dnv.SetPortraitSprite(bind.sprite);

                dnv.MarkDirtyRepaint();
            }

            graphView.schedule.Execute(() =>
            {
                foreach (var n in graphView.nodes.ToList()) n.MarkDirtyRepaint();
            }).ExecuteLater(16);
        }

        internal IEnumerable<ActionNodeView> CollectActionNodes()
        {
            if (graphView == null) return Enumerable.Empty<ActionNodeView>();
            return graphView.nodes.ToList().OfType<ActionNodeView>();
        }

        internal void ApplyActionsToNodes(List<ActionBinding> bindings)
        {
            if (graphView == null || bindings == null || bindings.Count == 0) return;

            var actionViews = CollectActionNodes().ToList();

            foreach (var bind in bindings)
            {
                var originalKey = bind.originalActionId ?? "";
                foreach (var av in actionViews)
                {
                    var curId = av.ActionId ?? "";
                    if (!string.Equals(curId, originalKey, StringComparison.Ordinal)) continue;

                    av.LoadNodeData(
                        bind.actionId ?? "",
                        bind.payloadJson ?? "",
                        bind.waitForCompletion,
                        bind.waitSeconds
                    );

                    av.MarkDirtyRepaint();
                }
            }

            graphView.schedule.Execute(() =>
            {
                foreach (var n in actionViews) n.MarkDirtyRepaint();
            }).ExecuteLater(16);
        }

        #endregion

        #region -------------------- DATA TYPES --------------------

        [Serializable]
        public class CharacterBinding
        {
            public string originalName;
            public string currentName;
            public Sprite sprite;
        }

        [Serializable]
        public class ActionBinding
        {
            public string originalActionId;
            public string actionId;
            public string payloadJson;
            public bool waitForCompletion;
            public float waitSeconds;
        }

        #endregion

        #region -------------------- CREATE NODE MENU --------------------

        private void ShowCreateNodeMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Dialog"), false, () =>
            {
                graphView?.CreateDialogNode("New Dialog", true);
            });

            menu.AddItem(new GUIContent("Choice"), false, () =>
            {
                graphView?.CreateChoiceNode("Choice", true);
            });

            menu.AddItem(new GUIContent("Action"), false, () =>
            {
                graphView?.CreateActionNode("Action", true);
            });

            try
            {
                if (addNodeBtn != null)
                {
                    var btnWorld = addNodeBtn.worldBound;
                    var screenPos = new Vector2(position.x + btnWorld.x, position.y + btnWorld.y + btnWorld.height);
                    var anchor = new Rect(screenPos, new Vector2(1f, 1f));
                    menu.DropDown(anchor);
                    return;
                }
            }
            catch { /* fallback */ }

            menu.ShowAsContext();
        }

        #endregion
    }
}
