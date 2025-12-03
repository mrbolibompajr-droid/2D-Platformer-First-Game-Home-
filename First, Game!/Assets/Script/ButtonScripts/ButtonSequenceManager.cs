using UnityEngine;
using System.Collections;

public class ButtonSequenceManager : MonoBehaviour
{
    public ButtonAnimator[] buttons;
    public float delayBetweenButtons = 0.2f;

    public void PlayAwaySequence(int clickedIndex)
    {
        StartCoroutine(PlaySequence(clickedIndex));
    }

    private IEnumerator PlaySequence(int clickedIndex)
    {
        float longestAwayTime = 0f;

        // Play Away on all other buttons
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i == clickedIndex) continue;

            buttons[i].PlayAway();

            if (buttons[i].awayDuration > longestAwayTime)
                longestAwayTime = buttons[i].awayDuration;

            yield return new WaitForSeconds(delayBetweenButtons);
        }

        // Wait for all Away animations to finish
        yield return new WaitForSeconds(longestAwayTime);

        // Play Start on clicked button
        buttons[clickedIndex].PlayStart();
    }
}
