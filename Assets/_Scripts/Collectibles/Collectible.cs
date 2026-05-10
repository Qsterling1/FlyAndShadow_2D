using System;
using System.Collections.Generic;
using UnityEngine;
using FlyShadow.EventBus;

[Serializable]
public class CollectibleEffectEntry
{
    [Header("Health")]
    public bool UseHealth;
    public int HealthAmount;

    [Header("Stamina")]
    public bool UseStamina;
    public float StaminaAmount;

    [Header("Speed")]
    public bool UseSpeed;
    public float SpeedDelta;

    [Header("Power")]
    public bool UsePower;
    public int PowerAmount;

    [Header("Currency")]
    public bool UseCurrency;
    public int CurrencyAmount;

    [Header("Audio")]
    public bool UseSound;
    public AudioCue SoundCue;

    [Header("Scale")]
    public bool UseScale;
    public float ScaleMultiplier = 1f;
    public float ScaleDuration;

    [Header("Phase Change")]
    public bool UsePhaseChange;
    [Range(-2, 2)] public int PhaseStep = 1;

    [Header("Visual Feedback")]
    public bool UseVisualFeedback;
    public float VisualGlowDelta;
    public float VisualContrastDelta;
    public float VisualDuration;

    [Header("Time Scale")]
    public bool UseTimeScale;
    public float TargetTimeScale = 0.5f;
    public float TimeDuration = 1f;
}

public class Collectible : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private List<CollectibleEffectEntry> _effects = new List<CollectibleEffectEntry>();

    [Header("Diagnostics")]
    [SerializeField] private bool _logPublishedEvents = false;

    private int _eventsPublishedThisPickup;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        _eventsPublishedThisPickup = 0;

        for (int i = 0; i < _effects.Count; i++)
        {
            ApplyEffect(_effects[i]);
        }

        if (_logPublishedEvents && _eventsPublishedThisPickup > 0)
        {
            Debug.Log($"Collectible '{name}' published {_eventsPublishedThisPickup} event(s).");
        }

        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ApplyEffect(CollectibleEffectEntry effect)
    {
        if (effect == null)
        {
            return;
        }

        if (effect.UseHealth && effect.HealthAmount != 0)
        {
            Publish(new PlayerHealthModifiedEvent { Amount = effect.HealthAmount }, $"Health {effect.HealthAmount}");
        }

        if (effect.UseStamina && !Mathf.Approximately(effect.StaminaAmount, 0f))
        {
            Publish(new PlayerStaminaModifiedEvent { Amount = effect.StaminaAmount }, $"Stamina {effect.StaminaAmount}");
        }

        if (effect.UseSpeed && !Mathf.Approximately(effect.SpeedDelta, 0f))
        {
            Publish(new PlayerSpeedModifiedEvent { Delta = effect.SpeedDelta }, $"Speed {effect.SpeedDelta}");
        }

        if (effect.UsePower && effect.PowerAmount != 0)
        {
            Publish(new PlayerPowerModifiedEvent { Amount = effect.PowerAmount }, $"Power {effect.PowerAmount}");
        }

        if (effect.UseCurrency && effect.CurrencyAmount != 0)
        {
            Publish(new CurrencyModifiedEvent { Amount = effect.CurrencyAmount }, $"Currency {effect.CurrencyAmount}");
        }

        if (effect.UseSound)
        {
            var cue = effect.SoundCue;
            if (cue != null)
            {
                Publish(new PlaySoundEvent { Cue = cue, SoundName = cue.Name }, $"Sound {cue.Name}");
            }
            else
            {
                Debug.LogWarning($"Collectible '{name}' has UseSound enabled but no AudioCue assigned.");
            }
        }

        if (effect.UseScale && effect.ScaleMultiplier > 0f && (!Mathf.Approximately(effect.ScaleMultiplier, 1f) || effect.ScaleDuration > 0f))
        {
            Publish(new PlayerScaleRequestedEvent { Multiplier = effect.ScaleMultiplier, Duration = Mathf.Max(0f, effect.ScaleDuration) }, $"Scale x{effect.ScaleMultiplier} ({effect.ScaleDuration}s)");
        }

        if (effect.UsePhaseChange && effect.PhaseStep != 0)
        {
            Publish(new PhaseChangeRequestedEvent { Step = effect.PhaseStep }, $"Phase step {effect.PhaseStep}");
        }

        if (effect.UseVisualFeedback && (effect.VisualGlowDelta != 0f || effect.VisualContrastDelta != 0f))
        {
            Publish(new VisualFeedbackRequestedEvent
            {
                GlowDelta = effect.VisualGlowDelta,
                ContrastDelta = effect.VisualContrastDelta,
                Duration = Mathf.Max(0f, effect.VisualDuration)
            }, $"Visual glow {effect.VisualGlowDelta} contrast {effect.VisualContrastDelta} ({effect.VisualDuration}s)");
        }

        if (effect.UseTimeScale && effect.TimeDuration > 0f)
        {
            Publish(new TimeScaleRequestedEvent
            {
                TargetScale = Mathf.Max(0f, effect.TargetTimeScale),
                Duration = effect.TimeDuration,
                ForceOverride = false
            }, $"Timescale {effect.TargetTimeScale} ({effect.TimeDuration}s)");
        }
    }

    private void Publish<T>(T payload, string description)
    {
        EventManager.Publish(payload);
        _eventsPublishedThisPickup++;

        if (_logPublishedEvents)
        {
            Debug.Log($"Collectible '{name}' -> {description}");
        }
    }
}
