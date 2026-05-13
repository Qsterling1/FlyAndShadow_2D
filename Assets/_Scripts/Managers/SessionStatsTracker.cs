using UnityEngine;
using FlyShadow.EventBus;

/// <summary>
/// Tracks comprehensive session statistics for end-of-level scoring and display.
/// Subscribes to EventBus for decoupled stat collection.
/// </summary>
public class SessionStatsTracker : MonoBehaviour
{
    // === Singleton Instance ===
    public static SessionStatsTracker instance;

    // === Public Properties (Read-Only Access) ===
    public float SessionTime => _sessionTime;
    public float DistanceTraveled => _distanceTraveled;
    public int ChiliCoinsCollected => _chiliCoinsCollected;
    public int ChickenCoinsCollected => _chickenCoinsCollected;
    public int DamageTaken => _damageTaken;
    public int ObstaclesAvoided => _obstaclesAvoided;
    public float TimeInRunningState => _timeInRunningState;
    public float TimeInFlappingState => _timeInFlappingState;
    public float TimeInFlyingState => _timeInFlyingState;
    public int RunningStateEntries => _runningStateEntries;
    public int FlappingStateEntries => _flappingStateEntries;
    public int FlyingStateEntries => _flyingStateEntries;
    public int SpinMovesExecuted => _spinMovesExecuted;
    public int NoseDivesExecuted => _noseDivesExecuted;
    public int BarrelRollsExecuted => _barrelRollsExecuted;
    public float MaxSpeedAchieved => _maxSpeedAchieved;
    public int TotalPickupsCollected => _totalPickupsCollected;

    // === Inspector Fields (For Debugging) ===
    [Header("Session Tracking")]
    [Tooltip("Time elapsed since session started.")]
    [SerializeField] private float _sessionTime;
    [Tooltip("Total distance traveled by the player.")]
    [SerializeField] private float _distanceTraveled;

    [Header("Currency Collection")]
    [SerializeField] private int _chiliCoinsCollected;
    [SerializeField] private int _chickenCoinsCollected;

    [Header("Combat & Obstacles")]
    [SerializeField] private int _damageTaken;
    [SerializeField] private int _obstaclesAvoided;

    [Header("State Time Tracking")]
    [SerializeField] private float _timeInRunningState;
    [SerializeField] private float _timeInFlappingState;
    [SerializeField] private float _timeInFlyingState;

    [Header("State Transition Counts")]
    [SerializeField] private int _runningStateEntries;
    [SerializeField] private int _flappingStateEntries;
    [SerializeField] private int _flyingStateEntries;

    [Header("Advanced Maneuvers")]
    [SerializeField] private int _spinMovesExecuted;
    [SerializeField] private int _noseDivesExecuted;
    [SerializeField] private int _barrelRollsExecuted;

    [Header("Speed & Pickups")]
    [SerializeField] private float _maxSpeedAchieved;
    [SerializeField] private int _totalPickupsCollected;

    // === Private Fields ===
    private PlayerState _currentPlayerState;
    private Transform _playerTransform;
    private Rigidbody2D _playerRigidbody;
    private float _startingPlayerPosition;
    private PlayerStats _playerStats;
    private int _previousHealth;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Debug.Log("[SessionStatsTracker] Awake - Singleton instance set successfully");
        }
        else
        {
            Debug.LogWarning("[SessionStatsTracker] Awake - Duplicate instance detected, destroying this object");
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        Debug.Log("[SessionStatsTracker] OnEnable - Subscribing to EventBus events");

        // Subscribe to relevant EventBus events
        EventManager.Subscribe<PlayerStateChangedEvent>(OnPlayerStateChanged);
        EventManager.Subscribe<CurrencyModifiedEvent>(OnCurrencyModified);
        EventManager.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventManager.Subscribe<PlayerManeuverExecutedEvent>(OnPlayerManeuverExecuted);
        EventManager.Subscribe<PlayerSpeedChangedEvent>(OnPlayerSpeedChanged);

        Debug.Log("[SessionStatsTracker] OnEnable - Event subscriptions active");
        EnsurePlayerReferences();
    }

    private void OnDisable()
    {
        Debug.LogWarning("[SessionStatsTracker] OnDisable - Unsubscribing from events (This should NOT happen during gameplay!)");

        // Clean unsubscribe pattern
        EventManager.Unsubscribe<PlayerStateChangedEvent>(OnPlayerStateChanged);
        EventManager.Unsubscribe<CurrencyModifiedEvent>(OnCurrencyModified);
        EventManager.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventManager.Unsubscribe<PlayerManeuverExecutedEvent>(OnPlayerManeuverExecuted);
        EventManager.Unsubscribe<PlayerSpeedChangedEvent>(OnPlayerSpeedChanged);

        DetachFromPlayerStats();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            DetachFromPlayerStats();
            instance = null;
        }
    }

    private void Start()
    {
        EnsurePlayerReferences();

        if (_playerTransform != null)
        {
            _startingPlayerPosition = _playerTransform.position.x;
        }

        if (_playerStats != null)
        {
            _previousHealth = _playerStats.CurrentHealth;
        }
    }

    private void Update()
    {
        EnsurePlayerReferences();

        if (GameManager.instance != null && GameManager.instance.CurrentState == GameState.Playing)
        {
            _sessionTime += Time.deltaTime;

            UpdateStateTime();
            UpdateDistance();
            UpdateMaxSpeed();
        }
        else
        {
        }
    }

    // === Event Handlers ===

    /// <summary>
    /// Handles player state changes to track time in each state and state entries.
    /// </summary>
    private void OnPlayerStateChanged(PlayerStateChangedEvent e)
    {
        _currentPlayerState = e.State;

        switch (e.State)
        {
            case PlayerState.Running:
                _runningStateEntries++;
                break;
            case PlayerState.Flapping:
                _flappingStateEntries++;
                break;
            case PlayerState.Flying:
                _flyingStateEntries++;
                break;
        }
    }

    /// <summary>
    /// Tracks currency collection.
    /// </summary>
    private void OnCurrencyModified(CurrencyModifiedEvent e)
    {
#if UNITY_EDITOR
        Debug.Log($"[SessionStatsTracker] OnCurrencyModified - Amount: {e.Amount}");
#endif
        if (e.Amount > 0)
        {
            if (CoinCounter.instance != null)
            {
                _chiliCoinsCollected = CoinCounter.instance.ChiliCoins;
                _chickenCoinsCollected = CoinCounter.instance.ChickenCoins;
#if UNITY_EDITOR
                Debug.Log($"[SessionStatsTracker] Coins updated - Chili: {_chiliCoinsCollected}, Chicken: {_chickenCoinsCollected}");
#endif
            }

            _totalPickupsCollected++;
        }
    }

    /// <summary>
    /// Handles game state changes to reset stats on new game.
    /// </summary>
    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        Debug.Log($"[SessionStatsTracker] OnGameStateChanged - State: {e.State}");
        if (e.State == GameState.LevelStarting)
        {
            Debug.Log("[SessionStatsTracker] LevelStarting detected - Resetting stats");
            EnsurePlayerReferences();
            ResetStats();
        }
    }

    /// <summary>
    /// Tracks advanced maneuvers executed by the player.
    /// </summary>
    private void OnPlayerManeuverExecuted(PlayerManeuverExecutedEvent e)
    {
#if UNITY_EDITOR
        Debug.Log($"[SessionStatsTracker] OnPlayerManeuverExecuted - Maneuver: {e.ManeuverType}");
#endif
        switch (e.ManeuverType)
        {
            case ManeuverType.Spin:
                _spinMovesExecuted++;
                break;
            case ManeuverType.NoseDive:
                _noseDivesExecuted++;
                break;
            case ManeuverType.BarrelRoll:
                _barrelRollsExecuted++;
                break;
        }
    }

    /// <summary>
    /// Tracks player speed changes to record max speed achieved.
    /// </summary>
    private void OnPlayerSpeedChanged(PlayerSpeedChangedEvent e)
    {
#if UNITY_EDITOR
        Debug.Log($"[SessionStatsTracker] OnPlayerSpeedChanged - CurrentSpeed: {e.CurrentSpeed}, MaxSpeed: {e.MaxSpeed}");
#endif
        if (e.CurrentSpeed > _maxSpeedAchieved)
        {
            _maxSpeedAchieved = e.CurrentSpeed;
        }
    }

    // === Private Methods ===

    private void EnsurePlayerReferences()
    {
        if (_playerTransform == null && PlayerController.instance != null)
        {
            _playerTransform = PlayerController.instance.transform;
        }

        if (_playerRigidbody == null && PlayerController.instance != null)
        {
            _playerRigidbody = PlayerController.instance.GetRigidbody();
        }

        if (_playerStats == null && PlayerStats.instance != null)
        {
            AttachToPlayerStats(PlayerStats.instance);
        }
    }

    private void AttachToPlayerStats(PlayerStats stats)
    {
        if (stats == null)
        {
            return;
        }

        if (_playerStats == stats)
        {
            return;
        }

        DetachFromPlayerStats();

        _playerStats = stats;
        _previousHealth = _playerStats.CurrentHealth;
        _playerStats.OnStatsChanged += OnPlayerStatsChanged;
    }

    private void DetachFromPlayerStats()
    {
        if (_playerStats != null)
        {
            _playerStats.OnStatsChanged -= OnPlayerStatsChanged;
            _playerStats = null;
        }
    }

    private void OnPlayerStatsChanged()
    {
        if (_playerStats == null)
        {
            return;
        }

        int currentHealth = _playerStats.CurrentHealth;
        if (currentHealth < _previousHealth)
        {
            _damageTaken += _previousHealth - currentHealth;
        }

        _previousHealth = currentHealth;
    }

    private void UpdateMaxSpeed()
    {
        if (_playerRigidbody != null)
        {
            float currentSpeed = Mathf.Abs(_playerRigidbody.linearVelocity.x);
            if (currentSpeed > _maxSpeedAchieved)
            {
                _maxSpeedAchieved = currentSpeed;
            }
        }
    }

    /// <summary>
    /// Updates the time spent in the current player state.
    /// </summary>
    private void UpdateStateTime()
    {
        float deltaTime = Time.deltaTime;

        switch (_currentPlayerState)
        {
            case PlayerState.Running:
                _timeInRunningState += deltaTime;
                break;
            case PlayerState.Flapping:
                _timeInFlappingState += deltaTime;
                break;
            case PlayerState.Flying:
                _timeInFlyingState += deltaTime;
                break;
        }
    }

    /// <summary>
    /// Updates the distance traveled based on player position.
    /// </summary>
    private void UpdateDistance()
    {
        if (_playerTransform != null)
        {
            _distanceTraveled = _playerTransform.position.x - _startingPlayerPosition;
        }
    }

    /// <summary>
    /// Resets all stats to initial values for a new session.
    /// </summary>
    public void ResetStats()
    {
        _sessionTime = 0f;
        _distanceTraveled = 0f;

        _chiliCoinsCollected = 0;
        _chickenCoinsCollected = 0;

        _damageTaken = 0;
        _obstaclesAvoided = 0;

        _timeInRunningState = 0f;
        _timeInFlappingState = 0f;
        _timeInFlyingState = 0f;

        _runningStateEntries = 0;
        _flappingStateEntries = 0;
        _flyingStateEntries = 0;

        _spinMovesExecuted = 0;
        _noseDivesExecuted = 0;
        _barrelRollsExecuted = 0;

        _maxSpeedAchieved = 0f;
        _totalPickupsCollected = 0;

        EnsurePlayerReferences();

        if (_playerTransform != null)
        {
            _startingPlayerPosition = _playerTransform.position.x;
        }

        if (_playerStats != null)
        {
            _previousHealth = _playerStats.CurrentHealth;
        }

        _currentPlayerState = PlayerState.Idle;
    }

    /// <summary>
    /// Registers an obstacle avoidance for performance tracking.
    /// Call this method when the player successfully avoids an obstacle.
    /// </summary>
    public void RegisterObstacleAvoidance()
    {
        _obstaclesAvoided++;
    }

    /// <summary>
    /// Returns a formatted time string (MM:SS).
    /// </summary>
    public string GetFormattedSessionTime()
    {
        int minutes = Mathf.FloorToInt(_sessionTime / 60f);
        int seconds = Mathf.FloorToInt(_sessionTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// Returns total complex movements executed.
    /// </summary>
    public int GetTotalComplexMovements()
    {
        return _spinMovesExecuted + _noseDivesExecuted + _barrelRollsExecuted;
    }
}
