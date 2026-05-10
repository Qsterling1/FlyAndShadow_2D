using UnityEngine;
using FlyShadow.EventBus;

namespace AI
{
    /// <summary>
    /// Main behavior controller for the Shadow AI enemy system.
    /// Implements adaptive AI interference based on player performance.
    /// </summary>
    public class ShadowAIController : MonoBehaviour
    {
        [Header("Shadow AI Configuration")]
        [SerializeField] private PlayerPerformanceTracker _performanceTracker;
        [Tooltip("Cooldown time between shadow aggression cycles")]
        [SerializeField] private float _aggroCooldown = 10f;
        [Tooltip("Enable debug logging and Inspector monitoring")]
        [SerializeField] private bool _debugMode = false;

        [Header("Follow Anchor System")]
        [Tooltip("Player transform to follow (automatically finds if null)")]
        [SerializeField] private Transform _playerTransform;
        [Tooltip("Camera transform for screen-space calculations (automatically finds if null)")]
        [SerializeField] private Transform _cameraTransform;
        [Tooltip("Local offset from player when dormant (keeps shadow off-screen)")]
        [SerializeField] private Vector3 _dormantLocalOffset = new Vector3(20f, 10f, 0f);
        [Tooltip("Enable continuous player following to prevent world-space drift")]
        [SerializeField] private bool _enableFollowAnchor = true;

        [Header("Trigger Conditions")]
        [Tooltip("Minimum flight time before shadow becomes aggressive")]
        [SerializeField] private float _flightTimeThreshold = 5.0f;
        [Tooltip("Performance score threshold to trigger shadow appearance")]
        [SerializeField] private float _goodPerformanceThreshold = 75f;

        [Header("Perching Behavior")]
        [Tooltip("Array of UI transform references for perch locations")]
        [SerializeField] private Transform[] _perchPoints;
        [Tooltip("Minimum time to remain perched")]
        [SerializeField] private float _minPerchDuration = 1.5f;
        [Tooltip("Maximum time to remain perched")]
        [SerializeField] private float _maxPerchDuration = 3.0f;
        [Tooltip("Speed of movement between perch points")]
        [SerializeField] private float _perchTransitionSpeed = 8f;

        [Header("Attack Telegraph")]
        [Tooltip("Telegraph controller for attack warnings")]
        [SerializeField] private ShadowTelegraphController _telegraphController;
        [Tooltip("Duration of telegraph warning before attack")]
        [SerializeField] private float _dashTelegraphTime = 0.75f;
        [Tooltip("Intensity of screen shake during telegraph")]
        [SerializeField] private float _screenShakeIntensity = 0.3f;
        [Tooltip("Animation curve for telegraph intensity")]
        [SerializeField] private AnimationCurve _telegraphCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Dash Attack")]
        [Tooltip("Speed of the dash attack movement")]
        [SerializeField] private float _dashSpeed = 15f;
        [Tooltip("Maximum distance the shadow will dash")]
        [SerializeField] private float _dashDistance = 8f;
        [Tooltip("Animation curve controlling dash speed over time")]
        [SerializeField] private AnimationCurve _dashSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Layer mask for player collision detection")]
        [SerializeField] private LayerMask _playerLayer = -1;

        [Header("Attack Outcomes")]
        [Tooltip("Radius for hit detection during dash attack")]
        [SerializeField] private float _hitDetectionRadius = 1.5f;
        [Tooltip("Enable debug visualization of hit detection area")]
        [SerializeField] private bool _debugDrawHitbox = true;

        [Header("Player Penalties (On Hit)")]
        [Tooltip("Power reduction when player is hit by shadow attack")]
        [SerializeField] private int _powerPenalty = 5;
        [Tooltip("Stamina reduction when player is hit by shadow attack")]
        [SerializeField] private float _staminaPenalty = 30f;
        [Tooltip("Reset player to running state when hit")]
        [SerializeField] private bool _resetToRunning = true;
        [Tooltip("Speed reduction multiplier applied after hit")]
        [SerializeField] private float _speedResetMultiplier = 0.5f;

        [Header("Debug Information")]
        [SerializeField, ReadOnly] private ShadowState _currentState;
        [SerializeField, ReadOnly] private float _nextAggroTime;
        [SerializeField, ReadOnly] private float _currentCooldownTimer;
        [SerializeField, ReadOnly] private int _currentPerchIndex;
        [SerializeField, ReadOnly] private float _perchStateTimer;
        [SerializeField, ReadOnly] private bool _isTelegraphing;
        [SerializeField, ReadOnly] private bool _isDashing;
        [SerializeField, ReadOnly] private float _dashProgress;

        [Header("Regression Instrumentation")]
        [Tooltip("Enable distance monitoring between shadow and player")]
        [SerializeField] private bool _enableDistanceMonitoring = true;
        [Tooltip("Warning threshold distance from player (in world units)")]
        [SerializeField] private float _distanceWarningThreshold = 50f;
        [SerializeField, ReadOnly] private float _currentPlayerDistance;
        [SerializeField, ReadOnly] private float _maxRecordedDistance;
        [SerializeField, ReadOnly] private bool _anchorSystemHealthy;

        // State Management
        private float _lastAggroTime;
        private PlayerState _currentPlayerState;
        private float _perchStartTime;
        private float _perchDuration;
        private Vector3 _targetPerchPosition;
        private bool _isMovingToPerch;

        // Dash Attack State
        private Vector3 _dashStartPosition;
        private Vector3 _dashTargetPosition;
        private float _dashStartTime;
        private float _dashDuration;
        private bool _dashHitDetected;

        // Public Properties
        public ShadowState CurrentState => _currentState;
        public bool IsActive => _currentState != ShadowState.Dormant;
        public bool CanBecomeAggressive => Time.time >= _nextAggroTime;

        private void Awake()
        {
            _currentState = ShadowState.Dormant;
            _lastAggroTime = -_aggroCooldown; // Allow immediate first activation
            _nextAggroTime = 0f;
            _currentPerchIndex = -1;
            _isMovingToPerch = false;
            _isDashing = false;
            _dashHitDetected = false;
            InitializeTelegraphIntegration();
            InitializeFollowAnchor();
            InitializeAnimationController();
            InitializeRenderLayering();
        }

        private void OnEnable()
        {
            // Subscribe to EventBus for decoupled player monitoring
            EventManager.Subscribe<PlayerStateChangedEvent>(OnPlayerStateChanged);
        }

        private void OnDisable()
        {
            // Clean unsubscribe pattern for memory management
            EventManager.Unsubscribe<PlayerStateChangedEvent>(OnPlayerStateChanged);
        }

        private void OnDestroy()
        {
            // Clean up telegraph event subscriptions
            if (_telegraphController != null)
            {
                _telegraphController.OnTelegraphStarted -= OnTelegraphStarted;
                _telegraphController.OnTelegraphCompleted -= OnTelegraphCompleted;
                _telegraphController.OnTelegraphCancelled -= OnTelegraphCancelled;
            }
        }

        private void Update()
        {
            UpdateFollowAnchor();
            UpdateCooldownTimer();
            UpdatePerchBehavior();
            UpdateDashBehavior();
            EvaluateShadowBehavior();
            UpdateDistanceMonitoring();

            if (_debugMode)
            {
                LogDebugInfo();
            }
        }

        /// <summary>
        /// Handles player state changes for Shadow AI decision making.
        /// </summary>
        /// <param name="stateEvent">Player state change event data</param>
        private void OnPlayerStateChanged(PlayerStateChangedEvent stateEvent)
        {
            _currentPlayerState = stateEvent.State;

            if (_debugMode)
            {
                Debug.Log($"[ShadowAI] Player state changed to: {stateEvent.State}");
            }
        }

        /// <summary>
        /// Updates cooldown timing and availability for next aggression cycle.
        /// </summary>
        private void UpdateCooldownTimer()
        {
            _currentCooldownTimer = _nextAggroTime - Time.time;
            _currentCooldownTimer = Mathf.Max(0f, _currentCooldownTimer);
        }

        /// <summary>
        /// Updates perching behavior including movement and timing.
        /// </summary>
        private void UpdatePerchBehavior()
        {
            if (_currentState != ShadowState.Perching)
            {
                return;
            }

            _perchStateTimer = Time.time - _perchStartTime;

            // Handle movement to perch point
            if (_isMovingToPerch)
            {
                MoveToCurrentPerch();
            }
            // Handle perch duration timing
            else if (_perchStateTimer >= _perchDuration)
            {
                // Perch duration complete - transition to telegraphing for attack
                TransitionToState(ShadowState.Telegraphing);
            }
        }

        /// <summary>
        /// Smoothly moves shadow to current target perch position.
        /// Refreshes target each frame to maintain player-relative positioning in scrolling world.
        /// </summary>
        private void MoveToCurrentPerch()
        {
            if (_targetPerchPosition == Vector3.zero)
            {
                return;
            }

            // KEY FIX: Refresh perch target position each frame to stay relative to scrolling world
            // Perch points are Transform references that scroll with the world/UI
            if (_perchPoints != null && _currentPerchIndex >= 0 && _currentPerchIndex < _perchPoints.Length)
            {
                Transform currentPerchTransform = _perchPoints[_currentPerchIndex];
                if (currentPerchTransform != null)
                {
                    _targetPerchPosition = currentPerchTransform.position;
                }
            }

            Vector3 currentPosition = transform.position;
            float distanceToTarget = Vector3.Distance(currentPosition, _targetPerchPosition);

            if (distanceToTarget < 0.1f)
            {
                // Reached perch point
                transform.position = _targetPerchPosition;
                _isMovingToPerch = false;

                if (_debugMode)
                {
                    Debug.Log($"[ShadowAI] Reached perch point {_currentPerchIndex}, settling for {_perchDuration:F1}s");
                }
            }
            else
            {
                // Move towards perch point
                Vector3 direction = (_targetPerchPosition - currentPosition).normalized;
                float moveDistance = _perchTransitionSpeed * Time.deltaTime;
                transform.position += direction * moveDistance;
            }
        }

        /// <summary>
        /// Updates dash attack behavior including movement and hit detection.
        /// </summary>
        private void UpdateDashBehavior()
        {
            if (_currentState != ShadowState.Attacking || !_isDashing)
            {
                return;
            }

            float elapsedTime = Time.time - _dashStartTime;
            _dashProgress = elapsedTime / _dashDuration;

            if (_dashProgress >= 1f)
            {
                // Dash complete
                CompleteDashAttack();
                return;
            }

            // Update dash movement
            PerformDashMovement();

            // Check for hit detection during dash
            if (!_dashHitDetected)
            {
                CheckForPlayerHit();
            }
        }

        /// <summary>
        /// Performs the physics-based dash movement using the configured speed curve.
        /// Refreshes target position each frame to track player in scrolling world.
        /// </summary>
        private void PerformDashMovement()
        {
            // KEY FIX: Refresh dash target to track moving player during scroll
            if (_playerTransform != null)
            {
                Vector3 dashDirection = (_dashTargetPosition - _dashStartPosition).normalized;
                float originalDistance = Vector3.Distance(_dashStartPosition, _dashTargetPosition);

                // Update target to current player position while maintaining dash distance limit
                Vector3 currentPlayerPosition = _playerTransform.position;
                float distanceToPlayer = Vector3.Distance(_dashStartPosition, currentPlayerPosition);

                if (distanceToPlayer > _dashDistance)
                {
                    _dashTargetPosition = _dashStartPosition + (dashDirection * _dashDistance);
                }
                else
                {
                    _dashTargetPosition = currentPlayerPosition;
                }
            }

            float curveValue = _dashSpeedCurve.Evaluate(_dashProgress);
            Vector3 currentPosition = Vector3.Lerp(_dashStartPosition, _dashTargetPosition, curveValue);
            transform.position = currentPosition;
        }

        /// <summary>
        /// Checks for collision with player during dash attack.
        /// </summary>
        private void CheckForPlayerHit()
        {
            Collider2D playerCollider = Physics2D.OverlapCircle(
                transform.position,
                _hitDetectionRadius,
                _playerLayer
            );

            if (playerCollider != null)
            {
                _dashHitDetected = true;
                OnPlayerHit(playerCollider);

                if (_debugMode)
                {
                    Debug.Log("[ShadowAI] Player hit detected during dash attack!");
                }
            }
        }

        /// <summary>
        /// Handles successful hit on player during dash attack.
        /// </summary>
        /// <param name="playerCollider">Player collider that was hit</param>
        private void OnPlayerHit(Collider2D playerCollider)
        {
            ApplyPlayerPenalties();

            if (_debugMode)
            {
                Debug.Log($"[ShadowAI] Player hit at position {playerCollider.transform.position} - penalties applied");
            }
        }

        /// <summary>
        /// Applies configured penalties to player using EventBus system.
        /// </summary>
        private void ApplyPlayerPenalties()
        {
            // Apply power penalty
            if (_powerPenalty > 0)
            {
                EventManager.Publish(new PlayerPowerModifiedEvent
                {
                    Amount = -_powerPenalty // Negative for penalty
                });

                if (_debugMode)
                {
                    Debug.Log($"[ShadowAI] Applied power penalty: -{_powerPenalty}");
                }
            }

            // Apply stamina penalty
            if (_staminaPenalty > 0)
            {
                EventManager.Publish(new PlayerStaminaModifiedEvent
                {
                    Amount = -_staminaPenalty // Negative for penalty
                });

                if (_debugMode)
                {
                    Debug.Log($"[ShadowAI] Applied stamina penalty: -{_staminaPenalty}");
                }
            }

            // Apply speed reduction
            if (_speedResetMultiplier < 1f && _speedResetMultiplier > 0f)
            {
                float speedReduction = 1f - _speedResetMultiplier;
                EventManager.Publish(new PlayerSpeedModifiedEvent
                {
                    Delta = -speedReduction // Negative for reduction
                });

                if (_debugMode)
                {
                    Debug.Log($"[ShadowAI] Applied speed penalty: -{speedReduction:F2} multiplier");
                }
            }

            // Force player state reset if configured
            if (_resetToRunning)
            {
                // Use phase change to force downgrade to running state
                EventManager.Publish(new PhaseChangeRequestedEvent
                {
                    Step = -2 // Force significant downgrade
                });

                if (_debugMode)
                {
                    Debug.Log("[ShadowAI] Forced player state reset to running");
                }
            }
        }

        /// <summary>
        /// Completes the dash attack and transitions to appropriate next state.
        /// </summary>
        private void CompleteDashAttack()
        {
            _isDashing = false;
            _dashProgress = 1f;

            if (_dashHitDetected)
            {
                // Successful hit - transition to retreating state
                TransitionToState(ShadowState.Retreating);

                if (_debugMode)
                {
                    Debug.Log("[ShadowAI] Dash attack completed - HIT");
                }
            }
            else
            {
                // Missed attack - also retreat but may taunt
                TransitionToState(ShadowState.Retreating);

                if (_debugMode)
                {
                    Debug.Log("[ShadowAI] Dash attack completed - MISS");
                }
            }
        }

        /// <summary>
        /// Main AI evaluation loop - determines when Shadow should become aggressive.
        /// </summary>
        private void EvaluateShadowBehavior()
        {
            if (_performanceTracker == null)
            {
                if (_debugMode)
                {
                    Debug.LogWarning("[ShadowAI] Performance tracker not assigned!");
                }
                return;
            }

            switch (_currentState)
            {
                case ShadowState.Dormant:
                    EvaluateDormantState();
                    break;

                case ShadowState.Stalking:
                    EvaluateStalkingState();
                    break;

                case ShadowState.Perching:
                    EvaluatePerchingState();
                    break;

                case ShadowState.Telegraphing:
                    EvaluateTelegraphingState();
                    break;

                case ShadowState.Attacking:
                    EvaluateAttackingState();
                    break;

                case ShadowState.Retreating:
                    EvaluateRetreatingState();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Evaluates conditions for leaving dormant state and beginning stalking.
        /// </summary>
        private void EvaluateDormantState()
        {
            // Check cooldown
            if (!CanBecomeAggressive)
            {
                return;
            }

            // Check performance triggers
            bool performanceTrigger = _performanceTracker.CurrentPerformanceScore >= _goodPerformanceThreshold;
            bool flightTimeTrigger = _performanceTracker.TimeInFlightState >= _flightTimeThreshold;
            bool playerInValidState = _currentPlayerState == PlayerState.Flying ||
                                    _currentPlayerState == PlayerState.Flapping;

            if (performanceTrigger && flightTimeTrigger && playerInValidState)
            {
                TransitionToState(ShadowState.Perching);

                if (_debugMode)
                {
                    Debug.Log($"[ShadowAI] Triggered perching - Performance: {_performanceTracker.CurrentPerformanceScore:F1}, " +
                             $"Flight Time: {_performanceTracker.TimeInFlightState:F1}");
                }
            }
        }

        /// <summary>
        /// Evaluates stalking state behavior and potential transitions.
        /// </summary>
        private void EvaluateStalkingState()
        {
            // Basic stalking duration for MVP - will be enhanced in later phases
            float stalkingDuration = 2f;

            if (Time.time - _lastAggroTime > stalkingDuration)
            {
                TransitionToState(ShadowState.Dormant);
                StartCooldown();

                if (_debugMode)
                {
                    Debug.Log("[ShadowAI] Stalking complete, returning to dormant");
                }
            }
        }

        /// <summary>
        /// Evaluates perching state behavior and potential transitions.
        /// </summary>
        private void EvaluatePerchingState()
        {
            // Perching evaluation is handled in UpdatePerchBehavior()
            // This method is for future enhancements like early exit conditions

            // Example: Exit perching if player performance drops significantly
            if (_performanceTracker != null &&
                _performanceTracker.CurrentPerformanceScore < _goodPerformanceThreshold * 0.5f)
            {
                if (_debugMode)
                {
                    Debug.Log("[ShadowAI] Player performance dropped, ending perch early");
                }

                TransitionToState(ShadowState.Dormant);
                StartCooldown();
            }
        }

        /// <summary>
        /// Evaluates telegraphing state behavior and transitions to attack.
        /// </summary>
        private void EvaluateTelegraphingState()
        {
            // Telegraphing evaluation is handled by ShadowTelegraphController
            // This method handles any additional conditions during telegraph phase

            // Telegraph controller will call OnTelegraphCompleted when ready to attack
        }

        /// <summary>
        /// Evaluates attacking state behavior during dash execution.
        /// </summary>
        private void EvaluateAttackingState()
        {
            // Attack evaluation is handled in UpdateDashBehavior()
            // This method can handle emergency exit conditions

            // Emergency exit if player moves too far away during attack
            if (!_isDashing)
            {
                // Attack completed or failed, transition should have been handled
                if (_debugMode)
                {
                    Debug.Log("[ShadowAI] Attack state without active dash - transitioning to retreat");
                }
                TransitionToState(ShadowState.Retreating);
            }
        }

        /// <summary>
        /// Evaluates retreating state behavior and return to dormant.
        /// </summary>
        private void EvaluateRetreatingState()
        {
            // Simple retreat logic - return to dormant after brief retreat period
            float retreatDuration = 1.5f;

            if (Time.time - _lastAggroTime > retreatDuration)
            {
                TransitionToState(ShadowState.Dormant);
                StartCooldown();

                if (_debugMode)
                {
                    Debug.Log("[ShadowAI] Retreat complete, returning to dormant");
                }
            }
        }

        /// <summary>
        /// Transitions Shadow AI to a new state with proper cleanup.
        /// </summary>
        /// <param name="newState">Target state to transition to</param>
        private void TransitionToState(ShadowState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            ShadowState previousState = _currentState;
            _currentState = newState;

            // Handle state entry logic
            switch (newState)
            {
                case ShadowState.Stalking:
                    OnEnterStalking();
                    break;

                case ShadowState.Perching:
                    OnEnterPerching();
                    break;

                case ShadowState.Telegraphing:
                    OnEnterTelegraphing();
                    break;

                case ShadowState.Attacking:
                    OnEnterAttacking();
                    break;

                case ShadowState.Retreating:
                    OnEnterRetreating();
                    break;

                case ShadowState.Dormant:
                    OnEnterDormant();
                    break;
            }

            if (_debugMode)
            {
                Debug.Log($"[ShadowAI] State transition: {previousState} -> {newState}");
            }
        }

        /// <summary>
        /// Handles entry into stalking state.
        /// </summary>
        private void OnEnterStalking()
        {
            _lastAggroTime = Time.time;

            // Phase 1 MVP: Basic stalking behavior
            // Enhanced behavior will be added in Phase 2-4
        }

        /// <summary>
        /// Handles entry into perching state.
        /// </summary>
        private void OnEnterPerching()
        {
            _perchStartTime = Time.time;
            _perchDuration = Random.Range(_minPerchDuration, _maxPerchDuration);
            _perchStateTimer = 0f;

            // Select random perch point
            if (_perchPoints != null && _perchPoints.Length > 0)
            {
                _currentPerchIndex = Random.Range(0, _perchPoints.Length);
                Transform targetPerchTransform = _perchPoints[_currentPerchIndex];

                if (targetPerchTransform != null)
                {
                    // Store perch position (will be refreshed each frame in MoveToCurrentPerch)
                    _targetPerchPosition = targetPerchTransform.position;
                    _isMovingToPerch = true;

                    if (_debugMode)
                    {
                        Debug.Log($"[ShadowAI] Starting perch at point {_currentPerchIndex} " +
                                 $"({_targetPerchPosition}) for {_perchDuration:F1}s");
                    }
                }
                else
                {
                    if (_debugMode)
                    {
                        Debug.LogWarning($"[ShadowAI] Perch point {_currentPerchIndex} is null!");
                    }
                    // Fallback to stalking if no valid perch point
                    TransitionToState(ShadowState.Stalking);
                }
            }
            else
            {
                if (_debugMode)
                {
                    Debug.LogWarning("[ShadowAI] No perch points configured! Falling back to stalking.");
                }
                // Fallback to stalking if no perch points configured
                TransitionToState(ShadowState.Stalking);
            }

            // Show shadow and set perching animation
            if (_animationController != null)
            {
                _animationController.SetVisibility(1f); // Fully visible
                _animationController.SetPerching(true);
            }
        }

        /// <summary>
        /// Handles entry into telegraphing state.
        /// </summary>
        private void OnEnterTelegraphing()
        {
            // Start telegraph sequence
            StartTelegraph();

            // Ensure shadow is visible during telegraph
            if (_animationController != null)
            {
                _animationController.SetPerching(false);
                _animationController.SetVisibility(1f);
            }

            if (_debugMode)
            {
                Debug.Log("[ShadowAI] Entering telegraphing state - warning player");
            }
        }

        /// <summary>
        /// Handles entry into attacking state and initiates dash attack.
        /// </summary>
        private void OnEnterAttacking()
        {
            InitiateDashAttack();

            if (_debugMode)
            {
                Debug.Log("[ShadowAI] Entering attacking state - initiating dash");
            }
        }

        /// <summary>
        /// Initiates the dash attack sequence toward the player.
        /// Uses player transform reference for continuous tracking during scrolling.
        /// </summary>
        private void InitiateDashAttack()
        {
            // Use cached player transform reference if available
            if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    if (_debugMode)
                    {
                        Debug.LogWarning("[ShadowAI] No player found for dash attack target!");
                    }
                    TransitionToState(ShadowState.Retreating);
                    return;
                }
                _playerTransform = player.transform;
            }

            // Set up dash parameters
            _dashStartPosition = transform.position;
            _dashTargetPosition = _playerTransform.position;

            // Limit dash distance
            Vector3 dashDirection = (_dashTargetPosition - _dashStartPosition).normalized;
            float distanceToPlayer = Vector3.Distance(_dashStartPosition, _dashTargetPosition);

            if (distanceToPlayer > _dashDistance)
            {
                _dashTargetPosition = _dashStartPosition + (dashDirection * _dashDistance);
            }

            // Calculate dash duration based on speed
            float actualDistance = Vector3.Distance(_dashStartPosition, _dashTargetPosition);
            _dashDuration = actualDistance / _dashSpeed;

            // Initialize dash state
            _dashStartTime = Time.time;
            _isDashing = true;
            _dashHitDetected = false;
            _dashProgress = 0f;

            // Update animation state
            if (_animationController != null)
            {
                _animationController.SetPerching(false);
                _animationController.SetAttacking(true);
            }

            if (_debugMode)
            {
                Debug.Log($"[ShadowAI] Dash attack initiated - Distance: {actualDistance:F1}, Duration: {_dashDuration:F2}s");
            }
        }

        /// <summary>
        /// Handles entry into retreating state.
        /// </summary>
        private void OnEnterRetreating()
        {
            // Clean up attack state
            _isDashing = false;
            _dashProgress = 0f;
            _dashHitDetected = false;

            // Update animation state
            if (_animationController != null)
            {
                _animationController.SetAttacking(false);
                // Shadow remains visible during retreat then fades when returning to dormant
            }

            if (_debugMode)
            {
                Debug.Log("[ShadowAI] Entering retreating state");
            }
        }

        /// <summary>
        /// Handles entry into dormant state.
        /// </summary>
        private void OnEnterDormant()
        {
            // Clean up any active shadow effects
            // Reset perching state variables
            _currentPerchIndex = -1;
            _perchStateTimer = 0f;
            _isMovingToPerch = false;
            _targetPerchPosition = Vector3.zero;

            // Hide shadow completely when dormant
            if (_animationController != null)
            {
                _animationController.SetVisibility(0f); // Fully invisible
                _animationController.SetPerching(false);
                _animationController.SetAttacking(false);
                _animationController.PlayIdle();
            }

            // Move shadow to dormant offset position (off-screen)
            if (_playerTransform != null && _enableFollowAnchor)
            {
                transform.position = _playerTransform.position + _dormantLocalOffset;
            }
        }

        /// <summary>
        /// Starts cooldown period before next aggression cycle.
        /// </summary>
        private void StartCooldown()
        {
            _nextAggroTime = Time.time + _aggroCooldown;

            if (_debugMode)
            {
                Debug.Log($"[ShadowAI] Starting cooldown for {_aggroCooldown} seconds");
            }
        }

        /// <summary>
        /// Forces Shadow AI into dormant state (for debugging or emergency stops).
        /// </summary>
        public void ForceDormant()
        {
            TransitionToState(ShadowState.Dormant);
            StartCooldown();
        }

        /// <summary>
        /// Forces Shadow AI to become immediately aggressive (for testing).
        /// </summary>
        [ContextMenu("Force Aggro (Debug)")]
        public void ForceAggro()
        {
            if (_debugMode)
            {
                _nextAggroTime = 0f;
                TransitionToState(ShadowState.Stalking);
                Debug.Log("[ShadowAI] Forced aggro activation");
            }
        }

        /// <summary>
        /// Outputs debug information to console when debug mode is enabled.
        /// </summary>
        private void LogDebugInfo()
        {
            if (Time.frameCount % 60 == 0) // Log once per second at 60 FPS
            {
                string perfInfo = _performanceTracker != null ?
                    $"Perf: {_performanceTracker.CurrentPerformanceScore:F1}" : "No Tracker";

                Debug.Log($"[ShadowAI] State: {_currentState}, {perfInfo}, " +
                         $"Cooldown: {_currentCooldownTimer:F1}s, Player: {_currentPlayerState}");
            }
        }

        /// <summary>
        /// Gets the number of valid perch points configured.
        /// </summary>
        public int GetValidPerchPointCount()
        {
            if (_perchPoints == null)
            {
                return 0;
            }

            int validCount = 0;
            foreach (Transform point in _perchPoints)
            {
                if (point != null)
                {
                    validCount++;
                }
            }
            return validCount;
        }

        /// <summary>
        /// Initializes telegraph controller integration.
        /// </summary>
        private void InitializeTelegraphIntegration()
        {
            if (_telegraphController == null)
            {
                _telegraphController = GetComponent<ShadowTelegraphController>();
            }

            if (_telegraphController != null)
            {
                // Subscribe to telegraph events
                _telegraphController.OnTelegraphStarted += OnTelegraphStarted;
                _telegraphController.OnTelegraphCompleted += OnTelegraphCompleted;
                _telegraphController.OnTelegraphCancelled += OnTelegraphCancelled;
            }
        }

        /// <summary>
        /// Triggers telegraph sequence for attack warning.
        /// Called when transitioning to telegraphing state.
        /// </summary>
        public void StartTelegraph()
        {
            if (_telegraphController != null)
            {
                _telegraphController.StartTelegraph();
            }
            else if (_debugMode)
            {
                Debug.LogWarning("[ShadowAI] Telegraph controller not configured!");
            }
        }

        /// <summary>
        /// Cancels current telegraph sequence.
        /// </summary>
        public void CancelTelegraph()
        {
            if (_telegraphController != null)
            {
                _telegraphController.CancelTelegraph();
            }
        }

        /// <summary>
        /// Called when telegraph sequence starts.
        /// </summary>
        private void OnTelegraphStarted()
        {
            _isTelegraphing = true;

            if (_debugMode)
            {
                Debug.Log("[ShadowAI] Telegraph sequence started");
            }
        }

        /// <summary>
        /// Called when telegraph sequence completes.
        /// </summary>
        private void OnTelegraphCompleted()
        {
            _isTelegraphing = false;

            // Telegraph complete - proceed to attack
            TransitionToState(ShadowState.Attacking);

            if (_debugMode)
            {
                Debug.Log("[ShadowAI] Telegraph completed, initiating attack");
            }
        }

        /// <summary>
        /// Called when telegraph sequence is cancelled.
        /// </summary>
        private void OnTelegraphCancelled()
        {
            _isTelegraphing = false;

            if (_debugMode)
            {
                Debug.Log("[ShadowAI] Telegraph cancelled");
            }
        }

        #region Follow Anchor System

        /// <summary>
        /// Initializes the follow anchor system for player tracking.
        /// </summary>
        private void InitializeFollowAnchor()
        {
            if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _playerTransform = player.transform;
                    if (_debugMode)
                    {
                        Debug.Log("[ShadowAI] Auto-found player transform");
                    }
                }
                else
                {
                    Debug.LogWarning("[ShadowAI] Player transform not found! Shadow AI will not function correctly.");
                }
            }

            if (_cameraTransform == null)
            {
                _cameraTransform = Camera.main?.transform;
                if (_cameraTransform != null && _debugMode)
                {
                    Debug.Log("[ShadowAI] Auto-found camera transform");
                }
            }
        }

        /// <summary>
        /// Updates shadow position to follow player anchor every frame.
        /// Prevents world-space drift in scrolling runner environment.
        /// </summary>
        private void UpdateFollowAnchor()
        {
            if (!_enableFollowAnchor || _playerTransform == null)
            {
                return;
            }

            // Dash attacks override follow anchor (they handle their own world-space movement)
            if (_isDashing)
            {
                return;
            }

            // During perching and telegraphing, maintain player-relative position
            // Shadow stays at its current world position but continuously re-anchors to player
            // This is the key fix: we refresh the target position each frame based on current player position
            if (_currentState == ShadowState.Perching && _isMovingToPerch)
            {
                // Perch targets are refreshed each frame to stay relative to player
                // This is handled in MoveToCurrentPerch() now
                return;
            }
        }

        #endregion

        #region Animation & Visibility Control

        /// <summary>
        /// Initializes the animation controller and auto-finds if not assigned.
        /// </summary>
        private void InitializeAnimationController()
        {
            if (_animationController == null)
            {
                _animationController = GetComponent<ShadowAnimationController>();
                if (_debugMode && _animationController != null)
                {
                    Debug.Log("[ShadowAI] Auto-found animation controller");
                }
            }

            if (_shadowRenderer == null)
            {
                _shadowRenderer = GetComponentInChildren<SpriteRenderer>();
                if (_debugMode && _shadowRenderer != null)
                {
                    Debug.Log("[ShadowAI] Auto-found sprite renderer in children");
                }
            }
        }

        /// <summary>
        /// Initializes render layering for shadow sprite.
        /// </summary>
        private void InitializeRenderLayering()
        {
            if (_shadowRenderer != null)
            {
                _shadowRenderer.sortingLayerName = _sortingLayerName;
                _shadowRenderer.sortingOrder = _sortingOrder;

                if (_debugMode)
                {
                    Debug.Log($"[ShadowAI] Render layering configured: Layer={_sortingLayerName}, Order={_sortingOrder}");
                }
            }
        }

        #endregion

        #region Regression Instrumentation

        /// <summary>
        /// Monitors distance between shadow and player to detect anchor drift.
        /// </summary>
        private void UpdateDistanceMonitoring()
        {
            if (!_enableDistanceMonitoring || _playerTransform == null)
            {
                return;
            }

            _currentPlayerDistance = Vector3.Distance(transform.position, _playerTransform.position);
            _maxRecordedDistance = Mathf.Max(_maxRecordedDistance, _currentPlayerDistance);
            _anchorSystemHealthy = _currentPlayerDistance <= _distanceWarningThreshold;

            if (!_anchorSystemHealthy && _debugMode)
            {
                Debug.LogWarning($"[ShadowAI] Anchor drift detected! Distance from player: {_currentPlayerDistance:F1} (max: {_maxRecordedDistance:F1})");
            }
        }

        #endregion

        #region Advanced Behavior Patterns (Phase 4)

        [Header("Advanced Behavior")]
        [Tooltip("Maximum consecutive attacks before forced cooldown")]
        [SerializeField] private int _maxConsecutiveAttacks = 3;
        [Tooltip("Multiplier for aggression when player successfully dodges")]
        [SerializeField] private float _dodgeEscalationMultiplier = 1.2f;
        [Tooltip("Enable perching on obstacle objects")]
        [SerializeField] private bool _enableObstaclePerching = true;
        [Tooltip("Duration of taunt animation after failed attack")]
        [SerializeField] private float _tauntDuration = 2f;
        [Tooltip("Audio controller for shadow sounds and music cues")]
        [SerializeField] private ShadowAudioController _audioController;

        [Header("Animation & Visibility")]
        [Tooltip("Animation controller for shadow sprite visibility and state animations")]
        [SerializeField] private ShadowAnimationController _animationController;
        [Tooltip("Sprite renderer for shadow visual (for sorting layer control)")]
        [SerializeField] private SpriteRenderer _shadowRenderer;
        [Tooltip("Sorting layer name for shadow sprite")]
        [SerializeField] private string _sortingLayerName = "Default";
        [Tooltip("Sorting order within layer (higher = in front)")]
        [SerializeField] private int _sortingOrder = 10;

        // Advanced behavior state tracking
        private int _consecutiveAttacks = 0;
        private int _consecutiveDodges = 0;
        private float _currentEscalationMultiplier = 1f;
        private bool _isTaunting = false;
        private float _tauntStartTime = 0f;

        /// <summary>
        /// Handles escalation logic based on player dodge performance.
        /// </summary>
        private void HandleDodgeEscalation()
        {
            _consecutiveDodges++;
            _currentEscalationMultiplier = Mathf.Min(
                _currentEscalationMultiplier * _dodgeEscalationMultiplier,
                3f // Maximum 3x escalation
            );

            if (_debugMode)
            {
                Debug.Log($"[ShadowAI] Dodge escalation: {_consecutiveDodges} dodges, " +
                         $"multiplier: {_currentEscalationMultiplier:F2}x");
            }

            // Trigger audio for dodge
            _audioController?.TriggerShadowDodged();
        }

        /// <summary>
        /// Handles successful hit logic and resets escalation.
        /// </summary>
        private void HandleSuccessfulHit()
        {
            _consecutiveDodges = 0;
            _currentEscalationMultiplier = 1f;
            _consecutiveAttacks++;

            if (_debugMode)
            {
                Debug.Log($"[ShadowAI] Successful hit! Consecutive attacks: {_consecutiveAttacks}");
            }

            // Trigger audio for hit
            _audioController?.TriggerShadowHit();

            // Check if max consecutive attacks reached
            if (_consecutiveAttacks >= _maxConsecutiveAttacks)
            {
                StartExtendedCooldown();
            }
        }

        /// <summary>
        /// Starts an extended cooldown period after max consecutive attacks.
        /// </summary>
        private void StartExtendedCooldown()
        {
            _consecutiveAttacks = 0;
            float extendedCooldown = _aggroCooldown * 2f; // Double normal cooldown
            _nextAggroTime = Time.time + extendedCooldown;

            if (_debugMode)
            {
                Debug.Log($"[ShadowAI] Extended cooldown started: {extendedCooldown}s");
            }
        }

        /// <summary>
        /// Initiates taunt behavior after failed attack.
        /// </summary>
        private void StartTaunt()
        {
            if (_tauntDuration <= 0f) return;

            _isTaunting = true;
            _tauntStartTime = Time.time;

            if (_debugMode)
            {
                Debug.Log($"[ShadowAI] Starting taunt for {_tauntDuration}s");
            }

            // Trigger taunt audio
            _audioController?.TriggerShadowTaunt();
        }

        /// <summary>
        /// Updates taunt behavior during retreating state.
        /// </summary>
        private void UpdateTaunt()
        {
            if (!_isTaunting) return;

            if (Time.time >= _tauntStartTime + _tauntDuration)
            {
                _isTaunting = false;

                if (_debugMode)
                {
                    Debug.Log("[ShadowAI] Taunt completed");
                }
            }
        }

        /// <summary>
        /// Basic aggro evaluation based on player performance and cooldown.
        /// </summary>
        private bool ShouldBecomeAggressive()
        {
            if (!CanBecomeAggressive) return false;
            if (_performanceTracker == null) return false;

            // Check basic trigger conditions
            bool highPerformance = _performanceTracker.CurrentPerformanceScore >= _goodPerformanceThreshold;
            bool longFlightTime = _performanceTracker.TimeInFlightState >= _flightTimeThreshold;

            return highPerformance || longFlightTime;
        }

        /// <summary>
        /// Enhanced aggro evaluation with escalation consideration.
        /// </summary>
        private bool ShouldBecomeAggressiveAdvanced()
        {
            if (!ShouldBecomeAggressive()) return false;

            // Apply escalation multiplier to thresholds
            float adjustedPerformanceThreshold = _goodPerformanceThreshold / _currentEscalationMultiplier;
            float adjustedFlightTimeThreshold = _flightTimeThreshold / _currentEscalationMultiplier;

            bool escalatedCondition = _performanceTracker.CurrentPerformanceScore >= adjustedPerformanceThreshold ||
                                    _performanceTracker.TimeInFlightState >= adjustedFlightTimeThreshold;

            if (_debugMode && escalatedCondition && _currentEscalationMultiplier > 1f)
            {
                Debug.Log($"[ShadowAI] Escalated aggro triggered! " +
                         $"Threshold adjusted by {_currentEscalationMultiplier:F2}x");
            }

            return escalatedCondition;
        }

        /// <summary>
        /// Enhanced state transition handling with audio integration.
        /// </summary>
        private void TransitionToStateAdvanced(ShadowState newState)
        {
            ShadowState previousState = _currentState;
            TransitionToState(newState);

            // Trigger appropriate audio events
            switch (newState)
            {
                case ShadowState.Stalking:
                    _audioController?.TriggerShadowAppear();
                    break;
                case ShadowState.Perching:
                    _audioController?.TriggerShadowPerching();
                    break;
                case ShadowState.Telegraphing:
                    _audioController?.TriggerShadowTelegraph();
                    break;
                case ShadowState.Attacking:
                    _audioController?.TriggerShadowAttack();
                    break;
                case ShadowState.Retreating:
                    _audioController?.TriggerShadowRetreat();
                    break;
            }
        }

        /// <summary>
        /// Handles obstacle perching behavior when enabled.
        /// </summary>
        private void HandleObstaclePerching()
        {
            if (!_enableObstaclePerching) return;

            // Find nearby obstacles tagged for perching
            GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
            if (obstacles.Length == 0) return;

            // Find closest obstacle within reasonable distance
            Transform closestObstacle = null;
            float closestDistance = float.MaxValue;
            float maxPerchDistance = 15f;

            foreach (GameObject obstacle in obstacles)
            {
                float distance = Vector3.Distance(transform.position, obstacle.transform.position);
                if (distance < closestDistance && distance <= maxPerchDistance)
                {
                    closestDistance = distance;
                    closestObstacle = obstacle.transform;
                }
            }

            // Move to obstacle perch position
            if (closestObstacle != null)
            {
                Vector3 perchOffset = Vector3.up * 2f; // Perch above obstacle
                _targetPerchPosition = closestObstacle.position + perchOffset;
                _isMovingToPerch = true;

                if (_debugMode)
                {
                    Debug.Log($"[ShadowAI] Perching on obstacle: {closestObstacle.name}");
                }
            }
        }

        /// <summary>
        /// Gets current advanced behavior status for debugging.
        /// </summary>
        public AdvancedBehaviorStatus GetAdvancedBehaviorStatus()
        {
            return new AdvancedBehaviorStatus
            {
                ConsecutiveAttacks = _consecutiveAttacks,
                ConsecutiveDodges = _consecutiveDodges,
                EscalationMultiplier = _currentEscalationMultiplier,
                IsTaunting = _isTaunting,
                TauntTimeRemaining = _isTaunting ? (_tauntStartTime + _tauntDuration - Time.time) : 0f,
                ObstaclePerchingEnabled = _enableObstaclePerching
            };
        }

        #endregion

        #region Testing and Balance Tuning (Task 4.3)

        [Header("Testing and Balance")]
        [Tooltip("Enable comprehensive testing mode with detailed logging")]
        [SerializeField] private bool _enableTestingMode = false;
        [Tooltip("Enable performance profiling for Shadow AI systems")]
        [SerializeField] private bool _enablePerformanceProfiling = false;
        [Tooltip("Enable balance tuning helper that suggests optimal parameters")]
        [SerializeField] private bool _enableBalanceTuning = false;

        // Testing state tracking
        private float _testSessionStartTime;
        private int _testAttacksExecuted = 0;
        private int _testHitsLanded = 0;
        private float _averageResponseTime = 0f;
        private float _totalResponseTime = 0f;

        /// <summary>
        /// Initializes testing systems if enabled.
        /// </summary>
        private void InitializeTestingSystems()
        {
            if (_enableTestingMode)
            {
                _testSessionStartTime = Time.time;
                Debug.Log("[ShadowAI] Testing mode enabled - comprehensive logging active");
            }

            if (_enablePerformanceProfiling)
            {
                Debug.Log("[ShadowAI] Performance profiling enabled");
            }

            if (_enableBalanceTuning)
            {
                Debug.Log("[ShadowAI] Balance tuning helper enabled");
                LogBalanceTuningRecommendations();
            }
        }

        /// <summary>
        /// Validates all Shadow AI systems and reports any configuration issues.
        /// </summary>
        [ContextMenu("Run System Validation")]
        public void RunSystemValidation()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ShadowAI] System validation should be run in Play mode");
                return;
            }

            Debug.Log("=== Shadow AI System Validation ===");

            // Component validation
            bool hasPerformanceTracker = _performanceTracker != null;
            bool hasTelegraphController = _telegraphController != null;
            bool hasAudioController = _audioController != null;
            bool hasValidPerchPoints = GetValidPerchPointCount() > 0;

            Debug.Log($"Performance Tracker: {(hasPerformanceTracker ? "✓" : "✗")}");
            Debug.Log($"Telegraph Controller: {(hasTelegraphController ? "✓" : "✗")}");
            Debug.Log($"Audio Controller: {(hasAudioController ? "✓" : "✗")}");
            Debug.Log($"Valid Perch Points: {GetValidPerchPointCount()}");

            // Configuration validation
            bool validCooldown = _aggroCooldown >= 5f;
            bool validThresholds = _goodPerformanceThreshold > 0 && _flightTimeThreshold > 0;
            bool validDashParams = _dashSpeed > 0 && _dashDistance > 0;
            bool validPenalties = _powerPenalty >= 0 && _staminaPenalty >= 0;

            Debug.Log($"Cooldown Settings: {(validCooldown ? "✓" : "⚠")} ({_aggroCooldown}s)");
            Debug.Log($"Trigger Thresholds: {(validThresholds ? "✓" : "✗")}");
            Debug.Log($"Dash Parameters: {(validDashParams ? "✓" : "✗")}");
            Debug.Log($"Penalty Settings: {(validPenalties ? "✓" : "✗")}");

            // System integration test
            if (hasPerformanceTracker && hasValidPerchPoints)
            {
                Debug.Log("System Integration: ✓ Ready for gameplay");
            }
            else
            {
                Debug.LogWarning("System Integration: ⚠ Missing critical components");
            }

            Debug.Log("=== Validation Complete ===");
        }

        /// <summary>
        /// Tests all trigger conditions and reports current state.
        /// </summary>
        [ContextMenu("Test Trigger Conditions")]
        public void TestTriggerConditions()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ShadowAI] Trigger testing should be run in Play mode");
                return;
            }

            Debug.Log("=== Trigger Condition Testing ===");

            if (_performanceTracker != null)
            {
                float score = _performanceTracker.CurrentPerformanceScore;
                float flightTime = _performanceTracker.TimeInFlightState;
                bool highPerf = _performanceTracker.IsHighPerformance;

                Debug.Log($"Performance Score: {score:F1}/100");
                Debug.Log($"Flight Time: {flightTime:F1}s (threshold: {_flightTimeThreshold}s)");
                Debug.Log($"High Performance: {highPerf}");
                Debug.Log($"Escalation Multiplier: {_currentEscalationMultiplier:F2}x");

                bool shouldTrigger = ShouldBecomeAggressive();
                bool advancedTrigger = ShouldBecomeAggressiveAdvanced();

                Debug.Log($"Basic Trigger: {(shouldTrigger ? "✓" : "✗")}");
                Debug.Log($"Advanced Trigger: {(advancedTrigger ? "✓" : "✗")}");
            }
            else
            {
                Debug.LogError("No performance tracker - cannot test triggers");
            }

            Debug.Log("=== Testing Complete ===");
        }

        /// <summary>
        /// Logs balance tuning recommendations based on current configuration.
        /// </summary>
        private void LogBalanceTuningRecommendations()
        {
            Debug.Log("=== Balance Tuning Recommendations ===");

            // Aggro cooldown recommendations
            if (_aggroCooldown < 8f)
            {
                Debug.Log("⚠ Aggro cooldown may be too short - consider 8-12s for better pacing");
            }
            else if (_aggroCooldown > 15f)
            {
                Debug.Log("⚠ Aggro cooldown may be too long - players might lose engagement");
            }

            // Performance threshold recommendations
            if (_goodPerformanceThreshold < 60f)
            {
                Debug.Log("⚠ Performance threshold very low - Shadow may appear too frequently");
            }
            else if (_goodPerformanceThreshold > 85f)
            {
                Debug.Log("⚠ Performance threshold very high - Shadow may rarely appear");
            }

            // Telegraph timing recommendations
            if (_dashTelegraphTime < 0.5f)
            {
                Debug.Log("⚠ Telegraph time very short - may not give players enough reaction time");
            }
            else if (_dashTelegraphTime > 1.2f)
            {
                Debug.Log("⚠ Telegraph time very long - may reduce tension and surprise");
            }

            // Dash speed vs distance balance
            float dashTime = _dashDistance / _dashSpeed;
            if (dashTime < 0.3f)
            {
                Debug.Log("⚠ Dash may be too fast to dodge - consider slower speed or shorter distance");
            }
            else if (dashTime > 1f)
            {
                Debug.Log("⚠ Dash may be too slow - consider faster speed or longer distance");
            }

            Debug.Log("=== Recommendations Complete ===");
        }

        /// <summary>
        /// Profiles Shadow AI performance and reports timing statistics.
        /// </summary>
        private void UpdatePerformanceProfiling()
        {
            if (!_enablePerformanceProfiling) return;

            // Log performance statistics periodically
            if (Time.frameCount % 300 == 0) // Every 5 seconds at 60 FPS
            {
                var status = GetAdvancedBehaviorStatus();
                float sessionTime = Time.time - _testSessionStartTime;
                float avgResponseTime = _testAttacksExecuted > 0 ? _totalResponseTime / _testAttacksExecuted : 0f;
                float hitRate = _testAttacksExecuted > 0 ? (_testHitsLanded / (float)_testAttacksExecuted) * 100f : 0f;

                Debug.Log($"[ShadowAI Profile] Session: {sessionTime:F1}s | " +
                         $"Attacks: {_testAttacksExecuted} | Hit Rate: {hitRate:F1}% | " +
                         $"Avg Response: {avgResponseTime:F2}s | Escalation: {status.EscalationMultiplier:F2}x");
            }
        }

        /// <summary>
        /// Records attack statistics for testing analysis.
        /// </summary>
        private void RecordAttackStatistics(bool wasHit, float responseTime)
        {
            if (!_enableTestingMode) return;

            _testAttacksExecuted++;
            if (wasHit) _testHitsLanded++;

            _totalResponseTime += responseTime;
            _averageResponseTime = _totalResponseTime / _testAttacksExecuted;

            if (_debugMode)
            {
                Debug.Log($"[ShadowAI Test] Attack #{_testAttacksExecuted}: " +
                         $"{(wasHit ? "HIT" : "DODGE")} | Response: {responseTime:F2}s");
            }
        }

        /// <summary>
        /// Generates comprehensive test report for balance tuning.
        /// </summary>
        [ContextMenu("Generate Test Report")]
        public void GenerateTestReport()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ShadowAI] Test report should be generated in Play mode");
                return;
            }

            Debug.Log("=== Shadow AI Test Report ===");

            float sessionTime = Time.time - _testSessionStartTime;
            float attackFrequency = _testAttacksExecuted > 0 ? sessionTime / _testAttacksExecuted : 0f;
            float hitRate = _testAttacksExecuted > 0 ? (_testHitsLanded / (float)_testAttacksExecuted) * 100f : 0f;

            var status = GetAdvancedBehaviorStatus();
            var audioConfig = _audioController?.GetAudioConfiguration();

            Debug.Log($"Session Duration: {sessionTime:F1} seconds");
            Debug.Log($"Total Attacks: {_testAttacksExecuted}");
            Debug.Log($"Successful Hits: {_testHitsLanded}");
            Debug.Log($"Hit Rate: {hitRate:F1}%");
            Debug.Log($"Average Response Time: {_averageResponseTime:F2}s");
            Debug.Log($"Attack Frequency: {attackFrequency:F1}s between attacks");
            Debug.Log($"Current Escalation: {status.EscalationMultiplier:F2}x");
            Debug.Log($"Consecutive Attacks: {status.ConsecutiveAttacks}");
            Debug.Log($"Consecutive Dodges: {status.ConsecutiveDodges}");

            if (audioConfig.HasValue)
            {
                var audio = audioConfig.Value;
                Debug.Log($"Audio Events Triggered: {audio.EventsTriggeredThisSession}");
            }

            // Balance recommendations
            if (hitRate < 20f)
            {
                Debug.Log("⚠ RECOMMENDATION: Hit rate low - consider shorter telegraph time or faster dash");
            }
            else if (hitRate > 60f)
            {
                Debug.Log("⚠ RECOMMENDATION: Hit rate high - consider longer telegraph time or player speed buffs");
            }
            else
            {
                Debug.Log("✓ Hit rate appears balanced");
            }

            Debug.Log("=== Report Complete ===");
        }

        #endregion

        /// <summary>
        /// Validates configuration and provides Inspector feedback.
        /// </summary>
        private void OnValidate()
        {
            if (_performanceTracker == null)
            {
                Debug.LogWarning("[ShadowAI] Performance tracker not assigned! " +
                               "Shadow AI will not function without performance data.");
            }

            if (_aggroCooldown < 1f)
            {
                Debug.LogWarning("[ShadowAI] Aggro cooldown is very short. " +
                               "Consider increasing for better player experience.");
            }

            if (_perchPoints != null && _perchPoints.Length == 0)
            {
                Debug.LogWarning("[ShadowAI] No perch points configured! " +
                               "Shadow will fall back to basic stalking behavior.");
            }

            if (_minPerchDuration >= _maxPerchDuration)
            {
                Debug.LogWarning("[ShadowAI] Min perch duration should be less than max duration!");
            }

            if (_perchTransitionSpeed <= 0f)
            {
                Debug.LogWarning("[ShadowAI] Perch transition speed should be positive!");
            }

            if (_telegraphController == null)
            {
                Debug.LogWarning("[ShadowAI] Telegraph controller not assigned! " +
                               "Telegraph and screen shake effects will be disabled.");
            }

            if (_dashTelegraphTime <= 0f)
            {
                Debug.LogWarning("[ShadowAI] Telegraph time should be positive!");
            }

            if (_screenShakeIntensity < 0f)
            {
                Debug.LogWarning("[ShadowAI] Screen shake intensity should not be negative!");
            }

            if (_dashSpeed <= 0f)
            {
                Debug.LogWarning("[ShadowAI] Dash speed should be positive!");
            }

            if (_dashDistance <= 0f)
            {
                Debug.LogWarning("[ShadowAI] Dash distance should be positive!");
            }

            if (_hitDetectionRadius <= 0f)
            {
                Debug.LogWarning("[ShadowAI] Hit detection radius should be positive!");
            }

            if (_powerPenalty < 0)
            {
                Debug.LogWarning("[ShadowAI] Power penalty should not be negative!");
            }

            if (_staminaPenalty < 0f)
            {
                Debug.LogWarning("[ShadowAI] Stamina penalty should not be negative!");
            }

            if (_speedResetMultiplier < 0f || _speedResetMultiplier > 1f)
            {
                Debug.LogWarning("[ShadowAI] Speed reset multiplier should be between 0 and 1!");
            }
        }

        /// <summary>
        /// Draws debug visualization for dash attack hit detection.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!_debugDrawHitbox || !_isDashing)
            {
                return;
            }

            // Draw hit detection radius during dash
            Gizmos.color = _dashHitDetected ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _hitDetectionRadius);

            // Draw dash path
            if (_isDashing)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_dashStartPosition, _dashTargetPosition);

                // Draw progress indicator
                Vector3 currentPos = Vector3.Lerp(_dashStartPosition, _dashTargetPosition, _dashProgress);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(currentPos, Vector3.one * 0.2f);
            }
        }
    }

    /// <summary>
    /// Shadow AI state machine enumeration.
    /// States will be expanded in Phase 2-4 of implementation.
    /// </summary>
    public enum ShadowState
    {
        Dormant,        // Not active, waiting for triggers
        Stalking,       // Monitoring player, preparing to act
        Perching,       // Positioned on UI, building tension (Phase 2)
        Telegraphing,   // Preparing attack with clear warning (Phase 3)
        Attacking,      // Executing dash attack (Phase 3)
        Retreating      // Withdrawing after attack attempt (Phase 4)
    }

    /// <summary>
    /// Data structure for advanced Shadow AI behavior status information.
    /// </summary>
    [System.Serializable]
    public struct AdvancedBehaviorStatus
    {
        public int ConsecutiveAttacks;
        public int ConsecutiveDodges;
        public float EscalationMultiplier;
        public bool IsTaunting;
        public float TauntTimeRemaining;
        public bool ObstaclePerchingEnabled;
    }
}