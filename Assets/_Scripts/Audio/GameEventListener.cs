// File: Assets/_Scripts/Audio/GameEventListener.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A generic, reusable component to listen for a GameEventWithData and invoke a UnityEvent.
/// </summary>
/// <typeparam name="T">The type of data this listener will respond to.</typeparam>
public abstract class GameEventListener<T> : MonoBehaviour
{
    [Header("Event Listening")]
    [SerializeField] private GameEventWithData<T> _gameEvent;
    [SerializeField] private UnityEvent<T> _unityEvent;

    private void OnEnable()
    {
        if (_gameEvent != null)
        {
            _gameEvent.RegisterListener(this);
        }
    }

    private void OnDisable()
    {
        if (_gameEvent != null)
        {
            _gameEvent.UnregisterListener(this);
        }
    }

    /// <summary>
    /// Called by the GameEvent when it is raised.
    /// </summary>
    /// <param name="data">The data passed from the event.</param>
    public void OnEventRaised(T data)
    {
        _unityEvent?.Invoke(data);
    }
}
