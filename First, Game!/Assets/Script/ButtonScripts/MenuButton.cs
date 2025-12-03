using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour, IPointerClickHandler
{
    public int buttonIndex;
    public ButtonSequenceManager sequenceManager;

    [Header("Optional Panel Movement")]
    public PanelFollower panelFollower;   // Drag your panel here
    public bool movesPanel = false;       // True only for buttons that move the panel

    public void OnPointerClick(PointerEventData eventData)
    {
        // Trigger button animations
        sequenceManager.PlayAwaySequence(buttonIndex);

        // Trigger panel follow
        if (movesPanel && panelFollower != null)
        {
            panelFollower.MoveToAndFollow(transform);
        }
    }
}
