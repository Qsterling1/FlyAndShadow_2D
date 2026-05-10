using UnityEngine;

/// <summary>
/// A manager script that controls a group of child ground tiles to create an infinite,
/// seamless scrolling ground plane. It works just like the BackgroundLooper.
/// </summary>
public class GroundLooper : MonoBehaviour
{
    [Tooltip("The object the ground should track, usually the Main Camera or Player.")]
    public Transform follow;
    [Tooltip("The number of child tile objects this script is managing.")]
    public int totalTiles = 3;

    [SerializeField]
    [Tooltip("A small overlap to prevent seams between tiles.")]
    private float _tileOverlap = 0.01f;

    private float tileWidth;
    private Transform[] tiles;

    void Start()
    {
        if (!follow) follow = Camera.main.transform;

        // Cache children and ensure totalTiles count is correct.
        totalTiles = Mathf.Min(totalTiles, transform.childCount);
        tiles = new Transform[totalTiles];
        for (int i = 0; i < totalTiles; i++)
            tiles[i] = transform.GetChild(i);

        // Measure width from the first tile's sprite.
        var sr = tiles[0].GetComponent<SpriteRenderer>();
        if(sr)
        {
            tileWidth = sr.bounds.size.x;
        } else {
            Debug.LogError("GroundLooper: The first ground tile is missing a SpriteRenderer component.");
        }

        // --- IMPORTANT ---
        // You will now position the tiles manually in the Scene view for a perfect,
        // seamless fit. This script will only handle the looping.
    }

    void LateUpdate()
    {
        if (!follow) return;

        // Check each individual tile to see if it needs to be looped.
        for (int i = 0; i < totalTiles; i++)
        {
            // Check if the tile's right edge is to the left of the camera's left edge.
            if (tiles[i].position.x + (tileWidth / 2) < (follow.position.x - (Camera.main.orthographicSize * Camera.main.aspect)))
            {
                // Find the current rightmost tile's position.
                float rightmostX = tiles[0].position.x;
                for (int j = 0; j < totalTiles; j++)
                {
                    if (tiles[j].position.x > rightmostX)
                    {
                        rightmostX = tiles[j].position.x;
                    }
                }
                // Move the off-screen tile to the far right, stitching it seamlessly.
                tiles[i].position = new Vector3(rightmostX + tileWidth - _tileOverlap, tiles[i].position.y, tiles[i].position.z);
            }
        }
    }
}