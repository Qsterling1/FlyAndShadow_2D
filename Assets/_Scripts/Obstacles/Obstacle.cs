using UnityEngine;
using System.Collections;

#region Effect Module Structs

// These structs are containers for our effects. Marking them as [System.Serializable]
// lets us see and edit them in the Inspector.

[System.Serializable]
public class DamageEffect
{
    [Tooltip("If checked, this effect will be applied on collision.")]
    public bool enabled;
    [Tooltip("The amount to modify. Use a NEGATIVE number for damage (e.g., -5) and a POSITIVE number for healing (e.g., 5).")]
    public int amount;

    [Tooltip("VFX to spawn at the collision point.")]
    public ParticleSystem impactVFX;
}

[System.Serializable]
public class KnockbackEffect
{
    public bool enabled;
    [Tooltip("The force applied to the player. Positive pushes away, negative pulls in.")]
    public float force;
    [Tooltip("The direction of the force relative to the collision point.")]
    public Vector2 direction = new Vector2(-1, 1); // Default is up and away
}

[System.Serializable]
public class SpeedEffect
{
    public bool enabled;
    [Tooltip("The speed multiplier to apply. > 1 for boost, < 1 for slow.")]
    public float speedMultiplier = 0.5f;
    [Tooltip("How long the effect lasts in seconds.")]
    public float duration = 3f;
}

[System.Serializable]
public class StunEffect
{
    public bool enabled;
    [Tooltip("How long the player's movement is stopped.")]
    public float duration = 0.5f;
    [Header("Feedback")]
    
    public ParticleSystem stunVFX;
}

[System.Serializable]
public class LaunchEffect
{
    public bool enabled;
    [Tooltip("The directional force to apply to the player.")]
    public Vector2 launchForce;
}

[System.Serializable]
public class GravityEffect
{
    public bool enabled;
    [Tooltip("The multiplier for the player's gravity. > 1 is heavier, < 1 is floatier.")]
    public float gravityMultiplier = 2f;
    [Tooltip("How long the effect lasts in seconds.")]
    public float duration = 3f;
}

[System.Serializable]
public class ResourceEffect
{
    public enum ResourceType { Stamina, Power }
    public bool enabled;
    [Tooltip("Which resource to affect.")]
    public ResourceType resourceToAffect;
    [Tooltip("The amount to modify the resource by. Positive drains, negative adds.")]

    public float amount;
}



    [System.Serializable]
    public class CameraShakeEffect
    {
        public bool enabled;
        [Tooltip("The intensity of the camera shake.")]
        public float magnitude = 0.1f;
        [Tooltip("The duration of the camera shake.")]
        public float duration = 0.5f;
    }

    [System.Serializable]
    public class TimeDilationEffect
    {
        public bool enabled;
        [Tooltip("The time scale to apply. < 1 for slow-mo, > 1 for fast-forward.")]
        [Range(0f, 2f)]
        public float timeScale = 0.5f;
        [Tooltip("How long the time dilation lasts.")]
        public float duration = 2f;
    }

    [System.Serializable]
    public class BounceEffect
    {
        [Tooltip("If enabled, the player will bounce off this surface.")]
        public bool enabled;
        [Tooltip("The vertical force applied to the player when they bounce.")]
        public float bounceForce = 20f;
    }

    #endregion

    /// <summary>
    /// A universal component for obstacles and interactive hazards/helpers.
    /// Its behavior is defined by a series of modular effects configured in the Inspector.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
       [SerializeField] private GameEvent _onPlayerObstacleTop;
       [SerializeField] private GameEvent _onPlayerObstacleFront;

[SerializeField] private GameEvent _onPlayerObstacleBack;

    [SerializeField] private GameEvent _onPlayerObstacleBottom;
[SerializeField] private GameEvent _onPlayerObstacleWeakPoint;



    [Header("--- CORE EFFECTS ---")]
        public DamageEffect damageEffect;
        public KnockbackEffect knockbackEffect;
        public SpeedEffect speedEffect;
        public StunEffect stunEffect;
        public BounceEffect bounceEffect;

        [Header("--- PHYSICS & CONTROL EFFECTS ---")]
        public LaunchEffect launchEffect;
        public GravityEffect gravityEffect;

        [Header("--- RESOURCE & BUFF EFFECTS ---")]
        public ResourceEffect resourceEffect;
       

        [Header("--- SENSORY & WORLD EFFECTS ---")]
        public CameraShakeEffect cameraShakeEffect;
        public TimeDilationEffect timeDilationEffect;

        private void OnCollisionEnter2D(Collision2D collision)
        {

        }

        /// <summary>
        /// This is the 'brain' method called by child Hitbox scripts.
        /// It directs the logic based on which part of the obstacle was hit.
        /// </summary>
        public void HandleHit(HitboxType type, GameObject playerObject)
        {
            // Get references to the player's components.
            PlayerStats playerStats = playerObject.GetComponent<PlayerStats>();
            PlayerController playerController = playerObject.GetComponent<PlayerController>();
            ThreeD2DPlayerStats playerStats3D2D = playerObject.GetComponent<ThreeD2DPlayerStats>();
            ThreeD2DPlayerController playerController3D2D = playerObject.GetComponent<ThreeD2DPlayerController>();

            bool has2DController = playerStats != null && playerController != null;
            bool has3D2DController = playerStats3D2D != null && playerController3D2D != null;

            if (!has2DController && !has3D2DController)
            {
                Debug.LogWarning($"Obstacle '{name}' could not find compatible player components on '{playerObject.name}'.", this);
                return;
            }

            // Decide which set of effects to apply based on the hitbox type.
            switch (type)
            {
                case HitboxType.Front:
                    if (has2DController)
                    {
                        ApplyAllFrontHitEffects(playerStats, playerController);
                    }
                    else
                    {
                        ApplyAllFrontHitEffects(playerStats3D2D, playerController3D2D);
                    }
                    _onPlayerObstacleFront?.Raise();
                    break;

                case HitboxType.Top:
                    if (bounceEffect.enabled)
                    {
                        if (has2DController)
                        {
                            ApplyBounceToPlayer(playerController);
                        }
                        else
                        {
                            ApplyBounceToPlayer(playerController3D2D);
                        }
                    }
                    _onPlayerObstacleTop?.Raise();
                    break;

            }
        }

        /// <summary>
        /// A helper method to apply all effects associated with a head-on collision.
        /// This is the new, optimized logic that calls all of your effects.
        /// </summary>
        private void ApplyAllFrontHitEffects(PlayerStats stats, PlayerController controller)
        {
            // --- Core Effects ---
            if (damageEffect.enabled) ApplyDamage(stats, controller.transform.position);
            if (knockbackEffect.enabled) ApplyKnockback(controller);
            if (speedEffect.enabled) ApplySpeed(controller);
            if (stunEffect.enabled) ApplyStun(controller);

            // --- Physics & Control Effects ---
            if (launchEffect.enabled) ApplyLaunch(controller);
            if (gravityEffect.enabled) ApplyGravity(controller);

            // --- Resource & Buff Effects ---
         

            // --- Sensory & World Effects ---
            if (cameraShakeEffect.enabled) ApplyCameraShake();
            if (timeDilationEffect.enabled) ApplyTimeDilation();
        }

        #region Effect Application Methods

        private void ApplyDamage(PlayerStats stats, Vector3 impactPosition)
        {
#if UNITY_EDITOR
            Debug.Log("--- TRYING TO APPLY DAMAGE ---");
#endif
            stats.ModifyHealth(damageEffect.amount);


            if (damageEffect.impactVFX != null)
            {
                Instantiate(damageEffect.impactVFX, impactPosition, Quaternion.identity);
            }
        }

        private void ApplyKnockback(PlayerController controller)
        {
            controller.GetRigidbody().AddForce(knockbackEffect.direction.normalized * knockbackEffect.force, ForceMode2D.Impulse);
        }

        private void ApplySpeed(PlayerController controller)
        {
            controller.ApplyTemporarySpeedChange(speedEffect.speedMultiplier, speedEffect.duration);
        }

        private void ApplyStun(PlayerController controller)
        {
            controller.ApplyStun(stunEffect.duration);
        }

        private void ApplyLaunch(PlayerController controller)
        {
            controller.GetRigidbody().AddForce(launchEffect.launchForce, ForceMode2D.Impulse);
        }

        private void ApplyGravity(PlayerController controller)
        {
            controller.ApplyTemporaryGravityChange(gravityEffect.gravityMultiplier, gravityEffect.duration);
        }

        private void ApplyResource(PlayerStats stats)
        {
            if (resourceEffect.resourceToAffect == ResourceEffect.ResourceType.Stamina)
            {
                stats.ModifyStamina(-resourceEffect.amount); // Note: we negate amount to match "positive drains"
            }
            else if (resourceEffect.resourceToAffect == ResourceEffect.ResourceType.Power)
            {
                stats.ModifyPower(-(int)resourceEffect.amount);
            }


        }

        private void ApplyBuffSteal(PlayerStats stats)
        {
            stats.CancelInvincibility();

        }

        private void ApplyCameraShake()
        {
            if (CameraController.instance != null)
            {
                CameraController.instance.Shake(cameraShakeEffect.duration, cameraShakeEffect.magnitude);
            }
        }

        private void ApplyTimeDilation()
        {
            if (TimeManager.instance != null)
            {
                TimeManager.instance.ApplyTimeDilation(timeDilationEffect.timeScale, timeDilationEffect.duration);
            }
        }

        private void ApplyBounceToPlayer(PlayerController controller)
        {
            // Reset player's vertical velocity to ensure a consistent bounce height.
            controller.GetRigidbody().linearVelocity = new Vector2(controller.GetRigidbody().linearVelocity.x, 0);

            // Apply the bounce force.
            controller.GetRigidbody().AddForce(Vector2.up * bounceEffect.bounceForce, ForceMode2D.Impulse);
        }

        #endregion

        #region 3D2D Effect Application Methods

        private void ApplyAllFrontHitEffects(ThreeD2DPlayerStats stats, ThreeD2DPlayerController controller)
        {
            if (stats == null || controller == null) return;

            if (damageEffect.enabled) ApplyDamage(stats, controller.transform.position);
            if (knockbackEffect.enabled) ApplyKnockback(controller);
            if (speedEffect.enabled) ApplySpeed(controller);
            if (stunEffect.enabled) ApplyStun(controller);

            if (launchEffect.enabled) ApplyLaunch(controller);
            if (gravityEffect.enabled) ApplyGravity(controller);

            if (cameraShakeEffect.enabled) ApplyCameraShake();
            if (timeDilationEffect.enabled) ApplyTimeDilation();
        }

        private void ApplyDamage(ThreeD2DPlayerStats stats, Vector3 impactPosition)
        {
            if (stats == null) return;

            stats.ModifyHealth(damageEffect.amount);

            if (damageEffect.impactVFX != null)
            {
                Instantiate(damageEffect.impactVFX, impactPosition, Quaternion.identity);
            }
        }

        private void ApplyKnockback(ThreeD2DPlayerController controller)
        {
            controller?.GetRigidbody()?.AddForce(knockbackEffect.direction.normalized * knockbackEffect.force, ForceMode2D.Impulse);
        }

        private void ApplySpeed(ThreeD2DPlayerController controller)
        {
            controller?.ApplyTemporarySpeedChange(speedEffect.speedMultiplier, speedEffect.duration);
        }

        private void ApplyStun(ThreeD2DPlayerController controller)
        {
            controller?.ApplyStun(stunEffect.duration);
        }

        private void ApplyLaunch(ThreeD2DPlayerController controller)
        {
            controller?.GetRigidbody()?.AddForce(launchEffect.launchForce, ForceMode2D.Impulse);
        }

        private void ApplyGravity(ThreeD2DPlayerController controller)
        {
            controller?.ApplyTemporaryGravityChange(gravityEffect.gravityMultiplier, gravityEffect.duration);
        }

        private void ApplyBounceToPlayer(ThreeD2DPlayerController controller)
        {
            if (controller == null) return;

            Rigidbody2D rb = controller.GetRigidbody();
            if (rb == null) return;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.up * bounceEffect.bounceForce, ForceMode2D.Impulse);
        }

        #endregion
    }
