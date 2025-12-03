using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MoveButton : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public MoveOnButton mover;          // Object to move
    public List<GameObject> targets;    // Waypoints (GameObjects in world space)

    public void OnPointerClick(PointerEventData eventData)
    {
        if (mover != null && targets.Count > 0)
            mover.MoveThroughSequence(targets);
    }
}
