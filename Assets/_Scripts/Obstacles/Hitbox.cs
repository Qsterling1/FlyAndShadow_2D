using UnityEngine;

/// <summary>
/// Defines the different types of interaction zones an obstacle can have.
/// This will be selected from a dropdown in the Inspector.
/// </summary>
public enum HitboxType
{
    Front,      // For head-on collisions that deal damage.
    Top,        // For when the player lands on top, often for a bounce.
    Bottom,     // For when the player hits the underside of a floating obstacle.
    Back,       // For collisions from behind.
    WeakPoint   // A special area that might damage the obstacle instead of the player.
}

/// <summary>
/// A reusable component for creating specific hit zones on an obstacle.
/// It detects collision with the player and reports its type to the parent Obstacle script.
/// </summary>
public class Hitbox : MonoBehaviour
{
    [Header("Hitbox Configuration")]
    [Tooltip("Set the type of this hitbox. The parent Obstacle script will react based on this type.")]
    [SerializeField] private HitboxType _hitboxType = HitboxType.Front;

    // --- Private Fields ---
    private Obstacle _parentObstacle; // A reference to the main script on the parent.
    private Collider2D _collider;     // Reference to this hitbox's own collider.

    private void Awake()
    {
        // Find the main Obstacle script on the parent object.
        _parentObstacle = GetComponentInParent<Obstacle>();
        _collider = GetComponent<Collider2D>();

        // --- Error Checking ---
        if (_parentObstacle == null)
        {
            Debug.LogError("Hitbox '" + name + "' cannot find an 'Obstacle' script on its parent!", this);
        }
        if (_collider == null)
        {
            Debug.LogError("Hitbox '" + name + "' is missing a Collider2D component!", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // When the player enters our trigger zone...
        if (other.CompareTag("Player"))
        {
            // ...if we have a valid reference to our parent...
            if (_parentObstacle != null)
            {
                // ...tell the parent that it was hit, and what type of hit it was.
               _parentObstacle.HandleHit(_hitboxType, other.gameObject);
            }
        }
    }
}