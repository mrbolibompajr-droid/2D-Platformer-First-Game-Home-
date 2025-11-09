using UnityEngine;

public class CameraTriggerZone : MonoBehaviour
{
    [Header("Assign the cameras")]
    public GameObject playerCamera; // The camera following the player
    public GameObject roomCamera;   // The room/overview camera

    private void Start()
    {
        // Make sure only the player camera is active at start
        if (playerCamera != null)
            playerCamera.SetActive(true);

        if (roomCamera != null)
            roomCamera.SetActive(false);
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
