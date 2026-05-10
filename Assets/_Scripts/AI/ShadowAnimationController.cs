using UnityEngine;

namespace AI
{
    /// <summary>
    /// Handles Shadow AI animation state management and visual feedback.
    /// Integrates with Unity Animator for smooth state transitions.
    /// </summary>
    public class ShadowAnimationController : MonoBehaviour
    {
        [Header("Animation Configuration")]
        [Tooltip("Animator component for shadow visual states")]
        [SerializeField] private Animator _shadowAnimator;
        [Tooltip("Speed multiplier for idle/perching animations")]
        [SerializeField] private float _idleAnimationSpeed = 1f;
        [Tooltip("Speed multiplier for attack animations")]
        [SerializeField] private float _attackAnimationSpeed = 1.5f;

        [Header("Animation States")]
        [Tooltip("Enable visual feedback during perching state")]
        [SerializeField] private bool _enablePerchingAnimation = true;
        [Tooltip("Enable visual feedback during attack preparation")]
        [SerializeField] private bool _enableAttackAnimation = true;
        [Tooltip("Enable taunt animation after failed attacks")]
        [SerializeField] private bool _enableTauntAnimation = true;

        [Header("Visual Effects")]
        [Tooltip("Sprite renderer for shadow visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [Tooltip("Color tint when perching")]
        [SerializeField] private Color _perchingColor = Color.white;
        [Tooltip("Color tint when preparing to attack")]
        [SerializeField] private Color _attackColor = Color.red;
        [Tooltip("Color tint when taunting")]
        [SerializeField] private Color _tauntColor = Color.yellow;

        [Header("Debug Information")]
        [SerializeField, ReadOnly] private bool _isPerching;
        [SerializeField, ReadOnly] private bool _isAttacking;
        [SerializeField, ReadOnly] private bool _isTaunting;

        // Animation parameter names (must match Animator Controller)
        private static readonly string ANIM_IS_PERCHING = "IsPerching";
        private static readonly string ANIM_IS_ATTACKING = "IsAttacking";
        private static readonly string ANIM_TAUNT_TRIGGER = "TauntTrigger";
        private static readonly string ANIM_IDLE_TRIGGER = "IdleTrigger";

        // State tracking
        private Color _originalColor;
        private bool _initialized = false;

        private void Awake()
        {
            InitializeAnimationController();
        }

        /// <summary>
        /// Initializes the animation controller and caches original values.
        /// </summary>
        private void InitializeAnimationController()
        {
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }

            if (_shadowAnimator == null)
            {
                _shadowAnimator = GetComponent<Animator>();
                if (_shadowAnimator == null)
                {
                    Debug.LogWarning("[ShadowAnimation] No Animator component found! " +
                                   "Animation features will be disabled.");
                }
            }

            _initialized = true;
        }

        /// <summary>
        /// Sets the perching animation state with visual feedback.
        /// </summary>
        /// <param name="isPerching">True when shadow is perching, false otherwise</param>
        public void SetPerching(bool isPerching)
        {
            if (!_initialized)
            {
                InitializeAnimationController();
            }

            _isPerching = isPerching;

            // Update animator parameter
            if (_shadowAnimator != null && _enablePerchingAnimation)
            {
                _shadowAnimator.SetBool(ANIM_IS_PERCHING, isPerching);
                _shadowAnimator.speed = _idleAnimationSpeed;
            }

            // Update visual feedback
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = isPerching ? _perchingColor : _originalColor;
            }
        }

        /// <summary>
        /// Sets the attacking animation state with visual feedback.
        /// </summary>
        /// <param name="isAttacking">True when shadow is attacking, false otherwise</param>
        public void SetAttacking(bool isAttacking)
        {
            if (!_initialized)
            {
                InitializeAnimationController();
            }

            _isAttacking = isAttacking;

            // Update animator parameter
            if (_shadowAnimator != null && _enableAttackAnimation)
            {
                _shadowAnimator.SetBool(ANIM_IS_ATTACKING, isAttacking);
                _shadowAnimator.speed = isAttacking ? _attackAnimationSpeed : _idleAnimationSpeed;
            }

            // Update visual feedback
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = isAttacking ? _attackColor : _originalColor;
            }
        }

        /// <summary>
        /// Plays a taunt animation with visual feedback.
        /// Called after failed attack attempts or successful dodges.
        /// </summary>
        public void PlayTaunt()
        {
            if (!_initialized)
            {
                InitializeAnimationController();
            }

            _isTaunting = true;

            // Trigger taunt animation
            if (_shadowAnimator != null && _enableTauntAnimation)
            {
                _shadowAnimator.SetTrigger(ANIM_TAUNT_TRIGGER);
                _shadowAnimator.speed = _idleAnimationSpeed;
            }

            // Update visual feedback
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _tauntColor;

                // Reset color after taunt duration
                Invoke(nameof(ResetTauntColor), 2f);
            }
        }

        /// <summary>
        /// Plays idle animation state.
        /// Used when transitioning to dormant or neutral states.
        /// </summary>
        public void PlayIdle()
        {
            if (!_initialized)
            {
                InitializeAnimationController();
            }

            // Reset all animation states
            _isPerching = false;
            _isAttacking = false;
            _isTaunting = false;

            // Update animator parameters
            if (_shadowAnimator != null)
            {
                _shadowAnimator.SetBool(ANIM_IS_PERCHING, false);
                _shadowAnimator.SetBool(ANIM_IS_ATTACKING, false);
                _shadowAnimator.SetTrigger(ANIM_IDLE_TRIGGER);
                _shadowAnimator.speed = _idleAnimationSpeed;
            }

            // Reset visual feedback
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }
        }

        /// <summary>
        /// Sets the overall animation speed multiplier.
        /// </summary>
        /// <param name="speedMultiplier">Speed multiplier (1.0 = normal speed)</param>
        public void SetAnimationSpeed(float speedMultiplier)
        {
            if (_shadowAnimator != null)
            {
                _shadowAnimator.speed = speedMultiplier;
            }
        }

        /// <summary>
        /// Sets shadow visibility (alpha channel).
        /// </summary>
        /// <param name="alpha">Alpha value (0 = invisible, 1 = fully visible)</param>
        public void SetVisibility(float alpha)
        {
            if (_spriteRenderer != null)
            {
                Color currentColor = _spriteRenderer.color;
                currentColor.a = Mathf.Clamp01(alpha);
                _spriteRenderer.color = currentColor;
            }
        }

        /// <summary>
        /// Gets the current animation state for debugging purposes.
        /// </summary>
        public ShadowAnimationState GetCurrentAnimationState()
        {
            return new ShadowAnimationState
            {
                IsPerching = _isPerching,
                IsAttacking = _isAttacking,
                IsTaunting = _isTaunting,
                CurrentColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white,
                AnimationSpeed = _shadowAnimator != null ? _shadowAnimator.speed : 1f
            };
        }

        /// <summary>
        /// Resets taunt color after taunt animation completes.
        /// </summary>
        private void ResetTauntColor()
        {
            _isTaunting = false;
            if (_spriteRenderer != null && !_isPerching && !_isAttacking)
            {
                _spriteRenderer.color = _originalColor;
            }
        }

        /// <summary>
        /// Validates animation configuration and provides Inspector feedback.
        /// </summary>
        private void OnValidate()
        {
            if (_shadowAnimator == null)
            {
                _shadowAnimator = GetComponent<Animator>();
                if (_shadowAnimator == null)
                {
                    Debug.LogWarning("[ShadowAnimation] No Animator component found! " +
                                   "Please attach an Animator or assign one in the Inspector.");
                }
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null)
                {
                    Debug.LogWarning("[ShadowAnimation] No SpriteRenderer component found! " +
                                   "Visual feedback will be disabled.");
                }
            }

            if (_idleAnimationSpeed <= 0f)
            {
                Debug.LogWarning("[ShadowAnimation] Idle animation speed should be positive!");
            }

            if (_attackAnimationSpeed <= 0f)
            {
                Debug.LogWarning("[ShadowAnimation] Attack animation speed should be positive!");
            }
        }

        /// <summary>
        /// Debug method to test animation states in the Inspector.
        /// </summary>
        [ContextMenu("Test Perching Animation")]
        private void TestPerchingAnimation()
        {
            SetPerching(!_isPerching);
        }

        /// <summary>
        /// Debug method to test attack animation in the Inspector.
        /// </summary>
        [ContextMenu("Test Attack Animation")]
        private void TestAttackAnimation()
        {
            SetAttacking(!_isAttacking);
        }

        /// <summary>
        /// Debug method to test taunt animation in the Inspector.
        /// </summary>
        [ContextMenu("Test Taunt Animation")]
        private void TestTauntAnimation()
        {
            PlayTaunt();
        }
    }

    /// <summary>
    /// Data structure for animation state information.
    /// </summary>
    [System.Serializable]
    public struct ShadowAnimationState
    {
        public bool IsPerching;
        public bool IsAttacking;
        public bool IsTaunting;
        public Color CurrentColor;
        public float AnimationSpeed;
    }
}