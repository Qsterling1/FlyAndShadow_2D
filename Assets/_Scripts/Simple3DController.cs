using UnityEngine;

/// <summary>
/// A temporary 3D character controller for testing the 2.5D visual direction.
/// This script is isolated from the existing 2D systems and uses 3D physics components.
/// </summary>
/// <remarks>
/// CRITICAL: This is a test-only script. Do not reference PlayerController.cs or RigidBody2D.
/// Designed for Inspector-First configuration with all values exposed and tunable.
/// </remarks>
public class Simple3DController : MonoBehaviour
{
    #region Inspector-Configurable Variables

    [Header("Movement Settings")]
    [Tooltip("Forward movement speed along the X-axis (auto-run speed).")]
    [SerializeField] private float runSpeed = 5f;

    [Header("Jump Settings")]
    [Tooltip("Upward force applied when jumping.")]
    [SerializeField] private float jumpForce = 10f;

    [Header("Ground Check Settings")]
    [Tooltip("Length of the raycast used to detect ground.")]
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Tooltip("Layer mask defining what is considered ground.")]
    [SerializeField] private LayerMask groundLayer;

    #endregion

    #region Private Component References

    private Rigidbody _rigidbody;
    private Animator _animator;

    #endregion

    #region Private State Variables

    private bool _isGrounded;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initialize component references on startup.
    /// </summary>
    private void Awake()
    {
        // Get required components attached to this GameObject
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();

        // Validate that required components are present
        if (_rigidbody == null)
        {
            Debug.LogError("[Simple3DController] Rigidbody component is missing on " + gameObject.name);
        }

        if (_animator == null)
        {
            Debug.LogError("[Simple3DController] Animator component is missing on " + gameObject.name);
        }
    }

    /// <summary>
    /// Handle input and animation updates each frame.
    /// </summary>
    private void Update()
    {
        // Perform ground check
        CheckGroundStatus();

        // Update animator with ground status
        if (_animator != null)
        {
            _animator.SetBool("isGrounded", _isGrounded);
            _animator.SetFloat("isRunning", runSpeed);
            _animator.SetFloat("yVelocity", _rigidbody.linearVelocity.y);
        }

        // Handle jump input
        HandleJumpInput();
    }

    /// <summary>
    /// Handle physics-based movement in fixed timestep.
    /// </summary>
    private void FixedUpdate()
    {
        // Apply auto-run movement
        ApplyAutoRun();
    }

    #endregion

    #region Movement Logic

    /// <summary>
    /// Apply constant forward movement along the X-axis.
    /// </summary>
    private void ApplyAutoRun()
    {
        if (_rigidbody == null) return;

        // Create velocity with constant X movement, preserving Y velocity (for jumping/falling), Z velocity is 0
        Vector3 targetVelocity = new Vector3(runSpeed, _rigidbody.linearVelocity.y, 0f);

        // Apply the velocity to the rigidbody
        _rigidbody.linearVelocity = targetVelocity;
    }

    #endregion

    #region Jump Logic

    /// <summary>
    /// Check for ground status using a downward raycast.
    /// </summary>
    private void CheckGroundStatus()
    {
        // Cast a ray downward from the character's position
        Ray groundRay = new Ray(transform.position, Vector3.down);

        // Check if the ray hits anything in the ground layer within the check distance
        _isGrounded = Physics.Raycast(groundRay, groundCheckDistance, groundLayer);
    }

    /// <summary>
    /// Handle jump input and apply jump force when conditions are met.
    /// </summary>
    private void HandleJumpInput()
    {
        if (_rigidbody == null) return;

        // Check if spacebar is pressed and character is grounded
        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            PerformJump();
        }
    }

    /// <summary>
    /// Apply upward force to perform a jump and trigger animation.
    /// </summary>
    private void PerformJump()
    {
        // Apply upward force using the jump force value
        _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        // Trigger jump animation
        if (_animator != null)
        {
            _animator.SetTrigger("Jump");
        }
    }

    #endregion

    #region Debug Visualization

    /// <summary>
    /// Draw debug visualization for the ground check raycast in the Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw the ground check ray
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
    }

    #endregion
}
