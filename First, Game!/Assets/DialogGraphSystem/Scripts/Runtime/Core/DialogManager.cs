using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DialogSystem.Runtime.Utils;
using DialogSystem.Runtime.UI;
using DialogSystem.Runtime.Models;
using DialogSystem.Runtime.Models.Nodes;

namespace DialogSystem.Runtime.Core
{
    /// <summary>
    /// Central runtime controller for DialogGraph playback.
    /// Supports generic "advance" input (any key/mouse/touch), typewriter flow,
    /// autoplay, choices, inline action nodes, history pausing, and per-line audio.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogManager : MonoSingleton<DialogManager>
    {
        #region -------- Inspector: Graphs & UI --------
        [Header("Debug")]
        [Tooltip("If enabled, prints internal flow messages to the Console.")]
        public bool doDebugLog = true;

        [Serializable]
        public class DialogGraphModel
        {
            [Tooltip("Graph asset to use for this conversation id.")]
            public DialogGraph dialogGraph;

            [Tooltip("String id used to play this conversation via PlayDialogByID().")]
            public string dialogID;
        }

        [Header("Dialog Graphs List")]
        [Tooltip("Available graphs that can be played by id.")]
        public List<DialogGraphModel> dialogGraphs = new List<DialogGraphModel>();

        [Header("UI Panel Reference")]
        [Tooltip("UI controller responsible for showing dialog text, speaker, portrait, and choices.")]
        public DialogUIController uiPanel;
        #endregion

        #region -------- Inspector: Flow Settings --------
        [Header("Typing & Flow Settings")]
        [Tooltip("Per-character delay for the typewriter effect (0 to render instantly).")]
        [Range(0.01f, 0.2f)] public float typingSpeed = 0.02f;

        [Tooltip("Default delay (seconds) before advancing when autoplay is enabled and the node has no displayTime.")]
        [Range(0.1f, 5f)] public float delayBeforeAutoNext = 1.0f;

        [Tooltip("Allow user input to reveal the current line instantly.")]
        public bool allowSkipLine = true;

        [Tooltip("Allow skipping and ending the entire conversation.")]
        public bool allowSkipAll = true;

        [Tooltip("If enabled, nodes advance automatically after display time.")]
        public bool autoPlay = false;
        #endregion

        #region -------- Inspector: Audio Settings --------
        [Header("Audio Config")]
        [Tooltip("Optional AudioSource for per-line voice/SFX.")]
        public AudioSource audioSource;

        [Tooltip("If the player skips a typing line, stop any playing line audio.")]
        public bool stopAudioOnSkipLine = true;

        [Tooltip("If the player skips the entire conversation, stop audio immediately.")]
        public bool stopAudioOnSkipAll = true;

        [Tooltip("Fade out audio when stopping instead of cutting instantly.")]
        public bool fadeOutAudioOnStop = true;

        [Tooltip("Fade-out time in seconds when stopping audio with fade.")]
        [Range(0f, 1f)] public float audioFadeOutTime = 0.08f;
        #endregion

        #region -------- Optional Actions --------
        [Header("Optional Actions Runner (leave null to disable)")]
        [Tooltip("Executes inline action nodes (global or conversation-scoped).")]
        public DialogActionRunner actionRunner;
        #endregion

        #region -------- Events --------
        public Action onDialogEnter;
        public Action onDialogExit;
        public event Action<string, string, string> OnLineShown;
        public event Action<string, string> OnChoicePicked;
        public event Action OnConversationReset;
        #endregion

        #region -------- State --------
        private DialogGraph currentGraph;
        private string currentDialogID = null;
        private string currentGuid;
        private DialogNode currentDialog;
        private ChoiceNode currentChoice;

        private ChoiceNode pendingChoiceFromDialog;
        private string pendingNextGuidAfterDialog;

        private Coroutine typingCoroutine;
        private Coroutine autoAdvanceCoroutine;
        private Coroutine audioFadeCoroutine;

        private bool isTyping = false;
        private bool conversationActive = false;
        private Action onDialogEndedCallback;
        private bool isPausedByHistory = false;

        public bool IsConversationActive => conversationActive;
        public bool IsTyping => isTyping;
        public bool IsPaused => isPausedByHistory;
        public DialogNode CurrentNode => currentDialog;
        #endregion

        #region -------- Unity Lifecycle --------
        protected override void Awake()
        {
            Debug.Log("[DialogManager] Awake called");

            if (uiPanel != null)
            {
                Debug.Log("[DialogManager] UI Panel found, initializing...");
                uiPanel.panelRoot?.SetActive(false);
                uiPanel.skipButton?.SetActive(false);
                uiPanel.choicesContainer?.gameObject.SetActive(false);
                uiPanel.UpdateAutoPlayIcon(autoPlay);
            }
            else
            {
                Debug.LogWarning("[DialogManager] UI Panel NOT assigned!");
            }
        }

        private void OnDisable()
        {
            Debug.Log("[DialogManager] OnDisable called");
            SafeStopTyping();
            CancelAutoAdvance();
            StopAudioImmediate();
        }

        private void Update()
        {
            if (!conversationActive || isPausedByHistory) return;

            if (CheckGenericAdvanceInput())
            {
                Debug.Log("[DialogManager] Generic advance input detected");
                OnDialogAreaClick();
            }
        }

        private static bool CheckGenericAdvanceInput()
        {
            if (Input.anyKeyDown) return true;

            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).phase == TouchPhase.Began) return true;

            return false;
        }
        #endregion

        #region -------- Public API --------
        public void PlayDialogByID(string targetDialogID, Action onDialogEnded = null)
        {
            currentDialog = null;
            Debug.Log($"[DialogManager] PlayDialogByID called: {targetDialogID}");

            var target = dialogGraphs.Find(d => d.dialogID == targetDialogID && d.dialogGraph != null);
            if (target != null)
            {
                StartDialog(target.dialogGraph, onDialogEnded);
                currentDialogID = targetDialogID;
            }
            else
            {
                Debug.LogWarning($"[DialogManager] No dialog found for id: {targetDialogID}");
            }
        }

        public void StartDialog(DialogGraph graph, Action onDialogEnded = null)
        {
            if (uiPanel == null)
            {
                Debug.LogError("[DialogManager] UI Panel not assigned. Cannot start dialog!");
                return;
            }

            Debug.Log($"[DialogManager] Starting dialog graph: {graph.name}");

            uiPanel.panelRoot?.SetActive(false);
            uiPanel.skipButton?.SetActive(false);
            uiPanel.choicesContainer?.gameObject.SetActive(false);

            currentGraph = graph;
            currentGuid = null;
            currentDialog = null;
            currentChoice = null;
            pendingChoiceFromDialog = null;
            pendingNextGuidAfterDialog = null;

            currentGuid = ResolveEntryGuid(graph);

            uiPanel.panelRoot?.SetActive(true);
            uiPanel.skipButton?.SetActive(allowSkipAll);

            conversationActive = true;
            onDialogEndedCallback = onDialogEnded;

            OnConversationReset?.Invoke();
            onDialogEnter?.Invoke();

            Debug.Log($"[DialogManager] Conversation started, entry GUID: {currentGuid}");

            GoTo(currentGuid);
        }

        public bool ToggleAutoPlay()
        {
            autoPlay = !autoPlay;
            Debug.Log($"[DialogManager] AutoPlay toggled: {autoPlay}");
            uiPanel?.UpdateAutoPlayIcon(autoPlay);
            return autoPlay;
        }

        public void SkipAll()
        {
            if (!conversationActive || !allowSkipAll) return;

            Debug.Log("[DialogManager] SkipAll triggered");

            if (stopAudioOnSkipAll) StopAudio(fadeOutAudioOnStop);
            StopImmediately();
        }

        public Coroutine InvokeGlobalAction(string actionId, string payloadJson = "", bool waitForCompletion = false, float preDelaySeconds = 0f)
        {
            if (actionRunner == null) { WarnOnceNoRunner(); return null; }
            Debug.Log($"[DialogManager] InvokeGlobalAction: {actionId}");
            return StartCoroutine(actionRunner.RunActionGlobal(actionId, payloadJson, waitForCompletion, preDelaySeconds));
        }

        public Coroutine InvokeConversationAction(string dialogId, string actionId, string payloadJson = "", bool waitForCompletion = false, float preDelaySeconds = 0f)
        {
            if (actionRunner == null) { WarnOnceNoRunner(); return null; }
            Debug.Log($"[DialogManager] InvokeConversationAction: {dialogId} / {actionId}");
            return StartCoroutine(actionRunner.RunActionForConversation(dialogId, actionId, payloadJson, waitForCompletion, preDelaySeconds));
        }
        #endregion

        #region -------- Core Flow --------
        private void GoTo(string guid)
        {
            Debug.Log($"[DialogManager] GoTo called: {guid}");
            SafeStopTyping();
            CancelAutoAdvance();
            StopAudioImmediate();

            pendingChoiceFromDialog = null;
            pendingNextGuidAfterDialog = null;

            if (string.IsNullOrEmpty(guid)) { EndDialog(); return; }

            var act = FindActionByGuid(guid);
            if (act != null)
            {
                Debug.Log($"[DialogManager] Found Action node: {act.actionId}");
                StartCoroutine(ResolveNextAfterActions(guid, resolved =>
                {
                    Debug.Log($"[DialogManager] Action resolved to: {resolved}");
                    if (string.IsNullOrEmpty(resolved)) { EndDialog(); return; }

                    var d = FindDialogByGuid(resolved);
                    if (d != null)
                    {
                        currentGuid = resolved;
                        currentDialog = d;
                        currentChoice = null;
                        ShowCurrentNode();
                        return;
                    }

                    var c = FindChoiceByGuid(resolved);
                    if (c != null)
                    {
                        currentGuid = resolved;
                        currentDialog = null;
                        currentChoice = c;
                        ShowCurrentNode();
                        return;
                    }

                    EndDialog();
                }));
                return;
            }

            currentGuid = guid;
            currentDialog = FindDialogByGuid(guid);
            currentChoice = (currentDialog == null) ? FindChoiceByGuid(guid) : null;

            if (currentDialog == null && currentChoice == null) { EndDialog(); return; }

            ShowCurrentNode();
        }

        private void ShowCurrentNode()
        {
            Debug.Log("[DialogManager] ShowCurrentNode called");

            if (currentDialog == null && currentChoice == null) { EndDialog(); return; }

            if (uiPanel != null)
            {
                if (currentDialog != null)
                {
                    Debug.Log($"[DialogManager] Showing Dialog node: {currentDialog.GetGuid()} Speaker: {currentDialog.speakerName}");
                    uiPanel.speakerName.text = currentDialog.speakerName;
                    uiPanel.portraitImage.sprite = currentDialog.speakerPortrait;
                }
                else
                {
                    Debug.Log("[DialogManager] Showing Choice node");
                    uiPanel.speakerName.text = "";
                    uiPanel.portraitImage.sprite = null;
                }
            }

            var shownText = currentDialog != null ? currentDialog.questionText : currentChoice.text;
            var speaker = currentDialog != null ? currentDialog.speakerName : string.Empty;
            OnLineShown?.Invoke(currentGuid, speaker, shownText);

            if (currentDialog != null) PlayLineAudio(currentDialog.dialogAudio);

            SafeStopTyping();
            StartTyping(shownText);
        }

        private void StartTyping(string line)
        {
            Debug.Log($"[DialogManager] StartTyping: {line}");
            if (typingSpeed <= 0.0001f)
            {
                uiPanel.dialogText.text = line;
                isTyping = false;
                HandleAfterTyping();
                return;
            }

            typingCoroutine = StartCoroutine(TypeText(line));
        }

        private IEnumerator TypeText(string line)
        {
            isTyping = true;
            uiPanel.dialogText.text = "";

            foreach (char c in line)
            {
                uiPanel.dialogText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }

            isTyping = false;
            Debug.Log("[DialogManager] Typing complete");
            HandleAfterTyping();
        }

        private void HandleAfterTyping()
        {
            Debug.Log("[DialogManager] HandleAfterTyping called");

            if (isPausedByHistory) return;

            if (currentChoice != null || pendingChoiceFromDialog != null)
            {
                ShowChoices(currentChoice ?? pendingChoiceFromDialog);
                return;
            }

            if (currentDialog != null)
            {
                var nextDirect = GetNextFromDialog(currentDialog.GetGuid());

                StartCoroutine(ResolveNextAfterActions(nextDirect, resolvedNext =>
                {
                    Debug.Log($"[DialogManager] ResolveNextAfterActions returned: {resolvedNext}");
                    if (string.IsNullOrEmpty(resolvedNext))
                    {
                        if (autoPlay)
                        {
                            CancelAutoAdvance();
                            autoAdvanceCoroutine = StartCoroutine(AutoEndAfterDelay(currentDialog));
                        }
                        return;
                    }

                    var choice = FindChoiceByGuid(resolvedNext);
                    if (choice != null)
                    {
                        pendingChoiceFromDialog = choice;
                        ShowChoices(choice);
                        return;
                    }

                    pendingNextGuidAfterDialog = resolvedNext;

                    if (autoPlay)
                    {
                        CancelAutoAdvance();
                        autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay(pendingNextGuidAfterDialog, currentDialog));
                    }
                }));
            }
        }
        #endregion

        #region -------- Choices --------
        private void ShowChoices(ChoiceNode cnode)
        {
            Debug.Log("[DialogManager] ShowChoices called");
            if (uiPanel?.choicesContainer == null || uiPanel.choiceButtonPrefab == null) return;

            uiPanel.choicesContainer.gameObject.SetActive(true);

            foreach (Transform child in uiPanel.choicesContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < cnode.choices.Count; i++)
            {
                int index = i;
                var btnGO = Instantiate(uiPanel.choiceButtonPrefab, uiPanel.choicesContainer);

                var tmp = btnGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp) tmp.text = cnode.choices[i].answerText;

                var btn = btnGO.GetComponent<UnityEngine.UI.Button>();
                if (btn) btn.onClick.AddListener(() => OnChoiceSelected(index));

                Debug.Log($"[DialogManager] Choice button created: {cnode.choices[i].answerText}");
            }
        }

        public void OnChoiceSelected(int index)
        {
            Debug.Log($"[DialogManager] OnChoiceSelected: {index}");
            var choiceNode = pendingChoiceFromDialog ?? currentChoice;
            if (choiceNode == null) return;
            if (index < 0 || index >= choiceNode.choices.Count) return;

            var picked = choiceNode.choices[index];

            OnChoicePicked?.Invoke(choiceNode.GetGuid(), picked.answerText);
            picked.onSelected?.Invoke();

            var nextGUID = GetNextFromChoice(choiceNode.GetGuid(), index);

            CancelAutoAdvance();
            StopAudioImmediate();

            uiPanel?.choicesContainer?.gameObject.SetActive(false);
            pendingChoiceFromDialog = null;

            if (!string.IsNullOrEmpty(nextGUID)) GoTo(nextGUID);
            else EndDialog();
        }
        #endregion

        #region -------- Click Handling (Generic) --------
        public void OnDialogAreaClick()
        {
            Debug.Log("[DialogManager] OnDialogAreaClick called");

            if (!conversationActive || isPausedByHistory) return;

            if (isTyping)
            {
                if (!allowSkipLine) return;

                SafeStopTyping();
                var full = currentDialog != null ? currentDialog.questionText : currentChoice?.text ?? string.Empty;
                uiPanel.dialogText.text = full;
                isTyping = false;

                if (stopAudioOnSkipLine && currentDialog != null) StopAudio(fadeOutAudioOnStop);

                CancelAutoAdvance();
                HandleAfterTyping();
                return;
            }

            if (pendingChoiceFromDialog != null || currentChoice != null) return;

            if (currentDialog != null)
            {
                if (!string.IsNullOrEmpty(pendingNextGuidAfterDialog))
                {
                    var next = pendingNextGuidAfterDialog;
                    pendingNextGuidAfterDialog = null;
                    GoTo(next);
                    return;
                }

                var nextGuid = GetNextFromDialog(currentDialog.GetGuid());
                if (!string.IsNullOrEmpty(nextGuid)) { GoTo(nextGuid); return; }
            }

            EndDialog();
        }
        #endregion

        #region -------- Auto-Advance --------
        private IEnumerator AutoAdvanceAfterDelay(string nextGuid, DialogNode nodeForTiming)
        {
            float wait = (nodeForTiming == null || nodeForTiming.displayTime < 1f) ? delayBeforeAutoNext : nodeForTiming.displayTime;
            Debug.Log($"[DialogManager] AutoAdvanceAfterDelay: {wait}s to {nextGuid}");
            yield return new WaitForSeconds(wait);
            autoAdvanceCoroutine = null;

            if (!isPausedByHistory && !string.IsNullOrEmpty(nextGuid)) GoTo(nextGuid);
        }

        private IEnumerator AutoEndAfterDelay(DialogNode nodeForTiming)
        {
            float wait = (nodeForTiming == null || nodeForTiming.displayTime < 1f) ? delayBeforeAutoNext : nodeForTiming.displayTime;
            Debug.Log($"[DialogManager] AutoEndAfterDelay: {wait}s");
            yield return new WaitForSeconds(wait);
            autoAdvanceCoroutine = null;

            if (!isPausedByHistory) EndDialog();
        }
        #endregion

        #region -------- Stop / End --------
        public void StopImmediately()
        {
            Debug.Log("[DialogManager] StopImmediately called");
            SafeStopTyping();
            CancelAutoAdvance();
            EndDialog();
        }

        private void EndDialog()
        {
            Debug.Log("[DialogManager] EndDialog called");
            conversationActive = false;

            CancelAutoAdvance();
            SafeStopTyping();
            StopAudio(fadeOutAudioOnStop);

            if (uiPanel != null)
            {
                uiPanel.panelRoot?.SetActive(false);
                if (uiPanel.dialogText != null) uiPanel.dialogText.text = "";
                if (uiPanel.speakerName != null) uiPanel.speakerName.text = "";
                if (uiPanel.portraitImage != null) uiPanel.portraitImage.sprite = null;

                if (uiPanel.choicesContainer != null)
                {
                    foreach (Transform child in uiPanel.choicesContainer)
                        Destroy(child.gameObject);
                    uiPanel.choicesContainer.gameObject.SetActive(false);
                }
            }

            pendingChoiceFromDialog = null;
            pendingNextGuidAfterDialog = null;

            OnConversationReset?.Invoke();
            onDialogExit?.Invoke();
            onDialogEndedCallback?.Invoke();
            onDialogEndedCallback = null;
        }
        #endregion

        #region -------- Graph Helpers --------
        private DialogNode FindDialogByGuid(string guid)
        {
            if (currentGraph?.nodes == null) return null;
            var node = currentGraph.nodes.FirstOrDefault(n => n != null && n.GetGuid() == guid);
            if (doDebugLog) Debug.Log($"[DialogManager] FindDialogByGuid({guid}) -> {node?.name}");
            return node;
        }

        private ChoiceNode FindChoiceByGuid(string guid)
        {
            if (currentGraph?.choiceNodes == null) return null;
            var node = currentGraph.choiceNodes.FirstOrDefault(n => n != null && n.GetGuid() == guid);
            if (doDebugLog) Debug.Log($"[DialogManager] FindChoiceByGuid({guid}) -> {node?.name}");
            return node;
        }

        private ActionNode FindActionByGuid(string guid)
        {
            if (currentGraph?.actionNodes == null) return null;
            var node = currentGraph.actionNodes.FirstOrDefault(n => n != null && n.GetGuid() == guid);
            if (doDebugLog) Debug.Log($"[DialogManager] FindActionByGuid({guid}) -> {node?.name}");
            return node;
        }

        private string ResolveEntryGuid(DialogGraph graph)
        {
            if (graph == null) return null;

            if (!string.IsNullOrEmpty(graph.startGuid))
            {
                var next = GetFirstOutgoingTarget(graph, graph.startGuid);
                if (!string.IsNullOrEmpty(next)) return next;

                if (doDebugLog)
                    Debug.LogWarning("[DialogManager] Start node is set but has no outgoing link. Connect Start → first node.");
                return null;
            }

            if (doDebugLog)
                Debug.LogWarning("[DialogManager] startGuid is empty. Set it in the graph (Start node).");

            return null;
        }

        private static string GetFirstOutgoingTarget(DialogGraph graph, string fromGuid)
        {
            if (graph?.links == null || string.IsNullOrEmpty(fromGuid)) return null;

            var link = graph.links
                .Where(l => l != null && l.fromGuid == fromGuid)
                .OrderBy(l => l.fromPortIndex)
                .FirstOrDefault();

            return link?.toGuid;
        }

        private string GetNextFromDialog(string guid)
        {
            if (string.IsNullOrEmpty(guid) || currentGraph?.links == null) return null;
            var link = currentGraph.links.FirstOrDefault(l => l.fromGuid == guid && l.fromPortIndex == 0);
            return link?.toGuid;
        }

        private string GetNextFromChoice(string guid, int choiceIndex)
        {
            if (string.IsNullOrEmpty(guid) || currentGraph?.links == null) return null;
            var link = currentGraph.links.FirstOrDefault(l => l.fromGuid == guid && l.fromPortIndex == choiceIndex);
            return link?.toGuid;
        }

        private string GetNextFromAction(string guid)
        {
            if (string.IsNullOrEmpty(guid) || currentGraph?.links == null) return null;
            var link = currentGraph.links.FirstOrDefault(l => l.fromGuid == guid && l.fromPortIndex == 0);
            return link?.toGuid;
        }
        #endregion

        #region -------- Helpers --------
        private void SafeStopTyping()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            isTyping = false;
        }

        private void CancelAutoAdvance()
        {
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }
        }
        #endregion

        #region -------- Actions Traversal --------
        private IEnumerator ResolveNextAfterActions(string nextDirect, Action<string> onResolved)
        {
            string cursor = nextDirect;

            while (!string.IsNullOrEmpty(cursor))
            {
                var act = FindActionByGuid(cursor);
                if (act == null) break;

                if (doDebugLog)
                    Debug.Log($"[DialogManager] Action '{act.actionId}' wait={act.waitForCompletion} delay={act.waitSeconds}");

                if (actionRunner != null)
                    yield return StartCoroutine(actionRunner.RunAction(act, currentDialogID));

                cursor = GetNextFromAction(act.GetGuid());
            }

            onResolved?.Invoke(cursor);
        }
        #endregion

        #region -------- Audio Control --------
        private void PlayLineAudio(AudioClip clip)
        {
            if (audioSource == null) return;

            Debug.Log($"[DialogManager] Play audio: {(clip ? clip.name : "null")} for node {(currentDialog?.name ?? "n/a")}");

            StopAudioImmediate();
            if (clip == null) return;

            audioSource.clip = clip;
            audioSource.time = 0f;
            audioSource.Play();
        }

        private void StopAudio(bool withFade)
        {
            if (audioSource == null || !audioSource.isPlaying) return;

            if (!withFade || audioFadeOutTime <= 0f)
            {
                StopAudioImmediate();
                return;
            }

            if (audioFadeCoroutine != null) StopCoroutine(audioFadeCoroutine);
            audioFadeCoroutine = StartCoroutine(FadeOutAudio(audioFadeOutTime));
        }

        private void StopAudioImmediate()
        {
            if (audioFadeCoroutine != null)
            {
                StopCoroutine(audioFadeCoroutine);
                audioFadeCoroutine = null;
            }

            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }
        }

        private IEnumerator FadeOutAudio(float duration)
        {
            if (audioSource == null || !audioSource.isPlaying)
            {
                StopAudioImmediate();
                yield break;
            }

            float startVol = audioSource.volume;
            float t = 0f;

            while (t < duration && audioSource != null && audioSource.isPlaying)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / duration);
                audioSource.volume = startVol * k;
                yield return null;
            }

            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
                audioSource.volume = startVol;
            }

            audioFadeCoroutine = null;
        }
        #endregion

        #region -------- History Integration --------
        public void PauseForHistory()
        {
            isPausedByHistory = true;

            if (isTyping && typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;

                if (uiPanel?.dialogText != null)
                {
                    uiPanel.dialogText.text = currentDialog != null
                        ? currentDialog.questionText
                        : currentChoice != null ? currentChoice.text
                        : string.Empty;
                }

                isTyping = false;
            }

            CancelAutoAdvance();
        }

        public void ResumeAfterHistory() => isPausedByHistory = false;

        public string GetCurrentLineText()
        {
            if (currentDialog != null) return currentDialog.questionText ?? string.Empty;
            if (currentChoice != null) return currentChoice.text ?? string.Empty;
            return string.Empty;
        }

        public string GetCurrentGuid() => currentGuid;
        #endregion

        #region -------- Internal --------
        private bool _warnedNoRunner = false;
        private void WarnOnceNoRunner()
        {
            if (_warnedNoRunner) return;
            _warnedNoRunner = true;
            Debug.LogWarning("[DialogManager] No DialogActionRunner assigned. Action calls will be ignored.");
        }
        #endregion
    }
}
