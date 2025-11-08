using DialogSystem.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogSystem.EditorTools.Windows
{
    /// <summary>
    /// Sidebar panel with tabs (Characters / Actions).
    /// - Characters: rename + portrait sprite (bulk apply by speaker)
    /// - Actions: edit Action Id / Payload / Wait settings (bulk apply by Action Id)
    /// </summary>
    public class EditorSidbarWindow : VisualElement
    {
        private readonly DialogGraphEditorWindow owner;

        private enum Tab { Characters, Actions }
        private Tab currentTab = Tab.Characters;

        // Header
        private Toolbar headerBar;
        private Button collapseBtn;
        private Button rescanBtn;
        private Button applyBtn;
        private Label titleLabel;

        // Tab bar + search
        private Toolbar tabBar;
        private ToolbarToggle tabCharacters;
        private ToolbarToggle tabActions;
        private ToolbarSearchField searchField;

        // Content
        private ScrollView listView;

        // Data
        private readonly Dictionary<string, CharacterRow> characterRows = new();
        private readonly Dictionary<string, ActionRow> actionRows = new();

        public EditorSidbarWindow(DialogGraphEditorWindow owner)
        {
            this.owner = owner;

            // Keep your existing USS hooks so styles continue to apply
            AddToClassList("dlg-char-sidebar");

            BuildHeader();
            BuildTabs();
            BuildList();

            SetTab(Tab.Characters, refresh: true);
        }

        #region ---------- Build UI ----------

        private void BuildHeader()
        {
            headerBar = new Toolbar();
            headerBar.AddToClassList("dlg-char-toolbar");

            // Collapse button (icon-only if your ICON_COLLAPSE exists, else shows text)
            collapseBtn = MakeToolbarButton("⮌", TextResources.ICON_COLLAPSE, "Collapse Sidebar", () => owner.ToggleSidebar());
            headerBar.Add(collapseBtn);

            titleLabel = new Label("Sidebar");
            titleLabel.AddToClassList("dlg-char-title");
            headerBar.Add(titleLabel);

            rescanBtn = MakeToolbarButton("Rescan", TextResources.ICON_RESCAN, "Rescan content from nodes", OnClickRescan);
            rescanBtn.AddToClassList("dlg-char-btn");
            headerBar.Add(rescanBtn);

            applyBtn = MakeToolbarButton("Apply", TextResources.ICON_APPLY, "Apply edits to matching nodes", OnClickApply);
            applyBtn.AddToClassList("dlg-char-btn");
            headerBar.Add(applyBtn);

            Add(headerBar);
        }

        private void BuildTabs()
        {
            tabBar = new Toolbar();

            tabCharacters = new ToolbarToggle { text = "Characters" };
            tabCharacters.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    tabActions.SetValueWithoutNotify(false);
                    SetTab(Tab.Characters, refresh: true);
                }
                else if (!tabActions.value) // keep one selected
                {
                    tabCharacters.SetValueWithoutNotify(true);
                }
            });
            tabBar.Add(tabCharacters);

            tabActions = new ToolbarToggle { text = "Actions" };
            tabActions.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    tabCharacters.SetValueWithoutNotify(false);
                    SetTab(Tab.Actions, refresh: true);
                }
                else if (!tabCharacters.value) // keep one selected
                {
                    tabActions.SetValueWithoutNotify(true);
                }
            });
            tabBar.Add(tabActions);

            tabCharacters.SetValueWithoutNotify(true);

            tabBar.Add(new ToolbarSpacer());

            searchField = new ToolbarSearchField();
            searchField.style.flexGrow = 1;
            searchField.RegisterValueChangedCallback(_ => FilterVisible());
            tabBar.Add(searchField);

            Add(tabBar);
        }

        private void BuildList()
        {
            listView = new ScrollView(ScrollViewMode.Vertical);
            listView.AddToClassList("dlg-char-list");
            Add(listView);
        }

        private Button MakeToolbarButton(string fallbackText, string iconPath, string tooltip, Action onClick)
        {
            var btn = new Button(onClick) { tooltip = tooltip };
            btn.style.flexDirection = FlexDirection.Row;
            btn.style.alignItems = Align.Center;

#if UNITY_EDITOR
            Texture2D tex = (!string.IsNullOrEmpty(iconPath)) ? AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath) : null;
            if (tex != null)
            {
                var img = new Image { image = tex, scaleMode = ScaleMode.ScaleToFit };
                img.style.width = 18;
                img.style.height = 18;
                btn.Add(img);
            }
            else
#endif
            {
                btn.text = fallbackText;
            }

            return btn;
        }

        #endregion

        #region ---------- External API ----------

        public void ClearAll()
        {
            characterRows.Clear();
            actionRows.Clear();
            listView?.Clear();
        }

        public void RebuildFromGraph()
        {
            listView.Clear();

            if (currentTab == Tab.Characters)
            {
                BuildCharactersFace();
            }
            else
            {
                BuildActionsFace();
            }

            // Apply search filter after building
            FilterVisible();
        }

        #endregion

        #region ---------- Characters face ----------

        private void BuildCharactersFace()
        {
            characterRows.Clear();

            var names = owner.CollectSpeakersFromNodes()
                             .Where(n => !string.IsNullOrWhiteSpace(n))
                             .Select(n => n.Trim())
                             .Distinct(StringComparer.Ordinal)
                             .OrderBy(n => n)
                             .ToList();

            foreach (var name in names)
            {
                var firstSprite = owner.FindFirstSpriteForSpeaker(name);
                var row = new CharacterRow(name, name, firstSprite);
                characterRows[name] = row;
                listView.Add(row);
            }
        }

        private class CharacterRow : VisualElement
        {
            public string OriginalName { get; private set; }
            public string CurrentName => nameField.value?.Trim();
            public Sprite Sprite => (Sprite)spriteField.value;

            private readonly TextField nameField;
            private readonly ObjectField spriteField;
            private readonly Image preview;

            public CharacterRow(string originalName, string currentName, Sprite sprite)
            {
                OriginalName = originalName;

                AddToClassList("char-row");

                var header = new VisualElement();
                header.AddToClassList("char-row-header");
                nameField = new TextField("Name") { value = currentName };
                nameField.AddToClassList("char-name-field");
                header.Add(nameField);
                Add(header);

                var body = new VisualElement(); body.AddToClassList("char-row-body");

                preview = new Image { scaleMode = ScaleMode.ScaleToFit };
                preview.AddToClassList("char-preview");
                body.Add(preview);

                var right = new VisualElement(); right.AddToClassList("char-row-right");
                spriteField = new ObjectField("Portrait")
                {
                    objectType = typeof(Sprite),
                    allowSceneObjects = false,
                    value = sprite
                };
                spriteField.AddToClassList("char-sprite-field");
                spriteField.RegisterValueChangedCallback(_ =>
                {
                    UpdatePreview(spriteField.value as Sprite);
                });
                right.Add(spriteField);

                body.Add(right);
                Add(body);

                UpdatePreview(sprite);
            }

            private void UpdatePreview(Sprite s) => preview.image = s ? s.texture : null;
        }

        #endregion

        #region ---------- Actions face ----------

        private void BuildActionsFace()
        {
            actionRows.Clear();

            var nodes = owner.CollectActionNodes()
                             .OrderBy(v => string.IsNullOrEmpty(v.ActionId) ? "~" : v.ActionId, StringComparer.OrdinalIgnoreCase)
                             .ThenBy(v => v.GUID, StringComparer.OrdinalIgnoreCase)
                             .ToList();

            // group by Action Id (bulk edit template per id)
            var groups = nodes.GroupBy(v => v.ActionId ?? "", StringComparer.OrdinalIgnoreCase);

            foreach (var g in groups)
            {
                var first = g.First();
                var row = new ActionRow(
                    originalActionId: g.Key,
                    actionId: first.ActionId ?? "",
                    payload: first.PayloadJson ?? "",
                    wait: first.WaitForCompletion,
                    waitSeconds: first.WaitSeconds);

                row.OnSelectAll = () =>
                {
                    var gv = owner.GetGraphView();
                    if (gv == null) return;

                    var matching = nodes.Where(v =>
                        string.Equals(v.ActionId ?? "", g.Key, StringComparison.OrdinalIgnoreCase)).Cast<GraphElement>();

                    gv.ClearSelection();
                    bool any = false;
                    foreach (var m in matching) { gv.AddToSelection(m); any = true; }
                    if (any) gv.FrameSelection();
                };

                actionRows[g.Key] = row;
                listView.Add(row);
            }
        }

        private class ActionRow : VisualElement
        {
            public string OriginalActionId { get; private set; }

            public string ActionId => idField.value?.Trim();
            public string Payload => payloadField.value ?? "";
            public bool WaitForCompletion => waitToggle.value;
            public float WaitSeconds => waitField.value;

            public Action OnSelectAll;

            private readonly TextField idField;
            private readonly TextField payloadField;
            private readonly Toggle waitToggle;
            private readonly FloatField waitField;

            public ActionRow(string originalActionId, string actionId, string payload, bool wait, float waitSeconds)
            {
                OriginalActionId = originalActionId ?? "";

                AddToClassList("char-row");   // reuse your existing row styling
                AddToClassList("action-row"); // optional hook to style differently

                // Header
                var header = new VisualElement();
                header.AddToClassList("char-row-header");

                idField = new TextField("Action Id") { value = actionId ?? "" };
                idField.AddToClassList("char-name-field");
                header.Add(idField);

                var selectBtn = new Button(() => OnSelectAll?.Invoke()) { text = "Go to" };
                selectBtn.AddToClassList("dlg-btn");
                selectBtn.AddToClassList("secondary");
                header.Add(selectBtn);

                Add(header);

                // Body
                var body = new VisualElement(); body.AddToClassList("char-row-body");

                var right = new VisualElement(); right.AddToClassList("char-row-right");

                payloadField = new TextField("Payload (JSON)") { multiline = true, value = payload ?? "" };
                payloadField.style.minHeight = 80;
                right.Add(payloadField);

                waitToggle = new Toggle("Wait For Completion") { value = wait };
                right.Add(waitToggle);

                waitField = new FloatField("Delay (sec)") { value = waitSeconds };
                right.Add(waitField);

                body.Add(right);
                Add(body);
            }
        }

        #endregion

        #region ---------- Actions ----------

        private void SetTab(Tab tab, bool refresh)
        {
            currentTab = tab;
            titleLabel.text = tab == Tab.Characters ? "Characters" : "Actions";

            if (refresh) RebuildFromGraph();
        }

        private void OnClickRescan() => RebuildFromGraph();

        private void OnClickApply()
        {
            if (currentTab == Tab.Characters)
            {
                var data = ExportCharacterBindings();
                owner.ApplySpritesToNodes(data);

                if (data.Any(b => !string.Equals(b.originalName?.Trim(), b.currentName?.Trim(), StringComparison.Ordinal)))
                    RebuildFromGraph();
            }
            else
            {
                var data = ExportActionBindings();
                owner.ApplyActionsToNodes(data);

                if (data.Any(b => !string.Equals(b.originalActionId ?? "", b.actionId ?? "", StringComparison.Ordinal)))
                    RebuildFromGraph();
            }
        }

        private void FilterVisible()
        {
            var q = searchField?.value ?? string.Empty;
            q = q.Trim();
            bool hasQ = !string.IsNullOrEmpty(q);

            foreach (var child in listView.Children())
            {
                if (!hasQ)
                {
                    child.style.display = DisplayStyle.Flex;
                    continue;
                }

                string hay = child is CharacterRow cr ? (cr.CurrentName ?? "")
                             : child is ActionRow ar ? ((ar.ActionId ?? "") + "\n" + (ar.Payload ?? ""))
                             : child.name ?? "";

                bool match = hay.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                child.style.display = match ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private List<DialogGraphEditorWindow.CharacterBinding> ExportCharacterBindings()
        {
            var list = new List<DialogGraphEditorWindow.CharacterBinding>(characterRows.Count);
            foreach (var kv in characterRows)
            {
                var r = kv.Value;
                list.Add(new DialogGraphEditorWindow.CharacterBinding
                {
                    originalName = r.OriginalName,
                    currentName = r.CurrentName,
                    sprite = r.Sprite,
                });
            }
            return list;
        }

        private List<DialogGraphEditorWindow.ActionBinding> ExportActionBindings()
        {
            var list = new List<DialogGraphEditorWindow.ActionBinding>(actionRows.Count);
            foreach (var kv in actionRows)
            {
                var r = kv.Value;
                list.Add(new DialogGraphEditorWindow.ActionBinding
                {
                    originalActionId = r.OriginalActionId,
                    actionId = r.ActionId,
                    payloadJson = r.Payload,
                    waitForCompletion = r.WaitForCompletion,
                    waitSeconds = r.WaitSeconds
                });
            }
            return list;
        }

        #endregion
    }
}
