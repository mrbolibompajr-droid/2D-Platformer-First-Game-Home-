using UnityEngine;
using System.Collections;

public class ButtonSequenceManager : MonoBehaviour
{
    public ButtonAnimator[] buttons;   // Assign in Inspector in correct order
    public float delayBetweenButtons = 0.2f;

    public void PlayAwaySequence(int clickedIndex)
    {
        StartCoroutine(PlaySequence(clickedIndex));
    }

    private IEnumerator PlaySequence(int clickedIndex)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i == clickedIndex)
                continue;

            buttons[i].PlayAway();
            yield return new WaitForSeconds(delayBetweenButtons);
        }
    }
}
