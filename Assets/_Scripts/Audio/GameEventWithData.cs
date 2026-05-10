// File: Assets/_Scripts/Audio/GameEventWithData.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An abstract base class for ScriptableObject-based events that carry data.
/// </summary>
/// <typeparam name="T">The type of data this event will carry.</typeparam>
public abstract class GameEventWithData<T> : ScriptableObject
{
    private readonly List<GameEventListener<T>> _listeners = new List<GameEventListener<T>>();

    /// <summary>
    /// Raises the event, notifying all registered listeners.
    /// </summary>
    /// <param name="data">The data to pass to the listeners.</param>
    public void Raise(T data)
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i].OnEventRaised(data);
        }
    }

    /// <summary>
    /// Registers a listener to be notified when the event is raised.
    /// </summary>
    /// <param name="listener">The listener to register.</param>
    public void RegisterListener(GameEventListener<T> listener)
    {
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
        }
    }

    /// <summary>
    /// Unregisters a listener so it will no longer be notified.
    /// </summary>
    /// <param name="listener">The listener to unregister.</param>
    public void UnregisterListener(GameEventListener<T> listener)
    {
        if (_listeners.Contains(listener))
        {
            _listeners.Remove(listener);
        }
    }
}
