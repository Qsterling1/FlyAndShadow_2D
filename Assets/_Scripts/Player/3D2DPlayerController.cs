using UnityEngine;
using System;
using System.Collections;
using FlyShadow.EventBus;

/// <summary>
/// 3D2D Player Controller - Uses 3D character model with 2D physics for 2.5D gameplay.
/// Animator component must be on a child GameObject.
/// </summary>
[System.Obsolete("Dead 3D2D family. Active player uses PlayerController. Do not wire into new code.")]
public class ThreeD2DPlayerController : MonoBehaviour
{
    // === Private Fields ===
    public static ThreeD2DPlayerController instance;
    private Rigidbody2D _rb;
    private CapsuleCollider2D _capsuleCollider;
    private ThreeD2DPlayerStats _playerStats; // Reference to the ThreeD2DPlayerStats component
    private Animator _animator; // Reference to the Animator component
    private bool _isGrounded;
    private float _timeSinceLeftGround = 0f;
    private bool _isStunned = false;
    [SerializeField] private PlayerState _currentState;

    // State Data
    private Vector2 _originalColliderSize;
    private Vector2 _originalColliderOffset;
    private float _originalRunSpeed;
    private float _minBaseRunSpeed;
    private Vector3 _originalScale;
    private bool _isSpeedBurstActive = false;

    private static readonly PlayerState[] _phaseOrder =
    {
        PlayerState.Running,
        PlayerState.Flapping,
        PlayerState.Flying
    };

    // Advanced Jump State
    private bool _canDoubleJump;
    private bool _isApplyingVariableJumpForce;
    private float _variableJumpTimer;
    private float _timeAtJumpApex;

    // Hang Time State
    private float _defaultGravityScale;

    // Advanced Maneuvers State
    private int _spacebarTaps = 0;
    private float _lastSpacebarTapTime = 0f;
    private bool _isBypassActive = false;
    private bool _isNoseDiveActive = false;
    private int _playerLayerIndex;
    private int _obstacleLayerIndex;
    // Animation State Hashes (for performance)
    private readonly int _animStateIdle = Animator.StringToHash("idle");
    private readonly int _animStateRun = Animator.StringToHash("run");
    private readonly int _animStateJump = Animator.StringToHash("jump");
    private readonly int _animStateFall = Animator.StringToHash("fall");

    // Footstep timing
    private float _lastFootstepTime;
    private readonly int _animStateSlide = Animator.StringToHash("slide");
    private readonly int _animStateFlying = Animator.StringToHash("Flying State");
    private readonly int _animTriggerSpinMove = Animator.StringToHash("Spin");
    private readonly int _animTriggerNoseDive = Animator.StringToHash("NoseDive");
    private readonly int _animTriggerBarrelRoll = Animator.StringToHash("BarrelRoll");


    // === Inspector Fields ===
    [Header("Progression Settings")]
    [Tooltip("The minimum power required to unlock the Double Jump ability.")]
    [SerializeField] private int _requiredPowerForDoubleJump = 1;
    [Tooltip("The minimum power required to unlock the Flapping state.")]
    [SerializeField] private int _requiredPowerForFlapping = 5;
    [Tooltip("The minimum power required to unlock the Flying state.")]
    [SerializeField] private int _requiredPowerForFlying = 15;
    [Header("Enabled Abilities")]
    [Tooltip("Allows the player to perform a double jump.")]
    [SerializeField] private bool _allowDoubleJump = true;
    [Tooltip("Allows the player to slide.")]
    [SerializeField] private bool _allowSlide = true;
    [Tooltip("Allows the player to enter the Flapping state.")]
    [SerializeField] private bool _allowFlapping = true;
    [Tooltip("Allows the player to enter the Flying state.")]
    [SerializeField] private bool _allowFlying = true;
    [Tooltip("Allows the player to use evasive maneuvers like Spin, Nose Dive, and Barrel Roll.")]
    [SerializeField] private bool _allowAdvancedManeuvers = true;

    [Header("State Settings")]
    [Tooltip("The state the player will begin the level in.")]
    [SerializeField] private PlayerState _startingState = PlayerState.Idle;

    [Header("Movement Settings")]
    [Tooltip("The constant forward speed of the player.")]
    [SerializeField] private float _runSpeed = 10f;
    [Tooltip("The vertical force applied when the player jumps from the ground.")]
    [SerializeField] private float _jumpForce = 10f;
    [Tooltip("The absolute maximum speed the player can reach through collectibles.")]
    [SerializeField] private float _maxRunSpeed = 20f;
    [SerializeField] private float _maxJumpForce = 20f;
    [Tooltip("The speed multiplier applied when stamina is fully depleted. < 1 to slow down.")]
    [SerializeField] private float _staminaDepletedSpeedMultiplier = 0.8f;
    [Header("Advanced Jump Settings")]
    [Tooltip("Allow the second jump to be triggered while the player is still ascending from the first jump.")]
    [SerializeField] private bool _allowDoubleJumpInJumpingState = true;
    [Tooltip("Allow the second jump to be triggered after the player has started falling.")]
    [SerializeField] private bool _allowDoubleJumpInFallingState = true;
    [Tooltip("The minimum upward force applied on a double jump (the 'tap' force).")]
    [SerializeField] private float _minDoubleJumpForce = 6f;
    [Tooltip("The maximum upward force that can be achieved by holding the jump button during a double jump.")]
    [SerializeField] private float _maxDoubleJumpForce = 10f;
    [Tooltip("The duration, in seconds, the player can hold the jump button to increase the force of their double jump.")]
    [SerializeField] private float _doubleJumpHoldDuration = 0.2f;

    [Header("Peak Jump Bonus")]
    [Tooltip("Enable a force bonus for timing the double jump at the apex of the first jump.")]
    [SerializeField] private bool _enablePeakJumpBonus = true;
    [Tooltip("The time window, in seconds, around the apex where the bonus can be triggered.")]
    [SerializeField] private float _peakJumpWindow = 0.1f;
    [Tooltip("The multiplier applied to the double jump force when executed within the peak window.")]
    [SerializeField] private float _peakJumpBonusMultiplier = 1.25f;

    [Header("Hang Time Settings")]
    [Tooltip("Enable a 'floaty' effect at the peak of the player's jump by reducing gravity.")]
    [SerializeField] private bool _enableHangTime = true;
    [Tooltip("The gravity scale applied to the player when they are at the apex of their jump.")]
    [SerializeField] private float _hangTimeGravityScale = 1f;
    [Tooltip("The vertical velocity threshold to trigger hang time.")]
    [SerializeField] private float _hangTimeVelocityThreshold = 1.5f;

    [Header("Sliding Settings")]
    [Tooltip("The rate at which stamina is drained per second while sliding.")]
    [SerializeField] private float _slideStaminaDrainRate = 25f;
    [Tooltip("The speed multiplier applied during the slide. 1 is no change, 1.5 is 50% faster.")]
    [SerializeField] private float _slideSpeedMultiplier = 1.2f;
    [Tooltip("The size of the collider when sliding.")]
    [SerializeField] private Vector2 _slideColliderSize = new Vector2(0.5f, 0.2f);
    [Tooltip("The offset of the collider when sliding to keep it grounded.")]
    [SerializeField] private Vector2 _slideColliderOffset = new Vector2(0, -0.1f);

    [Header("Flapping Settings")]
    [Tooltip("The upward force applied with each flap.")]
    [SerializeField] private float _flapForce = 8f;

    [Header("Flying Settings")]
    [Tooltip("A flat speed bonus added when the player first transitions into the Flying state.")]
    [SerializeField] private float _flightTransitionSpeedBonus = 5f;
    [Tooltip("The minimum horizontal speed the player will have when entering the flying state.")]
    [SerializeField] private float _minFlightSpeed = 15f;
    [Tooltip("The vertical speed when controlling the player's flight.")]
    [SerializeField] private float _flightControlSpeed = 8f;
    [Tooltip("The gravity scale applied during flight. 0 is weightless, higher values are heavier.")]
    [SerializeField] private float _flightGravityScale = 0.5f;
    [Tooltip("The speed multiplier for the flight speed burst.")]
    [SerializeField] private float _flightSpeedBurstMultiplier = 1.5f;
    [Tooltip("The rate at which stamina is drained per second during a flight speed burst.")]
    [SerializeField] private float _flightBurstStaminaDrainRate = 30f;

    [Header("Advanced Maneuvers")]
    [Tooltip("The rate at which stamina is drained per second while in an advanced maneuver (Spin, Barrel Roll).")]
    [SerializeField] private float _maneuverStaminaDrainRate = 10f;
    [Tooltip("The duration, in seconds, that the collision bypass (Spin/Barrel Roll) remains active.")]
    [SerializeField] private float _bypassDuration = 0.5f;
    [Tooltip("The time window, in seconds, to detect a triple-tap for the Barrel Roll.")]
    [SerializeField] private float _tripleTapTimeWindow = 0.3f;
    [Tooltip("The name of the player's physics layer.")]
    [SerializeField] private string _playerLayerName = "Player";
    [Tooltip("The name of the obstacle's physics layer.")]
    [SerializeField] private string _obstacleLayerName = "Obstacles";
    [Tooltip("The initial downward force for the Nose Dive.")]
    [SerializeField] private Vector2 _noseDiveForce = new Vector2(5, -10);
    [Tooltip("The duration of the main nose dive before recovery begins.")]
    [SerializeField] private float _noseDiveDuration = 0.3f;
    [Tooltip("The recovery force that provides a small upward boost after the dive.")]
    [SerializeField] private Vector2 _noseDiveRecoveryForce = new Vector2(2, 4);

    [Header("Ground Check Settings")]
    [Tooltip("The layer(s) that should be considered 'ground' for the purposes of jumping.")]
    [SerializeField] private LayerMask _groundLayer;
    [Tooltip("The manual offset for the ground check's position.")]
    [SerializeField] private Vector2 _groundCheckOffset = new Vector2(0, -0.5f);
    [Tooltip("The size of the box used to check for ground.")]
    [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.5f, 0.1f);
    [Tooltip("How far below the player's origin the ground check box is cast.")]
    [SerializeField] private float _groundCheckDistance = 0.1f;
    [Tooltip("How long the player can be airborne before the 'Falling' state is triggered.")]
    [SerializeField] private float _fallBufferTime = 0.1f;

    [Header("Animation Settings")]
    [Tooltip("LIVE value of the current animation state. For debugging only.")]
    [SerializeField] private string _currentAnimationStateName;
[Header("Audio Events")]
    [Tooltip("Event to raise when the player performs their first jump.")]
    [SerializeField] private GameEvent _onPlayerJumped;
    [Tooltip("Event raised when the player performs a double jump.")]
    [SerializeField] private GameEvent _onPlayerDoubleJumped;
    [Tooltip("Event raised when the player lands on the ground.")]
    [SerializeField] private GameEvent _onPlayerSlide;

    [SerializeField] private GameEvent _onPlayerLanded;
    [Tooltip("Event raised for each running footstep.")]

    [SerializeField] private GameEvent _onPlayerRunStep;
    [Tooltip("Time between footstep sounds while running.")]
    [SerializeField] private float _footstepInterval = 0.3f;
    [Tooltip("Event raised when the player activates the flight speed burst.")]
    [SerializeField] private GameEvent _onPlayerFlightSpeedBurst;
    

    // === Unity Methods ===
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            EventManager.Subscribe<PlayerSpeedModifiedEvent>(OnSpeedModified);
            EventManager.Subscribe<PlayerScaleRequestedEvent>(OnScaleRequested);
            EventManager.Subscribe<PhaseChangeRequestedEvent>(OnPhaseChangeRequested);
            EventManager.Subscribe<PlayerPowerChangedEvent>(OnPlayerPowerChanged);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        _rb = GetComponent<Rigidbody2D>();
        _capsuleCollider = GetComponent<CapsuleCollider2D>();
        _playerStats = GetComponent<ThreeD2DPlayerStats>();
        _animator = GetComponentInChildren<Animator>();

        _defaultGravityScale = _rb.gravityScale;
        _originalRunSpeed = _runSpeed;
        _minBaseRunSpeed = _runSpeed;

        // Store original collider values for slide swapping
        _originalColliderSize = _capsuleCollider.size;
        _originalColliderOffset = _capsuleCollider.offset;

        _playerLayerIndex = LayerMask.NameToLayer(_playerLayerName);
        _obstacleLayerIndex = LayerMask.NameToLayer(_obstacleLayerName);
        if (_playerLayerIndex == -1 || _obstacleLayerIndex == -1)
        {
            Debug.LogWarning("Player or Obstacle layer not found. Please check layer names in the Inspector and Project Settings.");
        }
        _originalScale = transform.localScale;

        EventManager.Publish(new PlayerPowerThresholdsEvent
        {
            DoubleJump = _requiredPowerForDoubleJump,
            Flapping = _requiredPowerForFlapping,
            Flying = _requiredPowerForFlying
        });
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            EventManager.Unsubscribe<PlayerSpeedModifiedEvent>(OnSpeedModified);
            EventManager.Unsubscribe<PlayerScaleRequestedEvent>(OnScaleRequested);
            EventManager.Unsubscribe<PhaseChangeRequestedEvent>(OnPhaseChangeRequested);
            EventManager.Unsubscribe<PlayerPowerChangedEvent>(OnPlayerPowerChanged);
        }
    }

    /// <summary>
    /// Provides controlled access to the player's Rigidbody2D component.
    /// </summary>
    public Rigidbody2D GetRigidbody() => _rb;

    private void OnSpeedModified(PlayerSpeedModifiedEvent e)
    {
        ModifyRunSpeed(e.Delta);
    }

    private void OnScaleRequested(PlayerScaleRequestedEvent e)
    {
        if (e.Multiplier <= 0f)
        {
            return;
        }

        if (e.Duration <= 0f)
        {
            ApplyPermanentScale(e.Multiplier);
        }
        else
        {
            ApplyTemporarySizeChange(e.Multiplier, e.Duration);
        }
    }

    private void OnPhaseChangeRequested(PhaseChangeRequestedEvent e)
    {
        if (e.Step == 0)
        {
            return;
        }

        ApplyPhaseStep(e.Step);
    }

    private void OnPlayerPowerChanged(PlayerPowerChangedEvent e)
    {
        CheckForPhaseShift();
    }

    private void ApplyPhaseStep(int step)
    {
        int direction = Math.Sign(step);
        int steps = Math.Abs(step);

        for (int i = 0; i < steps; i++)
        {
            var candidate = GetPhaseCandidate(direction);
            if (!candidate.HasValue)
            {
                break;
            }

            ChangeState(candidate.Value);
        }
    }

    private PlayerState? GetPhaseCandidate(int direction)
    {
        PlayerState referenceState = _currentState;
        if (referenceState != PlayerState.Running && referenceState != PlayerState.Flapping && referenceState != PlayerState.Flying)
        {
            referenceState = PlayerState.Running;
        }

        int currentIndex = Array.IndexOf(_phaseOrder, referenceState);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int targetIndex = currentIndex + direction;

        if (direction > 0)
        {
            if (targetIndex >= _phaseOrder.Length)
            {
                return null;
            }

            for (int i = targetIndex; i < _phaseOrder.Length; i++)
            {
                var candidate = _phaseOrder[i];
                if (IsPhaseAllowed(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
        else
        {
            if (targetIndex < 0)
            {
                return PlayerState.Running;
            }

            return _phaseOrder[targetIndex];
        }
    }

    private bool IsPhaseAllowed(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Flying:
                return _allowFlying;
            case PlayerState.Flapping:
                return _allowFlapping;
            case PlayerState.Running:
                return true;
            default:
                return false;
        }
    }

    private void ApplyPermanentScale(float multiplier)
    {
        float originalFootPosition = _capsuleCollider.bounds.min.y;
        transform.localScale = Vector3.Scale(transform.localScale, new Vector3(multiplier, multiplier, 1f));
        float newFootPosition = _capsuleCollider.bounds.min.y;
        float positionCorrection = originalFootPosition - newFootPosition;
        transform.position += new Vector3(0f, positionCorrection, 0f);
        _originalScale = transform.localScale;
    }

    /// <summary>
    /// Applies a temporary speed multiplier to the player.
    /// </summary>
    public void ApplyTemporarySpeedChange(float multiplier, float duration)
    {
        StartCoroutine(TemporarySpeedChangeCoroutine(multiplier, duration));
    }

    private IEnumerator TemporarySpeedChangeCoroutine(float multiplier, float duration)
    {
        float originalSpeed = _originalRunSpeed;
        _runSpeed *= multiplier;
        yield return new WaitForSeconds(duration);
        _runSpeed /= multiplier;
    }

    /// <summary>
    /// Applies a temporary stun effect, stopping player movement.
    /// </summary>
    public void ApplyStun(float duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        _isStunned = true;
        yield return new WaitForSeconds(duration);
        _isStunned = false;
    }

    /// <summary>
    /// Applies a temporary gravity multiplier to the player.
    /// </summary>
    public void ApplyTemporaryGravityChange(float multiplier, float duration)
    {
        StartCoroutine(TemporaryGravityChangeCoroutine(multiplier, duration));
    }

    private IEnumerator TemporaryGravityChangeCoroutine(float multiplier, float duration)
    {
        float originalGravity = _defaultGravityScale;
        _rb.gravityScale = originalGravity * multiplier;
        yield return new WaitForSeconds(duration);
        _rb.gravityScale = originalGravity;
    }

    private void Start()
    {
        ChangeState(_startingState);
        EventManager.Publish(new PlayerStateChangedEvent { State = _currentState });
    }

    private void Update()
    {
        CheckGrounded();
        ManageState();
        HandleVariableJump();
        HandleHangTime();
        HandleSlide();
        UpdateAnimatorParameters();
        HandleAnimations();

        if (_currentState == PlayerState.Flying)
        {
            HandleFlyingSpacebarInput();
        }
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }
    public void CheckForPhaseShift()
    {
        if (_playerStats == null) return;

        int p = _playerStats.CurrentPower;

        // 1) Compute whats allowed at the current power
        bool canDoubleJump = p >= _requiredPowerForDoubleJump;
        bool canFlap = p >= _requiredPowerForFlapping;
        bool canFly = p >= _requiredPowerForFlying;

        _allowDoubleJump = canDoubleJump;
        _allowFlapping = canFlap;
        _allowFlying = canFly;

        // If double jump is no longer allowed, clear the in-air double-jump flag
        if (!canDoubleJump) _canDoubleJump = false;

        // 2) Handle immediate DOWNGRADES if a state is no longer allowed
        if (!canFly && _currentState == PlayerState.Flying)
        {
            ChangeState(canFlap ? PlayerState.Flapping
                                : (_isGrounded ? PlayerState.Running : PlayerState.Falling));
            return;
        }
        if (!canFlap && _currentState == PlayerState.Flapping)
        {
            ChangeState(_isGrounded ? PlayerState.Running : PlayerState.Falling);
            return;
        }

        // 3) (Optional) Auto-upgrades when crossing thresholds upwards
        if (canFly && _currentState == PlayerState.Flapping)
        {
            ChangeState(PlayerState.Flying);
            return;
        }
        if (canFlap && _currentState == PlayerState.Jumping && !_isGrounded)
        {
            ChangeState(PlayerState.Flapping);
        }
    }
    // === State Machine ===
    private void ManageState()
    {
        switch (_currentState)
        {
            case PlayerState.Idle:
                HandleIdleState();
                break;
            case PlayerState.Running:
                HandleRunningState();
                break;
            case PlayerState.Jumping:
                HandleJumpingState();
                break;
            case PlayerState.Falling:
                HandleFallingState();
                break;
            case PlayerState.Sliding:
                HandleSlidingState();
                break;
            case PlayerState.Flapping:
                HandleFlappingState();
                break;
            case PlayerState.Flying:
                HandleFlyingState();
                break;
        }
    }

    private void ChangeState(PlayerState newState)
{
    if (_currentState == newState) return;

    PlayerState oldState = _currentState;

    switch (_currentState)
    {
        case PlayerState.Sliding:
            ExitSlidingState();
            break;
        case PlayerState.Flying:
            ExitFlyingState();
            break;
    }

_currentState = newState;

// Force an event publish when entering Running, even if nothing changed before
if (_currentState == PlayerState.Running)
{
    EventManager.Publish(new PlayerStateChangedEvent { State = _currentState });
}
else
{
    EventManager.Publish(new PlayerStateChangedEvent { State = _currentState });
}
    switch (_currentState)
    {
        case PlayerState.Running:
            EnterRunningState(oldState);
            break;
            
        case PlayerState.Sliding:
        _onPlayerSlide?.Raise();
            EnterSlidingState();
            break;
        case PlayerState.Flying:
            EnterFlyingState();
            break;
    }

    EventManager.Publish(new PlayerStateChangedEvent { State = _currentState });
}


    // --- State Handlers ---

    #region State Enter/Exit/Update Logic

    private void HandleIdleState()
    {
        // Automatically transition back to Running, as the player should never be stuck in Idle.
        ChangeState(PlayerState.Running);
    }

    private void HandleRunningState()
    {
        if (_allowAdvancedManeuvers && Input.GetKeyDown(KeyCode.UpArrow) && !_isBypassActive)
        {
            StartCoroutine(BypassCoroutine(_animTriggerSpinMove));
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PerformFirstJump();
            return;
        }

        if (_allowSlide && Input.GetKeyDown(KeyCode.DownArrow) && _playerStats.CurrentStamina > 0)
        {
            ChangeState(PlayerState.Sliding);
            return;
        }
if (_isGrounded && Time.time - _lastFootstepTime >= _footstepInterval)
{
    _onPlayerRunStep?.Raise();
    _lastFootstepTime = Time.time;
}
        if (_isGrounded)
        {
            _timeSinceLeftGround = 0f;
        }
        else
        {
            _timeSinceLeftGround += Time.deltaTime;
            if (_timeSinceLeftGround > _fallBufferTime)
            {
                ChangeState(PlayerState.Falling);
            }
        }
    }

    private void EnterRunningState(PlayerState oldState)
{
    _canDoubleJump = true;
    _isApplyingVariableJumpForce = false;
    _rb.gravityScale = _defaultGravityScale;
    _timeSinceLeftGround = 0f;

    if (oldState == PlayerState.Falling || oldState == PlayerState.Jumping ||
        oldState == PlayerState.Flapping || oldState == PlayerState.Flying)
    {
        _onPlayerLanded?.Raise();
    }
}


    private void HandleJumpingState()
    {
        // Auto-transition to a higher state if available
        if (_allowFlying && _currentState != PlayerState.Flying)
        {
            ChangeState(PlayerState.Flying);
            return;
        }
        if (_allowFlapping && !_allowFlying && _currentState != PlayerState.Flapping)
        {
            ChangeState(PlayerState.Flapping);
            return;
        }

        if (_allowDoubleJump && _allowDoubleJumpInJumpingState && Input.GetKeyDown(KeyCode.Space) && _canDoubleJump)
        {
            PerformDoubleJump();
        }

        // This is the missing piece: check if the player has started to fall.
        if (_rb.linearVelocity.y < 0)
        {
            ChangeState(PlayerState.Falling);
        }
    }
    private void HandleFallingState()
    {
        if (_isGrounded)
        {
            ChangeState(PlayerState.Running);
            return;
        }

        if (_allowDoubleJump && _allowDoubleJumpInFallingState && Input.GetKeyDown(KeyCode.Space) && _canDoubleJump)
        {
            PerformDoubleJump();
        }
        // Auto-transition to a higher state if available
        if (_allowFlying && _currentState != PlayerState.Flying)
        {
            ChangeState(PlayerState.Flying);
            return;
        }
        if (_allowFlapping && !_allowFlying && _currentState != PlayerState.Flapping)
        {
            ChangeState(PlayerState.Flapping);
            return;
        }

    }

    private void EnterSlidingState()
    {
        _capsuleCollider.size = _slideColliderSize;
        _capsuleCollider.offset = _slideColliderOffset;
        _runSpeed *= _slideSpeedMultiplier;
    }

    private void HandleSlidingState()
    {
        bool hasStamina = _playerStats.UseStamina(_slideStaminaDrainRate * Time.deltaTime);

        if (Input.GetKeyUp(KeyCode.DownArrow) || !hasStamina)
        {
            if (_isGrounded)
            {
                ChangeState(PlayerState.Running);
            }
            else
            {
                ChangeState(PlayerState.Falling);
            }
        }
    }

    private void ExitSlidingState()
    {
        _capsuleCollider.size = _originalColliderSize;
        _capsuleCollider.offset = _originalColliderOffset;
        _runSpeed /= _slideSpeedMultiplier;
    }

    private void HandleFlappingState()
    {
        if (_isGrounded)
        {
            ChangeState(PlayerState.Running);
            return;
        }

        if (_allowAdvancedManeuvers && Input.GetKeyDown(KeyCode.UpArrow) && !_isBypassActive)
        {
            StartCoroutine(BypassCoroutine(_animTriggerSpinMove));
            return;
        }

        if (_allowAdvancedManeuvers && Input.GetKeyDown(KeyCode.DownArrow) && !_isNoseDiveActive)
        {
            StartCoroutine(NoseDiveCoroutine());
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PerformFlap();
        }
    }

    private void EnterFlyingState()

    {
        _rb.gravityScale = _flightGravityScale;
        _isApplyingVariableJumpForce = false;

        // This is the logic you already have to prevent speed loss
        _runSpeed = Mathf.Max(_runSpeed, _minFlightSpeed);

        // This is the NEW logic to add the accomplishment burst
        _runSpeed += _flightTransitionSpeedBonus;

        // Optional but recommended: Clamp the speed so it doesn't go over the max
        _runSpeed = Mathf.Min(_runSpeed, _maxRunSpeed);
    }

    private void HandleFlyingState()
    {
        // Null guard to prevent NullReferenceException
        if (_playerStats == null)
        {
            Debug.LogWarning("PlayerStats is null in HandleFlyingState");
            return;
        }

        // --- New, Stamina-Aware Speed Burst & Flight Control Logic ---
        bool isHoldingBurst = Input.GetKey(KeyCode.Space);

        // Check if the player is trying to burst
        if (isHoldingBurst)
        {
            // Try to use stamina. This will return 'false' if we're out.
            bool drainSuccessful = _playerStats.UseStamina(_flightBurstStaminaDrainRate * Time.deltaTime);

            // Only apply the burst effect IF the stamina drain was successful.
            if (drainSuccessful)
            {
                // If the burst isn't already active, apply the speed multiplier ONCE.
                if (!_isSpeedBurstActive)
                {
                    _isSpeedBurstActive = true;
                    _runSpeed *= _flightSpeedBurstMultiplier;
                    _runSpeed = Mathf.Min(_runSpeed, _maxRunSpeed); // Clamp to max speed
                    _onPlayerFlightSpeedBurst?.Raise();
                }
            }
            // If the drain failed (out of stamina), we MUST stop the burst.
            else if (_isSpeedBurstActive)
            {
                _isSpeedBurstActive = false;
                _runSpeed /= _flightSpeedBurstMultiplier; // Return to normal speed.
            }
        }
        // If the player lets go of the key, stop the burst.
        else if (_isSpeedBurstActive)
        {
            _isSpeedBurstActive = false;
            _runSpeed /= _flightSpeedBurstMultiplier; // Return to normal speed.
        }

        // Handle vertical movement
        float verticalInput = Input.GetAxisRaw("Vertical");
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, verticalInput * _flightControlSpeed);
    }

    private void ExitFlyingState()
    {
        _rb.gravityScale = _defaultGravityScale;
        if (_isSpeedBurstActive)
        {
            StopAllCoroutines();
            _runSpeed = _originalRunSpeed;
            _isSpeedBurstActive = false;
        }
    }

    #endregion

    // === Core Logic ===
    private void HandleMovement()
    {
        if (_isStunned) return;


        //... (rest of the method is the same)
        if (_currentState == PlayerState.Flying)
        {
            _rb.linearVelocity = new Vector2(_runSpeed, _rb.linearVelocity.y);
            return;
        }

        if (_currentState != PlayerState.Idle)
        {
            // Determine the target speed based on stamina level
            float targetSpeed = _runSpeed;
            if (_playerStats != null && _playerStats.CurrentStamina <= 0.1f) // Use a small threshold to avoid float precision issues
            {
                // If out of stamina, apply the speed penalty
                targetSpeed *= _staminaDepletedSpeedMultiplier;
            }
            _rb.linearVelocity = new Vector2(targetSpeed, _rb.linearVelocity.y);
        }
    }


    private void PerformFirstJump()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
        _onPlayerJumped?.Raise();
        EventManager.Publish(new PlaySoundEvent { SoundName = "Player_Jump" });
        if (_animator != null)
        {
            _animator.SetTrigger("Jump");
        }
        ChangeState(PlayerState.Jumping);
    }

    private void PerformDoubleJump()
    {
        float currentBonus = 1f;
        if (_enablePeakJumpBonus && Time.time >= _timeAtJumpApex && Time.time <= _timeAtJumpApex + _peakJumpWindow)
        {
            currentBonus = _peakJumpBonusMultiplier;
        }

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _minDoubleJumpForce * currentBonus);
        _canDoubleJump = false;
        _isApplyingVariableJumpForce = true;
        _variableJumpTimer = 0f;
        if (_animator != null)
        {
            _animator.SetTrigger("Jump");
        }
        _onPlayerDoubleJumped?.Raise();
        ChangeState(PlayerState.Jumping);
    }

    private void PerformFlap()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _flapForce);
        if (_animator != null)
        {
            _animator.SetTrigger("Jump");
        }
    }


    private void HandleVariableJump()
    {
        if (!_isApplyingVariableJumpForce) return;

        if (Input.GetKey(KeyCode.Space) && _variableJumpTimer < _doubleJumpHoldDuration)
        {
            _variableJumpTimer += Time.deltaTime;
            float forceToAdd = (_maxDoubleJumpForce - _minDoubleJumpForce) * (Time.deltaTime / _doubleJumpHoldDuration);
            _rb.AddForce(Vector2.up * forceToAdd, ForceMode2D.Impulse);
        }
        else
        {
            _isApplyingVariableJumpForce = false;
        }
    }

    private void HandleHangTime()
    {
        if (!_enableHangTime) return;
        if (_isGrounded)
        {
            _rb.gravityScale = _defaultGravityScale;
            return;
        }
        if (_currentState == PlayerState.Flapping || _currentState == PlayerState.Flying)
        {
            return;
        }
        if (Mathf.Abs(_rb.linearVelocity.y) < _hangTimeVelocityThreshold)
        {
            _rb.gravityScale = _hangTimeGravityScale;
        }
        else
        {
            _rb.gravityScale = _defaultGravityScale;
        }
    }

    // 11/6/2025 AI-Tag
    // This was created with the help of Assistant, a Unity Artificial Intelligence product.
    private void CheckGrounded()
    {
        Collider2D groundCollider = Physics2D.OverlapBox(transform.position + (Vector3)_groundCheckOffset, _groundCheckSize, 0f, _groundLayer);
        _isGrounded = groundCollider != null;
    }

    private void HandleGroundCheck()
    {
        // Create scaled versions of the offset and size based on the player's current scale.
        Vector2 scaledOffset = new Vector2(_groundCheckOffset.x * transform.localScale.x, _groundCheckOffset.y * transform.localScale.y);
        Vector2 scaledSize = new Vector2(_groundCheckSize.x * transform.localScale.x, _groundCheckSize.y * transform.localScale.y);

        // Perform the BoxCast using the new scaled values.
        Vector2 castOrigin = (Vector2)transform.position + scaledOffset;
        _isGrounded = Physics2D.BoxCast(castOrigin, scaledSize, 0f, Vector2.down, _groundCheckDistance, _groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector2 boxPosition = (Vector2)transform.position + _groundCheckOffset + (Vector2.down * _groundCheckDistance);
        Gizmos.DrawWireCube(boxPosition, _groundCheckSize);
    }

    private void HandleAnimations()
    {
        if (_animator == null) return; // Null guard

        // Velocity tracking
        _animator.SetFloat("yVelocity", _rb.linearVelocity.y);
        _animator.SetFloat("runSpeed", Mathf.Abs(_rb.linearVelocity.x));

        // State booleans
        _animator.SetBool("isGrounded", _isGrounded);
        _animator.SetBool("isRunning", _currentState == PlayerState.Running);
        _animator.SetBool("isSliding", _currentState == PlayerState.Sliding);
        _animator.SetBool("isFlapping", _currentState == PlayerState.Flapping);
        _animator.SetBool("isFlying", _currentState == PlayerState.Flying);

        // Flight vertical input
        if (_currentState == PlayerState.Flying)
        {
            float verticalInput = Input.GetAxisRaw("Vertical");
            _animator.SetFloat("verticalInput", verticalInput);
        }

        UpdateAnimationStateDebug();
    }

    // 11/6/2025 AI-Tag
    // This was created with the help of Assistant, a Unity Artificial Intelligence product.
    private void UpdateAnimatorParameters()
    {
        if (_animator == null) return;

        _animator.SetBool("isGrounded", _isGrounded);
        _animator.SetFloat("yVelocity", _rb.linearVelocity.y);
        _animator.SetFloat("runSpeed", Mathf.Abs(_rb.linearVelocity.x));
    }

    // 11/6/2025 AI-Tag
    // This was created with the help of Assistant, a Unity Artificial Intelligence product.
    private void HandleSlide()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && _isGrounded && Mathf.Abs(_rb.linearVelocity.x) > 0)
        {
            _animator.SetBool("isSliding", true);
            _capsuleCollider.size = _slideColliderSize;
            _capsuleCollider.offset = _slideColliderOffset;
        }
        else
        {
            _animator.SetBool("isSliding", false);
            _capsuleCollider.size = _originalColliderSize;
            _capsuleCollider.offset = _originalColliderOffset;
        }
    }

    private void UpdateAnimationStateDebug()
    {
        if (_animator == null) return; // Null guard to prevent NullReferenceException

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.shortNameHash == _animStateIdle) _currentAnimationStateName = "Idle";
        else if (stateInfo.shortNameHash == _animStateRun) _currentAnimationStateName = "Run";
        else if (stateInfo.shortNameHash == _animStateJump) _currentAnimationStateName = "Jump";
        else if (stateInfo.shortNameHash == _animStateFall) _currentAnimationStateName = "Fall";
        else if (stateInfo.shortNameHash == _animStateSlide) _currentAnimationStateName = "Slide";
        else if (stateInfo.shortNameHash == _animStateFlying) _currentAnimationStateName = "Flying State";
        else _currentAnimationStateName = "Unknown State";
    }

    #region Advanced Maneuvers

    private void HandleFlyingSpacebarInput()
    {
        if (!_allowAdvancedManeuvers)
        {
            return;
        }

        if (Time.time - _lastSpacebarTapTime > _tripleTapTimeWindow)
        {
            _spacebarTaps = 0;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _lastSpacebarTapTime = Time.time;
            _spacebarTaps++;

            if (_spacebarTaps >= 3)
            {
                if (!_isBypassActive)
                {
                    StartCoroutine(BypassCoroutine(_animTriggerBarrelRoll));
                }

                _spacebarTaps = 0;
            }
        }
    }

    private IEnumerator BypassCoroutine(int animationTriggerHash)
    {
        // Null guard to prevent NullReferenceException
        if (_playerStats == null)
        {
            Debug.LogWarning("PlayerStats is null in BypassCoroutine");
            yield break;
        }

        _isBypassActive = true;
        _animator.SetTrigger(animationTriggerHash);

        // Publish maneuver event for stat tracking
        ManeuverType maneuverType = animationTriggerHash == _animTriggerSpinMove ? ManeuverType.Spin : ManeuverType.BarrelRoll;
        EventManager.Publish(new PlayerManeuverExecutedEvent { ManeuverType = maneuverType });
        float elapsed = 0f;

        Physics2D.IgnoreLayerCollision(_playerLayerIndex, _obstacleLayerIndex, true);
        while (elapsed < _bypassDuration)
        {
            // Try to use stamina for the maneuver each frame.
            if (!_playerStats.UseStamina(_maneuverStaminaDrainRate * Time.deltaTime))
            {
                break; // If we run out of stamina, end the maneuver early.
            }
            elapsed += Time.deltaTime;
            yield return null; // Wait for the next frame.
        }

        Physics2D.IgnoreLayerCollision(_playerLayerIndex, _obstacleLayerIndex, false);
        _isBypassActive = false;
    }

    private IEnumerator NoseDiveCoroutine()
    {
        _isNoseDiveActive = true;
        _animator.SetTrigger(_animTriggerNoseDive);

        // Publish maneuver event for stat tracking
        EventManager.Publish(new PlayerManeuverExecutedEvent { ManeuverType = ManeuverType.NoseDive });

        _rb.gravityScale = 0f;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0);

        _rb.AddForce(_noseDiveForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(_noseDiveDuration);

        _rb.AddForce(_noseDiveRecoveryForce, ForceMode2D.Impulse);

        _rb.gravityScale = _defaultGravityScale;
        _isNoseDiveActive = false;
    }
    /// <summary>
    /// Modifies the player's base run speed by a given amount, clamped within limits.
    /// </summary>
    /// <param name="amount">The amount to add (can be negative to decrease speed).</param>
    public void ModifyRunSpeed(float amount)
    {
        if (Mathf.Approximately(amount, 0f))
        {
            return;
        }

        _originalRunSpeed = Mathf.Clamp(_originalRunSpeed + amount, _minBaseRunSpeed, _maxRunSpeed);
        _runSpeed = Mathf.Clamp(_runSpeed + amount, _originalRunSpeed, _maxRunSpeed);

        // Publish speed change event for stat tracking
        EventManager.Publish(new PlayerSpeedChangedEvent { CurrentSpeed = _runSpeed, MaxSpeed = _maxRunSpeed });
    }


    /// <summary>
    /// Modifies the player's base jump force by a given amount, clamped within limits.
    /// </summary>
    /// <param name="amount">The amount to add (can be negative to decrease jump force).</param>
    public void ModifyJumpForce(float amount)
    {
        // Add the amount to the current jump force, but ensure it never goes above our max jump force.
        // We'll use 1f as a minimum to prevent a zero or negative jump.
        _jumpForce = Mathf.Clamp(_jumpForce + amount, 1f, _maxJumpForce);
    }
    /// <summary>
    /// Applies a temporary size change to the player, safely adjusting position to stay grounded.
    /// </summary>
    public void ApplyTemporarySizeChange(float multiplier, float duration)
    {
        // We check for a positive multiplier to avoid errors.
        if (multiplier > 0)
        {
            StartCoroutine(TemporarySizeChangeCoroutine(multiplier, duration));
        }
    }

    private IEnumerator TemporarySizeChangeCoroutine(float multiplier, float duration)
    {
        // 1. Get the original foot position before scaling
        float originalFootPosition = _capsuleCollider.bounds.min.y;

        // 2. Apply the new scale
        transform.localScale *= multiplier;

        // 3. Get the new foot position and calculate the difference
        float newFootPosition = _capsuleCollider.bounds.min.y;
        float positionCorrection = originalFootPosition - newFootPosition;

        // 4. Adjust the player's position to keep their feet planted
        transform.position += new Vector3(0, positionCorrection, 0);

        // 5. Wait for the effect to expire
        yield return new WaitForSeconds(duration);

        // 6. Revert the scale and re-adjust position to stay grounded
        originalFootPosition = _capsuleCollider.bounds.min.y;
        transform.localScale = _originalScale; // Revert to original size
        newFootPosition = _capsuleCollider.bounds.min.y;
        positionCorrection = originalFootPosition - newFootPosition;
        transform.position += new Vector3(0, positionCorrection, 0);
    }
    #endregion
    public PlayerState GetCurrentState()
{
    return _currentState;
}
}

