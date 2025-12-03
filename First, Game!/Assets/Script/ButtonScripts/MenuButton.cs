using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour, IPointerClickHandler
{
    public int buttonIndex; // 0 = Start, 1 = Load, etc.
    public ButtonSequenceManager sequenceManager;

    public void OnPointerClick(PointerEventData eventData)
    {
        sequenceManager.PlayAwaySequence(buttonIndex);
    }
}
