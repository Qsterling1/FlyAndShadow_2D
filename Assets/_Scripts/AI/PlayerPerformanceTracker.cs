using UnityEngine;
using FlyShadow.EventBus;

namespace AI
{
    /// <summary>
    /// Tracks player performance metrics to provide data for adaptive AI systems.
    /// Subscribes to EventBus for decoupled player state monitoring.
    /// </summary>
    public class PlayerPerformanceTracker : MonoBehaviour
    {
        [Header("Performance Tracking Configuration")]
        [SerializeField] private float _flightTimeThreshold = 5.0f;
        [Tooltip("Time window to track obstacle avoidance success rate")]
        [SerializeField] private float _obstacleAvoidanceWindow = 3.0f;
        [Tooltip("Minimum score to be considered high performance")]
        [SerializeField] private float _highPerformanceScore = 75f;

        [Header("Debug Information")]
        [SerializeField, ReadOnly] private float _currentPerformanceScore;
        [SerializeField, ReadOnly] private float _timeInFlightState;
        [SerializeField, ReadOnly] private int _obstaclesAvoided;
        [SerializeField, ReadOnly] private int _damageTaken;
        [SerializeField, ReadOnly] private float _sessionStartTime;

        // Performance calculation variables
        private PlayerState _currentPlayerState;
        private float _flightStateStartTime;
        private float _lastObstacleAvoidTime;
        private int _totalObstacleEncounters;
        private int _successfulAvoidances;

        // Public Properties
        public float CurrentPerformanceScore => _currentPerformanceScore;
        public bool IsHighPerformance => _currentPerformanceScore >= _highPerformanceScore;
        public float TimeInFlightState => _timeInFlightState;
        public int ObstaclesAvoided => _obstaclesAvoided;
        public int DamageTaken => _damageTaken;

        private void Awake()
        {
            _sessionStartTime = Time.time;
            ResetTracking();
        }

        private void OnEnable()
        {
            // Subscribe to relevant EventBus events for decoupled monitoring
            EventManager.Subscribe<PlayerStateChangedEvent>(OnPlayerStateChanged);
            EventManager.Subscribe<PlayerHealthModifiedEvent>(OnPlayerHealthModified);
            // Note: Obstacle avoidance events would need to be added to GameEvents.cs in future
        }

        private void OnDisable()
        {
            // Clean unsubscribe pattern for memory management
            EventManager.Unsubscribe<PlayerStateChangedEvent>(OnPlayerStateChanged);
            EventManager.Unsubscribe<PlayerHealthModifiedEvent>(OnPlayerHealthModified);
        }

        private void Update()
        {
            UpdateFlightTime();
            CalculatePerformanceScore();
        }

        /// <summary>
        /// Handles player state transitions to track performance metrics.
        /// </summary>
        /// <param name="stateEvent">Player state change event data</param>
        private void OnPlayerStateChanged(PlayerStateChangedEvent stateEvent)
        {
            PlayerState previousState = _currentPlayerState;
            _currentPlayerState = stateEvent.State;

            // Track flight state entry
            if (stateEvent.State == PlayerState.Flying && previousState != PlayerState.Flying)
            {
                _flightStateStartTime = Time.time;
            }

            // Track flight state exit
            if (previousState == PlayerState.Flying && stateEvent.State != PlayerState.Flying)
            {
                // Flight session ended - this could be good (reached goal) or bad (crashed)
                float flightSession = Time.time - _flightStateStartTime;
                if (flightSession > _flightTimeThreshold)
                {
                    // Successful extended flight counts as good performance
                    RegisterSuccessfulMoment();
                }
            }
        }

        /// <summary>
        /// Tracks damage taken for performance calculation.
        /// </summary>
        /// <param name="healthEvent">Health modification event data</param>
        private void OnPlayerHealthModified(PlayerHealthModifiedEvent healthEvent)
        {
            if (healthEvent.Amount < 0)
            {
                _damageTaken += Mathf.Abs(healthEvent.Amount);
                RegisterPerformancePenalty();
            }
        }

        /// <summary>
        /// Updates time spent in flight state.
        /// </summary>
        private void UpdateFlightTime()
        {
            if (_currentPlayerState == PlayerState.Flying)
            {
                _timeInFlightState = Time.time - _flightStateStartTime;
            }
        }

        /// <summary>
        /// Calculates current performance score based on tracked metrics.
        /// Score is 0-100 based on flight time, avoidance rate, and damage taken.
        /// </summary>
        private void CalculatePerformanceScore()
        {
            float sessionTime = Time.time - _sessionStartTime;

            // Avoid division by zero
            if (sessionTime <= 0f)
            {
                _currentPerformanceScore = 50f; // Neutral starting score
                return;
            }

            // Base score components (0-100 scale)
            float flightTimeScore = Mathf.Clamp01(_timeInFlightState / _flightTimeThreshold) * 40f;
            float avoidanceScore = (_totalObstacleEncounters > 0) ?
                (_successfulAvoidances / (float)_totalObstacleEncounters) * 30f : 30f;
            float survivalScore = Mathf.Clamp01(1f - (_damageTaken * 0.1f)) * 30f;

            _currentPerformanceScore = flightTimeScore + avoidanceScore + survivalScore;
            _currentPerformanceScore = Mathf.Clamp(_currentPerformanceScore, 0f, 100f);
        }

        /// <summary>
        /// Call this when player successfully avoids an obstacle.
        /// Note: This requires obstacle collision events to be added to EventBus system.
        /// </summary>
        public void RegisterObstacleAvoidance()
        {
            _obstaclesAvoided++;
            _successfulAvoidances++;
            _totalObstacleEncounters++;
            _lastObstacleAvoidTime = Time.time;
        }

        /// <summary>
        /// Call this when player hits an obstacle.
        /// Note: This requires obstacle collision events to be added to EventBus system.
        /// </summary>
        public void RegisterObstacleCollision()
        {
            _totalObstacleEncounters++;
            RegisterPerformancePenalty();
        }

        /// <summary>
        /// Registers a positive performance moment for score calculation.
        /// </summary>
        private void RegisterSuccessfulMoment()
        {
            // Bonus points for extended flight sessions, successful sequences, etc.
            // This method can be expanded as we identify more success patterns
        }

        /// <summary>
        /// Applies a temporary penalty to performance calculation.
        /// </summary>
        private void RegisterPerformancePenalty()
        {
            // Performance penalties are primarily handled through damage tracking
            // This method can be expanded for other penalty types
        }

        /// <summary>
        /// Resets all tracking data. Called on initialization or level restart.
        /// </summary>
        public void ResetTracking()
        {
            _currentPerformanceScore = 50f; // Neutral starting score
            _timeInFlightState = 0f;
            _obstaclesAvoided = 0;
            _damageTaken = 0;
            _flightStateStartTime = 0f;
            _lastObstacleAvoidTime = 0f;
            _totalObstacleEncounters = 0;
            _successfulAvoidances = 0;
            _currentPlayerState = PlayerState.Running;
        }

        /// <summary>
        /// Returns a performance summary for debugging or AI decision making.
        /// </summary>
        public PerformanceSummary GetPerformanceSummary()
        {
            return new PerformanceSummary
            {
                Score = _currentPerformanceScore,
                IsHighPerforming = IsHighPerformance,
                FlightTime = _timeInFlightState,
                ObstaclesAvoided = _obstaclesAvoided,
                DamageTaken = _damageTaken,
                AvoidanceRate = (_totalObstacleEncounters > 0) ?
                    _successfulAvoidances / (float)_totalObstacleEncounters : 0f
            };
        }
    }

    /// <summary>
    /// Data structure for performance summary information.
    /// </summary>
    [System.Serializable]
    public struct PerformanceSummary
    {
        public float Score;
        public bool IsHighPerforming;
        public float FlightTime;
        public int ObstaclesAvoided;
        public int DamageTaken;
        public float AvoidanceRate;
    }

    /// <summary>
    /// ReadOnly attribute for Inspector display of runtime values.
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }
}