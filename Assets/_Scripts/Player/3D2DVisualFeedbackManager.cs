using System;
using System.Collections.Generic;
using UnityEngine;
using FlyShadow.EventBus;

/// <summary>
/// 3D2D version - Smoothly applies glow/contrast feedback based on player vitals and collectible boosts.
/// Uses SkinnedMeshRenderer for 3D character models.
/// </summary>
[System.Obsolete("Dead 3D2D family. Active player uses VisualFeedbackManager. Do not wire into new code.")]
public class ThreeD2DVisualFeedbackManager : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private SkinnedMeshRenderer _meshRenderer;
    [SerializeField] private string _glowPropertyName = "_GlowIntensity";
    [SerializeField] private string _contrastPropertyName = "_Contrast";

    [Header("Phase Caps (Glow)")]
    [SerializeField] private float _runningGlowCap = 0.2f;
    [SerializeField] private float _flappingGlowCap = 0.5f;
    [SerializeField] private float _flyingGlowCap = 1f;

    [Header("Phase Caps (Contrast)")]
    [SerializeField] private float _runningContrastCap = 0.1f;
    [SerializeField] private float _flappingContrastCap = 0.35f;
    [SerializeField] private float _flyingContrastCap = 0.75f;

    [Header("Contribution Weights")]
    [Range(0f, 1f)] [SerializeField] private float _healthGlowWeight = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float _powerGlowWeight = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float _healthContrastWeight = 0.3f;
    [Range(0f, 1f)] [SerializeField] private float _powerContrastWeight = 0.7f;

    [Header("Smoothing")]
    [SerializeField] private float _glowRiseRate = 2f;
    [SerializeField] private float _glowDecayRate = 3f;
    [SerializeField] private float _contrastRiseRate = 2f;
    [SerializeField] private float _contrastDecayRate = 3f;
    [SerializeField] private float _downgradeDamping = 0.5f;

    private readonly List<ActiveVisualBoost> _boosts = new List<ActiveVisualBoost>();
    private MaterialPropertyBlock _propertyBlock;

    private float _latestHealthNormalized = 1f;
    private float _latestPowerNormalized;
    private PlayerState _currentPhase = PlayerState.Running;
    private int _currentPhaseIndex;

    private float _persistentGlowOffset;
    private float _persistentContrastOffset;

    private float _targetGlow;
    private float _targetContrast;
    private float _currentGlow;
    private float _currentContrast;

    private void Awake()
    {
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        _propertyBlock = new MaterialPropertyBlock();

        EventManager.Subscribe<PlayerVitalsChangedEvent>(OnVitalsChanged);
        EventManager.Subscribe<PlayerStateChangedEvent>(OnPlayerStateChanged);
        EventManager.Subscribe<VisualFeedbackRequestedEvent>(OnVisualFeedbackRequested);
    }

    private void OnDestroy()
    {
        EventManager.Unsubscribe<PlayerVitalsChangedEvent>(OnVitalsChanged);
        EventManager.Unsubscribe<PlayerStateChangedEvent>(OnPlayerStateChanged);
        EventManager.Unsubscribe<VisualFeedbackRequestedEvent>(OnVisualFeedbackRequested);
    }

    private void Update()
    {
        UpdateBoosts();
        UpdateTargets();
        SmoothValues();
        ApplyMaterial();
    }

    private void OnVitalsChanged(PlayerVitalsChangedEvent payload)
    {
        _latestHealthNormalized = payload.MaxHealth > 0 ? Mathf.Clamp01(payload.CurrentHealth / (float)payload.MaxHealth) : 0f;
        _latestPowerNormalized = payload.MaxPower > 0 ? Mathf.Clamp01(payload.CurrentPower / (float)payload.MaxPower) : 0f;
    }

    private void OnPlayerStateChanged(PlayerStateChangedEvent payload)
    {
        int newIndex = GetPhaseIndex(payload.State);
        if (newIndex < _currentPhaseIndex)
        {
            _persistentGlowOffset *= _downgradeDamping;
            _persistentContrastOffset *= _downgradeDamping;
            ClampPersistentOffsets();
        }

        _currentPhase = payload.State;
        _currentPhaseIndex = newIndex;
    }

    private void OnVisualFeedbackRequested(VisualFeedbackRequestedEvent payload)
    {
        if (payload.Duration <= 0f)
        {
            _persistentGlowOffset += payload.GlowDelta;
            _persistentContrastOffset += payload.ContrastDelta;
            ClampPersistentOffsets();
            return;
        }

        _boosts.Add(new ActiveVisualBoost
        {
            Glow = payload.GlowDelta,
            Contrast = payload.ContrastDelta,
            Duration = Mathf.Max(0.01f, payload.Duration),
            Remaining = Mathf.Max(0.01f, payload.Duration)
        });
    }

    private void UpdateBoosts()
    {
        float delta = Time.unscaledDeltaTime;
        for (int i = _boosts.Count - 1; i >= 0; i--)
        {
            var boost = _boosts[i];
            boost.Remaining -= delta;
            if (boost.Remaining <= 0f)
            {
                _boosts.RemoveAt(i);
            }
            else
            {
                _boosts[i] = boost;
            }
        }
    }

    private void UpdateTargets()
    {
        float phaseGlowCap = GetGlowCap(_currentPhase);
        float phaseContrastCap = GetContrastCap(_currentPhase);

        float baseGlow = Mathf.Clamp01((_latestHealthNormalized * _healthGlowWeight) + (_latestPowerNormalized * _powerGlowWeight));
        float baseContrast = Mathf.Clamp01((_latestHealthNormalized * _healthContrastWeight) + (_latestPowerNormalized * _powerContrastWeight));

        float boostGlow = _persistentGlowOffset;
        float boostContrast = _persistentContrastOffset;

        for (int i = 0; i < _boosts.Count; i++)
        {
            var boost = _boosts[i];
            float weight = Mathf.Clamp01(boost.Remaining / boost.Duration);
            boostGlow += boost.Glow * weight;
            boostContrast += boost.Contrast * weight;
        }

        _targetGlow = Mathf.Clamp(baseGlow * phaseGlowCap + boostGlow, 0f, phaseGlowCap);
        _targetContrast = Mathf.Clamp(baseContrast * phaseContrastCap + boostContrast, 0f, phaseContrastCap);
    }

    private void SmoothValues()
    {
        float delta = Time.unscaledDeltaTime;
        float glowRate = _targetGlow > _currentGlow ? _glowRiseRate : _glowDecayRate;
        float contrastRate = _targetContrast > _currentContrast ? _contrastRiseRate : _contrastDecayRate;

        _currentGlow = Mathf.MoveTowards(_currentGlow, _targetGlow, glowRate * delta);
        _currentContrast = Mathf.MoveTowards(_currentContrast, _targetContrast, contrastRate * delta);
    }

    private void ApplyMaterial()
    {
        if (_meshRenderer == null)
        {
            return;
        }

        _meshRenderer.GetPropertyBlock(_propertyBlock);

        if (!string.IsNullOrEmpty(_glowPropertyName))
        {
            _propertyBlock.SetFloat(_glowPropertyName, _currentGlow);
        }

        if (!string.IsNullOrEmpty(_contrastPropertyName))
        {
            _propertyBlock.SetFloat(_contrastPropertyName, _currentContrast);
        }

        _meshRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void ClampPersistentOffsets()
    {
        float glowCap = GetGlowCap(_currentPhase);
        float contrastCap = GetContrastCap(_currentPhase);
        _persistentGlowOffset = Mathf.Clamp(_persistentGlowOffset, -glowCap, glowCap);
        _persistentContrastOffset = Mathf.Clamp(_persistentContrastOffset, -contrastCap, contrastCap);
    }

    private float GetGlowCap(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Flying:
                return _flyingGlowCap;
            case PlayerState.Flapping:
                return _flappingGlowCap;
            default:
                return _runningGlowCap;
        }
    }

    private float GetContrastCap(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Flying:
                return _flyingContrastCap;
            case PlayerState.Flapping:
                return _flappingContrastCap;
            default:
                return _runningContrastCap;
        }
    }

    private int GetPhaseIndex(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Flying:
                return 2;
            case PlayerState.Flapping:
                return 1;
            default:
                return 0;
        }
    }

    private struct ActiveVisualBoost
    {
        public float Glow;
        public float Contrast;
        public float Duration;
        public float Remaining;
    }
}
