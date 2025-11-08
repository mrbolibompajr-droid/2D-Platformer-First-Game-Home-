using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogSystem.Runtime
{
    [DisallowMultipleComponent]
    public class DialogueHistoryItem : MonoBehaviour
    {
        #region ---------------- UI References ----------------
        [Header("UI References")]
        public Image icon;
        public TextMeshProUGUI speaker;
        public TextMeshProUGUI line;
        #endregion

        #region ---------------- Style ----------------
        [Header("Style")]
        public bool italicForChoices = true;
        [Range(0.5f, 1.5f)] public float choiceFontScale = 0.95f;
        public bool hideIconForChoices = true;
        #endregion

        #region ---------------- Fallbacks ----------------
        [Header("Fallbacks")]
        public Sprite fallbackIcon;
        public Sprite choiceIcon;
        #endregion

        #region ---------------- State ----------------
        private float _baseLineFontSize = -1f;
        #endregion

        #region ---------------- Unity Lifecycle ----------------
        private void Awake()
        {
            if (line != null && _baseLineFontSize < 0f)
                _baseLineFontSize = line.fontSize;
        }
        #endregion

        #region ---------------- Binding ----------------
        public void Bind(string speakerName, string text, Sprite portrait, bool isChoice)
        {
            // Speaker: show "(Your Choice)" style for choices
            if (speaker != null)
            {
                var name = string.IsNullOrEmpty(speakerName) ? "Your Choice" : speakerName;
                speaker.text = isChoice ? $"({name})" : name;
            }

            // Line text + style
            if (line != null)
            {
                line.text = text ?? string.Empty;
                if (_baseLineFontSize < 0f) _baseLineFontSize = line.fontSize;
                line.fontStyle = (isChoice && italicForChoices) ? FontStyles.Italic : FontStyles.Normal;
                line.fontSize = _baseLineFontSize * (isChoice ? choiceFontScale : 1f);
            }

            // Icon logic
            if (icon != null)
            {
                if (isChoice && hideIconForChoices)
                {
                    icon.enabled = false;
                }
                else
                {
                    icon.enabled = true;
                    icon.sprite = isChoice
                        ? (choiceIcon != null ? choiceIcon : null)
                        : (portrait != null ? portrait : fallbackIcon);
                    icon.enabled = icon.sprite != null;
                }
            }
        }
        #endregion
    }
}
