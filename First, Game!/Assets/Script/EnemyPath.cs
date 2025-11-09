using UnityEngine;

[System.Serializable]
public class PatrolPoint
{
    public Transform point;
    public bool waitHere = false;
    public float waitDuration = 1f;
    public bool flipHere = false;
    public bool Action = false;
    [HideInInspector] public bool done = false;
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

    private int currentIndex = 0; // Patrol point the enemy is moving toward
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
        if (patrolPoints.Length == 0 || rb == null) return;

        if (isWaiting)
        {
            rb.linearVelocity = Vector2.zero;
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                isWaiting = false;
                patrolPoints[currentIndex].done = true;
                AdvanceToNextPoint();
            }
        }
    }

    void FixedUpdate()
    {
        if (patrolPoints.Length == 0 || rb == null) return;

        PatrolPoint currentPoint = patrolPoints[currentIndex];
        if (currentPoint.point == null) return;

        if (!isWaiting)
        {
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
                if (rb.linearVelocity.x > 0 && transform.localScale.x < 0) Flip();
                else if (rb.linearVelocity.x < 0 && transform.localScale.x > 0) Flip();
            }
            else if (movementType == MovementType.Both)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            if (Vector3.SqrMagnitude(rb.position - (Vector2)targetPos) < 0.01f)
                HandlePointArrival();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void HandlePointArrival()
    {
        PatrolPoint p = patrolPoints[currentIndex];
        p.done = true;

        if (p.flipHere && movementType != MovementType.Both) Flip();

        if (p.waitHere)
        {
            isWaiting = true;
            waitTimer = p.waitDuration;
            return;
        }

        if (p.Action)
        {
            rb.linearVelocity = Vector2.zero;
            ActionManager manager = GetComponent<ActionManager>();
            if (manager != null)
                manager.StartAction();
            return;
        }

        AdvanceToNextPoint();
    }

    private void AdvanceToNextPoint()
    {
        if (loopPatrol)
        {
            currentIndex++;
            if (currentIndex >= patrolPoints.Length) currentIndex = 0;
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
        {
            patrolPoints[pointIndex].Action = false;
            HandlePointArrival();
        }
    }

    private void OnDrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length < 2) return;

        Vector3 enemyPos = Application.isPlaying ? transform.position : patrolPoints[0].point.position;

        // Determine segment index for green line (current segment)
        int segmentIndex = Application.isPlaying ? Mathf.Clamp(currentIndex - 1, 0, patrolPoints.Length - 2) : 0;

        // Draw path lines
        for (int i = 0; i < patrolPoints.Length - 1; i++)
        {
            PatrolPoint startPoint = patrolPoints[i];
            PatrolPoint endPoint = patrolPoints[i + 1];
            if (startPoint.point == null || endPoint.point == null) continue;

            Vector3 start = startPoint.point.position;
            Vector3 end = endPoint.point.position;

            if (Application.isPlaying)
            {
                if (i < segmentIndex)
                {
                    // Fully passed segments (blue)
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(start, end);
                }
                else if (i == segmentIndex)
                {
                    // Last passed segment ends at enemy
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(start, enemyPos);

                    // Current segment (green)
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(enemyPos, end);
                }
                else
                {
                    // Future segments (red)
                    Gizmos.color = new Color(0.7f, 0.3f, 0.3f);
                    Gizmos.DrawLine(start, end);
                }
            }
            else
            {
                // Edit mode: full path in green
                Gizmos.color = Color.green;
                Gizmos.DrawLine(start, end);
            }
        }

        // Draw patrol points
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            PatrolPoint p = patrolPoints[i];
            if (p.point == null) continue;

            if (p.Action) Gizmos.color = Color.yellow;
            else if (p.waitHere && !p.done) Gizmos.color = new Color(1f, 0f, 1f);
            else if (Application.isPlaying && i < currentIndex) Gizmos.color = Color.blue;
            else Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(p.point.position, 0.3f);
        }
    }
}
