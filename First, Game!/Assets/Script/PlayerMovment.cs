using System.Collections; // Needed for IEnumerator
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
    public PlayerCombat combat;

    [Header("Audio Pooler")]
    public AudioPooler audioPooler; // Assign in Inspector

    // ---- SOUND SYSTEM ----
    [Header("Walking Sound Settings")]
    public string leftWalkingSoundName = "WalkingLeft";
    public string rightWalkingSoundName = "WalkingRight";
    public float walkingFadeSpeed = 2f;
    private AudioSource walkingAudioSource;
    private GameObject walkingAudioObject;
    private string currentWalkingSound = "";
    private float walkingEffectTimer = 0f;
    public float walkingEffectInterval = 0.1f;

    [Header("Dash Sound Settings")]
    public string dashSoundName = "Dash";
    public float dashFadeSpeed = 4f;
    private AudioSource dashAudioSource;
    private GameObject dashAudioObject;

    [Header("Wall Slide Sound Settings")]
    public string leftWallSlideSoundName = "WallSlideLeft";
    public string rightWallSlideSoundName = "WallSlideRight";
    public float wallSlideFadeSpeed = 2f;
    private AudioSource wallSlideAudioSource;
    private GameObject wallSlideAudioObject;
    private string currentWallSlideSound = "";

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

        if (audioPooler == null)
            Debug.LogError("AudioPooler is not assigned in the Inspector!");
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

        combat?.HandleNormalAttackInput();
        combat?.HandleHeavyAttackInput();
        combat?.HandleDashAttackInput();

        HandleWalkingSound();
        HandleDashSound();
        HandleWallSlideSound();
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
                audioPooler.SpawnFromPool("Jump", transform.position);
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
                audioPooler.SpawnFromPool("DoubleJump", transform.position);
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

        if (touchingLeft)
        {
            wallJumpDirection = 1f;
            leftWallJumpEffect?.Spawn(leftWallJumpSpawn.position);
            audioPooler.SpawnFromPool("WallJumpLeft", transform.position);
        }
        else if (touchingRight)
        {
            wallJumpDirection = -1f;
            rightWallJumpEffect?.Spawn(rightWallJumpSpawn.position);
            audioPooler.SpawnFromPool("WallJumpRight", transform.position);
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
        float horizontalVelocity = Mathf.Abs(rb.linearVelocity.x);

        if (!isGrounded || horizontalVelocity < 0.1f)
        {
            walkingEffectTimer = 0f;
            return;
        }

        walkingEffectTimer -= Time.deltaTime;
        if (walkingEffectTimer > 0f) return;

        if (rb.linearVelocity.x < 0)
            leftWalkingEffect?.Spawn(leftWalkingSpawn.position);
        else
            rightWalkingEffect?.Spawn(rightWalkingSpawn.position);

        walkingEffectTimer = walkingEffectInterval;
    }

    private void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing &&
            Time.time >= lastDashTime + dashCooldown &&
            !isWallSliding && !isWallJumping)
        {
            dashDirection = new Vector2(inputX, 0).normalized;
            if (dashDirection == Vector2.zero)
                dashDirection = facingRight ? Vector2.right : Vector2.left;

            isDashing = true;
            dashTimer = dashDuration;
            lastDashTime = Time.time;
        }
    }

    // WALKING SOUND (LOOPING)
    private void HandleWalkingSound()
    {
        float horizontalVelocity = Mathf.Abs(rb.linearVelocity.x);

        if (!isGrounded || horizontalVelocity < 0.1f)
        {
            StartCoroutine(FadeOutAudio(walkingAudioSource, walkingAudioObject));
            currentWalkingSound = "";
            return;
        }

        string targetSound = rb.linearVelocity.x < 0 ? leftWalkingSoundName : rightWalkingSoundName;

        if (currentWalkingSound != targetSound)
        {
            StartCoroutine(FadeOutAudio(walkingAudioSource, walkingAudioObject));
            PlayWalkingSound(targetSound);
        }

        if (walkingAudioSource != null && !walkingAudioSource.isPlaying)
            walkingAudioSource.Play();

        StartCoroutine(FadeInAudio(walkingAudioSource, walkingFadeSpeed));
    }

    private void PlayWalkingSound(string name)
    {
        walkingAudioObject = audioPooler.SpawnFromPool(name, transform.position);
        if (walkingAudioObject != null)
        {
            walkingAudioSource = walkingAudioObject.GetComponent<AudioSource>();
            walkingAudioSource.volume = 0f;
            walkingAudioSource.loop = true;
            walkingAudioSource.Play();
            currentWalkingSound = name;
        }
    }

    // DASH SOUND
    private void HandleDashSound()
    {
        if (isDashing)
        {
            if (dashAudioSource == null)
            {
                dashAudioObject = audioPooler.SpawnFromPool(dashSoundName, transform.position);
                if (dashAudioObject != null)
                {
                    dashAudioSource = dashAudioObject.GetComponent<AudioSource>();
                    dashAudioSource.volume = 0f;
                    dashAudioSource.loop = true;
                    dashAudioSource.Play();
                }
            }

            StartCoroutine(FadeInAudio(dashAudioSource, dashFadeSpeed));
        }
        else
        {
            StartCoroutine(FadeOutAudio(dashAudioSource, dashAudioObject));
        }
    }

    // WALL SLIDE SOUND
    private void HandleWallSlideSound()
    {
        bool slidingLeft = Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, groundLayer)
                            && !isGrounded && rb.linearVelocity.y < 0f;

        bool slidingRight = Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, groundLayer)
                             && !isGrounded && rb.linearVelocity.y < 0f;

        if (!slidingLeft && !slidingRight)
        {
            StartCoroutine(FadeOutAudio(wallSlideAudioSource, wallSlideAudioObject));
            currentWallSlideSound = "";
            return;
        }

        string targetSound = slidingLeft ? leftWallSlideSoundName : rightWallSlideSoundName;

        if (currentWallSlideSound != targetSound)
        {
            StartCoroutine(FadeOutAudio(wallSlideAudioSource, wallSlideAudioObject));

            wallSlideAudioObject = audioPooler.SpawnFromPool(targetSound, transform.position);
            if (wallSlideAudioObject != null)
            {
                wallSlideAudioSource = wallSlideAudioObject.GetComponent<AudioSource>();
                wallSlideAudioSource.volume = 0f;
                wallSlideAudioSource.loop = true;
                wallSlideAudioSource.Play();
                currentWallSlideSound = targetSound;
            }
        }

        StartCoroutine(FadeInAudio(wallSlideAudioSource, wallSlideFadeSpeed));
    }

    // GENERIC FADE COROUTINES
    private IEnumerator FadeInAudio(AudioSource source, float speed)
    {
        if (source == null) yield break;

        while (source.volume < 1f)
        {
            source.volume += Time.deltaTime * speed;
            yield return null;
        }
        source.volume = 1f;
    }

    private IEnumerator FadeOutAudio(AudioSource source, GameObject obj)
    {
        if (source == null) yield break;

        while (source.volume > 0f)
        {
            source.volume -= Time.deltaTime * 2f;
            yield return null;
        }

        source.volume = 0f;

        if (obj != null) obj.SetActive(false);

        if (source == walkingAudioSource) walkingAudioSource = null;
        if (source == dashAudioSource) dashAudioSource = null;
        if (source == wallSlideAudioSource) wallSlideAudioSource = null;

        if (obj == walkingAudioObject) walkingAudioObject = null;
        if (obj == dashAudioObject) dashAudioObject = null;
        if (obj == wallSlideAudioObject) wallSlideAudioObject = null;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    public bool IsDashing() => isDashing;

    private void OnDrawGizmosSelected()
    {
        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        if (leftWallCheck) Gizmos.DrawWireSphere(leftWallCheck.position, checkRadius);
        if (rightWallCheck) Gizmos.DrawWireSphere(rightWallCheck.position, checkRadius);
    }
}
