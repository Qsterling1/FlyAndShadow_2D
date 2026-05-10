// GameEvent.cs

using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that serves as a global event channel.
/// Systems can raise this event without needing to know who is listening.
/// </summary>
[CreateAssetMenu(fileName = "New Game Event", menuName = "Black Boy Fly/Game Event")]
public class GameEvent : ScriptableObject
{
    // Using a C# Action is a clean and efficient way to implement the observer pattern.
    private event Action _onEventRaised;

    /// <summary>
    /// Broadcasts this event to all registered listeners.
    /// </summary>
    public void Raise()
    {
        // As requested, this log provides clear debugging information in the console.
        Debug.Log($"<color=cyan>EVENT RAISED:</color> {this.name}");
        _onEventRaised?.Invoke();
    }

    /// <summary>
    /// Registers a listener to this event.
    /// </summary>
    public void RegisterListener(Action listener)
    {
        _onEventRaised += listener;
    }

    /// <summary>
    /// Unregisters a listener from this event.
    /// </summary>
    public void UnregisterListener(Action listener)
    {
        _onEventRaised -= listener;
    }
}