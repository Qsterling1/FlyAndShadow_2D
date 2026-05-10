using UnityEngine;
using System.Collections.Generic;

// This class defines an entry in the Encounter library for a level.
// It includes the rules for when and how often this encounter can be chosen.
[System.Serializable]
public class EncounterEntry
{
    [Tooltip("The Encounter asset itself.")]
    public Encounter encounter;
    [Tooltip("The chance for this encounter to be chosen. Higher numbers are more likely.")]
    public int weight = 10;
    [Tooltip("This encounter cannot be chosen until the player has traveled at least this far.")]
    public float minDistanceRequired = 0f;
}

[CreateAssetMenu(fileName = "New Level Definition", menuName = "Black Boy Fly/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    [Header("Level Configuration")]
    [Tooltip("A descriptive name for the level.")]
    public string levelName = "New Level";

    [Header("Player Speed Control")]
    [Tooltip("Controls the player's speed based on distance traveled. The X-axis is distance, Y-axis is speed.")]
    public AnimationCurve playerSpeedCurve = new AnimationCurve(new Keyframe(0, 5), new Keyframe(1000, 15));

    [Header("Player Ability Rules")]
    public bool allowFlapping = false;
    public bool allowFlying = false;

    [Header("Encounter Library")]
    [Tooltip("The complete list of encounters that can appear in this level, with their selection rules.")]
    public List<EncounterEntry> encounters;
}