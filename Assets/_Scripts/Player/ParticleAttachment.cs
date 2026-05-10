using UnityEngine;

/// <summary>
/// Simple component for attaching particle effects to GameObjects.
/// Provides Inspector-friendly controls for quick VFX prototyping without code.
/// </summary>
public class ParticleAttachment : MonoBehaviour
{
    [Header("Particle Settings")]
    [Tooltip("The particle effect prefab to spawn when triggered.")]
    [SerializeField] private ParticleSystem particleEffectPrefab;

    [Header("Trigger Options")]
    [Tooltip("Automatically play effect when this object collides with the player.")]
    [SerializeField] private bool playOnTriggerEnter = false;

    [Tooltip("Automatically play effect when this object spawns/starts.")]
    [SerializeField] private bool playOnStart = false;

    [Header("Position Override")]
    [Tooltip("Optional: Override spawn position. If null, uses this GameObject's position.")]
    [SerializeField] private Transform spawnPoint = null;

    [Header("Debug")]
    [Tooltip("Log particle effect events to console.")]
    [SerializeField] private bool debugLogging = false;

    private void Start()
    {
        if (playOnStart)
        {
            PlayEffect();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playOnTriggerEnter && other.CompareTag("Player"))
        {
            PlayEffect();
        }
    }

    /// <summary>
    /// Plays the assigned particle effect at this GameObject's position (or spawn point override).
    /// </summary>
    public void PlayEffect()
    {
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        PlayEffectAt(position);
    }

    /// <summary>
    /// Plays the assigned particle effect at a custom position.
    /// </summary>
    /// <param name="position">The world position to spawn the particle effect.</param>
    public void PlayEffectAt(Vector3 position)
    {
        if (particleEffectPrefab == null)
        {
            if (debugLogging)
            {
                Debug.LogWarning($"[ParticleAttachment] No particle prefab assigned on '{gameObject.name}'");
            }
            return;
        }

        ParticleSystem particle = null;

        // Try object pooler first for performance
        if (ObjectPooler.Instance != null)
        {
            GameObject pooledObject = ObjectPooler.Instance.SpawnFromPool(
                particleEffectPrefab.gameObject,
                position,
                Quaternion.identity
            );

            if (pooledObject != null)
            {
                particle = pooledObject.GetComponent<ParticleSystem>();
            }
        }

        // Fallback to direct instantiation if pooler unavailable or spawn failed
        if (particle == null)
        {
            particle = Instantiate(particleEffectPrefab, position, Quaternion.identity);
        }

        if (particle != null)
        {
            // Play the particle effect
            particle.Play();

            // Calculate total duration for auto-cleanup
            var main = particle.main;
            float duration = main.duration;

            // Add max lifetime to ensure all particles finish
            if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            {
                duration += main.startLifetime.constant;
            }
            else if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            {
                duration += main.startLifetime.constantMax;
            }

            // Auto-destroy after particle finishes playing
            Destroy(particle.gameObject, duration);

            if (debugLogging)
            {
                Debug.Log($"[ParticleAttachment] Played effect '{particleEffectPrefab.name}' at {position} on '{gameObject.name}'");
            }
        }
        else
        {
            Debug.LogError($"[ParticleAttachment] Failed to spawn particle effect on '{gameObject.name}'");
        }
    }
}
