using UnityEngine;
using System.Collections.Generic;

public class ActionManager : MonoBehaviour
{
    [System.Serializable]
    public class ActionData
    {
        public int patrolIndex;               // Which patrol point this action is for
        public bool requireKeyPress = false; // Require player to press a key
        public KeyCode key = KeyCode.E;      // Default key
        public bool requireTrigger = false;  // Require enemy to enter a trigger collider
        public Collider2D triggerCollider;   // Collider for trigger zone
        public bool requireItem = false;     // Require player to have an item
        public string itemID;                // Identifier for the item
    }

    public EnemyPath enemy;                  // Reference to your EnemyPath component
    public List<ActionData> actions;         // List of action definitions

    private bool waiting = false;

    private void Update()
    {
        if (!waiting || enemy == null) return;

        ActionData currentAction = actions.Find(a => a.patrolIndex == enemy.CurrentIndex);
        if (currentAction == null) return;

        // Key press
        if (currentAction.requireKeyPress && Input.GetKeyDown(currentAction.key))
        {
            CompleteAction();
            return;
        }

        // Trigger collider
        if (currentAction.requireTrigger && currentAction.triggerCollider != null)
        {
            if (currentAction.triggerCollider.bounds.Contains(enemy.transform.position))
            {
                CompleteAction();
                return;
            }
        }

        // Item check placeholder
        if (currentAction.requireItem)
        {
            // Example: if (Inventory.HasItem(currentAction.itemID)) CompleteAction();
        }
    }

    public void StartAction()
    {
        if (!waiting)
        {
            waiting = true;

            // If no action defined for this patrol index, release immediately
            ActionData currentAction = actions.Find(a => a.patrolIndex == enemy.CurrentIndex);
            if (currentAction == null)
            {
                enemy.ReleaseAction(enemy.CurrentIndex);
                waiting = false;
            }
        }
    }

    private void CompleteAction()
    {
        waiting = false;
        enemy.ReleaseAction(enemy.CurrentIndex);
    }
}
