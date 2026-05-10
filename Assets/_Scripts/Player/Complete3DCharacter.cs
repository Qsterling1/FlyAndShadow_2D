using UnityEngine;
using System;
using System.Collections;
using FlyShadow.EventBus;

/// <summary>
/// Complete 3D player controller - production-ready port of all 2D PlayerController logic.
/// Uses Rigidbody (3D), Physics.Raycast for ground checking, and 2.5D movement (X-axis only, Z frozen).
/// Integrates with Frankenstein_AC animator controller with all required parameters.
/// </summary>
public class Complete3DCharacter : MonoBehaviour
{
    // === Static Instance ===
    public static Complete3DCharacter instance;

    // === Component References ===
    private Rigidbody _rb;
    private CapsuleCollider _capsuleCollider;
    private PlayerStats _playerStats;
    private Animator _animator;

    // === State Fields ===
    private bool _isGrounded;
    private bool _isStunned = false;
    [SerializeField] private PlayerState _currentState;

    // State Data
    private Vector2 _originalColliderSize; // Stored as Vector2 for compatibility with 2D logic
    private float _originalColliderHeight;
    private float _originalColliderRadius;
    private Vector3 _originalColliderCenter;
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

    // Footstep timing
    private float _lastFootstepTime;

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
    [Tooltip("The height of the capsule collider when sliding (3D).")]
    [SerializeField] private float _slideColliderHeight = 1f;
    [Tooltip("The Y offset of the collider center when sliding.")]
    [SerializeField] private float _slideColliderCenterY = 0.5f;

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
    [Tooltip("The initial downward force for the Nose Dive (converted to 3D).")]
    [SerializeField] private Vector2 _noseDiveForce = new Vector2(5, -10);
    [Tooltip("The duration of the main nose dive before recovery begins.")]
    [SerializeField] private float _noseDiveDuration = 0.3f;
    [Tooltip("The recovery force that provides a small upward boost after the dive (converted to 3D).")]
    [SerializeField] private Vector2 _noseDiveRecoveryForce = new Vector2(2, 4);

    [Header("Ground Check Settings (3D)")]
    [Tooltip("The layer(s) that should be considered 'ground' for the purposes of jumping.")]
    [SerializeField] private LayerMask _groundLayer;
    [Tooltip("The distance of the ground check raycast.")]
    [SerializeField] private float _groundCheckDistance = 0.2f;
    [Tooltip("The radius for spherecast ground detection (0 = raycast only).")]
    [SerializeField] private float _groundCheckRadius = 0.3f;

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

    // === Unity Lifecycle ===
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

        _rb = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _playerStats = GetComponent<PlayerStats>();
        _animator = GetComponent<Animator>();

        if (_rb == null) Debug.LogError("[Complete3DCharacter] Rigidbody component missing!");
        if (_capsuleCollider == null) Debug.LogError("[Complete3DCharacter] CapsuleCollider component missing!");
        if (_playerStats == null) Debug.LogWarning("[Complete3DCharacter] PlayerStats component missing!");
        if (_animator == null) Debug.LogWarning("[Complete3DCharacter] Animator component missing!");

        // 3D Rigidbody uses gravityScale via Physics.gravity, but we'll use drag manipulation
        // Store default gravity scale equivalent (we'll control via Rigidbody.useGravity and custom forces)
        _defaultGravityScale = 3f; // Match 2D default

        _originalRunSpeed = _runSpeed;
        _minBaseRunSpeed = _runSpeed;

        // Store original collider dimensions (3D capsule)
        _originalColliderHeight = _capsuleCollider.height;
        _originalColliderRadius = _capsuleCollider.radius;
        _originalColliderCenter = _capsuleCollider.center;

        _playerLayerIndex = LayerMask.NameToLayer(_playerLayerName);
        _obstacleLayerIndex = LayerMask.NameToLayer(_obstacleLayerName);
        if (_playerLayerIndex == -1 || _obstacleLayerIndex == -1)
        {
            Debug.LogWarning("[Complete3DCharacter] Player or Obstacle layer not found. Check layer names.");
        }

        _originalScale = transform.localScale;

        // Publish power thresholds
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

    private void Start()
    {
        ChangeState(_startingState);
        EventManager.Publish(new PlayerStateChangedEvent { State = _currentState });

        // BUGFIX: Initialize ability flags based on starting power
        // Without this, Inspector defaults (_allowFlying = true) override progression
        if (_playerStats != null)
        {
            int startingPower = _playerStats.CurrentPower;
            _allowDoubleJump = startingPower >= _requiredPowerForDoubleJump;
            _allowFlapping = startingPower >= _requiredPowerForFlapping;
            _allowFlying = startingPower >= _requiredPowerForFlying;
        }
        else
        {
            // No PlayerStats component - disable all progression-locked abilities
            _allowDoubleJump = false;
            _allowFlapping = false;
            _allowFlying = false;
        }
    }

    private void Update()
    {
        HandleGroundCheck();
        ManageState();
        HandleVariableJump();
        HandleHangTime();
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

    // === Public API (for Obstacles) ===
    /// <summary>
    /// Provides controlled access to the player's Rigidbody component.
    /// </summary>
    public Rigidbody GetRigidbody() => _rb;

    public PlayerState GetCurrentState() => _currentState;

    public void ApplyTemporarySpeedChange(float multiplier, float duration)
    {
        StartCoroutine(TemporarySpeedChangeCoroutine(multiplier, duration));
    }

    public void ApplyStun(float duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }

    public void ApplyTemporaryGravityChange(float multiplier, float duration)
    {
        StartCoroutine(TemporaryGravityChangeCoroutine(multiplier, duration));
    }

    public void ApplyTemporarySizeChange(float multiplier, float duration)
    {
        if (multiplier > 0)
        {
            StartCoroutine(TemporarySizeChangeCoroutine(multiplier, duration));
        }
    }

    public void ModifyRunSpeed(float amount)
    {
        if (Mathf.Approximately(amount, 0f)) return;

        _originalRunSpeed = Mathf.Clamp(_originalRunSpeed + amount, _minBaseRunSpeed, _maxRunSpeed);
        _runSpeed = Mathf.Clamp(_runSpeed + amount, _originalRunSpeed, _maxRunSpeed);

        EventManager.Publish(new PlayerSpeedChangedEvent { CurrentSpeed = _runSpeed, MaxSpeed = _maxRunSpeed });
    }

    public void ModifyJumpForce(float amount)
    {
        _jumpForce = Mathf.Clamp(_jumpForce + amount, 1f, _maxJumpForce);
    }

    // === Event Handlers ===
    private void OnSpeedModified(PlayerSpeedModifiedEvent e)
    {
        ModifyRunSpeed(e.Delta);
    }

    private void OnScaleRequested(PlayerScaleRequestedEvent e)
    {
        if (e.Multiplier <= 0f) return;

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
        if (e.Step == 0) return;
        ApplyPhaseStep(e.Step);
    }

    private void OnPlayerPowerChanged(PlayerPowerChangedEvent e)
    {
        CheckForPhaseShift();
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

        // Exit current state
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

        // Publish state change event
        EventManager.Publish(new PlayerStateChangedEvent { State = _currentState });

        // Enter new state
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
    }

    // === State Handlers ===
    private void HandleIdleState()
    {
        if (Input.anyKeyDown)
        {
            ChangeState(PlayerState.Running);
        }
    }

    private void HandleRunningState()
    {
        if (_allowAdvancedManeuvers && Input.GetKeyDown(KeyCode.UpArrow) && !_isBypassActive)
        {
            StartCoroutine(BypassCoroutine("Spin"));
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PerformFirstJump();
            return;
        }

        if (_allowSlide && Input.GetKeyDown(KeyCode.DownArrow) && _playerStats != null && _playerStats.CurrentStamina > 0)
        {
            ChangeState(PlayerState.Sliding);
            return;
        }

        // Footstep sounds
        if (_isGrounded && Time.time - _lastFootstepTime >= _footstepInterval)
        {
            _onPlayerRunStep?.Raise();
            _lastFootstepTime = Time.time;
        }

        if (!_isGrounded)
        {
            ChangeState(PlayerState.Falling);
        }
    }

    private void EnterRunningState(PlayerState oldState)
    {
        _canDoubleJump = true;
        _isApplyingVariableJumpForce = false;

        if (oldState == PlayerState.Falling || oldState == PlayerState.Jumping ||
            oldState == PlayerState.Flapping || oldState == PlayerState.Flying)
        {
            _onPlayerLanded?.Raise();
        }
    }

    private void HandleJumpingState()
    {
        // Auto-transition to higher state if available
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

        // Check if started falling
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

        // Auto-transition to higher state if available
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

    private void HandleSlidingState()
    {
        bool hasStamina = _playerStats != null && _playerStats.UseStamina(_slideStaminaDrainRate * Time.deltaTime);

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

    private void EnterSlidingState()
    {
        // Modify capsule collider for sliding (crouch)
        _capsuleCollider.height = _slideColliderHeight;
        _capsuleCollider.center = new Vector3(_originalColliderCenter.x, _slideColliderCenterY, _originalColliderCenter.z);
        _runSpeed *= _slideSpeedMultiplier;
    }

    private void ExitSlidingState()
    {
        // Restore original collider
        _capsuleCollider.height = _originalColliderHeight;
        _capsuleCollider.center = _originalColliderCenter;
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
            StartCoroutine(BypassCoroutine("Spin"));
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

    private void HandleFlyingState()
    {
        // Stamina-aware speed burst
        bool isHoldingBurst = Input.GetKey(KeyCode.Space);

        if (isHoldingBurst && _playerStats != null)
        {
            bool drainSuccessful = _playerStats.UseStamina(_flightBurstStaminaDrainRate * Time.deltaTime);

            if (drainSuccessful)
            {
                if (!_isSpeedBurstActive)
                {
                    _isSpeedBurstActive = true;
                    _runSpeed *= _flightSpeedBurstMultiplier;
                    _runSpeed = Mathf.Min(_runSpeed, _maxRunSpeed);
                    _onPlayerFlightSpeedBurst?.Raise();
                }
            }
            else if (_isSpeedBurstActive)
            {
                _isSpeedBurstActive = false;
                _runSpeed /= _flightSpeedBurstMultiplier;
            }
        }
        else if (_isSpeedBurstActive)
        {
            _isSpeedBurstActive = false;
            _runSpeed /= _flightSpeedBurstMultiplier;
        }

        // Handle vertical movement (3D)
        float verticalInput = Input.GetAxisRaw("Vertical");
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, verticalInput * _flightControlSpeed, 0f);
    }

    private void EnterFlyingState()
    {
        _isApplyingVariableJumpForce = false;

        // Apply flight speed bonus
        _runSpeed = Mathf.Max(_runSpeed, _minFlightSpeed);
        _runSpeed += _flightTransitionSpeedBonus;
        _runSpeed = Mathf.Min(_runSpeed, _maxRunSpeed);
    }

    private void ExitFlyingState()
    {
        if (_isSpeedBurstActive)
        {
            _runSpeed = _originalRunSpeed;
            _isSpeedBurstActive = false;
        }
    }

    // === Core Movement ===
    private void HandleMovement()
    {
        if (_isStunned) return;

        // 2.5D movement: X-axis forward, Z-axis always 0
        if (_currentState == PlayerState.Flying)
        {
            _rb.linearVelocity = new Vector3(_runSpeed, _rb.linearVelocity.y, 0f);
            return;
        }

        if (_currentState != PlayerState.Idle)
        {
            float targetSpeed = _runSpeed;
            if (_playerStats != null && _playerStats.CurrentStamina <= 0.1f)
            {
                targetSpeed *= _staminaDepletedSpeedMultiplier;
            }
            _rb.linearVelocity = new Vector3(targetSpeed, _rb.linearVelocity.y, 0f);
        }
    }

    // === Jump Logic ===
    private void PerformFirstJump()
    {
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpForce, 0f);
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

        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _minDoubleJumpForce * currentBonus, 0f);
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
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _flapForce, 0f);

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
            _rb.AddForce(Vector3.up * forceToAdd, ForceMode.Impulse);
        }
        else
        {
            _isApplyingVariableJumpForce = false;
        }
    }

    private void HandleHangTime()
    {
        if (!_enableHangTime) return;
        if (_isGrounded) return;
        if (_currentState == PlayerState.Flapping || _currentState == PlayerState.Flying) return;

        // In 3D, we can't directly set gravityScale, but we can reduce gravity effect
        if (Mathf.Abs(_rb.linearVelocity.y) < _hangTimeVelocityThreshold)
        {
            // Apply upward force to simulate reduced gravity
            Vector3 counterGravity = -Physics.gravity * (_defaultGravityScale - _hangTimeGravityScale) * Time.deltaTime;
            _rb.AddForce(counterGravity, ForceMode.Acceleration);
        }
    }

    // === Ground Check (3D) ===
    private void HandleGroundCheck()
    {
        // Use SphereCast or Raycast from character position downward
        Vector3 origin = transform.position;

        if (_groundCheckRadius > 0.01f)
        {
            // SphereCast for more reliable detection
            _isGrounded = Physics.SphereCast(origin, _groundCheckRadius, Vector3.down, out RaycastHit hit, _groundCheckDistance, _groundLayer);
        }
        else
        {
            // Simple Raycast
            _isGrounded = Physics.Raycast(origin, Vector3.down, _groundCheckDistance, _groundLayer);
        }
    }

    // === Animations (3D Animator) ===
private void HandleAnimations()
{
    if (_animator == null) return;

    // Update animator parameters per 3D_Character_Animation_Bible.md
    
    // BUGFIX 1: Set speed for blend tree. Must be high for both Running AND Sliding.
    if (_currentState == PlayerState.Idle)
    {
        _animator.SetFloat("isRunning", 0f);
    }
    else if (_currentState != PlayerState.Jumping && _currentState != PlayerState.Falling && _currentState != PlayerState.Flapping)
    {
        // This keeps the blend tree active for Running and Sliding
        _animator.SetFloat("isRunning", _runSpeed);
    }

    // BUGFIX 2: Set vertical velocity (FIXED from linearVelocity)
    _animator.SetFloat("yVelocity", _rb.linearVelocity.y);

    // Set other parameters
    _animator.SetFloat("verticalInput", _currentState == PlayerState.Flying ? Input.GetAxisRaw("Vertical") : 0f);
    _animator.SetBool("isGrounded", _isGrounded);
    _animator.SetBool("isSliding", _currentState == PlayerState.Sliding);
    _animator.SetBool("isFlying", _currentState == PlayerState.Flying);
    _animator.SetBool("isFlapping", _currentState == PlayerState.Flapping);
}

    // === Advanced Maneuvers ===
    private void HandleFlyingSpacebarInput()
    {
        if (!_allowAdvancedManeuvers) return;

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
                    StartCoroutine(BypassCoroutine("BarrelRoll"));
                }
                _spacebarTaps = 0;
            }
        }
    }

    private IEnumerator BypassCoroutine(string maneuverName)
    {
        _isBypassActive = true;

        if (_animator != null)
        {
            _animator.SetTrigger(maneuverName == "Spin" ? "Spin" : "BarrelRoll");
        }

        ManeuverType maneuverType = maneuverName == "Spin" ? ManeuverType.Spin : ManeuverType.BarrelRoll;
        EventManager.Publish(new PlayerManeuverExecutedEvent { ManeuverType = maneuverType });

        float elapsed = 0f;
        Physics.IgnoreLayerCollision(_playerLayerIndex, _obstacleLayerIndex, true);

        while (elapsed < _bypassDuration)
        {
            if (_playerStats != null && !_playerStats.UseStamina(_maneuverStaminaDrainRate * Time.deltaTime))
            {
                break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        Physics.IgnoreLayerCollision(_playerLayerIndex, _obstacleLayerIndex, false);
        _isBypassActive = false;
    }

    private IEnumerator NoseDiveCoroutine()
    {
        _isNoseDiveActive = true;

        if (_animator != null)
        {
            _animator.SetTrigger("NoseDive");
        }

        EventManager.Publish(new PlayerManeuverExecutedEvent { ManeuverType = ManeuverType.NoseDive });

        // Convert 2D force to 3D (X, Y, 0)
        Vector3 diveForce3D = new Vector3(_noseDiveForce.x, _noseDiveForce.y, 0f);
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, 0);
        _rb.AddForce(diveForce3D, ForceMode.Impulse);

        yield return new WaitForSeconds(_noseDiveDuration);

        Vector3 recoveryForce3D = new Vector3(_noseDiveRecoveryForce.x, _noseDiveRecoveryForce.y, 0f);
        _rb.AddForce(recoveryForce3D, ForceMode.Impulse);

        _isNoseDiveActive = false;
    }

    // === Phase System ===
    private void ApplyPhaseStep(int step)
    {
        int direction = Math.Sign(step);
        int steps = Math.Abs(step);

        for (int i = 0; i < steps; i++)
        {
            var candidate = GetPhaseCandidate(direction);
            if (!candidate.HasValue) break;
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
        if (currentIndex < 0) currentIndex = 0;

        int targetIndex = currentIndex + direction;

        if (direction > 0)
        {
            if (targetIndex >= _phaseOrder.Length) return null;

            for (int i = targetIndex; i < _phaseOrder.Length; i++)
            {
                var candidate = _phaseOrder[i];
                if (IsPhaseAllowed(candidate)) return candidate;
            }
            return null;
        }
        else
        {
            if (targetIndex < 0) return PlayerState.Running;
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

    public void CheckForPhaseShift()
    {
        if (_playerStats == null) return;

        int p = _playerStats.CurrentPower;

        bool canDoubleJump = p >= _requiredPowerForDoubleJump;
        bool canFlap = p >= _requiredPowerForFlapping;
        bool canFly = p >= _requiredPowerForFlying;

        _allowDoubleJump = canDoubleJump;
        _allowFlapping = canFlap;
        _allowFlying = canFly;

        if (!canDoubleJump) _canDoubleJump = false;

        // Handle downgrades
        if (!canFly && _currentState == PlayerState.Flying)
        {
            ChangeState(canFlap ? PlayerState.Flapping : (_isGrounded ? PlayerState.Running : PlayerState.Falling));
            return;
        }
        if (!canFlap && _currentState == PlayerState.Flapping)
        {
            ChangeState(_isGrounded ? PlayerState.Running : PlayerState.Falling);
            return;
        }

        // Auto-upgrades
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

    // === Effect Coroutines ===
    private IEnumerator TemporarySpeedChangeCoroutine(float multiplier, float duration)
    {
        _runSpeed *= multiplier;
        yield return new WaitForSeconds(duration);
        _runSpeed /= multiplier;
    }

    private IEnumerator StunCoroutine(float duration)
    {
        _isStunned = true;
        yield return new WaitForSeconds(duration);
        _isStunned = false;
    }

    private IEnumerator TemporaryGravityChangeCoroutine(float multiplier, float duration)
    {
        // In 3D, simulate gravity change by applying counter-forces
        float timer = 0f;
        while (timer < duration)
        {
            Vector3 gravityAdjustment = -Physics.gravity * (multiplier - 1f) * Time.deltaTime;
            _rb.AddForce(gravityAdjustment, ForceMode.Acceleration);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void ApplyPermanentScale(float multiplier)
    {
        float originalFootPosition = _capsuleCollider.bounds.min.y;
        transform.localScale = Vector3.Scale(transform.localScale, new Vector3(multiplier, multiplier, multiplier));
        float newFootPosition = _capsuleCollider.bounds.min.y;
        float positionCorrection = originalFootPosition - newFootPosition;
        transform.position += new Vector3(0f, positionCorrection, 0f);
        _originalScale = transform.localScale;
    }

    private IEnumerator TemporarySizeChangeCoroutine(float multiplier, float duration)
    {
        float originalFootPosition = _capsuleCollider.bounds.min.y;
        transform.localScale *= multiplier;
        float newFootPosition = _capsuleCollider.bounds.min.y;
        float positionCorrection = originalFootPosition - newFootPosition;
        transform.position += new Vector3(0, positionCorrection, 0);

        yield return new WaitForSeconds(duration);

        originalFootPosition = _capsuleCollider.bounds.min.y;
        transform.localScale = _originalScale;
        newFootPosition = _capsuleCollider.bounds.min.y;
        positionCorrection = originalFootPosition - newFootPosition;
        transform.position += new Vector3(0, positionCorrection, 0);
    }

    // === Debug Visualization ===
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position;

        if (_groundCheckRadius > 0.01f)
        {
            // Draw sphere for spherecast
            Gizmos.DrawWireSphere(origin + Vector3.down * _groundCheckDistance, _groundCheckRadius);
        }

        // Draw ground check ray
        Gizmos.DrawRay(origin, Vector3.down * _groundCheckDistance);
    }
}