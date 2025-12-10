using UnityEngine;
using UnityEngine.EventSystems;

public class StartSceneChange : MonoBehaviour, IPointerClickHandler
{

    public SceneSwitcher sceneSwitcher;

    [SerializeField] float currentTime;
    [SerializeField] float waitingTime;

    public float clickedTimes;
    public bool hasClicked = false;

    private void Update()
    {
        if (clickedTimes == 2f)
        {
            hasClicked = true;
        }

        if (hasClicked)
        {
            currentTime += Time.deltaTime;
        }

        if (currentTime >= waitingTime)
        {
            sceneSwitcher.teleportSceneTwo = true;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        clickedTimes += 1;
    }
}
