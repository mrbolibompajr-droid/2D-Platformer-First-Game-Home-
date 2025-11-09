using UnityEngine;
using System.Collections;

public class CameraController2D : MonoBehaviour
{
    [Header("Player Follow Settings")]
    public Transform player;           // Player to follow
    public Vector3 offset = Vector3.zero;
    public float followSmoothTime = 0.2f;
    public Vector2 deadZone = Vector2.zero; // Optional: x/y dead zone

    [Header("Global Bounds")]
    public float globalMinX, globalMaxX, globalMinY, globalMaxY;

    private Vector3 velocity = Vector3.zero;
    private bool isTransitioning = false;

    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
        if (cam.orthographic == false)
        {
            Debug.LogWarning("Camera must be orthographic for 2D camera bounds to work correctly.");
        }
    }

    void LateUpdate()
    {
        if (player == null || isTransitioning) return;

        Vector3 targetPos = player.position + offset;

        // Dead zone logic
        Vector3 diff = targetPos - transform.position;
        if (Mathf.Abs(diff.x) <= deadZone.x) targetPos.x = transform.position.x;
        if (Mathf.Abs(diff.y) <= deadZone.y) targetPos.y = transform.position.y;

        // Smooth follow
        Vector3 smoothPos = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, followSmoothTime);

        // Clamp to global bounds
        float camHalfWidth = cam.orthographicSize * ((float)Screen.width / Screen.height);
        float camHalfHeight = cam.orthographicSize;

        smoothPos.x = Mathf.Clamp(smoothPos.x, globalMinX + camHalfWidth, globalMaxX - camHalfWidth);
        smoothPos.y = Mathf.Clamp(smoothPos.y, globalMinY + camHalfHeight, globalMaxY - camHalfHeight);

        transform.position = new Vector3(smoothPos.x, smoothPos.y, transform.position.z);
    }

    /// <summary>
    /// Move the camera to a specific target with optional zoom over duration
    /// </summary>
    public void MoveCameraTo(Vector3 targetPosition, float targetSize, float duration)
    {
        StartCoroutine(CameraTransition(targetPosition, targetSize, duration));
    }

    private IEnumerator CameraTransition(Vector3 targetPosition, float targetSize, float duration)
    {
        isTransitioning = true;

        Vector3 startPos = transform.position;
        float startSize = cam.orthographicSize;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / duration); // smooth interpolation

            transform.position = new Vector3(
                Mathf.Lerp(startPos.x, targetPosition.x, t),
                Mathf.Lerp(startPos.y, targetPosition.y, t),
                transform.position.z
            );

            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);

            yield return null;
        }

        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        cam.orthographicSize = targetSize;

        isTransitioning = false;
    }

    /// <summary>
    /// Helper to return camera to the player
    /// </summary>
    public void ReturnToPlayer(float duration, float normalSize)
    {
        if (player != null)
        {
            Vector3 targetPos = player.position + offset;
            MoveCameraTo(targetPos, normalSize, duration);
        }
    }
}
