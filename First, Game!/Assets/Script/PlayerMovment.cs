using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public float doubleJumpForce = 10f;
    public int maxDoubleJumps = 1;
    private int doubleJumpsRemaining;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float checkRadius = 0.25f;

    [Header("Wall Checks (World Space)")]
    public Transform leftWallCheck;
    public Transform rightWallCheck;
    public float wallCheckOffset = 0.6f;

    [Header("Wall Slide Settings")]
    public float wallSlideSpeed = 2f;
    public float wallSlideEffectInterval = 0.1f;
    public float wallSlideStartDelay = 0.2f;

    [Header("Wall Slide Stamina")]
    public float maxWallSlideTime = 1.5f;

    [Header("Wall Jump Settings")]
    public float wallJumpXForce = 8f;
    public float wallJumpYForce = 12f;

    [Header("Wall Jump Forgiveness")]
    public float wallJumpBufferTime = 0.2f;
    private float lastWallTouchTime = -1f;

    [Header("Spawn Points")]
    public Transform normalJumpSpawn;
    public Transform doubleJumpSpawn;
    public Transform leftWallSlideSpawn;
    public Transform rightWallSlideSpawn;
    public Transform leftWallJumpSpawn;
    public Transform rightWallJumpSpawn;
    public Transform leftWalkingSpawn;
    public Transform rightWalkingSpawn;

    [Header("Effects")]
    public JumpEffectSpawner normalJumpEffect;
    public JumpEffectSpawner doubleJumpEffect;
    public JumpEffectSpawner leftWallSlideEffect;
    public JumpEffectSpawner rightWallSlideEffect;
    public JumpEffectSpawner leftWallJumpEffect;
    public JumpEffectSpawner rightWallJumpEffect;
    public JumpEffectSpawner leftWalkingEffect;
    public JumpEffectSpawner rightWalkingEffect;

    [Header("Dash Settings")]
    public float dashHorizontalForce = 15f;
    public float dashVerticalForce = 5f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.5f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float lastDashTime = -10f;
    private Vector2 dashDirection;

    [Header("Combat")]
    public PlayerCombat combat; // reference to PlayerCombat script

    private Rigidbody2D rb;
    private float inputX;
    private bool isGrounded;
    private bool isWallSliding;
    private bool facingRight = true;

    private float wallSlideEffectTimer = 0f;
    private float wallSlideStartTimer = 0f;
    private float wallSlideTimer = 0f;
    private bool wallSlideLocked = false;

    private bool isWallJumping = false;
    private float wallJumpTime = 0.2f;
    private float wallJumpTimer = 0f;
    private float wallJumpDirection = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        doubleJumpsRemaining = maxDoubleJumps;

        if (combat == null)
            combat = GetComponent<PlayerCombat>();
    }

    private void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (isGrounded)
        {
            wallSlideLocked = false;
            wallSlideTimer = 0f;
        }

        if (inputX > 0 && !facingRight) Flip();
        if (inputX < 0 && facingRight) Flip();

        HandleJumping();
        HandleWallSlide();
        HandleWalkingEffect();
        HandleDashInput();

        // --- Attack handling ---
        combat?.HandleNormalAttackInput();
        combat?.HandleHeavyAttackInput();
        combat?.HandleDashAttackInput();
    }

    private void FixedUpdate()
    {
        if (isWallJumping)
        {
            wallJumpTimer -= Time.fixedDeltaTime;
            if (wallJumpTimer <= 0f)
                isWallJumping = false;
        }

        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f) isDashing = false;
            else
            {
                rb.linearVelocity = dashDirection * dashHorizontalForce + Vector2.up * dashVerticalForce;
                return;
            }
        }

        if (!isWallJumping && !isDashing)
            rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);
    }

    private void LateUpdate()
    {
        leftWallCheck.position = transform.position + Vector3.left * wallCheckOffset;
        rightWallCheck.position = transform.position + Vector3.right * wallCheckOffset;
    }

    private void HandleJumping()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool wallJumpAvailable = (Time.time - lastWallTouchTime <= wallJumpBufferTime) && !wallSlideLocked;

            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                normalJumpEffect?.Spawn(normalJumpSpawn.position);
                doubleJumpsRemaining = maxDoubleJumps;
            }
            else if (wallJumpAvailable)
            {
                PerformWallJump();
            }
            else if (!isWallSliding && doubleJumpsRemaining > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
                doubleJumpEffect?.Spawn(doubleJumpSpawn.position);
                doubleJumpsRemaining--;
            }
        }
    }

    private void HandleWallSlide()
    {
        bool touchingLeftWall = Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, groundLayer);
        bool touchingRightWall = Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, groundLayer);

        bool slidingLeft = touchingLeftWall && !isGrounded && rb.linearVelocity.y < 0f;
        bool slidingRight = touchingRightWall && !isGrounded && rb.linearVelocity.y < 0f;

        if (slidingLeft || slidingRight) lastWallTouchTime = Time.time;

        if (wallSlideLocked)
        {
            isWallSliding = false;
            return;
        }

        if (!slidingLeft && !slidingRight)
        {
            isWallSliding = false;
            wallSlideEffectTimer = 0f;
            wallSlideStartTimer = 0f;
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));

        wallSlideTimer += Time.deltaTime;
        if (wallSlideTimer >= maxWallSlideTime)
        {
            wallSlideLocked = true;
            isWallSliding = false;
            return;
        }

        if (!isWallSliding)
        {
            isWallSliding = true;
            wallSlideStartTimer = wallSlideStartDelay;
            return;
        }

        if (wallSlideStartTimer > 0f)
        {
            wallSlideStartTimer -= Time.deltaTime;
            return;
        }

        wallSlideEffectTimer -= Time.deltaTime;
        if (wallSlideEffectTimer <= 0f)
        {
            if (slidingLeft) leftWallSlideEffect?.Spawn(leftWallSlideSpawn.position);
            else rightWallSlideEffect?.Spawn(rightWallSlideSpawn.position);
            wallSlideEffectTimer = wallSlideEffectInterval;
        }
    }

    private void PerformWallJump()
    {
        float yForce = wallJumpYForce;
        bool touchingLeft = Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, groundLayer);
        bool touchingRight = Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, groundLayer);

        if (touchingLeft || (Time.time - lastWallTouchTime <= wallJumpBufferTime && lastWallTouchTime > 0 && !touchingRight))
        {
            wallJumpDirection = 1f;
            leftWallJumpEffect?.Spawn(leftWallJumpSpawn.position);
        }
        else if (touchingRight || (Time.time - lastWallTouchTime <= wallJumpBufferTime && lastWallTouchTime > 0))
        {
            wallJumpDirection = -1f;
            rightWallJumpEffect?.Spawn(rightWallJumpSpawn.position);
        }

        rb.linearVelocity = new Vector2(wallJumpXForce * wallJumpDirection, yForce);
        isWallJumping = true;
        wallJumpTimer = wallJumpTime;
        isWallSliding = false;
        doubleJumpsRemaining = maxDoubleJumps;
        lastWallTouchTime = -1f;
    }

    private void HandleWalkingEffect()
    {
        if (!isGrounded || Mathf.Abs(inputX) < 0.1f) return;
        if (inputX < 0) leftWalkingEffect?.Spawn(leftWalkingSpawn.position);
        else if (inputX > 0) rightWalkingEffect?.Spawn(rightWalkingSpawn.position);
    }

    private void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && Time.time >= lastDashTime + dashCooldown && !isWallSliding && !isWallJumping)
        {
            dashDirection = new Vector2(inputX, 0).normalized;
            if (dashDirection == Vector2.zero) dashDirection = facingRight ? Vector2.right : Vector2.left;

            isDashing = true;
            dashTimer = dashDuration;
            lastDashTime = Time.time;
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    // --- PUBLIC WRAPPER METHODS ---
    public bool IsDashing() => isDashing;

    private void OnDrawGizmosSelected()
    {
        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        if (leftWallCheck) Gizmos.DrawWireSphere(leftWallCheck.position, checkRadius);
        if (rightWallCheck) Gizmos.DrawWireSphere(rightWallCheck.position, checkRadius);
    }
}
