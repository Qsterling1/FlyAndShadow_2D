using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    public Transform follow;                  // Drag Main Camera here
    [Range(0f, 1f)] public float parallax = 0.5f;
    public int totalTiles = 3;                // Number of child sprites in this group

    [SerializeField]
    [Tooltip("A small overlap to prevent seams between tiles.")]
    private float _tileOverlap = 0.01f;

    private Camera cam;
    private float tileWidth;
    private Transform[] tiles;

    void Start()
    {
        if (!follow) follow = Camera.main.transform;
        cam = Camera.main;

        // Clamp to actual children
        totalTiles = Mathf.Min(totalTiles, transform.childCount);

        // Cache children
        tiles = new Transform[totalTiles];
        for (int i = 0; i < totalTiles; i++)
            tiles[i] = transform.GetChild(i);

        // Measure width from first sprite (includes scale)
        var sr = tiles[0].GetComponent<SpriteRenderer>();
        tileWidth = sr.bounds.size.x;

        // --- CODE REMOVED ---
        // The block of code that automatically positioned the tiles side-by-side
        // has been removed. You will now position the tiles manually in the Scene view.
    }

    void LateUpdate()
    {
        if (!cam || !follow) return;

        // Parallax follow
        Vector3 p = transform.position;
        p.x = follow.position.x * parallax;
        transform.position = p;

        // Camera left edge in world units
        float halfWidth = cam.orthographicSize * cam.aspect;
        float cameraLeft = follow.position.x - halfWidth;

        // When a tile’s right edge goes past the left edge, move it to the far right
        for (int i = 0; i < totalTiles; i++)
        {
            float rightEdge = tiles[i].position.x + tileWidth * 0.5f;
            if (rightEdge < cameraLeft)
            {
                // Find current rightmost tile
                float maxX = tiles[0].position.x;
                for (int j = 1; j < totalTiles; j++)
                    if (tiles[j].position.x > maxX) maxX = tiles[j].position.x;

                tiles[i].position = new Vector3(
                    maxX + tileWidth - _tileOverlap,     // tiny overlap to kill any blue line
                    tiles[i].position.y,
                    tiles[i].position.z
                );
            }
        }
    }
}