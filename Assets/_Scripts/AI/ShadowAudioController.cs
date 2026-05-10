using UnityEngine;
using FlyShadow.EventBus;

namespace AI
{
    /// <summary>
    /// Handles Shadow AI audio integration through GameEvent system.
    /// Provides event-driven audio triggers for all shadow behaviors and state changes.
    /// </summary>
    public class ShadowAudioController : MonoBehaviour
    {
        [Header("Audio Integration")]
        [Tooltip("GameEvent triggered when shadow appears/becomes active")]
        [SerializeField] private GameEvent _onShadowAppear;
        [Tooltip("GameEvent triggered when shadow begins attack sequence")]
        [SerializeField] private GameEvent _onShadowAttack;
        [Tooltip("GameEvent triggered when shadow retreats/becomes dormant")]
        [SerializeField] private GameEvent _onShadowRetreat;
        [Tooltip("GameEvent triggered when shadow successfully hits player")]
        [SerializeField] private GameEvent _onShadowHit;
        [Tooltip("GameEvent triggered when player successfully dodges shadow attack")]
        [SerializeField] private GameEvent _onShadowDodged;

        [Header("Advanced Audio Cues")]
        [Tooltip("GameEvent for shadow perching/stalking audio")]
        [SerializeField] private GameEvent _onShadowPerching;
        [Tooltip("GameEvent for telegraph/warning audio")]
        [SerializeField] private GameEvent _onShadowTelegraph;
        [Tooltip("GameEvent for shadow taunt after failed attack")]
        [SerializeField] private GameEvent _onShadowTaunt;

        [Header("Audio Timing Configuration")]
        [Tooltip("Delay before triggering retreat audio (seconds)")]
        [SerializeField] private float _retreatAudioDelay = 0.5f;
        [Tooltip("Delay before triggering hit/dodge audio (seconds)")]
        [SerializeField] private float _attackResultAudioDelay = 0.2f;
        [Tooltip("Enable debug logging for audio events")]
        [SerializeField] private bool _debugAudioEvents = false;

        [Header("Debug Information")]
        [SerializeField, ReadOnly] private string _lastAudioEvent;
        [SerializeField, ReadOnly] private float _lastEventTime;
        [SerializeField, ReadOnly] private int _audioEventsThisSession;

        // Component references
        private ShadowAIController _shadowAI;
        private bool _initialized = false;

        // Event tracking
        private float _sessionStartTime;

        private void Awake()
        {
            InitializeAudioController();
        }

        /// <summary>
        /// Initializes the audio controller and discovers required components.
        /// </summary>
        private void InitializeAudioController()
        {
            // Auto-discover ShadowAIController
            _shadowAI = GetComponent<ShadowAIController>();
            if (_shadowAI == null)
            {
                _shadowAI = GetComponentInParent<ShadowAIController>();
            }

            if (_shadowAI == null)
            {
                Debug.LogWarning("[ShadowAudio] No ShadowAIController found. Audio integration may be limited.", this);
            }

            _sessionStartTime = Time.time;
            _initialized = true;

            LogAudioEvent("ShadowAudioController initialized");
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                InitializeAudioController();
            }

            // Subscribe to shadow AI events
            SubscribeToShadowEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromShadowEvents();
        }

        /// <summary>
        /// Subscribes to Shadow AI state change events for audio triggering.
        /// </summary>
        private void SubscribeToShadowEvents()
        {
            // Note: In a full implementation, we would subscribe to custom shadow events
            // For now, we provide public methods for the ShadowAIController to call
            LogAudioEvent("Subscribed to shadow events");
        }

        /// <summary>
        /// Unsubscribes from Shadow AI events.
        /// </summary>
        private void UnsubscribeFromShadowEvents()
        {
            // Unsubscribe from any EventBus subscriptions here
            LogAudioEvent("Unsubscribed from shadow events");
        }

        #region Public Audio Trigger Methods

        /// <summary>
        /// Triggers audio for shadow appearance/activation.
        /// </summary>
        public void TriggerShadowAppear()
        {
            TriggerGameEvent(_onShadowAppear, "Shadow Appear");
        }

        /// <summary>
        /// Triggers audio for shadow perching behavior.
        /// </summary>
        public void TriggerShadowPerching()
        {
            TriggerGameEvent(_onShadowPerching, "Shadow Perching");
        }

        /// <summary>
        /// Triggers audio for shadow telegraph/warning phase.
        /// </summary>
        public void TriggerShadowTelegraph()
        {
            TriggerGameEvent(_onShadowTelegraph, "Shadow Telegraph");
        }

        /// <summary>
        /// Triggers audio for shadow attack initiation.
        /// </summary>
        public void TriggerShadowAttack()
        {
            TriggerGameEvent(_onShadowAttack, "Shadow Attack");
        }

        /// <summary>
        /// Triggers audio for successful shadow hit on player.
        /// </summary>
        public void TriggerShadowHit()
        {
            StartCoroutine(DelayedAudioTrigger(_onShadowHit, "Shadow Hit", _attackResultAudioDelay));
        }

        /// <summary>
        /// Triggers audio for player successfully dodging shadow attack.
        /// </summary>
        public void TriggerShadowDodged()
        {
            StartCoroutine(DelayedAudioTrigger(_onShadowDodged, "Shadow Dodged", _attackResultAudioDelay));
        }

        /// <summary>
        /// Triggers audio for shadow taunt after failed attack.
        /// </summary>
        public void TriggerShadowTaunt()
        {
            TriggerGameEvent(_onShadowTaunt, "Shadow Taunt");
        }

        /// <summary>
        /// Triggers audio for shadow retreat/deactivation.
        /// </summary>
        public void TriggerShadowRetreat()
        {
            StartCoroutine(DelayedAudioTrigger(_onShadowRetreat, "Shadow Retreat", _retreatAudioDelay));
        }

        #endregion

        #region Audio Sequence Methods

        /// <summary>
        /// Triggers a complete attack sequence audio chain.
        /// </summary>
        /// <param name="wasHit">Whether the attack hit the player</param>
        public void TriggerAttackSequence(bool wasHit)
        {
            TriggerShadowAttack();

            // Schedule result audio
            if (wasHit)
            {
                TriggerShadowHit();
            }
            else
            {
                TriggerShadowDodged();
                // Optional: Add taunt after dodge
                StartCoroutine(DelayedAudioTrigger(_onShadowTaunt, "Shadow Taunt", _attackResultAudioDelay + 1f));
            }
        }

        /// <summary>
        /// Triggers a complete appearance sequence audio chain.
        /// </summary>
        public void TriggerAppearanceSequence()
        {
            TriggerShadowAppear();
            // Short delay before perching audio
            StartCoroutine(DelayedAudioTrigger(_onShadowPerching, "Shadow Perching", 0.5f));
        }

        /// <summary>
        /// Triggers a complete retreat sequence audio chain.
        /// </summary>
        public void TriggerRetreatSequence()
        {
            TriggerShadowRetreat();
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Triggers a GameEvent with debug logging.
        /// </summary>
        /// <param name="gameEvent">The GameEvent to trigger</param>
        /// <param name="eventName">Name for debugging</param>
        private void TriggerGameEvent(GameEvent gameEvent, string eventName)
        {
            if (gameEvent != null)
            {
                gameEvent.Raise();
                LogAudioEvent($"Triggered: {eventName}");
            }
            else
            {
                LogAudioEvent($"Missing GameEvent: {eventName}");
            }
        }

        /// <summary>
        /// Coroutine for triggering delayed audio events.
        /// </summary>
        private System.Collections.IEnumerator DelayedAudioTrigger(GameEvent gameEvent, string eventName, float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            TriggerGameEvent(gameEvent, eventName);
        }

        /// <summary>
        /// Logs audio events for debugging.
        /// </summary>
        /// <param name="eventDescription">Description of the audio event</param>
        private void LogAudioEvent(string eventDescription)
        {
            _lastAudioEvent = eventDescription;
            _lastEventTime = Time.time;
            _audioEventsThisSession++;

            if (_debugAudioEvents)
            {
                Debug.Log($"[ShadowAudio] {eventDescription} at {Time.time:F2}s", this);
            }
        }

        #endregion

        #region Context Menu Methods

        /// <summary>
        /// Tests shadow appearance audio (Inspector debug method).
        /// </summary>
        [ContextMenu("Test Appearance Audio")]
        private void TestAppearanceAudio()
        {
            if (Application.isPlaying)
            {
                TriggerAppearanceSequence();
            }
        }

        /// <summary>
        /// Tests shadow attack audio (Inspector debug method).
        /// </summary>
        [ContextMenu("Test Attack Audio")]
        private void TestAttackAudio()
        {
            if (Application.isPlaying)
            {
                TriggerAttackSequence(UnityEngine.Random.value > 0.5f);
            }
        }

        /// <summary>
        /// Tests shadow retreat audio (Inspector debug method).
        /// </summary>
        [ContextMenu("Test Retreat Audio")]
        private void TestRetreatAudio()
        {
            if (Application.isPlaying)
            {
                TriggerRetreatSequence();
            }
        }

        #endregion

        #region Validation and Configuration

        /// <summary>
        /// Validates audio configuration and provides Inspector feedback.
        /// </summary>
        private void OnValidate()
        {
            if (_retreatAudioDelay < 0f)
            {
                Debug.LogWarning("[ShadowAudio] Retreat audio delay should not be negative!");
            }

            if (_attackResultAudioDelay < 0f)
            {
                Debug.LogWarning("[ShadowAudio] Attack result audio delay should not be negative!");
            }
        }

        /// <summary>
        /// Gets audio configuration summary for external systems.
        /// </summary>
        public AudioConfigurationData GetAudioConfiguration()
        {
            return new AudioConfigurationData
            {
                RetreatDelay = _retreatAudioDelay,
                AttackResultDelay = _attackResultAudioDelay,
                HasAppearEvent = _onShadowAppear != null,
                HasAttackEvent = _onShadowAttack != null,
                HasRetreatEvent = _onShadowRetreat != null,
                HasHitEvent = _onShadowHit != null,
                HasDodgeEvent = _onShadowDodged != null,
                EventsTriggeredThisSession = _audioEventsThisSession
            };
        }

        #endregion
    }

    /// <summary>
    /// Data structure for Shadow AI audio configuration information.
    /// </summary>
    [System.Serializable]
    public struct AudioConfigurationData
    {
        public float RetreatDelay;
        public float AttackResultDelay;
        public bool HasAppearEvent;
        public bool HasAttackEvent;
        public bool HasRetreatEvent;
        public bool HasHitEvent;
        public bool HasDodgeEvent;
        public int EventsTriggeredThisSession;
    }
}