// File: Assets/_Scripts/Events/IntGameEvent.cs
using UnityEngine;

/// <summary>
/// A concrete implementation of a GameEvent that carries an integer payload.
/// </summary>
[CreateAssetMenu(menuName = "Events/Int Game Event")]
public class IntGameEvent : GameEventWithData<int> {}
