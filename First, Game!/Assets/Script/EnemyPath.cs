using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class PatrolPoint
{
    public Transform point;
    public bool waitHere = false;
    public float waitDuration = 1f;
    public bool flipHere = false;
    public bool Action = false; // Pauses enemy at this point until released
}

public class EnemyPath : MonoBehaviour
{
    public enum MovementType { Horizontal, Vertical, Both }

    [Header("Patrol Settings")]
    public PatrolPoint[] patrolPoints;
    public float speed = 3f;
    public MovementType movementType = MovementType.Horizontal;

    [Header("Optional Behavior")]
    public bool loopPatrol = true; // If false, stops at last point

    private int currentIndex = 0;
    public int CurrentIndex => currentIndex;

    private int lastReachedIndex = -1; // Tracks last completed point

    private Rigidbody2D rb;
    private Animator anim;

    private bool isWaiting = false;
    private float waitTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogError("EnemyPath: Rigidbody2D missing on " + gameObject.name);

        anim = GetComponent<Animator>();
        if (anim != null && patrolPoints.Length > 0)
            anim.SetBool("IsRunning", true);

        if (patrolPoints.Length == 0)
            Debug.LogWarning("EnemyPath: No patrol points assigned on " + gameObject.name);
    }

    void Update()
    {
        if (patrolPoints.Length == 0 || rb == null)
            return;

        PatrolPoint currentPoint = patrolPoints[currentIndex];

        // Handle wait timer (not Action)
        if (isWaiting && !currentPoint.Action && currentPoint.waitHere)
        {
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

        // Enemy moves unless waiting
        rb.linearVelocity = (!isWaiting) ? direction * speed : Vector2.zero;

        // Flip / rotate sprite
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

        // Check arrival
        float sqrDist = (rb.position - targetPos).sqrMagnitude;
        if (sqrDist < 0.01f)
        {
            rb.linearVelocity = Vector2.zero; // stop exactly at point
            HandlePointArrival();
        }

        // Debug
        Debug.Log($"Enemy State => Position: {transform.position}, Rotation: {transform.eulerAngles}, Scale: {transform.localScale}");
    }

    void HandlePointArrival()
    {
        PatrolPoint p = patrolPoints[currentIndex];

        if (p.flipHere && movementType != MovementType.Both) Flip();

        // Mark completed for donePath visualization
        lastReachedIndex = currentIndex;

        if (p.waitHere || p.Action)
        {
            isWaiting = true;
            waitTimer = p.waitHere ? p.waitDuration : 0f; // timer only for waitHere
        }
        else
        {
            MoveToNextPoint();
        }
    }

    private void MoveToNextPoint()
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
                rb.linearVelocity = Vector2.zero; // stop at final point
        }
    }

    private void Flip()
    {
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    private string GetPointLetter(int index)
    {
        return ((char)('A' + index)).ToString();
    }

    public void ReleaseAction(int pointIndex)
    {
        if (pointIndex == currentIndex && pointIndex >= 0 && pointIndex < patrolPoints.Length)
        {
            patrolPoints[pointIndex].Action = false;
            isWaiting = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        Vector3 enemyPos = Application.isPlaying ? transform.position : Vector3.zero;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i].point == null) continue;
            Transform t = patrolPoints[i].point;

            // Sphere colors
            if (i <= lastReachedIndex)
                Gizmos.color = Color.blue;   // Done
            else if (patrolPoints[i].Action)
                Gizmos.color = Color.yellow; // Action
            else
                Gizmos.color = Color.red;    // Upcoming

            Gizmos.DrawWireSphere(t.position, 0.3f);

            // Draw line to next point
            if (i + 1 < patrolPoints.Length && patrolPoints[i + 1].point != null)
            {
                Vector3 nextPos = patrolPoints[i + 1].point.position;

                if (Application.isPlaying && i == lastReachedIndex)
                {
                    // Dynamic current segment
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(t.position, enemyPos); // done behind enemy

                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(enemyPos, nextPos);    // current segment ahead
                }
                else if (i < lastReachedIndex)
                {
                    Gizmos.color = Color.blue;  // fully done
                    Gizmos.DrawLine(t.position, nextPos);
                }
                else
                {
                    Gizmos.color = Color.white; // future
                    Gizmos.DrawLine(t.position, nextPos);
                }
            }

#if UNITY_EDITOR
            Handles.color = Color.white;
            Handles.Label(t.position + Vector3.up * 0.5f, GetPointLetter(i));

            if (!Application.isPlaying)
            {
                Vector3 newPos = Handles.PositionHandle(t.position, Quaternion.identity);
                if (newPos != t.position)
                {
                    Undo.RecordObject(t, "Move Patrol Point");
                    t.position = newPos;
                }
            }
#endif
        }
    }
}
