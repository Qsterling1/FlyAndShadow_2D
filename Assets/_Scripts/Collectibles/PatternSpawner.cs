using UnityEngine;
using System.Collections.Generic;

public enum PatternType
{
    Line,
    Grid,
    Arc
}

/// <summary>
/// A data-only class for configuring collectible spawn patterns in the Inspector.
/// </summary>
[System.Serializable]
public class CollectiblePattern
{
    [Tooltip("A name for this pattern for easy reference.")]
    public string patternName;

    [Tooltip("The type of pattern to spawn.")]
    public PatternType patternType;

    [Tooltip("The collectible prefab to spawn in this pattern.")]
    public GameObject collectiblePrefab;
    
    [Header("Pattern Dimensions")]
    [Tooltip("The number of items in this pattern.")]
    public int itemCount;

    [Tooltip("The spacing between each item in the pattern.")]
    public float spacing;
    
    [Tooltip("The vertical offset from the player's position where the pattern will spawn.")]
    public float yOffsetFromPlayer;

    [Tooltip("A random offset to apply to each spawned item, to create variation.")]
    public Vector2 randomOffset;
}

/// <summary>
/// A manager that spawns collectibles in a predefined pattern.
/// The pattern is selected from a list configured in the Inspector.
/// </summary>
public class PatternSpawner : MonoBehaviour
{
    [Header("Pattern List")]
    [Tooltip("The list of patterns this spawner can choose from.")]
    public List<CollectiblePattern> patterns;

    [Header("Required References")]
    [Tooltip("The player's transform, used to position the patterns relative to the player.")]
    public Transform player;

    [Header("Spawn Positioning")]
    [Tooltip("How far ahead of the player the pattern will spawn.")]
    [SerializeField] private float _spawnXOffset = 35f;

    [Header("Testing & Debugging")]
    [Tooltip("Press this key to spawn the first pattern in the list for debugging.")]
    public KeyCode debugSpawnKey = KeyCode.P;

    private void Update()
    {
        // Debugging key press to test the spawner
        if (Input.GetKeyDown(debugSpawnKey) && patterns.Count > 0)
        {
           int randomIndex = Random.Range(0, patterns.Count);
SpawnPattern(patterns[randomIndex]);
        }
    }

    /// <summary>
    /// Spawns a collectible pattern based on the provided configuration.
    /// </summary>
    /// <param name="pattern">The pattern to spawn.</param>
    public void SpawnPattern(CollectiblePattern pattern)
    {
        if (ObjectPooler.Instance == null || pattern.collectiblePrefab == null)
        {
            Debug.LogError("Collectible Prefab or ObjectPooler instance is not assigned for pattern: " + pattern.patternName);
            return;
        }

        Vector3 startPosition = new Vector3(
            player.position.x + _spawnXOffset, // Spawns off-screen to the right
            player.position.y + pattern.yOffsetFromPlayer,
            0f
        );

        switch (pattern.patternType)
        {
            case PatternType.Line:
                SpawnLinePattern(pattern, startPosition);
                break;
            case PatternType.Grid:
                // TODO: Implement later
                break;
            case PatternType.Arc:
                // TODO: Implement later
                break;
            default:
                // Handle single or other types
                break;
        }
    }

    private void SpawnLinePattern(CollectiblePattern pattern, Vector3 startPosition)
    {
        for (int i = 0; i < pattern.itemCount; i++)
        {
            Vector3 spawnPosition = startPosition + new Vector3(pattern.spacing * i, 0, 0);

            // Add a random offset if it's not a zero vector
            if (pattern.randomOffset != Vector2.zero)
            {
                spawnPosition += new Vector3(
                    Random.Range(-pattern.randomOffset.x, pattern.randomOffset.x),
                    Random.Range(-pattern.randomOffset.y, pattern.randomOffset.y),
                    0
                );
            }

            ObjectPooler.Instance.SpawnFromPool(pattern.collectiblePrefab, spawnPosition, Quaternion.identity);
        }
    }
}