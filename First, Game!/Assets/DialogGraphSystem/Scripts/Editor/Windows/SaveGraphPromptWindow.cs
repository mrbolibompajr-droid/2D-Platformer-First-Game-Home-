using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogSystem.EditorTools.Windows
{
    internal class SaveGraphPromptWindow : EditorWindow
    {
        #region ---------------- Types ----------------
        public enum SaveMode { UseLoaded, OverwriteExisting, SaveAsNew }
        #endregion

        #region ---------------- Callbacks & State ----------------
        private Action<string> _onConfirm;
        private Action _onCancel;

        private string _currentSuggestedName;
        private string _loadedGraphName;
        private List<string> _existingNames = new();
        private bool _isGraphEmpty;
        #endregion

        #region ---------------- UI Elements ----------------
        private EnumField _modeField;
        private TextField _newNameField;
        private PopupField<string> _existingPopup;
        private Label _warningLabel;
        #endregion

        #region ---------------- Open ----------------
        public static void Open(
            string currentName,
            string loadedGraphName,
            List<string> existingNames,
            bool isGraphEmpty,
            Action<string> onConfirm,
            Action onCancel = null)
        {
            var w = CreateInstance<SaveGraphPromptWindow>();
            w.titleContent = new GUIContent("Save Dialog Graph…");
            w._onConfirm = onConfirm;
            w._onCancel = onCancel;
            w._currentSuggestedName = string.IsNullOrEmpty(currentName) ? "NewDialogGraph" : currentName;
            w._loadedGraphName = loadedGraphName;
            w._existingNames = existingNames ?? new List<string>();
            w._isGraphEmpty = isGraphEmpty;

            w.minSize = new Vector2(460, 240);
            w.maxSize = new Vector2(800, 320);
            w.ShowUtility();

            w.BuildUI();
            w.Focus();
        }
        #endregion

        #region ---------------- UI Build ----------------
        private void BuildUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            var title = new Label("Choose how to save the current graph");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6;
            root.Add(title);

            // Mode
            var defaultMode = !string.IsNullOrEmpty(_loadedGraphName) ? SaveMode.UseLoaded : SaveMode.SaveAsNew;
            _modeField = new EnumField("Mode", defaultMode);
            root.Add(_modeField);

            // Loaded info
            if (!string.IsNullOrEmpty(_loadedGraphName))
            {
                var loadedRow = new VisualElement();
                loadedRow.style.marginTop = 4;
                var loadedLbl = new Label($"Loaded: {_loadedGraphName}");
                loadedLbl.style.opacity = 0.85f;
                loadedRow.Add(loadedLbl);
                root.Add(loadedRow);
            }

            // Overwrite existing popup
            var names = _existingNames.Count > 0 ? _existingNames : new List<string> { "(none found)" };
            _existingPopup = new PopupField<string>("Overwrite", names, 0);
            _existingPopup.SetEnabled(_existingNames.Count > 0);
            root.Add(_existingPopup);

            // New name field
            _newNameField = new TextField("Save As")
            {
                value = _currentSuggestedName,
                isDelayed = true
            };
            root.Add(_newNameField);

            // Warning label
            _warningLabel = new Label();
            _warningLabel.style.marginTop = 4;
            _warningLabel.style.whiteSpace = WhiteSpace.Normal;
            if (_isGraphEmpty)
            {
                _warningLabel.text = "Warning: current graph appears to be EMPTY. Overwriting an existing conversation will erase it.";
                _warningLabel.style.color = Color.yellow;
            }
            root.Add(_warningLabel);

            // Buttons row
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.FlexEnd;
            btnRow.style.marginTop = 12;

            var cancelBtn = new Button(() => { _onCancel?.Invoke(); Close(); }) { text = "Cancel" };
            var saveBtn = new Button(OnClickSave) { text = "Save" };

            btnRow.Add(cancelBtn);
            btnRow.Add(saveBtn);
            root.Add(btnRow);

            // Enable/disable logic and events
            UpdateEnables();
            _modeField.RegisterValueChangedCallback(_ => UpdateEnables());

            // Keyboard shortcuts: Enter = Save, Esc = Cancel
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    OnClickSave();
                    evt.StopImmediatePropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    _onCancel?.Invoke();
                    Close();
                    evt.StopImmediatePropagation();
                }
            });
        }
        #endregion

        #region ---------------- Logic ----------------
        private void UpdateEnables()
        {
            var mode = (SaveMode)_modeField.value;

            // Defensive: disable UseLoaded if none loaded
            if (string.IsNullOrEmpty(_loadedGraphName) && mode == SaveMode.UseLoaded)
            {
                _modeField.SetValueWithoutNotify(SaveMode.SaveAsNew);
                mode = SaveMode.SaveAsNew;
            }

            _existingPopup?.SetEnabled(mode == SaveMode.OverwriteExisting && _existingNames.Count > 0);
            _newNameField?.SetEnabled(mode == SaveMode.SaveAsNew);
        }

        private void OnClickSave()
        {
            var mode = (SaveMode)_modeField.value;
            string finalName = null;

            switch (mode)
            {
                case SaveMode.UseLoaded:
                    finalName = _loadedGraphName;
                    break;

                case SaveMode.OverwriteExisting:
                    finalName = _existingNames.Count > 0 ? _existingPopup.value : null;
                    break;

                case SaveMode.SaveAsNew:
                    finalName = SanitizeName(_newNameField.value);
                    if (string.IsNullOrEmpty(finalName))
                    {
                        EditorUtility.DisplayDialog("Invalid Name", "Please enter a valid file name.", "OK");
                        return;
                    }
                    // If name already exists, confirm overwrite
                    if (_existingNames.Any(n => string.Equals(n, finalName, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!EditorUtility.DisplayDialog(
                                "Name Already Exists",
                                $"A graph named \"{finalName}\" already exists. Overwrite it?",
                                "Overwrite", "Cancel"))
                            return;
                    }
                    break;
            }

            if (string.IsNullOrEmpty(finalName))
            {
                EditorUtility.DisplayDialog("No Selection", "Please choose a valid target to save.", "OK");
                return;
            }

            // If overwriting while graph is empty, demand explicit confirmation
            if (_isGraphEmpty)
            {
                if (!EditorUtility.DisplayDialog(
                        "Overwrite with an EMPTY graph?",
                        $"You are about to save an empty graph as \"{finalName}\". This will overwrite the existing content if it exists.",
                        "I understand, continue", "Cancel"))
                    return;
            }

            _onConfirm?.Invoke(finalName);
            Close();
        }
        #endregion

        #region ---------------- Helpers ----------------
        private static string SanitizeName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var clean = new string(s.Trim().Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return clean;
        }
        #endregion
    }
}
