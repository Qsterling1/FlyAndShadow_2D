using UnityEngine;
using System.Collections;

/// <summary>
/// A professional-grade 3D follow camera adapted from the 2D version. It includes smooth damping, look-ahead,
/// a dead zone, vertical clamping, dynamic zoom based on target speed, and a shake effect.
/// All features are highly configurable in the Inspector.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController3D : MonoBehaviour
{
    public static CameraController3D instance;

    [Header("Target & Damping")]
    [Tooltip("The Rigidbody of the target the camera should follow (the Player).")]
    [SerializeField] private Rigidbody _target;
    [Tooltip("How quickly the camera catches up to the target. Lower values are slower/smoother.")]
    [SerializeField] private float _smoothTime = 0.3f;

    [Header("Look-Ahead & Dead Zone")]
    [Tooltip("How far the camera will look ahead of the player in the direction of movement.")]
    [SerializeField] private float _lookAheadDistance = 4f;
    [Tooltip("The size of the area where the player can move without the camera following.")]
    [SerializeField] private Vector2 _deadZoneSize = new Vector2(2f, 2f);
    
    [Header("Vertical Clamping")]
    [Tooltip("Enable to constrain the camera's Y position.")]
    [SerializeField] private bool _enableClamping = true;
    [Tooltip("The minimum Y position the camera can have.")]
    [SerializeField] private float _minY = 0f;
    [Tooltip("The maximum Y position the camera can have.")]
    [SerializeField] private float _maxY = 10f;
    
    [Header("Dynamic Zoom Settings")]
    [Tooltip("Enable to make the camera zoom in/out based on the target's speed.")]
    [SerializeField] private bool _enableDynamicZoom = true;
    [Tooltip("The target's speed at which the camera is fully zoomed in.")]
    [SerializeField] private float _minZoomSpeed = 5f;
    [Tooltip("The target's speed at which the camera is fully zoomed out.")]
    [SerializeField] private float _maxZoomSpeed = 20f;
    [Tooltip("The camera's orthographic size when fully zoomed in.")]
    [SerializeField] private float _minZoomSize = 5f;
    [Tooltip("The camera's orthographic size when fully zoomed out.")]
    [SerializeField] private float _maxZoomSize = 8f;
    [Tooltip("How quickly the camera adjusts its zoom level.")]
    [SerializeField] private float _zoomSmoothTime = 0.5f;
    
    [Header("Shake Settings")]
    [Tooltip("Default duration for the camera shake effect.")]
    [SerializeField] private float _defaultShakeDuration = 0.5f;
    [Tooltip("Default magnitude (intensity) for the camera shake effect.")]
    [SerializeField] private float _defaultShakeMagnitude = 0.1f;

    // --- Private Fields ---
    private Camera _cam;
    private Vector3 _targetPosition;
    private Vector3 _velocity = Vector3.zero;
    private float _zoomVelocity = 0f;
    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        _cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
         Debug.Log("5. CameraController LateUpdate");
        if (_target == null) return;

        // 1. Calculate the base target position with look-ahead
        float targetX = _target.transform.position.x + _lookAheadDistance;
        float targetY = _target.transform.position.y;
        _targetPosition = new Vector3(targetX, targetY, transform.position.z);

        // 2. Apply the dead zone
        float deltaX = _targetPosition.x - transform.position.x;
        float deltaY = _targetPosition.y - transform.position.y;
        
        if (Mathf.Abs(deltaX) < _deadZoneSize.x / 2f)
        {
            _targetPosition.x = transform.position.x;
        }
        if (Mathf.Abs(deltaY) < _deadZoneSize.y / 2f)
        {
            _targetPosition.y = transform.position.y;
        }

        // 3. Apply vertical clamping
        if (_enableClamping)
        {
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, _minY, _maxY);
        }

        // 4. Smoothly move the camera
        transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _velocity, _smoothTime);

        // 5. Handle dynamic zoom
        if (_enableDynamicZoom)
        {
            HandleZoom();
        }
    }

    private void HandleZoom()
    {
        // Get the percentage of speed within our defined range
        float currentSpeed = Mathf.Abs(_target.linearVelocity.x);
        float speedPercent = Mathf.InverseLerp(_minZoomSpeed, _maxZoomSpeed, currentSpeed);

        // Lerp between min and max zoom sizes based on the speed percentage
        float targetSize = Mathf.Lerp(_minZoomSize, _maxZoomSize, speedPercent);

        // Smoothly adjust the camera's actual orthographic size
        _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize, targetSize, ref _zoomVelocity, _zoomSmoothTime);
    }

    /// <summary>
    /// Triggers a camera shake with the default duration and magnitude.
    /// </summary>
    public void Shake()
    {
        Shake(_defaultShakeDuration, _defaultShakeMagnitude);
    }
    
    /// <summary>
    /// Triggers a camera shake with a custom duration and magnitude.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalPosition = transform.position;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.position = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
    }
    
    /// <summary>
    /// Draws visual gizmos in the Scene view to make tuning easier.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (_target == null) return;
        
        // Draw Dead Zone
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(_deadZoneSize.x, _deadZoneSize.y, 1f));

        // Draw Clamping Boundaries
        if (_enableClamping)
        {
            Gizmos.color = Color.red;
            float cameraWidth = 2f * _cam.orthographicSize * _cam.aspect;
            Gizmos.DrawLine(new Vector3(transform.position.x - cameraWidth, _minY, 0), new Vector3(transform.position.x + cameraWidth, _minY, 0));
            Gizmos.DrawLine(new Vector3(transform.position.x - cameraWidth, _maxY, 0), new Vector3(transform.position.x + cameraWidth, _maxY, 0));
        }
    }
}