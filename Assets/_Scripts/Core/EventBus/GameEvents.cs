using System;

namespace FlyShadow.EventBus
{
    /// <summary>
    /// Container for strongly typed game event payloads.
    /// </summary>
    public struct PlaySoundEvent
    {
        public AudioCue Cue;
        public string SoundName;
    }

    /// <summary>
    /// Raised when the player's health changes.
    /// </summary>
    public struct PlayerHealthModifiedEvent
    {
        public int Amount;
    }

    /// <summary>
    /// Raised when the player's stamina changes.
    /// </summary>
    public struct PlayerStaminaModifiedEvent
    {
        public float Amount;
    }

    /// <summary>
    /// Raised when the player's base speed should change.
    /// </summary>
    public struct PlayerSpeedModifiedEvent
    {
        public float Delta;
    }

    /// <summary>
    /// Raised when the player's power should change.
    /// </summary>
    public struct PlayerPowerModifiedEvent
    {
        public int Amount;
    }

    /// <summary>
    /// Raised when the shared currency total should change.
    /// </summary>
    public struct CurrencyModifiedEvent
    {
        public int Amount;
    }

    /// <summary>
    /// Raised when the player's scale should change.
    /// </summary>
    public struct PlayerScaleRequestedEvent
    {
        public float Multiplier;
        public float Duration;
    }

    /// <summary>
    /// Requests a phase change step for the player controller.
    /// </summary>
    public struct PhaseChangeRequestedEvent
    {
        public int Step;
    }

    /// <summary>
    /// Requests a temporary or permanent change to glow/contrast feedback.
    /// </summary>
    public struct VisualFeedbackRequestedEvent
    {
        public float GlowDelta;
        public float ContrastDelta;
        public float Duration;
    }

    /// <summary>
    /// Requests a time scale change that restores after the requested duration.
    /// </summary>
    public struct TimeScaleRequestedEvent
    {
        public float TargetScale;
        public float Duration;
        public bool ForceOverride;
    }

    /// <summary>
    /// Broadcasts power/health snapshots for presentation systems.
    /// </summary>
    public struct PlayerVitalsChangedEvent
    {
        public int CurrentHealth;
        public int MaxHealth;
        public int CurrentPower;
        public int MaxPower;
    }

    /// <summary>
    /// Broadcast when the player's state machine changes high-level state.
    /// </summary>
    public struct PlayerStateChangedEvent
    {
        public PlayerState State;
    }

    /// <summary>
    /// Raised when power thresholds relevant to progression are available.
    /// </summary>
    public struct PlayerPowerThresholdsEvent
    {
        public int DoubleJump;
        public int Flapping;
        public int Flying;
    }

    /// <summary>
    /// Raised whenever the player's power value changes so listeners can react.
    /// </summary>
    public struct PlayerPowerChangedEvent
    {
        public int CurrentPower;
    }

    /// <summary>
    /// Broadcast whenever the high level game state changes.
    /// </summary>
    public struct GameStateChangedEvent
    {
        public GameState State;
    }

    /// <summary>
    /// Published when the game mode changes (Story Mode vs Endless Mode).
    /// </summary>
    public struct GameModeChangedEvent
    {
        public GameMode Mode;
    }

    /// <summary>
    /// Raised when the player executes an advanced maneuver (spin, nose dive, barrel roll).
    /// </summary>
    public struct PlayerManeuverExecutedEvent
    {
        public ManeuverType ManeuverType;
    }

    /// <summary>
    /// Raised when the player's speed changes to track max speed achieved.
    /// </summary>
    public struct PlayerSpeedChangedEvent
    {
        public float CurrentSpeed;
        public float MaxSpeed;
    }

    /// <summary>
    /// Defines the types of advanced maneuvers the player can execute.
    /// </summary>
    public enum ManeuverType
    {
        Spin,
        NoseDive,
        BarrelRoll
    }
}
