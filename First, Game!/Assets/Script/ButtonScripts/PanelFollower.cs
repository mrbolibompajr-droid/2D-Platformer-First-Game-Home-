using UnityEngine;
using System.Collections;

public class PanelFollower : MonoBehaviour
{
    [Header("Initial Move Settings")]
    public float moveDuration = 0.5f;   // Time to reach the button initially
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Follow Settings")]
    public float followSmooth = 10f;    // Smooth vertical follow after initial move

    private Transform target;
    private bool isFollowing = false;

    public void MoveToAndFollow(Transform newTarget)
    {
        StopAllCoroutines();  // Stop previous moves if any
        target = newTarget;
        StartCoroutine(MoveToTargetCoroutine());
    }

    private IEnumerator MoveToTargetCoroutine()
    {
        if (target == null) yield break;

        isFollowing = false;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(transform.position.x, target.position.y, transform.position.z);

        float timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / moveDuration;
            t = moveCurve.Evaluate(t);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Ensure exact final position
        transform.position = endPos;

        // Start following the button Y
        isFollowing = true;
    }

    private void Update()
    {
        if (isFollowing && target != null)
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, target.position.y, Time.deltaTime * followSmooth);
            transform.position = pos;
        }
    }
}
