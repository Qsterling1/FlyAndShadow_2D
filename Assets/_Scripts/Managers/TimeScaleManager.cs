using System.Collections.Generic;
using UnityEngine;
using FlyShadow.EventBus;

/// <summary>
/// Centralizes temporary time scale changes and restores the baseline safely.
/// </summary>
public class TimeScaleManager : MonoBehaviour
{
    [SerializeField] private float _baseTimeScale = 1f;
    [SerializeField] private float _maxTimeScale = 3f;

    private readonly Stack<ActiveTimeRequest> _requests = new Stack<ActiveTimeRequest>();
    private bool _isGamePaused;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        EventManager.Subscribe<TimeScaleRequestedEvent>(OnTimeScaleRequested);
        EventManager.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        ApplyCurrentRequest();
    }

    private void OnDestroy()
    {
        EventManager.Unsubscribe<TimeScaleRequestedEvent>(OnTimeScaleRequested);
        EventManager.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void Update()
    {
        if (_requests.Count == 0)
        {
            return;
        }

        var request = _requests.Pop();
        request.Remaining -= Time.unscaledDeltaTime;
        if (request.Remaining > 0f)
        {
            _requests.Push(request);
        }
        else
        {
            ApplyCurrentRequest();
        }
    }

    private void OnTimeScaleRequested(TimeScaleRequestedEvent payload)
    {
        float duration = Mathf.Max(0.01f, payload.Duration);
        float target = Mathf.Clamp(payload.TargetScale, 0f, _maxTimeScale);

        if (payload.ForceOverride)
        {
            _requests.Clear();
        }

        _requests.Push(new ActiveTimeRequest
        {
            TargetScale = target,
            Remaining = duration
        });

        ApplyCurrentRequest();
    }

    private void OnGameStateChanged(GameStateChangedEvent payload)
    {
        switch (payload.State)
        {
            case GameState.Paused:
            case GameState.GameOver:
            case GameState.LevelComplete:
                _isGamePaused = true;
                Time.timeScale = 0f;
                break;
            default:
                bool wasPaused = _isGamePaused;
                _isGamePaused = false;
                if (wasPaused)
                {
                    ApplyCurrentRequest();
                }
                break;
        }
    }

    private void ApplyCurrentRequest()
    {
        if (_isGamePaused)
        {
            Time.timeScale = 0f;
            return;
        }

        if (_requests.Count > 0)
        {
            var request = _requests.Peek();
            Time.timeScale = Mathf.Clamp(request.TargetScale, 0f, _maxTimeScale);
        }
        else
        {
            Time.timeScale = _baseTimeScale;
        }
    }

    private struct ActiveTimeRequest
    {
        public float TargetScale;
        public float Remaining;
    }
}
