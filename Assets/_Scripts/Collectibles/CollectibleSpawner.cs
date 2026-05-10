using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Spawnable
{
    [Tooltip("The collectible prefab to spawn.")]
    public GameObject prefab;

    [Tooltip("A weight for this prefab's chance of being chosen. Higher numbers are more likely.")]
    [Range(0f, 1f)]
    public float spawnChance;
}

public class CollectibleSpawner : MonoBehaviour
{
    [Header("Collectibles List")]
    [Tooltip("The list of all prefabs that can be spawned. The sliders act as weights.")]
    public List<Spawnable> spawnableCollectibles;

    [Header("Spawn Timing")]
    public float minSpawnInterval = 1.5f;
public float maxSpawnInterval = 4f;
    public float initialSpawnDelay = 2f;
public float verticalSpawnRadius = 5f;
    // Adjusted from 3 for clarity

    [Header("Spawn Positioning")]
  
    public float xOffset = 35f;

    [Header("Required Reference")]
    public Transform player;
    [Tooltip("Optional reference to the classic PlayerController.")]
    [SerializeField] private PlayerController playerController;
    [Tooltip("Optional reference to the ThreeD2DPlayerController.")]
    [SerializeField] private ThreeD2DPlayerController threeD2DPlayerController;

    private float nextSpawnTime;

    private void Start()
    {
        AutoAssignPlayerTransform();
        nextSpawnTime = Time.time + initialSpawnDelay;
    }

    private void Update()
    {
        if (player == null)
        {
            AutoAssignPlayerTransform();
            if (player == null) return;
        }

        if (Time.time >= nextSpawnTime)
        {
            SpawnCollectible();
            float spawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnCollectible()
    {
        if (ObjectPooler.Instance == null || spawnableCollectibles.Count == 0) return;

        // --- Weighted Random Selection Logic ---
        
        // Step 1: Add up all the 'spawnChance' values to get the total size of our "pie".
        float totalChance = spawnableCollectibles.Sum(item => item.spawnChance);
        if (totalChance <= 0) return; 

        // Step 2: Pick a random point somewhere within that total size.
        float randomValue = Random.Range(0, totalChance);
        float cumulativeChance = 0f;

        // Step 3: Go through each "slice of the pie" and see where our random point landed.
        foreach (var item in spawnableCollectibles)
        {
            // Add the current item's chance to our cumulative total.
            cumulativeChance += item.spawnChance;

            // If our random point is less than or equal to the cumulative total, we've found our item!
            if (randomValue <= cumulativeChance)
            {
                if (item.prefab != null)
                {
                    float yPosition = Random.Range(player.position.y - verticalSpawnRadius, player.position.y + verticalSpawnRadius);
                    Vector3 spawnPosition = new Vector3(player.position.x + xOffset, yPosition, 0f);

                    ObjectPooler.Instance.SpawnFromPool(item.prefab, spawnPosition, Quaternion.identity);
                }
                return; // Exit the method since we've successfully spawned an item.
            }
        }
    }

    private void AutoAssignPlayerTransform()
    {
        if (player != null) return;

        if (threeD2DPlayerController == null && playerController == null)
        {
            threeD2DPlayerController = FindObjectOfType<ThreeD2DPlayerController>();
            if (threeD2DPlayerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }
        }

        if (threeD2DPlayerController != null)
        {
            player = threeD2DPlayerController.transform;
        }
        else if (playerController != null)
        {
            player = playerController.transform;
        }
    }
}
