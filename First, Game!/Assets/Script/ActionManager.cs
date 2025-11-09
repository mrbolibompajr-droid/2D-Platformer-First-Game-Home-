using UnityEngine;
using System.Collections.Generic;

public class ActionManager : MonoBehaviour
{
    [System.Serializable]
    public class ActionData
    {
        public int patrolIndex;
        public bool requireKeyPress = false;
        public KeyCode key = KeyCode.E;
        public bool requireTrigger = false;
        public Collider2D triggerCollider;
        public bool requireItem = false;
        public string itemID;
    }

    public EnemyPath enemy;
    public List<ActionData> actions;

    private bool waiting = false;

    private void Update()
    {
        if (!waiting || enemy == null) return;

        ActionData currentAction = actions.Find(a => a.patrolIndex == enemy.CurrentIndex);
        if (currentAction == null) return;

        if (currentAction.requireKeyPress && Input.GetKeyDown(currentAction.key))
        {
            CompleteAction();
            return;
        }

        if (currentAction.requireTrigger && currentAction.triggerCollider != null)
        {
            if (currentAction.triggerCollider.bounds.Contains(enemy.transform.position))
            {
                CompleteAction();
                return;
            }
        }

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
