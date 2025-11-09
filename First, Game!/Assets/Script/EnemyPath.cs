using UnityEngine;

[System.Serializable]
public class PatrolPoint
{
    public Transform point;
    public bool waitHere = false;
    public float waitDuration = 1f;
    public bool flipHere = false;
    public bool Action = false; // Pauses enemy at this point until released
    [HideInInspector] public bool done = false; // Tracks completed points for gizmos
}

public class EnemyPath : MonoBehaviour
{
    public enum MovementType { Horizontal, Vertical, Both }

    [Header("Patrol Settings")]
    public PatrolPoint[] patrolPoints;
    public float speed = 3f;
    public MovementType movementType = MovementType.Horizontal;

    [Header("Optional Behavior")]
    public bool loopPatrol = true;

    private int currentIndex = 0;
    public int CurrentIndex => currentIndex;

    private Rigidbody2D rb;
    private Animator anim;

    private bool isWaiting = false;
    private float waitTimer = 0f;

    private void Awake()
    {
        if (patrolPoints == null)
            patrolPoints = new PatrolPoint[0];

        if (patrolPoints.Length == 0)
        {
            GameObject container = GameObject.Find("__PatrolPoints__");
            if (container == null)
                container = new GameObject("__PatrolPoints__");

            PatrolPoint defaultPoint = new PatrolPoint();
            GameObject pointGO = new GameObject("Point 0");
            pointGO.transform.position = transform.position;
            pointGO.transform.parent = container.transform;
            defaultPoint.point = pointGO.transform;

            patrolPoints = new PatrolPoint[] { defaultPoint };
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogError("EnemyPath: Rigidbody2D missing on " + gameObject.name);

        anim = GetComponent<Animator>();
        if (anim != null && patrolPoints.Length > 0)
            anim.SetBool("IsRunning", true);
    }

    void Update()
    {
        if (patrolPoints.Length == 0 || rb == null)
            return;

        PatrolPoint currentPoint = patrolPoints[currentIndex];

        // Only wait if waitHere and timer is active, NOT for Action yet
        if (isWaiting)
        {
            rb.linearVelocity = Vector2.zero;
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
                isWaiting = false;
        }
    }

    void FixedUpdate()
    {
        if (patrolPoints.Length == 0 || rb == null)
            return;

        PatrolPoint currentPoint = patrolPoints[currentIndex];
        if (currentPoint.point == null) return;

        // Stop only if we are actually at an action point
        if (isWaiting || (currentPoint.Action && ReachedPoint(currentPoint.point.position)))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetPos = currentPoint.point.position;
        Vector2 direction = (targetPos - rb.position).normalized;

        switch (movementType)
        {
            case MovementType.Horizontal:
                direction.y = 0;
                direction = direction.normalized;
                break;
            case MovementType.Vertical:
                direction.x = 0;
                direction = direction.normalized;
                break;
        }

        rb.linearVelocity = direction * speed;

        if (movementType == MovementType.Horizontal)
        {
            if (rb.linearVelocity.x > 0 && transform.localScale.x < 0)
                Flip();
            else if (rb.linearVelocity.x < 0 && transform.localScale.x > 0)
                Flip();
        }
        else if (movementType == MovementType.Both)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (ReachedPoint(targetPos))
            HandlePointArrival();
    }

    private bool ReachedPoint(Vector3 pos)
    {
        return Vector3.SqrMagnitude(rb.position - (Vector2)pos) < 0.01f;
    }

    void HandlePointArrival()
    {
        PatrolPoint p = patrolPoints[currentIndex];

        if (p.flipHere && movementType != MovementType.Both)
            Flip();

        if (p.waitHere)
        {
            isWaiting = true;
            waitTimer = p.waitDuration;
        }

        if (p.Action)
        {
            rb.linearVelocity = Vector2.zero; // stop at action point
            return;
        }

        p.done = true;

        if (loopPatrol)
        {
            currentIndex++;
            if (currentIndex >= patrolPoints.Length)
                currentIndex = 0;
        }
        else
        {
            if (currentIndex < patrolPoints.Length - 1)
                currentIndex++;
            else
                rb.linearVelocity = Vector2.zero;
        }
    }

    private void Flip()
    {
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    public void ReleaseAction(int pointIndex)
    {
        if (pointIndex == currentIndex && pointIndex >= 0 && pointIndex < patrolPoints.Length)
            patrolPoints[pointIndex].Action = false;
    }

    private void OnDrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            PatrolPoint p = patrolPoints[i];
            if (p.point == null) continue;

            // Circle color
            if (p.Action)
                Gizmos.color = Color.yellow;
            else if (p.done)
                Gizmos.color = Color.blue;
            else
                Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(p.point.position, 0.3f);

            // Line to next point
            if (i + 1 < patrolPoints.Length && patrolPoints[i + 1].point != null)
            {
                if (p.done && i + 1 <= currentIndex)
                    Gizmos.color = Color.blue;
                else if (i == currentIndex)
                    Gizmos.color = Color.green;
                else
                    Gizmos.color = Color.red;

                Gizmos.DrawLine(p.point.position, patrolPoints[i + 1].point.position);
            }
        }
    }
}
