using UnityEngine;
using UnityEngine.EventSystems;

public class MoveButton : MonoBehaviour, IPointerClickHandler
{
    public Transform target;
    public MoveOnButton mover;


    public void OnPointerClick(PointerEventData eventData)
    {
        mover.MoveTo(target);
    }
}
