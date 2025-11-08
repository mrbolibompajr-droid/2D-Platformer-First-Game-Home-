using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public Vector2 offset = new Vector2(0, 2f);
    public float smoothSpeed = 5f;

    [Header("Camera Shake")]
    public bool enableShake = true;
    public float shakeMagnitude = 0f;
    private float shakeTimer;
    private Vector3 initialPos;

    [Header("Motion Blur")]
    public bool enableMotionBlur = true;
    public Volume postProcessingVolume; // assign a Volume with Motion Blur
    private MotionBlur motionBlur;
    private float blurTimer;

    private void Start()
    {
        initialPos = transform.localPosition;

        if (postProcessingVolume && postProcessingVolume.profile.TryGet<MotionBlur>(out var mb))
            motionBlur = mb;
    }

    private void Update()
    {
        FollowTarget();
        HandleShake();
        HandleMotionBlur();
    }

    private void FollowTarget()
    {
        if (!target) return;

        Vector3 targetPos = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }

    #region Shake
    public void TriggerShake(float duration = 0.15f, float magnitude = 0.3f)
    {
        if (!enableShake) return;
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    private void HandleShake()
    {
        if (shakeTimer > 0)
        {
            Vector3 shakeOffset = Random.insideUnitCircle * shakeMagnitude;
            transform.localPosition = new Vector3(initialPos.x + shakeOffset.x, initialPos.y + shakeOffset.y, transform.localPosition.z);
            shakeTimer -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = initialPos;
        }
    }
    #endregion

    #region Motion Blur
    public void TriggerMotionBlur(float duration = 0.15f, float intensity = 0.8f)
    {
        if (!enableMotionBlur || motionBlur == null) return;
        blurTimer = duration;
        motionBlur.intensity.value = intensity;
    }

    private void HandleMotionBlur()
    {
        if (blurTimer > 0)
            blurTimer -= Time.deltaTime;
        else if (motionBlur != null)
            motionBlur.intensity.value = 0f;
    }
    #endregion
}
