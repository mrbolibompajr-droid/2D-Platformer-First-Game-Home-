using UnityEngine;
using DialogSystem.Runtime.Core;

public class DialogueZoneTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string dialogID;                     // ID registered in DialogManager
    public KeyCode keyToActivate = KeyCode.E;  // Key to trigger dialogue
    public bool onlyOnce = false;              // Trigger only once

    private bool playerInside = false;
    private bool triggered = false;

    private void Update()
    {
        if (playerInside && !triggered && Input.GetKeyDown(keyToActivate))
        {
            TriggerDialogue();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // make sure your player has tag "Player"
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }

    private void TriggerDialogue()
    {
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.PlayDialogByID(dialogID);
            if (onlyOnce) triggered = true;
        }
    }
}
