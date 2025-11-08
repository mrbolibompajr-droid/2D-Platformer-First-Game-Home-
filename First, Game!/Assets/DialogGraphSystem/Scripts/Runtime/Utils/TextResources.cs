using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogSystem.Runtime.Utils
{
    /// <summary>
    /// Centralized string constants (and optional helpers) for icon paths.
    /// - Editor code (AssetDatabase) should use the *ASSET* paths.
    /// - Runtime code (Resources.Load) should use the *RES* keys.
    /// </summary>
    public static class TextResources
    {
        #region ---------------- Editor Icon Asset Paths ----------------
        // Use with: UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path)
        public const string ICON_EXPORT_ASSET = "Assets/DialogGraphSystem/Resources/UI/EditorUI/Export.png";
        public const string ICON_IMPORT_ASSET = "Assets/DialogGraphSystem/Resources/UI/EditorUI/Import.png";
        public const string ICON_SAVE = "Assets/DialogGraphSystem/Resources/UI/EditorUI/SaveIcon.png";
        public const string ICON_LOAD = "Assets/DialogGraphSystem/Resources/UI/EditorUI/LoadIcon.png";
        public const string ICON_ADD = "Assets/DialogGraphSystem/Resources/UI/EditorUI/plus.png";
        public const string ICON_CLEAR = "Assets/DialogGraphSystem/Resources/UI/EditorUI/trash.png";
        public const string ICON_SIDEBAR = "Assets/DialogGraphSystem/Resources/UI/EditorUI/Sidebar.png";
        public const string ICON_COLLAPSE = "Assets/DialogGraphSystem/Resources/UI/EditorUI/Collapse.png";
        public const string ICON_RESCAN = "Assets/DialogGraphSystem/Resources/UI/EditorUI/Rescan.png";
        public const string ICON_APPLY = "Assets/DialogGraphSystem/Resources/UI/EditorUI/Apply.png";
        public const string ICON_EXPORT = "Assets/DialogGraphSystem/Resources/UI/EditorUI/Export.png";
        public const string ICON_IMPORT = "Assets/DialogGraphSystem/Resources/UI/EditorUI/Import.png";

        #endregion

        #region ---------------- Editor USS Asset Paths ----------------

        public const string STYLE_PATH = "USS/DialogGraphEditorUSS";
        public const string GRAPHS_FOLDER = "Assets/DialogGraphSystem/Resources/Conversation/";

        #endregion

        #region ---------------- Folder Asset Paths ----------------

        public const string EXPORT_FOLDER = "Assets/DialogGraphSystem/Exports";
        public const string IMPORT_FOLDER = "Assets/DialogGraphSystem/Resources/Conversation";
        public const string CONVERSATION_FOLDER = "Assets/DialogGraphSystem/Resources/Conversation";

        #endregion


        #region ---------------- Runtime Icon Resource Keys ----------------
        // Use with: Resources.Load<Texture2D>(key)  (no extension)
        public const string ICON_EXPORT_RES = "UI/EditorUI/Export";
        public const string ICON_IMPORT_RES = "UI/EditorUI/Import";
        #endregion

#if UNITY_EDITOR
        #region ---------------- Editor Load Helpers ----------------
        public static Texture2D LoadEditorIconAsset(string assetPath) =>
            UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

        public static Texture2D ExportIconEditor() => LoadEditorIconAsset(ICON_EXPORT_ASSET);
        public static Texture2D ImportIconEditor() => LoadEditorIconAsset(ICON_IMPORT_ASSET);
        #endregion
#endif

        #region ---------------- Runtime Load Helpers ----------------
        public static Texture2D LoadRuntimeIcon(string resourceKey) =>
            Resources.Load<Texture2D>(resourceKey);
        #endregion
    }
}
