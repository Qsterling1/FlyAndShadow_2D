using UnityEngine;
public enum MovementType
{
    None,
    Hover,
    Bounce,
    Roll,
    Directional
}
public class ItemAnimation : MonoBehaviour
{
    [Header("Movement Type")]
    [Tooltip("Select the type of movement for this item.")]
    [SerializeField] private MovementType movementType = MovementType.Hover;

    [Header("Roll & Directional Settings")]
    [Tooltip("The speed for rolling or directional movement.")]
    [SerializeField] private float moveSpeed = 2f;
    [Tooltip("The direction of movement (e.g., X= -1, Y= 0 for left).")]
    [SerializeField] private Vector2 moveDirection = new Vector2(-1, 0);

    [Header("Bounce Settings")]
    [Tooltip("The vertical force applied when the item hits the ground.")]
    [SerializeField] private float bounceForce = 8f;
    [Tooltip("The layer that the item should consider 'ground' for bouncing.")]
    [SerializeField] private LayerMask groundLayer;

    // === Hover Animation Settings ===
    [Header("Hover Animation")]
    [Tooltip("The speed of the bobbing motion.")]
    [SerializeField] private float hoverSpeed = 1f;
    [Tooltip("The height of the bobbing motion in Unity units.")]
    [SerializeField] private float hoverHeight = 0.5f;

    // === Grow / Shrink (Pulse) Settings ===
    [Header("Pulse (Grow/Shrink) Effect")]
    [Tooltip("Enable or disable the pulsing size effect.")]
    [SerializeField] private bool enablePulse = false;
    [Tooltip("The speed of the pulse (cycles per second).")]
    [SerializeField] private float pulseSpeed = 2f;
    [Tooltip("The maximum scale multiplier (1 = normal size).")]
    [SerializeField] private float pulseScaleMultiplier = 1.2f;

    // === Particle Effect Settings ===
    [Header("Particle Effect")]
    [Tooltip("Enable or disable the particle effect on this item.")]
    [SerializeField] private bool enableParticleEffect = false;
    [Tooltip("The particle system to play when triggered.")]
    [SerializeField] private ParticleSystem _particleEffect;

    // === Glow & Sparkle Settings ===
    [Header("Glow & Sparkle Effects")]
    [Tooltip("Enable or disable the glowing outline effect.")]
    [SerializeField] private bool enableGlowEffect = false;
    [Tooltip("The SpriteRenderer that will glow.")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [Tooltip("Base glow color.")]
    [SerializeField] private Color glowColor = Color.white;
    [Tooltip("Maximum glow intensity.")]
    [SerializeField, Range(0f, 2f)] private float glowIntensity = 1f;
    [Tooltip("Glow pulsing speed.")]
    [SerializeField, Range(0.1f, 10f)] private float glowPulseSpeed = 2f;

    [Tooltip("Enable or disable a sparkle particle effect around the item.")]
    [SerializeField] private bool enableSparkleEffect = false;
    [Tooltip("The particle system for sparkles (optional).")]
    [SerializeField] private ParticleSystem sparkleEffect;
    [Tooltip("Speed of sparkle rotation or emission changes.")]
    [SerializeField, Range(0.1f, 10f)] private float sparkleSpeed = 1.5f;

    private Vector3 _initialPosition;
    private Vector3 _initialScale;
    private Rigidbody2D _rb;
    private bool _isInitialized = false;
    private Material _glowMaterial;
    private float _glowBaseIntensity;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _initialPosition = transform.position;
        _initialScale = transform.localScale;

        if (movementType == MovementType.Bounce || movementType == MovementType.Roll || movementType == MovementType.Directional)
        {
            if (_rb == null)
            {
                Debug.LogError("ItemAnimation: A Rigidbody2D is required for Bounce, Roll, or Directional movement.", this);
                enabled = false;
                return;
            }
        }

        if (movementType == MovementType.Directional)
        {
            if (_rb.bodyType == RigidbodyType2D.Kinematic)
            {
                _rb.linearVelocity = moveDirection.normalized * moveSpeed;
            }
        }

        if (enableGlowEffect && targetRenderer != null)
        {
            _glowMaterial = targetRenderer.material;
            _glowBaseIntensity = _glowMaterial.HasProperty("_OutlineWidth") ? _glowMaterial.GetFloat("_OutlineWidth") : 0f;
        }

        if (enableSparkleEffect && sparkleEffect != null)
        {
            sparkleEffect.Play();
        }

        _isInitialized = true;
    }

    void FixedUpdate()
    {
        if (!_isInitialized) return;

        switch (movementType)
        {
            case MovementType.Hover:
                float newY = _initialPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
                transform.position = new Vector3(_initialPosition.x, newY, _initialPosition.z);
                break;
            case MovementType.Roll:
                if (_rb != null)
                {
                    _rb.MoveRotation(_rb.rotation - moveSpeed * 100 * Time.fixedDeltaTime);
                }
                break;
            case MovementType.Directional:
                if (_rb != null && _rb.bodyType != RigidbodyType2D.Kinematic)
                {
                    Vector2 targetVelocity = moveDirection.normalized * moveSpeed;
                    _rb.linearVelocity = new Vector2(targetVelocity.x, _rb.linearVelocity.y);
                }
                break;
        }
    }

    void Update()
    {
        // --- Pulse Grow/Shrink ---
        if (enablePulse)
        {
            float scale = 1f + (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * (pulseScaleMultiplier - 1f);
            transform.localScale = _initialScale * scale;
        }

        // --- Glow ---
        if (enableGlowEffect && _glowMaterial != null)
        {
            float pulse = Mathf.Abs(Mathf.Sin(Time.time * glowPulseSpeed)) * glowIntensity;
            if (_glowMaterial.HasProperty("_OutlineColor"))
                _glowMaterial.SetColor("_OutlineColor", glowColor * (0.5f + pulse));
            if (_glowMaterial.HasProperty("_OutlineWidth"))
                _glowMaterial.SetFloat("_OutlineWidth", _glowBaseIntensity + pulse * 0.1f);
        }

        // --- Sparkle ---
        if (enableSparkleEffect && sparkleEffect != null)
        {
            var main = sparkleEffect.main;
            main.startRotation = Mathf.Sin(Time.time * sparkleSpeed) * Mathf.PI;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (movementType != MovementType.Bounce || _rb == null) return;
        if ((groundLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            if (collision.contacts[0].normal.y > 0.5f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, bounceForce);
            }
        }
    }

    public void TriggerEffect()
    {
        if (enableParticleEffect && _particleEffect != null)
        {
            _particleEffect.Play();
        }
    }
}
