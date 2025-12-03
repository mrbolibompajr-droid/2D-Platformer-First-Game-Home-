using UnityEngine;
using System.Collections;

public class CameraTriggerZone : MonoBehaviour
{
    [Header("Assign the cameras")]
    public GameObject playerCamera;        // The camera following the player (Cinemachine)
    public GameObject roomCamera;          // Room overview camera (Cinemachine)

    [Header("Assign the Player looking cameras")]
    public GameObject leftLookingCamera;   // Ledge peek left camera
    public GameObject rightLookingCamera;  // Ledge peek right camera

    [Header("Ledge Camera Wait Timer")]
    [SerializeField] private float edgeHoldTime = 3f; // Time required to stay on edge
    private float edgeTimer = 0f;

    [Header("Main Camera Option")]
    [SerializeField] private bool useMainCamera = false;   // Toggle to use normal camera
    [SerializeField] private Camera mainCamera;           // The normal non-Cinemachine camera
    [SerializeField] private float transitionDelay = 1f;  // Delay before switching to mainCamera

    private PlayerMovement playerMovement;
    private Coroutine mainCameraCoroutine;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        // Ensure only the player camera is active at start
        if (playerCamera != null)
            playerCamera.SetActive(true);

        if (roomCamera != null)
            roomCamera.SetActive(false);

        if (leftLookingCamera != null)
            leftLookingCamera.SetActive(false);

        if (rightLookingCamera != null)
            rightLookingCamera.SetActive(false);

        if (mainCamera != null)
            mainCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        HandleLedgeCameras();
    }

    private void HandleLedgeCameras()
    {
        // Increment edge timer when player is looking left OR right on the ground
        if ((playerMovement.isLookingLeft || playerMovement.isLookingRight) && playerMovement.groundCheck)
        {
            edgeTimer += Time.deltaTime;
        }
        else
        {
            edgeTimer = 0f;
        }

        // Left peek camera
        if (playerMovement.isLookingLeft && !playerMovement.isLookingRight && playerMovement.groundCheck && edgeTimer >= edgeHoldTime)
        {
            if (playerCamera != null) playerCamera.SetActive(false);
            if (leftLookingCamera != null) leftLookingCamera.SetActive(true);
        }
        else
        {
            if (leftLookingCamera != null) leftLookingCamera.SetActive(false);
            if (playerCamera != null) playerCamera.SetActive(true);
        }

        // Right peek camera
        if (playerMovement.isLookingRight && !playerMovement.isLookingLeft && playerMovement.groundCheck && edgeTimer >= edgeHoldTime)
        {
            if (playerCamera != null) playerCamera.SetActive(false);
            if (rightLookingCamera != null) rightLookingCamera.SetActive(true);
        }
        else
        {
            if (rightLookingCamera != null) rightLookingCamera.SetActive(false);
            if (playerCamera != null) playerCamera.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Switch off player camera, enable room camera (Cinemachine)
        if (playerCamera != null) playerCamera.SetActive(false);
        if (roomCamera != null) roomCamera.SetActive(true);

        // Start coroutine to switch to mainCamera if enabled
        if (useMainCamera && mainCamera != null)
        {
            mainCameraCoroutine = StartCoroutine(SwitchToMainCameraAfterDelay());
        }
    }

    private IEnumerator SwitchToMainCameraAfterDelay()
    {
        yield return new WaitForSeconds(transitionDelay);

        if (roomCamera != null) roomCamera.SetActive(false);
        if (mainCamera != null) mainCamera.gameObject.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Stop any pending mainCamera switch
        if (mainCameraCoroutine != null)
        {
            StopCoroutine(mainCameraCoroutine);
            mainCameraCoroutine = null;
        }

        // Disable mainCamera if active
        if (mainCamera != null && mainCamera.gameObject.activeSelf)
            mainCamera.gameObject.SetActive(false);

        // Enable room camera briefly for fading effect
        if (roomCamera != null)
            roomCamera.SetActive(true);

        // Re-enable player camera
        if (playerCamera != null)
            playerCamera.SetActive(true);

        // Disable room camera after fade
        if (roomCamera != null)
            roomCamera.SetActive(false);
    }
}
