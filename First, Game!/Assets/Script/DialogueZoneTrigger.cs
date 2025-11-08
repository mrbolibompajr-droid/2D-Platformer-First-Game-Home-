using UnityEngine;
using DialogSystem.Runtime.Core;

public class DialogueKeyTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string dialogID;                     // ID registered in DialogManager
    public KeyCode keyToActivate = KeyCode.E;  // Key to trigger dialogue
    public bool onlyOnce = false;              // Trigger only once

    private bool triggered = false;

    private void Awake()
    {
        Debug.Log("[DialogueKeyTrigger] Awake called");
    }

    private void OnEnable()
    {
        Debug.Log("[DialogueKeyTrigger] OnEnable called");
    }

    private void Update()
    {
        if (!triggered && Input.GetKeyDown(keyToActivate))
        {
            Debug.Log("[DialogueKeyTrigger] Key pressed: " + keyToActivate);
            TriggerDialogue();
        }
    }

    private void TriggerDialogue()
    {
        Debug.Log("[DialogueKeyTrigger] TriggerDialogue called for dialogID: " + dialogID);

        if (DialogManager.Instance != null)
        {
            Debug.Log("[DialogueKeyTrigger] Found DialogManager instance");
            DialogManager.Instance.PlayDialogByID(dialogID);

            if (onlyOnce) triggered = true;
            Debug.Log("[DialogueKeyTrigger] Dialogue triggered. OnlyOnce = " + onlyOnce);
        }
        else
        {
            Debug.LogError("[DialogueKeyTrigger] No DialogManager instance found in scene!");
        }
    }
}
