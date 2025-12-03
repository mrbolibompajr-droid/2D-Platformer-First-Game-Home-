using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveOnButton : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveDuration = 1f;   // Time to move between points
    public float waitTime = 1f;       // Time to wait at each waypoint

    private bool isMoving = false;

    /// <summary>
    /// Move the object through a sequence of world-space GameObjects
    /// </summary>
    public void MoveThroughSequence(List<GameObject> targets)
    {
        if (targets == null || targets.Count == 0) return;

        if (!isMoving)
            StartCoroutine(MoveSequenceCoroutine(targets));
    }

    private IEnumerator MoveSequenceCoroutine(List<GameObject> targets)
    {
        isMoving = true;

        foreach (GameObject target in targets)
        {
            if (target == null) continue;

            Vector3 startPos = transform.position;
            Vector3 endPos = target.transform.position;
            float timer = 0f;

            while (timer < moveDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / moveDuration);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            // Ensure exact position at the end
            transform.position = endPos;

            // Wait at the target
            yield return new WaitForSeconds(waitTime);
        }

        isMoving = false;
    }
}
