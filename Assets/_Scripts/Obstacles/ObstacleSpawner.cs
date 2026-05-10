using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A data class for configuring a single type of spawnable obstacle with a spawn weight.
/// </summary>
[System.Serializable]
public class SpawnableObstacle
{
    [Tooltip("The obstacle prefab to spawn.")]
    public GameObject prefab;

    [Tooltip("A weight for this prefab's chance of being chosen. Higher numbers are more likely.")]
    [Range(0.1f, 1f)]
    public float spawnWeight = 1f;
}

/// <summary>
/// Manages the periodic, random spawning of obstacles ahead of the player,
/// ensuring they only spawn within a defined vertical range above a ground reference point.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Pool")]
    [Tooltip("The list of all obstacle prefabs that can be spawned. The sliders act as weights.")]
    public List<SpawnableObstacle> obstaclePool;

    [Header("Spawn Timing")]
    [Tooltip("The minimum time, in seconds, between obstacle spawns.")]
    public float minSpawnInterval = 2f;
    [Tooltip("The maximum time, in seconds, between obstacle spawns.")]
    public float maxSpawnInterval = 5f;

    [Header("Spawn Positioning")]
    [Tooltip("Optional reference to the classic PlayerController script.")]
    [SerializeField] private PlayerController playerController;
    [Tooltip("Optional reference to the ThreeD2DPlayerController script.")]
    [SerializeField] private ThreeD2DPlayerController threeD2DPlayerController;
[Tooltip("How far above or below the player obstacles can spawn during Flappy/Flying states.")]
[SerializeField] private float verticalSpawnRadius = 5f;
    [Tooltip("A required reference to the player's transform.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("A required reference to your 'Ground_Group' or any object representing the ground level.")]
    [SerializeField] private Transform groundLevelReference;
    [Tooltip("How far ahead of the player obstacles will spawn.")]
    [SerializeField] private float spawnXOffset = 40f;
    [Tooltip("The minimum height above the ground reference that an obstacle can spawn.")]
    [SerializeField] private float minSpawnHeightAboveGround = 1f;
    [Tooltip("The maximum height above the ground reference that an obstacle can spawn.")]
    [SerializeField] private float maxSpawnHeightAboveGround = 10f;

    // --- Private Fields ---
    private float _nextSpawnTime;
    private float _totalSpawnWeight;
    private Func<PlayerState> _getPlayerState;

    private void Start()
    {
        // --- Input Validation ---
        // Attempt to auto-wire controller references if not assigned.
        if (playerController == null && threeD2DPlayerController == null)
        {
            threeD2DPlayerController = FindObjectOfType<ThreeD2DPlayerController>();
            if (threeD2DPlayerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }
        }

        if (threeD2DPlayerController != null)
        {
            _getPlayerState = threeD2DPlayerController.GetCurrentState;
        }
        else if (playerController != null)
        {
            _getPlayerState = playerController.GetCurrentState;
        }
        else
        {
            Debug.LogError("Obstacle Spawner could not locate a player controller!", this);
            enabled = false;
            return;
        }

        if (playerTransform == null)
        {
            if (threeD2DPlayerController != null)
            {
                playerTransform = threeD2DPlayerController.transform;
            }
            else if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
        }

        if (playerTransform == null)
        {
            Debug.LogError("Obstacle Spawner is missing the Player Transform reference!", this);
            enabled = false;
            return;
        }

        if (groundLevelReference == null)
        {
            Debug.LogError("Obstacle Spawner is missing the Ground Level Reference!", this);
            enabled = false;
            return;
        }

        // --- Initialization ---
        // Calculate the total weight of all spawnable obstacles once at the start for efficiency.
        _totalSpawnWeight = obstaclePool.Sum(obstacle => obstacle.spawnWeight);

        // Set the initial spawn time.
        _nextSpawnTime = Time.time + UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void Update()
    {
        // Check if it's time to spawn a new obstacle.
        if (Time.time >= _nextSpawnTime)
        {
            SpawnObstacle();

            // Schedule the next spawn time.
            float spawnInterval = UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval);
            _nextSpawnTime = Time.time + spawnInterval;
        }
    }

    /// <summary>
    /// Selects a random obstacle based on weight and spawns it in a valid position.
    /// </summary>
    private void SpawnObstacle()
    {
        if (ObjectPooler.Instance == null || obstaclePool.Count == 0 || _totalSpawnWeight <= 0)
        {
            return;
        }

        // --- Step 1: Select a Random Obstacle by Weight ---
        GameObject prefabToSpawn = null;
        float randomValue = UnityEngine.Random.Range(0, _totalSpawnWeight);

        foreach (var obstacle in obstaclePool)
        {
            if (randomValue <= obstacle.spawnWeight)
            {
                prefabToSpawn = obstacle.prefab;
                break;
            }
            randomValue -= obstacle.spawnWeight;
        }
        
        // Failsafe in case something goes wrong with the weights.
        if (prefabToSpawn == null && obstaclePool.Count > 0)
        {
            prefabToSpawn = obstaclePool[0].prefab;
        }

        // --- Step 2: Calculate Spawn Position ---
        if (prefabToSpawn != null)
        {
            // Horizontal position is always a fixed offset from the player.
            float spawnX = playerTransform.position.x + spawnXOffset;

            // Vertical position is a random range based on the ground reference.
            float spawnY;
        var stateProvider = _getPlayerState;
        PlayerState currentState = stateProvider != null ? stateProvider() : PlayerState.Running;

if (currentState == PlayerState.Running)

{
 // If running, lock the spawn position to the ground level
   spawnY = groundLevelReference.position.y + UnityEngine.Random.Range(minSpawnHeightAboveGround, maxSpawnHeightAboveGround);
}
else // This will handle the Flapping and Flying states
{
    // If in the air, spawn in a vertical radius around the player
    float playerY = playerTransform.position.y;
    spawnY = UnityEngine.Random.Range(playerY - verticalSpawnRadius, playerY + verticalSpawnRadius);
}

         Vector3 spawnPosition = new Vector3(spawnX, spawnY, playerTransform.position.z);


            // --- Step 3: Spawn the Obstacle from the Pool ---
            ObjectPooler.Instance.SpawnFromPool(prefabToSpawn, spawnPosition, Quaternion.identity);
        }
    }
}
