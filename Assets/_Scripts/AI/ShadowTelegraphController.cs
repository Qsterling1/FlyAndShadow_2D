using UnityEngine;
using System.Collections;

namespace AI
{
    /// <summary>
    /// Handles Shadow AI telegraph and screen shake effects for attack preparation.
    /// Provides clear visual feedback and timing for player reaction windows.
    /// </summary>
    public class ShadowTelegraphController : MonoBehaviour
    {
        [Header("Attack Telegraph Configuration")]
        [Tooltip("Duration of telegraph warning before attack")]
        [SerializeField] private float _dashTelegraphTime = 0.75f;
        [Tooltip("Intensity of screen shake during telegraph")]
        [SerializeField] private float _screenShakeIntensity = 0.3f;
        [Tooltip("Screen shake duration during telegraph")]
        [SerializeField] private float _screenShakeDuration = 0.5f;
        [Tooltip("Animation curve for telegraph intensity over time")]
        [SerializeField] private AnimationCurve _telegraphCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Visual Telegraph Effects")]
        [Tooltip("Shadow sprite renderer for visual effects")]
        [SerializeField] private SpriteRenderer _shadowSprite;
        [Tooltip("Color during telegraph warning")]
        [SerializeField] private Color _telegraphColor = Color.red;
        [Tooltip("Flash frequency during telegraph")]
        [SerializeField] private float _telegraphFlashFrequency = 8f;
        [Tooltip("Enable particle effects during telegraph")]
        [SerializeField] private bool _enableTelegraphParticles = true;

        [Header("Screen Shake Settings")]
        [Tooltip("Camera to apply screen shake to (auto-finds if null)")]
        [SerializeField] private Camera _targetCamera;
        [Tooltip("Maximum shake distance from original position")]
        [SerializeField] private float _maxShakeDistance = 1f;
        [Tooltip("Enable screen shake effects")]
        [SerializeField] private bool _enableScreenShake = true;

        [Header("Debug Information")]
        [SerializeField, ReadOnly] private bool _isTelegraphing;
        [SerializeField, ReadOnly] private float _telegraphProgress;
        [SerializeField, ReadOnly] private bool _isShaking;

        // State tracking
        private Color _originalSpriteColor;
        private Vector3 _originalCameraPosition;
        private Coroutine _telegraphCoroutine;
        private Coroutine _shakeCoroutine;
        private bool _initialized = false;

        // Events for integration with Shadow AI Controller
        public System.Action OnTelegraphStarted;
        public System.Action OnTelegraphCompleted;
        public System.Action OnTelegraphCancelled;

        public bool IsTelegraphing => _isTelegraphing;
        public float TelegraphProgress => _telegraphProgress;

        private void Awake()
        {
            InitializeTelegraphController();
        }

        /// <summary>
        /// Initializes the telegraph controller and caches original values.
        /// </summary>
        private void InitializeTelegraphController()
        {
            // Cache original sprite color
            if (_shadowSprite != null)
            {
                _originalSpriteColor = _shadowSprite.color;
            }

            // Auto-find camera if not assigned
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
                if (_targetCamera == null)
                {
                    _targetCamera = FindObjectOfType<Camera>();
                }
            }

            // Cache original camera position
            if (_targetCamera != null)
            {
                _originalCameraPosition = _targetCamera.transform.position;
            }

            _initialized = true;
        }

        /// <summary>
        /// Starts the telegraph sequence for an incoming attack.
        /// </summary>
        public void StartTelegraph()
        {
            if (!_initialized)
            {
                InitializeTelegraphController();
            }

            if (_isTelegraphing)
            {
                Debug.LogWarning("[ShadowTelegraph] Telegraph already in progress!");
                return;
            }

            _telegraphCoroutine = StartCoroutine(TelegraphSequence());
        }

        /// <summary>
        /// Cancels the current telegraph sequence.
        /// </summary>
        public void CancelTelegraph()
        {
            if (_telegraphCoroutine != null)
            {
                StopCoroutine(_telegraphCoroutine);
                _telegraphCoroutine = null;
            }

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }

            ResetTelegraphEffects();
            OnTelegraphCancelled?.Invoke();
        }

        /// <summary>
        /// Main telegraph sequence coroutine.
        /// </summary>
        private IEnumerator TelegraphSequence()
        {
            _isTelegraphing = true;
            _telegraphProgress = 0f;
            OnTelegraphStarted?.Invoke();

            // Start screen shake if enabled
            if (_enableScreenShake)
            {
                _shakeCoroutine = StartCoroutine(ScreenShakeSequence());
            }

            float elapsedTime = 0f;
            Color originalColor = _shadowSprite != null ? _shadowSprite.color : Color.white;

            while (elapsedTime < _dashTelegraphTime)
            {
                _telegraphProgress = elapsedTime / _dashTelegraphTime;
                float curveValue = _telegraphCurve.Evaluate(_telegraphProgress);

                // Apply visual telegraph effects
                if (_shadowSprite != null)
                {
                    ApplyTelegraphVisualEffects(curveValue, originalColor);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Telegraph complete
            _telegraphProgress = 1f;
            ResetTelegraphEffects();
            OnTelegraphCompleted?.Invoke();

            _isTelegraphing = false;
            _telegraphCoroutine = null;
        }

        /// <summary>
        /// Applies visual effects during telegraph sequence.
        /// </summary>
        /// <param name="intensity">Telegraph intensity (0-1)</param>
        /// <param name="originalColor">Original sprite color</param>
        private void ApplyTelegraphVisualEffects(float intensity, Color originalColor)
        {
            if (_shadowSprite == null) return;

            // Flash effect based on telegraph curve
            float flashValue = Mathf.Sin(Time.time * _telegraphFlashFrequency) * 0.5f + 0.5f;
            flashValue *= intensity;

            // Interpolate between original color and telegraph color
            Color targetColor = Color.Lerp(originalColor, _telegraphColor, flashValue);
            _shadowSprite.color = targetColor;

            // Scale effect (optional visual enhancement)
            float scaleMultiplier = 1f + (intensity * 0.2f); // 20% max scale increase
            _shadowSprite.transform.localScale = Vector3.one * scaleMultiplier;
        }

        /// <summary>
        /// Screen shake sequence coroutine.
        /// </summary>
        private IEnumerator ScreenShakeSequence()
        {
            if (_targetCamera == null)
            {
                yield break;
            }

            _isShaking = true;
            Vector3 originalPosition = _targetCamera.transform.position;
            float elapsedTime = 0f;

            while (elapsedTime < _screenShakeDuration && _isTelegraphing)
            {
                float progress = elapsedTime / _screenShakeDuration;
                float curveValue = _telegraphCurve.Evaluate(progress);
                float shakeIntensity = _screenShakeIntensity * curveValue;

                // Generate random shake offset
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity),
                    0f
                );

                // Constrain shake within max distance
                if (shakeOffset.magnitude > _maxShakeDistance)
                {
                    shakeOffset = shakeOffset.normalized * _maxShakeDistance;
                }

                _targetCamera.transform.position = originalPosition + shakeOffset;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Reset camera position
            _targetCamera.transform.position = originalPosition;
            _isShaking = false;
            _shakeCoroutine = null;
        }

        /// <summary>
        /// Resets all telegraph effects to original state.
        /// </summary>
        private void ResetTelegraphEffects()
        {
            // Reset sprite appearance
            if (_shadowSprite != null)
            {
                _shadowSprite.color = _originalSpriteColor;
                _shadowSprite.transform.localScale = Vector3.one;
            }

            // Reset camera position
            if (_targetCamera != null && _isShaking)
            {
                _targetCamera.transform.position = _originalCameraPosition;
            }

            _isTelegraphing = false;
            _telegraphProgress = 0f;
            _isShaking = false;
        }

        /// <summary>
        /// Manually triggers a screen shake effect (for testing or other uses).
        /// </summary>
        /// <param name="intensity">Shake intensity</param>
        /// <param name="duration">Shake duration</param>
        public void TriggerScreenShake(float intensity, float duration)
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }

            _shakeCoroutine = StartCoroutine(ManualScreenShake(intensity, duration));
        }

        /// <summary>
        /// Manual screen shake coroutine for standalone effects.
        /// </summary>
        private IEnumerator ManualScreenShake(float intensity, float duration)
        {
            if (_targetCamera == null) yield break;

            Vector3 originalPosition = _targetCamera.transform.position;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-intensity, intensity),
                    Random.Range(-intensity, intensity),
                    0f
                );

                _targetCamera.transform.position = originalPosition + shakeOffset;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _targetCamera.transform.position = originalPosition;
        }

        /// <summary>
        /// Gets telegraph timing configuration for external systems.
        /// </summary>
        public TelegraphTimingData GetTelegraphTiming()
        {
            return new TelegraphTimingData
            {
                TelegraphDuration = _dashTelegraphTime,
                ShakeDuration = _screenShakeDuration,
                ShakeIntensity = _screenShakeIntensity,
                IsConfiguredProperly = _shadowSprite != null && _targetCamera != null
            };
        }

        /// <summary>
        /// Validates telegraph configuration and provides Inspector feedback.
        /// </summary>
        private void OnValidate()
        {
            if (_dashTelegraphTime <= 0f)
            {
                Debug.LogWarning("[ShadowTelegraph] Telegraph time should be positive!");
            }

            if (_screenShakeIntensity < 0f)
            {
                Debug.LogWarning("[ShadowTelegraph] Screen shake intensity should not be negative!");
            }

            if (_maxShakeDistance <= 0f)
            {
                Debug.LogWarning("[ShadowTelegraph] Max shake distance should be positive!");
            }

            if (_telegraphFlashFrequency <= 0f)
            {
                Debug.LogWarning("[ShadowTelegraph] Telegraph flash frequency should be positive!");
            }
        }

        /// <summary>
        /// Debug method to test telegraph sequence in the Inspector.
        /// </summary>
        [ContextMenu("Test Telegraph Sequence")]
        private void TestTelegraphSequence()
        {
            if (Application.isPlaying)
            {
                if (_isTelegraphing)
                {
                    CancelTelegraph();
                }
                else
                {
                    StartTelegraph();
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up coroutines and reset effects
            CancelTelegraph();
        }
    }

    /// <summary>
    /// Data structure for telegraph timing information.
    /// </summary>
    [System.Serializable]
    public struct TelegraphTimingData
    {
        public float TelegraphDuration;
        public float ShakeDuration;
        public float ShakeIntensity;
        public bool IsConfiguredProperly;
    }
}