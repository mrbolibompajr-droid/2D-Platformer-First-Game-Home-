using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DialogSystem.Runtime.DialogHistory;

namespace DialogSystem.Runtime
{
    /// <summary>
    /// Pooled, scroll-to-bottom history list renderer for <see cref="HistoryEntry"/>.
    /// Expects a row prefab of type <see cref="DialogueHistoryItem"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueHistoryView : MonoBehaviour
    {
        #region -------- UI --------
        [Header("UI")]
        [Tooltip("Root panel GameObject that contains the history UI.")]
        public GameObject root;

        [Tooltip("Vertical ScrollRect controlling the content viewport.")]
        public ScrollRect scrollRect;

        [Tooltip("Parent transform for pooled item rows.")]
        public Transform contentRoot;

        [Tooltip("Row prefab (PF_DialogueHistoryItem).")]
        public DialogueHistoryItem itemPrefab;
        #endregion

        #region -------- Pool --------
        [Header("Pool")]
        [Min(0), Tooltip("Pre-instantiate this many pooled rows on start.")]
        public int prewarm = 24;

        [Tooltip("If true, choice rows hide their icon by default.")]
        public bool hideChoiceIcon = true;

        private readonly List<DialogueHistoryItem> pool = new();
        private int activeCount = 0;
        private bool dirtyScrollToBottom;
        #endregion

        #region -------- Unity Lifecycle --------
        private void Start()
        {
            Prewarm(prewarm);
        }

        private void LateUpdate()
        {
            if (!dirtyScrollToBottom) return;
            dirtyScrollToBottom = false;

            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f; // snap to bottom (newest)
        }
        #endregion

        #region -------- Public API --------
        /// <summary>Shows the history panel.</summary>
        public void Show()
        {
            if (root) root.SetActive(true);
        }

        /// <summary>Hides the history panel.</summary>
        public void Hide()
        {
            if (root) root.SetActive(false);
        }

        /// <summary>
        /// Rebuilds the list from <paramref name="entries"/> and scrolls to bottom.
        /// </summary>
        public void Refresh(IReadOnlyList<HistoryEntry> entries)
        {
            ReturnAll();
            EnsurePool(entries != null ? entries.Count : 0);

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                    BindToNext(entries[i]);
            }

            dirtyScrollToBottom = true;
        }

        /// <summary>
        /// Appends one entry to the end and scrolls to bottom.
        /// </summary>
        public void AppendItem(HistoryEntry entry)
        {
            EnsurePool(activeCount + 1);
            BindToNext(entry);
            dirtyScrollToBottom = true;
        }
        #endregion

        #region -------- Pool Helpers --------
        private void Prewarm(int count)
        {
            EnsurePool(count);
        }

        private void EnsurePool(int count)
        {
            if (!itemPrefab)
            {
                Debug.LogError("[DialogueHistoryView] Item prefab not assigned.");
                return;
            }
            if (!contentRoot)
            {
                Debug.LogError("[DialogueHistoryView] Content root not assigned.");
                return;
            }

            while (pool.Count < count)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                item.gameObject.SetActive(false);
                item.hideIconForChoices = hideChoiceIcon; // pass choice icon policy down
                pool.Add(item);
            }
        }

        private void ReturnAll()
        {
            for (int i = 0; i < activeCount; i++)
            {
                var item = pool[i];
                if (item) item.gameObject.SetActive(false);
            }
            activeCount = 0;
        }

        private void BindToNext(HistoryEntry e)
        {
            if (activeCount >= pool.Count || e == null) return;

            var item = pool[activeCount++];
            item.gameObject.SetActive(true);

            // For choices, null out portrait if icons are hidden
            var portrait = (e.kind == HistoryKind.Choice && item.hideIconForChoices) ? null : e.portrait;

            item.Bind(
                e.kind == HistoryKind.Choice ? "Your Choice" : e.speaker,
                e.text,
                portrait,
                e.kind == HistoryKind.Choice
            );
        }
        #endregion
    }
}
