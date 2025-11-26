using UnityEngine;

public class CameraTriggerZone : MonoBehaviour
{
    [Header("Assign the cameras")]
    public GameObject playerCamera; // The camera following the player
    public GameObject roomCamera;   // The room/overview camera
    public GameObject leftLookingCamera;   // The room/overview camera
    public GameObject rightLookingCamera;   // The room/overview camera

    [Header("Ledge Camera Wait Timer")]
    [SerializeField] private float edgeHoldTime = 3f; // Time required to stay on edge
    [SerializeField] private float edgeTimer = 0f;
    
    //[Header("Idle Camera Wait Timer")]
    //[SerializeField] private float idleHoldTime = 3f; // Time required to stay on edge
    //[SerializeField] private float idleTimer = 0f;

    private PlayerMovement playerMovement;
    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        // Make sure only the player camera is active at start
        if (playerCamera != null)
            playerCamera.SetActive(true);

        if (roomCamera != null)
            roomCamera.SetActive(false);
    }
    private void Update()
    {
        #region LedgeLeap
        if (!playerMovement.isLookingLeft || !playerMovement.isLookingRight)
        {
            edgeTimer += Time.deltaTime;
        }
        else
        {
            edgeTimer = 0f;   // reset if player stops looking or leaves ground
        }

        if (!playerMovement.isLookingLeft && playerMovement.isLookingRight == true && playerMovement.groundCheck == true && edgeTimer >= edgeHoldTime)
        {
            if (playerCamera != null)
                playerCamera.SetActive(false);

            if (leftLookingCamera != null)
                leftLookingCamera.SetActive(true);
        }
        else
        {
            if (leftLookingCamera != null)
                leftLookingCamera.SetActive(false);

            if (playerCamera != null)
                playerCamera.SetActive(true);
        }



        if (!playerMovement.isLookingRight && playerMovement.isLookingLeft == true && playerMovement.groundCheck == true && edgeTimer >= edgeHoldTime)
        {
            if (playerCamera != null)
                playerCamera.SetActive(false);

            if (rightLookingCamera != null)
                rightLookingCamera.SetActive(true);
        }
        else
        {
            if (rightLookingCamera != null)
                rightLookingCamera.SetActive(false);

            if (playerCamera != null)
                playerCamera.SetActive(true);
        }
        #endregion
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerCamera != null)
                playerCamera.SetActive(false);

            if (roomCamera != null)
                roomCamera.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (roomCamera != null)
                roomCamera.SetActive(false);

            if (playerCamera != null)
                playerCamera.SetActive(true);
        }
    }
}
