using UnityEngine;

public class MoveOnButton : MonoBehaviour
{
    public float moveDuration = 1f;

    private bool isMoving = false;
    private Vector3 startPos;
    private Vector3 targetPos;
    private float timer;

    public void MoveTo(Transform target)
    {
        startPos = transform.position;
        targetPos = target.position;
        timer = 0f;
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving) return;

        timer += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, timer / moveDuration);

        transform.position = Vector3.Lerp(startPos, targetPos, t);

        if (t >= 1f)
            isMoving = false;
    }
}
