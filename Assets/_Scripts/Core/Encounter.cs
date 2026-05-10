using UnityEngine;
using System.Collections.Generic;

// This defines what type of spawner an action will be sent to.
// We can add more types here later (e.g., Enemy, PowerUp).
public enum SpawnerType
{
    Collectible,
    Obstacle,
    Feather
}

// This is a single instruction for the Level Manager to execute.
[System.Serializable]
public class SpawnAction
{
    [Tooltip("A name for this action, for organization.")]
    public string actionName;
    [Tooltip("Which spawner should handle this action?")]
    public SpawnerType targetSpawner;
    [Tooltip("The index of the pattern/prefab in the target spawner's list.")]
    public int itemIndexToSpawn;
    [Tooltip("The distance into the encounter when this action should trigger.")]
    public float spawnAtDistance;
}

[CreateAssetMenu(fileName = "New Encounter", menuName = "Black Boy Fly/Encounter")]
public class Encounter : ScriptableObject
{
    [Header("Encounter Settings")]
    [Tooltip("The total length of this encounter in meters.")]
    public float durationMeters = 100f;

    [Header("Pacing Rules")]
    [Tooltip("Prevents this encounter from being chosen again until this many meters have passed.")]
    public float cooldownMeters = 200f;
    [Tooltip("This encounter can only be chosen after the prerequisite has been completed.")]
    public Encounter prerequisite;

    [Header("Dynamic Difficulty")]
    [Tooltip("If player health is below this percentage (0-1), spawn rates of helpful items can be boosted.")]
    [Range(0, 1)] public float healthThresholdForHelp = 0.3f;

    [Header("Spawn Actions")]
    [Tooltip("The list of spawn instructions to execute during this encounter.")]
    public List<SpawnAction> spawnActions;
}