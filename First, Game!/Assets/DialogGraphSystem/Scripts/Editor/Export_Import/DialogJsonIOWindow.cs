using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using DialogSystem.Runtime.Models;
using DialogSystem.Runtime.Models.Nodes;
using DialogSystem.Runtime.Utils;

namespace DialogSystem.EditorTools.ExportImport
{
    public class DialogJsonIOWindow : EditorWindow
    {
        #region ---------------- Icons & Menu ----------------
        private Texture2D _iconExport;
        private Texture2D _iconImport;

        [MenuItem("Tools/Dialog/JSON Import-Export...")]
        public static void Open()
        {
            var win = GetWindow<DialogJsonIOWindow>("Dialog JSON I/O");
            win.minSize = new Vector2(560, 520);
            win.Show();
        }
        #endregion

        #region ---------------- UI State ----------------
        private enum Tab { Export, Import }
        private Tab _tab = Tab.Export;

        // EditorPrefs keys
        private const string kPrefsKeyRoot = "DialogJsonIOWindow";
        private const string kPrefsExportFolder = kPrefsKeyRoot + ".exportFolder";
        private const string kPrefsImportFolder = kPrefsKeyRoot + ".importFolder";
        private const string kPrefsRecent = kPrefsKeyRoot + ".recent";
        private const int kMaxRecent = 8;

        private Vector2 _scroll;
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _pillStyle;
        #endregion

        #region ---------------- Export State ----------------
        private DialogGraph _exportGraph;
        private string _exportFileName = "";
        private string _exportFolder = TextResources.EXPORT_FOLDER;
        private bool _exportPretty = true;
        private bool _exportIncludePositions = true;
        private bool _exportOverwrite = true;
        private bool _exportUseGraphName = true;
        #endregion

        #region ---------------- Import State ----------------
        private TextAsset _importJsonAsset;
        private string _importExternalPath = "";
        private string _importFolder = TextResources.IMPORT_FOLDER;
        private string _importTargetName = "ImportedConversation";
        private string _loadedJson = "";
        private string _jsonError = "";
        private DialogGraphExport _previewDto;

        private enum ImportMode { CreateNewGraph, MergeIntoExisting }
        private ImportMode _mode = ImportMode.CreateNewGraph;

        private DialogGraph _targetGraph;  // for merge
        private DialogNode _rootNode;      // for merge strategies

        private enum MergeStrategy { ReplaceChildren, Append }
        private MergeStrategy _mergeStrategy = MergeStrategy.ReplaceChildren;

        private enum GuidPolicy { Preserve, RegenerateOnConflict, RegenerateAll }
        private GuidPolicy _guidPolicy = GuidPolicy.RegenerateOnConflict;

        private bool _backupBeforeImport = true;
        private bool _recordUndo = true;
        private bool _useJsonPositions = true;
        private Vector2 _autoLayoutOffset = new Vector2(40, 80);

        // Pro preview toggles (disabled in this build)
        private const bool PRO_UNLOCKED = false;
        private bool _pro_AutoLayoutGrid = true;
        #endregion

        #region ---------------- Unity ----------------
        private void OnEnable()
        {
            _exportFolder = EditorPrefs.GetString(kPrefsExportFolder, _exportFolder);
            _importFolder = EditorPrefs.GetString(kPrefsImportFolder, _importFolder);

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            _boxStyle = new GUIStyle("HelpBox") { padding = new RectOffset(8, 8, 8, 8) };
            _pillStyle = new GUIStyle(EditorStyles.miniButtonMid) { fontStyle = FontStyle.Bold };

            _iconExport = AssetDatabase.LoadAssetAtPath<Texture2D>(TextResources.ICON_EXPORT);
            _iconImport = AssetDatabase.LoadAssetAtPath<Texture2D>(TextResources.ICON_IMPORT);
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(kPrefsExportFolder, _exportFolder);
            EditorPrefs.SetString(kPrefsImportFolder, _importFolder);
        }
        #endregion

        #region ---------------- GUI Root ----------------
        private void OnGUI()
        {
            DrawToolbar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case Tab.Export: DrawExport(); break;
                case Tab.Import: DrawImport(); break;
            }
            EditorGUILayout.EndScrollView();

            DragAndDropArea();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var exportTex = _iconExport ? _iconExport : (Texture2D)EditorGUIUtility.IconContent("d_SaveAs").image;
                var importTex = _iconImport ? _iconImport : (Texture2D)EditorGUIUtility.IconContent("d_Import").image;

                var prevIconSize = EditorGUIUtility.GetIconSize();
                EditorGUIUtility.SetIconSize(new Vector2(16, 16));

                var exportContent = new GUIContent(" Export", exportTex);
                var importContent = new GUIContent(" Import", importTex);

                var style = EditorStyles.toolbarButton;
                float w = Mathf.Ceil(Mathf.Max(style.CalcSize(exportContent).x, style.CalcSize(importContent).x));

                if (GUILayout.Toggle(_tab == Tab.Export, exportContent, style, GUILayout.Width(w)))
                    _tab = Tab.Export;

                if (GUILayout.Toggle(_tab == Tab.Import, importContent, style, GUILayout.Width(w)))
                    _tab = Tab.Import;

                EditorGUIUtility.SetIconSize(prevIconSize);
                GUILayout.FlexibleSpace();
            }
        }
        #endregion

        #region ---------------- Export UI ----------------
        private void DrawExport()
        {
            EditorGUILayout.LabelField("Export DialogGraph → JSON", _headerStyle);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(_boxStyle))
            {
                _exportGraph = (DialogGraph)EditorGUILayout.ObjectField("Graph", _exportGraph, typeof(DialogGraph), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginDisabledGroup(_exportUseGraphName);
                    _exportFileName = EditorGUILayout.TextField(
                        "File Name",
                        string.IsNullOrEmpty(_exportFileName) && _exportGraph != null && _exportUseGraphName
                            ? SafeFile(_exportGraph.name)
                            : _exportFileName
                    );
                    EditorGUI.EndDisabledGroup();

                    _exportUseGraphName = GUILayout.Toggle(_exportUseGraphName, "Use Graph Name", GUILayout.Width(120));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(new GUIContent("Folder (Assets)"));
                    EditorGUILayout.SelectableLabel(_exportFolder, EditorStyles.textField, GUILayout.Height(18));
                    if (GUILayout.Button("Select...", GUILayout.Width(80)))
                    {
                        var abs = EditorUtility.OpenFolderPanel("Choose export folder (inside Assets)", Application.dataPath, "");
                        if (IsUnderAssets(abs, out var rel)) _exportFolder = rel;
                        else if (!string.IsNullOrEmpty(abs))
                            EditorUtility.DisplayDialog("Folder not under Assets", "Please choose a folder inside your project's Assets.", "OK");
                    }
                }

                _exportPretty = EditorGUILayout.ToggleLeft("Pretty Print", _exportPretty);
                _exportIncludePositions = EditorGUILayout.ToggleLeft("Include Node Positions", _exportIncludePositions);
                _exportOverwrite = EditorGUILayout.ToggleLeft("Overwrite if file exists", _exportOverwrite);

                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Use Selected Graph", GUILayout.Width(160)))
                    {
                        var sel = Selection.activeObject as DialogGraph;
                        if (sel) _exportGraph = sel;
                    }

                    var canExport = _exportGraph != null && Directory.Exists(AbsoluteFromAssets(_exportFolder));
                    EditorGUI.BeginDisabledGroup(!canExport);
                    if (GUILayout.Button(new GUIContent("Export", EditorGUIUtility.IconContent("d_SaveAs").image), GUILayout.Width(120)))
                        DoExport();
                    EditorGUI.EndDisabledGroup();
                }
            }

            EditorGUILayout.Space(6);
            DrawRecentSection();
        }

        private void DoExport()
        {
            if (_exportGraph == null) return;

            var fileName = _exportUseGraphName && _exportGraph != null
                ? SafeFile(_exportGraph.name)
                : (string.IsNullOrEmpty(_exportFileName) ? "DialogGraph" : SafeFile(_exportFileName));

            var assetRelDir = _exportFolder;
            var absDir = AbsoluteFromAssets(assetRelDir);
            if (!Directory.Exists(absDir)) Directory.CreateDirectory(absDir);

            var relPath = $"{assetRelDir}/{fileName}.json";
            var absPath = AbsoluteFromAssets(relPath);

            if (File.Exists(absPath) && !_exportOverwrite)
            {
                if (!EditorUtility.DisplayDialog("File exists", $"Overwrite existing file?\n{relPath}", "Overwrite", "Cancel"))
                    return;
            }

            var export = BuildExportDTO(_exportGraph);
            var json = JsonUtility.ToJson(export, _exportPretty);
            File.WriteAllText(absPath, json, Encoding.UTF8);
            AssetDatabase.Refresh();

            ShowNotification(new GUIContent($"Exported → {relPath}"));
            AddRecent(absPath);
        }
        #endregion

        #region ---------------- Import UI ----------------
        private void DrawImport()
        {
            EditorGUILayout.LabelField("Import JSON → DialogGraph", _headerStyle);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(_boxStyle))
            {
                EditorGUILayout.LabelField("Source JSON", EditorStyles.boldLabel);

                _importJsonAsset = (TextAsset)EditorGUILayout.ObjectField("TextAsset (optional)", _importJsonAsset, typeof(TextAsset), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(new GUIContent("External File"));
                    EditorGUILayout.SelectableLabel(_importExternalPath, EditorStyles.textField, GUILayout.Height(18));
                    if (GUILayout.Button("Browse...", GUILayout.Width(80)))
                    {
                        var p = EditorUtility.OpenFilePanel("Choose JSON file", GetInitialImportFolder(), "json");
                        if (!string.IsNullOrEmpty(p)) _importExternalPath = p;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Load / Preview", GUILayout.Height(22))) LoadJsonForPreview();

                    EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_loadedJson));
                    if (GUILayout.Button("Validate", GUILayout.Height(22))) ValidateJson();
                    if (GUILayout.Button("Format", GUILayout.Height(22))) PrettyFormatJson();
                    EditorGUI.EndDisabledGroup();
                }

                if (!string.IsNullOrEmpty(_jsonError))
                    EditorGUILayout.HelpBox(_jsonError, MessageType.Error);

                if (_previewDto != null)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                        int dialogCount = _previewDto.dialogNodes?.Count ?? 0;
                        int choiceCount = _previewDto.choiceNodes?.Count ?? 0;
                        int actionCount = _previewDto.actionNodes?.Count ?? 0;
                        int linkCount = _previewDto.links?.Count ?? 0;

                        EditorGUILayout.LabelField($"Dialog Nodes: {dialogCount}");
                        EditorGUILayout.LabelField($"Choice Nodes: {choiceCount}");
                        EditorGUILayout.LabelField($"Action Nodes: {actionCount}");
                        EditorGUILayout.LabelField($"Start Node: {(_previewDto.startNode != null ? "Yes" : "No")}");
                        EditorGUILayout.LabelField($"End Node: {(_previewDto.endNode != null ? "Yes" : "No")}");
                        EditorGUILayout.LabelField($"Links: {linkCount}");
                        EditorGUILayout.LabelField($"Entry GUID: {(_previewDto.startNode?.guid ?? "<none>")}");
                    }
                }

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Import Options", EditorStyles.boldLabel);

                _mode = (ImportMode)EditorGUILayout.EnumPopup("Mode", _mode);

                if (_mode == ImportMode.CreateNewGraph)
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        _importTargetName = EditorGUILayout.TextField("Asset Name", _importTargetName);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PrefixLabel(new GUIContent("Folder (Assets)"));
                            EditorGUILayout.SelectableLabel(_importFolder, EditorStyles.textField, GUILayout.Height(18));
                            if (GUILayout.Button("Select...", GUILayout.Width(80)))
                            {
                                var abs = EditorUtility.OpenFolderPanel("Choose graph folder (inside Assets)", Application.dataPath, "");
                                if (IsUnderAssets(abs, out var rel)) _importFolder = rel;
                                else if (!string.IsNullOrEmpty(abs))
                                    EditorUtility.DisplayDialog("Folder not under Assets", "Please choose a folder inside your project's Assets.", "OK");
                            }
                        }
                    }
                }
                else
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        _targetGraph = (DialogGraph)EditorGUILayout.ObjectField("Target Graph", _targetGraph, typeof(DialogGraph), false);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            _rootNode = (DialogNode)EditorGUILayout.ObjectField("Root Node", _rootNode, typeof(DialogNode), false);
                            if (GUILayout.Button("Use Selected Node", GUILayout.Width(140)))
                                _rootNode = Selection.activeObject as DialogNode;
                        }
                        _mergeStrategy = (MergeStrategy)EditorGUILayout.EnumPopup("Merge Strategy", _mergeStrategy);
                    }
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    _guidPolicy = (GuidPolicy)EditorGUILayout.EnumPopup("GUID Policy", _guidPolicy);
                    _backupBeforeImport = EditorGUILayout.ToggleLeft("Backup target graph to JSON before import", _backupBeforeImport);
                    _recordUndo = EditorGUILayout.ToggleLeft("Record Undo", _recordUndo);
                    _useJsonPositions = EditorGUILayout.ToggleLeft("Use JSON node positions", _useJsonPositions);

                    EditorGUI.BeginDisabledGroup(!PRO_UNLOCKED);
                    _pro_AutoLayoutGrid = EditorGUILayout.ToggleLeft("(Pro) Auto-layout grid for imported nodes", _pro_AutoLayoutGrid);
                    EditorGUI.EndDisabledGroup();

                    if (!PRO_UNLOCKED)
                        EditorGUILayout.HelpBox("Pro features are previewed here and currently disabled.", MessageType.None);
                }

                EditorGUILayout.Space(4);
                DrawJsonEditorArea();

                EditorGUILayout.Space(8);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    var canImport =
                        _previewDto != null &&
                        ((_mode == ImportMode.CreateNewGraph) ||
                         (_targetGraph != null && (_mergeStrategy == MergeStrategy.Append || _rootNode != null)));

                    EditorGUI.BeginDisabledGroup(!canImport);
                    if (GUILayout.Button(new GUIContent("Import", EditorGUIUtility.IconContent("d_Import").image), GUILayout.Width(140), GUILayout.Height(24)))
                        DoImport();
                    EditorGUI.EndDisabledGroup();
                }
            }

            EditorGUILayout.Space(6);
            DrawRecentSection();
        }

        private void DrawJsonEditorArea()
        {
            EditorGUILayout.LabelField("JSON Preview / Editor", EditorStyles.boldLabel);
            var height = Mathf.Clamp(EditorGUIUtility.singleLineHeight * 8f, 120f, position.height * 0.35f);
            EditorGUI.BeginChangeCheck();
            _loadedJson = EditorGUILayout.TextArea(_loadedJson ?? "", GUILayout.MinHeight(height));
            if (EditorGUI.EndChangeCheck())
            {
                _previewDto = null;
                _jsonError = "";
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload from Source")) LoadJsonForPreview();
                if (GUILayout.Button("Save JSON As...")) SaveJsonToAssets();
            }
        }
        #endregion

        #region ---------------- Export DTO Builder ----------------
        private DialogGraphExport BuildExportDTO(DialogGraph graph)
        {
            var export = new DialogGraphExport();

            // Dialog nodes
            if (graph.nodes != null)
            {
                foreach (var n in graph.nodes.Where(n => n != null))
                {
                    export.dialogNodes.Add(new DialogExportDialogNode
                    {
                        title = (n.name ?? "Node").Replace("Node_", ""),
                        guid = n.GetGuid(),
                        speaker = n.speakerName,
                        question = n.questionText,
                        nodePositionX = _exportIncludePositions ? n.GetPosition().x : 0f,
                        nodePositionY = _exportIncludePositions ? n.GetPosition().y : 0f,
                        displayTime = n.displayTime,
                    });
                }
            }

            // Choice nodes
            if (graph.choiceNodes != null)
            {
                foreach (var c in graph.choiceNodes.Where(c => c != null))
                {
                    var dto = new DialogExportChoiceNode
                    {
                        guid = c.GetGuid(),
                        text = c.text,
                        nodePositionX = _exportIncludePositions ? c.GetPosition().x : 0f,
                        nodePositionY = _exportIncludePositions ? c.GetPosition().y : 0f,
                        choices = new List<ExportChoice>(),
                    };

                    if (c.choices != null)
                    {
                        foreach (var ch in c.choices)
                        {
                            dto.choices.Add(new ExportChoice
                            {
                                answerText = ch.answerText,
                                nextNodeGUID = ch.nextNodeGUID
                            });
                        }
                    }

                    export.choiceNodes.Add(dto);
                }
            }

            // Action nodes
            if (graph.actionNodes != null)
            {
                foreach (var a in graph.actionNodes.Where(a => a != null))
                {
                    export.actionNodes.Add(new DialogExportActionNode
                    {
                        guid = a.GetGuid(),
                        actionId = a.actionId,
                        payloadJson = a.payloadJson,
                        waitForCompletion = a.waitForCompletion,
                        waitSeconds = a.waitSeconds,
                        nodePositionX = _exportIncludePositions ? a.GetPosition().x : 0f,
                        nodePositionY = _exportIncludePositions ? a.GetPosition().y : 0f
                    });
                }
            }

            if (!string.IsNullOrEmpty(graph.startGuid))
            {
                export.startNode = new ExportStartNode
                {
                    isInitialized = true,
                    guid = graph.startGuid,
                    nodePositionX = graph.startPosition.x,
                    nodePositionY = graph.startPosition.y
                };
            }
            if (!string.IsNullOrEmpty(graph.endGuid))
            {
                export.endNode = new ExportEndNode
                {
                    isInitialized = true,
                    guid = graph.endGuid,
                    nodePositionX = graph.endPosition.x,
                    nodePositionY = graph.endPosition.y
                };
            }

            if (graph.links != null)
            {
                export.links = graph.links.Select(l => new ExportLink
                {
                    fromGuid = l.fromGuid,
                    toGuid = l.toGuid,
                    fromPortIndex = l.fromPortIndex
                }).ToList();
            }

            return export;
        }
        #endregion

        #region ---------------- Import Logic ----------------
        private void DoImport()
        {
            if (_previewDto == null)
            {
                EditorUtility.DisplayDialog("No JSON", "Load a JSON file and pass validation first.", "OK");
                return;
            }

            if (_mode == ImportMode.CreateNewGraph)
            {
                CreateNewGraphFromDto(_previewDto);
            }
            else
            {
                if (_targetGraph == null)
                {
                    EditorUtility.DisplayDialog("Missing Target Graph", "Choose a target graph.", "OK");
                    return;
                }
                if (_mergeStrategy == MergeStrategy.ReplaceChildren && _rootNode == null)
                {
                    EditorUtility.DisplayDialog("Missing Root Node", "Choose a root node for Replace Children.", "OK");
                    return;
                }

                if (_backupBeforeImport) BackupGraphToJson(_targetGraph);
                if (_recordUndo) Undo.RegisterCompleteObjectUndo(_targetGraph, "Import Dialog JSON");

                if (_mergeStrategy == MergeStrategy.ReplaceChildren)
                    MergeReplaceChildren(_targetGraph, _rootNode, _previewDto);
                else
                    MergeAppend(_targetGraph, _previewDto);

                EditorUtility.SetDirty(_targetGraph);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ShowNotification(new GUIContent("Import complete"));
            }
        }

        private void CreateNewGraphFromDto(DialogGraphExport dto)
        {
            var absDir = AbsoluteFromAssets(_importFolder);
            if (!Directory.Exists(absDir)) Directory.CreateDirectory(absDir);

            var assetName = SafeFile(_importTargetName);
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{_importFolder}/{assetName}.asset");

            var graph = ScriptableObject.CreateInstance<DialogGraph>();
            AssetDatabase.CreateAsset(graph, uniquePath);

            _ = BuildFromDto(graph, dto, rootForAttach: null, addAttachLink: false);

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = graph;
            ShowNotification(new GUIContent($"Created {Path.GetFileName(uniquePath)}"));
        }

        private void MergeReplaceChildren(DialogGraph graph, DialogNode root, DialogGraphExport dto)
        {
            // Import nodes/links and add one attach link: root -> imported entry (port 0)
            _ = BuildFromDto(graph, dto, rootForAttach: root, addAttachLink: true);
        }

        private void MergeAppend(DialogGraph graph, DialogGraphExport dto)
        {
            // Import nodes/links without attach link
            _ = BuildFromDto(graph, dto, rootForAttach: null, addAttachLink: false);
        }

        /// <summary>
        /// Create nodes and links from DTO into 'graph'.
        /// If 'rootForAttach' is provided and 'addAttachLink' is true, also link root→importedEntry (port 0).
        /// Returns mapped entry GUID in the target graph (or null).
        /// </summary>
        private string BuildFromDto(DialogGraph graph, DialogGraphExport dto, DialogNode rootForAttach, bool addAttachLink)
        {
            if (graph.nodes == null) graph.nodes = new List<DialogNode>();
            if (graph.choiceNodes == null) graph.choiceNodes = new List<ChoiceNode>();
            if (graph.actionNodes == null) graph.actionNodes = new List<DialogSystem.Runtime.Models.Nodes.ActionNode>();
            if (graph.links == null) graph.links = new List<GraphLink>();

            var existing = new HashSet<string>();
            var map = new Dictionary<string, string>();

            // Start / End (editor markers)
            if (dto.startNode != null && !string.IsNullOrEmpty(dto.startNode.guid))
            {
                graph.startGuid = dto.startNode.guid;
                graph.startPosition = new Vector2(dto.startNode.nodePositionX, dto.startNode.nodePositionY);
                map[dto.startNode.guid] = graph.startGuid;
            }
            else
            {
                graph.startGuid = Guid.NewGuid().ToString("N");
                graph.startPosition = new Vector2(-320f, 80f);
            }

            if (dto.endNode != null && !string.IsNullOrEmpty(dto.endNode.guid))
            {
                graph.endGuid = dto.endNode.guid;
                graph.endPosition = new Vector2(dto.endNode.nodePositionX, dto.endNode.nodePositionY);
                map[dto.endNode.guid] = graph.endGuid;
            }
            else
            {
                graph.endGuid = Guid.NewGuid().ToString("N");
                graph.endPosition = new Vector2(720f, 80f);
            }

            graph.startInitialized = dto.startNode != null && dto.startNode.isInitialized;
            graph.endInitialized = dto.endNode != null && dto.endNode.isInitialized;

            // Collect existing GUIDs to detect conflicts
            existing.UnionWith(graph.nodes.Where(n => n != null).Select(n => n.GetGuid()));
            existing.UnionWith(graph.choiceNodes.Where(n => n != null).Select(n => n.GetGuid()));
            existing.UnionWith(graph.actionNodes.Where(n => n != null).Select(n => n.GetGuid()));

            // Map dialog nodes
            foreach (var d in dto.dialogNodes ?? Enumerable.Empty<DialogExportDialogNode>())
            {
                var src = string.IsNullOrEmpty(d.guid) ? Guid.NewGuid().ToString("N") : d.guid;
                var dst = src;

                if (_guidPolicy == GuidPolicy.RegenerateAll || (_guidPolicy == GuidPolicy.RegenerateOnConflict && existing.Contains(dst)))
                    dst = Guid.NewGuid().ToString("N");

                map[src] = dst;
                existing.Add(dst);
            }

            // Map choice nodes
            foreach (var c in dto.choiceNodes ?? Enumerable.Empty<DialogExportChoiceNode>())
            {
                var src = string.IsNullOrEmpty(c.guid) ? Guid.NewGuid().ToString("N") : c.guid;
                var dst = src;

                if (_guidPolicy == GuidPolicy.RegenerateAll || (_guidPolicy == GuidPolicy.RegenerateOnConflict && existing.Contains(dst)))
                    dst = Guid.NewGuid().ToString("N");

                map[src] = dst;
                existing.Add(dst);
            }

            // Map action nodes
            foreach (var a in dto.actionNodes ?? Enumerable.Empty<DialogExportActionNode>())
            {
                var src = string.IsNullOrEmpty(a.guid) ? Guid.NewGuid().ToString("N") : a.guid;
                var dst = src;

                if (_guidPolicy == GuidPolicy.RegenerateAll || (_guidPolicy == GuidPolicy.RegenerateOnConflict && existing.Contains(dst)))
                    dst = Guid.NewGuid().ToString("N");

                map[src] = dst;
                existing.Add(dst);
            }

            // Create DialogNode sub-assets
            foreach (var d in dto.dialogNodes ?? Enumerable.Empty<DialogExportDialogNode>())
            {
                var so = ScriptableObject.CreateInstance<DialogNode>();
                so.SetGuid(map[d.guid]);
                so.name = "Node_" + (string.IsNullOrEmpty(d.title) ? "Untitled" : SafeFile(d.title));
                so.speakerName = d.speaker;
                so.questionText = d.question;
                so.displayTime = d.displayTime;

                so.SetPosition(_useJsonPositions
                    ? new Vector2(d.nodePositionX, d.nodePositionY)
                    : (rootForAttach != null ? rootForAttach.GetPosition() + _autoLayoutOffset : Vector2.zero));

                graph.nodes.Add(so);
                AssetDatabase.AddObjectToAsset(so, graph);
            }

            // Create ChoiceNode sub-assets
            foreach (var c in dto.choiceNodes ?? Enumerable.Empty<DialogExportChoiceNode>())
            {
                var so = ScriptableObject.CreateInstance<ChoiceNode>();
                so.SetGuid(map[c.guid]);
                so.name = "ChoiceNode";
                so.text = c.text;

                so.SetPosition(_useJsonPositions
                    ? new Vector2(c.nodePositionX, c.nodePositionY)
                    : (rootForAttach != null ? rootForAttach.GetPosition() + _autoLayoutOffset : Vector2.zero));

                so.choices = new List<Choice>();
                foreach (var ch in c.choices ?? Enumerable.Empty<ExportChoice>())
                {
                    so.choices.Add(new Choice
                    {
                        answerText = ch.answerText,
                        nextNodeGUID = null // authoritative links will be created below
                    });
                }

                graph.choiceNodes.Add(so);
                AssetDatabase.AddObjectToAsset(so, graph);
            }

            // Create ActionNode sub-assets
            foreach (var a in dto.actionNodes ?? Enumerable.Empty<DialogExportActionNode>())
            {
                var so = ScriptableObject.CreateInstance<DialogSystem.Runtime.Models.Nodes.ActionNode>();
                so.SetGuid(map[a.guid]);
                so.name = "ActionNode";
                so.actionId = a.actionId;
                so.payloadJson = a.payloadJson;
                so.waitForCompletion = a.waitForCompletion;
                so.waitSeconds = a.waitSeconds;

                var pos = _useJsonPositions
                    ? new Vector2(a.nodePositionX, a.nodePositionY)
                    : (rootForAttach != null ? rootForAttach.GetPosition() + _autoLayoutOffset : Vector2.zero);

                so.SetPosition(pos);

                graph.actionNodes.Add(so);
                AssetDatabase.AddObjectToAsset(so, graph);
            }

            // Build links buffer (JSON links primary; fallback to embedded choice links)
            var linkBuffer = new List<ExportLink>();
            if (dto.links != null && dto.links.Count > 0)
            {
                linkBuffer.AddRange(dto.links);
            }
            else
            {
                int idx;
                foreach (var c in dto.choiceNodes ?? Enumerable.Empty<DialogExportChoiceNode>())
                {
                    idx = 0;
                    foreach (var ch in c.choices ?? Enumerable.Empty<ExportChoice>())
                    {
                        if (!string.IsNullOrEmpty(ch.nextNodeGUID))
                        {
                            linkBuffer.Add(new ExportLink
                            {
                                fromGuid = c.guid,
                                toGuid = ch.nextNodeGUID,
                                fromPortIndex = idx
                            });
                        }
                        idx++;
                    }
                }
            }

            // Map & add links
            foreach (var l in linkBuffer)
            {
                if (string.IsNullOrEmpty(l.fromGuid) || string.IsNullOrEmpty(l.toGuid)) continue;
                if (!map.TryGetValue(l.fromGuid, out var fromMapped)) continue;
                if (!map.TryGetValue(l.toGuid, out var toMapped)) continue;

                graph.links.Add(new GraphLink
                {
                    fromGuid = fromMapped,
                    toGuid = toMapped,
                    fromPortIndex = l.fromPortIndex
                });
            }

            // Attach imported entry to provided root (if requested)
            string mappedEntry = dto.startNode != null && !string.IsNullOrEmpty(dto.startNode.guid) && map.TryGetValue(dto.startNode.guid, out var entry) ? entry : null;
            if (addAttachLink && rootForAttach != null && !string.IsNullOrEmpty(mappedEntry))
            {
                graph.links.Add(new GraphLink
                {
                    fromGuid = rootForAttach.GetGuid(),
                    toGuid = mappedEntry,
                    fromPortIndex = 0
                });
            }

            return mappedEntry;
        }
        #endregion

        #region ---------------- JSON Load/Validate ----------------
        private void LoadJsonForPreview()
        {
            _jsonError = "";
            _previewDto = null;

            try
            {
                string json = null;
                if (_importJsonAsset != null)
                {
                    _importExternalPath = "";
                    json = _importJsonAsset.text;
                }
                else if (!string.IsNullOrEmpty(_importExternalPath) && File.Exists(_importExternalPath))
                {
                    json = File.ReadAllText(_importExternalPath, Encoding.UTF8);
                    TryRememberImportFolder(_importExternalPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("No Source", "Assign a TextAsset or choose an external JSON file.", "OK");
                    return;
                }

                _loadedJson = json;
                ValidateJson();
            }
            catch (Exception ex)
            {
                _jsonError = "Load failed: " + ex.Message;
            }
        }

        private void ValidateJson()
        {
            try
            {
                _previewDto = JsonUtility.FromJson<DialogGraphExport>(_loadedJson);
                bool ok = _previewDto != null &&
                          (_previewDto.dialogNodes != null || _previewDto.choiceNodes != null || _previewDto.actionNodes != null);
                _jsonError = ok ? "" : "Invalid or empty JSON.";
            }
            catch (Exception ex)
            {
                _previewDto = null;
                _jsonError = "JSON parse error: " + ex.Message;
            }
        }

        private void PrettyFormatJson()
        {
            if (string.IsNullOrEmpty(_loadedJson)) return;
            try
            {
                var dto = JsonUtility.FromJson<DialogGraphExport>(_loadedJson);
                if (dto != null)
                {
                    _loadedJson = JsonUtility.ToJson(dto, true);
                    _jsonError = "";
                }
                else _jsonError = "Cannot format: invalid JSON.";
            }
            catch (Exception ex)
            {
                _jsonError = "Format failed: " + ex.Message;
            }
        }

        private void SaveJsonToAssets()
        {
            var relPath = EditorUtility.SaveFilePanelInProject("Save JSON", "DialogGraph.json", "json", "Choose save location");
            if (string.IsNullOrEmpty(relPath)) return;
            File.WriteAllText(AbsoluteFromAssets(relPath), _loadedJson ?? "", Encoding.UTF8);
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("Saved JSON to " + relPath));
            AddRecent(AbsoluteFromAssets(relPath));
        }
        #endregion

        #region ---------------- Recent / Backup helpers ----------------
        private void DrawRecentSection()
        {
            var recent = GetRecent();
            if (recent.Count == 0) return;

            EditorGUILayout.LabelField("Recent Files", _headerStyle);
            using (new EditorGUILayout.VerticalScope(_boxStyle))
            {
                foreach (var abs in recent.ToList())
                {
                    var exists = File.Exists(abs);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var label = exists ? ShortenPath(abs) : $"(Missing) {ShortenPath(abs)}";
                        EditorGUILayout.SelectableLabel(label, GUILayout.Height(16));

                        if (GUILayout.Button("Reveal", GUILayout.Width(60)))
                            EditorUtility.RevealInFinder(abs);

                        if (GUILayout.Button("Load", GUILayout.Width(60)))
                        {
                            _importJsonAsset = null;
                            _importExternalPath = abs;
                            LoadJsonForPreview();
                        }

                        if (!exists)
                        {
                            if (GUILayout.Button("Remove", GUILayout.Width(60)))
                                RemoveRecent(abs);
                        }
                    }
                }
            }
        }

        private void AddRecent(string abs)
        {
            var list = GetRecent();
            list.Remove(abs);
            list.Insert(0, abs);
            if (list.Count > kMaxRecent) list.RemoveAt(list.Count - 1);
            EditorPrefs.SetString(kPrefsRecent, string.Join("|", list));
        }

        private void RemoveRecent(string abs)
        {
            var list = GetRecent();
            list.Remove(abs);
            EditorPrefs.SetString(kPrefsRecent, string.Join("|", list));
        }

        private List<string> GetRecent()
        {
            var raw = EditorPrefs.GetString(kPrefsRecent, "");
            var list = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            return list;
        }

        private void BackupGraphToJson(DialogGraph graph)
        {
            if (graph == null) return;
            var dto = BuildExportDTO(graph);
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var folder = _exportFolder;
            var absDir = AbsoluteFromAssets(folder);
            if (!Directory.Exists(absDir)) Directory.CreateDirectory(absDir);
            var relPath = $"{folder}/{SafeFile(graph.name)}_BACKUP_{ts}.json";
            File.WriteAllText(AbsoluteFromAssets(relPath), JsonUtility.ToJson(dto, true), Encoding.UTF8);
            AssetDatabase.Refresh();
            AddRecent(AbsoluteFromAssets(relPath));
        }
        #endregion

        #region ---------------- Drag & Drop ----------------
        private void DragAndDropArea()
        {
            var evt = Event.current;
            var rect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "Drag & Drop a .json file here to preview/import", EditorStyles.helpBox);

            if (!rect.Contains(evt.mousePosition)) return;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                var paths = DragAndDrop.paths;
                if (paths != null && paths.Length > 0 && paths[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        _importJsonAsset = null;
                        _importExternalPath = paths[0];
                        LoadJsonForPreview();
                    }
                    evt.Use();
                }
            }
        }
        #endregion

        #region ---------------- Path Helpers ----------------
        private static bool IsUnderAssets(string absolutePath, out string assetsRelative)
        {
            assetsRelative = null;
            if (string.IsNullOrEmpty(absolutePath)) return false;
            absolutePath = absolutePath.Replace("\\", "/");
            var assetsAbs = Application.dataPath.Replace("\\", "/");
            if (!absolutePath.StartsWith(assetsAbs, StringComparison.OrdinalIgnoreCase)) return false;
            assetsRelative = "Assets" + absolutePath.Substring(assetsAbs.Length);
            return true;
        }

        private static string AbsoluteFromAssets(string assetsRelative)
        {
            if (string.IsNullOrEmpty(assetsRelative)) return null;
            var rel = assetsRelative.Replace("\\", "/");
            if (!rel.StartsWith("Assets/") && rel != "Assets") throw new Exception("Path must start with 'Assets/'");
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", rel)).Replace("\\", "/");
        }

        private static string SafeFile(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var ch in name)
                sb.Append(invalid.Contains(ch) ? '_' : ch);
            return sb.ToString();
        }

        private string GetInitialImportFolder()
        {
            if (!string.IsNullOrEmpty(_importExternalPath))
                return Path.GetDirectoryName(_importExternalPath);
            if (!string.IsNullOrEmpty(_importFolder))
                return AbsoluteFromAssets(_importFolder);
            return Application.dataPath;
        }

        private void TryRememberImportFolder(string externalAbsPath)
        {
            var dir = Path.GetDirectoryName(externalAbsPath)?.Replace("\\", "/");
            if (IsUnderAssets(externalAbsPath, out var rel))
            {
                var relFolder = Path.GetDirectoryName(rel)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(relFolder))
                {
                    _importFolder = relFolder;
                    EditorPrefs.SetString(kPrefsImportFolder, _importFolder);
                }
            }
        }

        private static string ShortenPath(string abs, int max = 70)
        {
            if (abs.Length <= max) return abs;
            var file = Path.GetFileName(abs);
            var dir = Path.GetDirectoryName(abs).Replace("\\", "/");
            var start = dir.Length > 10 ? dir.Substring(0, 10) : dir;
            var end = dir.Length > 10 ? dir.Substring(dir.Length - 10) : "";
            return $"{start}…/{end}/{file}";
        }
        #endregion
    }
}
