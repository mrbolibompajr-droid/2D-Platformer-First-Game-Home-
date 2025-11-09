using UnityEngine;

[System.Serializable]
public class PatrolPoint
{
    public Transform point;
    public bool waitHere = false;
    public float waitDuration = 1f;
    public bool flipHere = false;
    public bool Action = false; // Stops enemy at this point until manually released
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

        // Handle waiting points
        if (isWaiting)
        {
            rb.linearVelocity = Vector2.zero;
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                isWaiting = false;

                // Mark the wait point as done
                patrolPoints[currentIndex].done = true;

                AdvanceToNextPoint();
            }
        }
    }

    void FixedUpdate()
    {
        if (patrolPoints.Length == 0 || rb == null)
            return;

        PatrolPoint currentPoint = patrolPoints[currentIndex];
        if (currentPoint.point == null) return;

        // Move toward point if not waiting or action-stopped
        if (!isWaiting)
        {
            Vector2 targetPos = currentPoint.point.position;
            Vector2 direction = (targetPos - rb.position).normalized;

            // Apply movement constraints
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

            // Flip / rotate
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

            // Check if reached point
            if (Vector3.SqrMagnitude(rb.position - (Vector2)targetPos) < 0.01f)
                HandlePointArrival();
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // stop if waiting
        }
    }

    void HandlePointArrival()
    {
        PatrolPoint p = patrolPoints[currentIndex];

        // Mark as done
        p.done = true;

        // Flip if needed
        if (p.flipHere && movementType != MovementType.Both)
            Flip();

        // Wait points
        if (p.waitHere)
        {
            isWaiting = true;
            waitTimer = p.waitDuration;
            return;
        }

        // Action points stop AFTER arriving at point
        if (p.Action)
        {
            rb.linearVelocity = Vector2.zero;

            // Notify ActionManager
            ActionManager manager = GetComponent<ActionManager>();
            if (manager != null)
            {
                manager.StartAction();
            }

            return; // wait here until released
        }

        // Otherwise, immediately advance
        AdvanceToNextPoint();
    }

    private void AdvanceToNextPoint()
    {
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

            // Point color
            if (p.Action)
                Gizmos.color = Color.yellow;
            else if (p.waitHere && !p.done)
                Gizmos.color = new Color(1f, 0f, 1f); // purple/pink
            else if (p.done)
                Gizmos.color = Color.blue;
            else
                Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(p.point.position, 0.3f);

            // Line to next point
            if (i + 1 < patrolPoints.Length && patrolPoints[i + 1].point != null)
            {
                Vector3 start = p.point.position;
                Vector3 end = patrolPoints[i + 1].point.position;

                if (i < currentIndex - 1) // already done path
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(start, end);
                }
                else if (i == currentIndex - 1) // current segment
                {
                    if (rb != null)
                    {
                        Vector3 enemyPos = rb.position;
                        Vector3 clampedPos = Vector3.MoveTowards(start, end, Vector3.Distance(start, enemyPos));
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(clampedPos, end);
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(start, clampedPos);
                    }
                    else
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(start, end);
                    }
                }
                else // future paths
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
}
