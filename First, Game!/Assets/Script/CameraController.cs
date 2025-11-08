using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    //public Transform target;
    //public Vector2 offset = new Vector2(0, 2f);
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

    Vector3 offset;
    [SerializeField] Transform target;


    private Bounds _cameraBounds;//nova
    private Vector3 _targetPostion;//nova
    private Camera _mainCamera;//nova

    private void Awake() => _mainCamera = Camera.main;//nova

    private void Start()
    {
        initialPos = transform.localPosition;

        if (postProcessingVolume && postProcessingVolume.profile.TryGet<MotionBlur>(out var mb))
            motionBlur = mb;

        

        var height = _mainCamera.orthographicSize;//nova
        var width = height * _mainCamera.aspect;//nova

        var minX = Globals.WorldBounds.min.x;//noav
        var minY = Globals.WorldBounds.min.y;//nova

        var maxX = Globals.WorldBounds.max.x;//nova
        var maxY = Globals.WorldBounds.max.y;//nova

        _cameraBounds = new Bounds();//nova
        _cameraBounds.SetMinMax(
            new Vector3(minX, minY, 0.0f),
            new Vector3(maxX, maxY, 0.0f)
            );//nova
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
        //transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        
        _targetPostion = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);//nova
        _targetPostion = GetCameraBounds();//nova

        transform.position = _targetPostion;

    }

    private Vector3 GetCameraBounds()//nova
    {
        return new Vector3(
            Mathf.Clamp(_targetPostion.x, _cameraBounds.min.x, _cameraBounds.max.x),
            Mathf.Clamp(_targetPostion.y, _cameraBounds.min.y, _cameraBounds.max.y),
            transform.position.z
            );//nova
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
