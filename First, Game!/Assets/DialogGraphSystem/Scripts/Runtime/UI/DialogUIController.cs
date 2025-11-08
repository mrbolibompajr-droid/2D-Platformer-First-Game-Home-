using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DialogSystem.Runtime.Core;

namespace DialogSystem.Runtime.UI
{
    /// <summary>
    /// Lightweight UI bridge used by DialogManager to show/hide and bind dialog content.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogUIController : MonoBehaviour
    {
        #region ---------------- Inspector - Main UI Elements ----------------
        [Header("Main UI Elements")]
        public GameObject panelRoot;
        public TextMeshProUGUI speakerName;
        public TextMeshProUGUI dialogText;
        public Image portraitImage;
        #endregion

        #region ---------------- Inspector - Choices UI ----------------
        [Header("Choices UI")]
        public Transform choicesContainer;
        public GameObject choiceButtonPrefab;
        #endregion

        #region ---------------- Inspector - Skip Button ----------------
        [Header("Skip Button")]
        public GameObject skipButton;
        #endregion

        #region ---------------- Inspector - Panel Button ----------------
        [Header("Dialog Panel Btn")]
        public Button dialogPanelButton;
        #endregion

        #region ---------------- Inspector - AutoPlay ----------------
        [Header("AutoPlay Button Config")]
        public Button autoPlayButton;
        public GameObject pauseIcon;
        public GameObject playIcon;
        #endregion

        #region ---------------- API - AutoPlay ----------------
        /// <summary>
        /// Toggles the manager's autoplay state and updates the play/pause icon.
        /// Wire this to the autoplay button's onClick.
        /// </summary>
        public void ToggleAutoPlayIcon()
        {
            if (autoPlayButton == null || pauseIcon == null || playIcon == null) return;

            var mgr = DialogManager.Instance;
            if (mgr == null) return;

            bool isAutoPlay = mgr.ToggleAutoPlay();
            UpdateAutoPlayIcon(isAutoPlay);
        }

        /// <summary>
        /// External sync for autoplay icon (e.g., on awake or when changed elsewhere).
        /// </summary>
        public void UpdateAutoPlayIcon(bool isAutoPlay)
        {
            if (autoPlayButton == null || pauseIcon == null || playIcon == null) return;
            SafeSetActive(pauseIcon, isAutoPlay);
            SafeSetActive(playIcon, !isAutoPlay);
        }
        #endregion

        #region ---------------- API - Listeners ----------------
        /// <summary>
        /// Assigns the main panel click to reveal/advance dialog.
        /// </summary>
        public void SetDialogPanelBtnListener(UnityAction action)
        {
            if (dialogPanelButton == null || action == null) return;
            dialogPanelButton.onClick.RemoveAllListeners();
            dialogPanelButton.onClick.AddListener(action);
        }
        #endregion

        #region ---------------- API - Convenience ----------------
        public void SetPanelVisible(bool visible) => SafeSetActive(panelRoot, visible);
        public void SetSkipVisible(bool visible) => SafeSetActive(skipButton, visible);

        public void SetChoicesVisible(bool visible)
        {
            if (choicesContainer != null)
                choicesContainer.gameObject.SetActive(visible);
        }

        public void SetSpeaker(string name)
        {
            if (speakerName != null) speakerName.text = name ?? string.Empty;
        }

        public void SetText(string text)
        {
            if (dialogText != null) dialogText.text = text ?? string.Empty;
        }

        public void SetPortrait(Sprite sprite)
        {
            if (portraitImage != null) portraitImage.sprite = sprite;
        }
        #endregion

        #region ---------------- Helpers ----------------
        private static void SafeSetActive(GameObject go, bool state)
        {
            if (go != null) go.SetActive(state);
        }
        #endregion
    }
}
