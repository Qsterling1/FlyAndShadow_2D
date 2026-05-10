using UnityEngine;
using System; // Required for the 'Action' event type.
using System.Collections; // Required for Coroutines.
using FlyShadow.EventBus;
/// <summary>
/// 3D2D version of PlayerStats - Uses SkinnedMeshRenderer for 3D character models.
/// Manages the player's core survival stats like Health and Stamina.
/// It also handles taking damage, invincibility, and notifies the UI when stats change.
/// </summary>
public class ThreeD2DPlayerStats : MonoBehaviour
{
    // === Singleton Instance ===
    // Provides a globally accessible reference to this script, so other scripts
    // can easily call its functions (e.g., ThreeD2DPlayerStats.instance.ModifyHealth(-10)).
    public static ThreeD2DPlayerStats instance;

    // === Events ===
    // An event that the UI can subscribe to. When stats change, we invoke this event,
    // and any listening UI elements (like health bars) will automatically update.
    public event Action OnStatsChanged;
    public event Action<int> OnPowerChanged;

    [Header("Event Channels")]
    [SerializeField] private GameEvent _onPlayerDied;
    
    [SerializeField] private GameEvent _onPlayerHurt;


    // === Public Properties ===
    // Read-only properties to allow other scripts to see the current stat values
    // without being able to change them directly.
    public int CurrentHealth => _currentHealth;
    public float CurrentStamina => _currentStamina;
    public int MaxHealth => _maxHealth;
    public float MaxStamina => _maxStamina;

    // === Inspector Fields ===
    [Header("Component References")]
    [Tooltip("The SkinnedMeshRenderer of the 3D player character. Used for the flashing effect when invincible.")]
    [SerializeField] private SkinnedMeshRenderer _meshRenderer;

    [Header("Power Settings")]
    [SerializeField] private int _currentPower;
    [SerializeField] private int _maxPower = 20;

    [Header("Health Settings")]
    [SerializeField] private int _maxHealth = 5;
    [SerializeField] private int _startingHealth = 5;

    [Header("Stamina Settings")]
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _startingStamina = 100f;
    [Tooltip("The amount of stamina regained per second.")]
    [SerializeField] private float _staminaRegenRate = 15f;
    [Tooltip("The delay in seconds after using stamina before it starts regenerating.")]
    [SerializeField] private float _staminaRegenDelay = 1f;

    [Header("Invincibility Settings")]
    [Tooltip("How long the player is invincible after taking damage, in seconds.")]
    [SerializeField] private float _invincibilityDuration = 1.5f;
    [Tooltip("How fast the player's sprite flashes while invincible.")]
    [SerializeField] private float _flashInterval = 0.1f;
    [Header("Live Debugging Values")]
    [SerializeField] private int _currentHealth;
    [SerializeField] private float _currentStamina;
    [SerializeField] private bool _isInvincible = false;

    // --- Private Fields ---
    private float _staminaRegenTimer;
    private Coroutine _invincibilityCoroutine;
    private bool _isStaminaRegenPaused = false;

    // === Unity Methods ===
    private void Awake()
    {
        // Standard Singleton setup.
        if (instance == null)
        {
            instance = this;
            EventManager.Subscribe<PlayerHealthModifiedEvent>(OnHealthModified);
            EventManager.Subscribe<PlayerStaminaModifiedEvent>(OnStaminaModified);
            EventManager.Subscribe<PlayerPowerModifiedEvent>(OnPowerModified);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            EventManager.Unsubscribe<PlayerHealthModifiedEvent>(OnHealthModified);
            EventManager.Unsubscribe<PlayerStaminaModifiedEvent>(OnStaminaModified);
            EventManager.Unsubscribe<PlayerPowerModifiedEvent>(OnPowerModified);
        }
    }

    private void Start()
    {
        // Set the initial stats when the game starts.
        _currentHealth = _startingHealth;
        _currentStamina = _startingStamina;

        // Trigger the event once at the start to make sure all UI elements
        // display the correct initial values.
        OnStatsChanged?.Invoke();
        BroadcastVitals();
        OnPowerChanged?.Invoke(_currentPower);
        EventManager.Publish(new PlayerPowerChangedEvent { CurrentPower = _currentPower });
    }

    private void Update()
    {
        HandleStaminaRegeneration();
    }

    // === Public Methods ===

    /// <summary>
    /// Modifies the player's health. Positive values heal, negative values cause damage.
    /// </summary>
    /// <param name="amount">The amount to change health by.</param>
    public void ModifyHealth(int amount)
    {
        // If we are trying to apply damage (a negative amount)...
        if (amount < 0)
        {
            // ...check if the player is currently invincible. If so, do nothing.
            if (_isInvincible)
            {
                return;
            }
            // If not invincible, apply damage and start the invincibility frames.
            if (_invincibilityCoroutine != null)
            {
                StopCoroutine(_invincibilityCoroutine);
            }
            _invincibilityCoroutine = StartCoroutine(InvincibilityCoroutine());
             _onPlayerHurt?.Raise();
        }

        // Apply the health change and ensure it doesn't go above max or below zero.
        _currentHealth = Mathf.Clamp(_currentHealth + amount, 0, _maxHealth);

        // Notify any listeners (like the UI) that stats have changed.
        OnStatsChanged?.Invoke();
        BroadcastVitals();

        // Check for death condition.
        if (_currentHealth <= 0)
        {
            _onPlayerDied?.Raise();
            Die();
        }
    }

    /// <summary>
    /// Handles player death by stopping all coroutines and disabling components.
    /// </summary>
    private void Die()
    {
        // Stop all coroutines (including invincibility flashing)
        StopAllCoroutines();

        // Make mesh visible to prevent "floating head" bug
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = true;
        }

        // Clear invincibility flag
        _isInvincible = false;

        // Disable controller to stop Update() loop
        ThreeD2DPlayerController controller = GetComponent<ThreeD2DPlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        // Disable this component to stop its Update() loop
        this.enabled = false;
    }

    private void OnHealthModified(PlayerHealthModifiedEvent e)
    {
        ModifyHealth(e.Amount);
    }

    private void OnStaminaModified(PlayerStaminaModifiedEvent e)
    {
        ModifyStamina(e.Amount);
    }

    private void OnPowerModified(PlayerPowerModifiedEvent e)
    {
        ModifyPower(e.Amount);
    }

    public int CurrentPower => _currentPower;

    /// <summary>
    /// Adds stamina to the player, for example when picking up a green feather.
    /// </summary>
    /// <param name="amount">The amount of stamina to add.</param>
    public void ModifyStamina(float amount)
    {
        _currentStamina = Mathf.Clamp(_currentStamina + amount, 0, _maxStamina);
        OnStatsChanged?.Invoke();
        BroadcastVitals();
    }

    /// <summary>
    /// Consumes a flat amount of stamina for an action like sliding or boosting.
    /// </summary>
    /// <param name="cost">The amount of stamina the action costs.</param>
    /// <returns>True if the player had enough stamina, false otherwise.</returns>
    public bool UseStamina(float cost)
    {
        if (_currentStamina >= cost)
        {
            _currentStamina -= cost;
            _staminaRegenTimer = 0f; // Reset the regen delay timer.
            OnStatsChanged?.Invoke();
            BroadcastVitals();
            return true;
        }
        return false;
    }

    public void ModifyPower(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        _currentPower = Mathf.Clamp(_currentPower + amount, 0, _maxPower);
        OnPowerChanged?.Invoke(_currentPower);
        BroadcastVitals();
        EventManager.Publish(new PlayerPowerChangedEvent { CurrentPower = _currentPower });
    }


    // === Private Methods & Coroutines ===

    private void BroadcastVitals()
    {
        EventManager.Publish(new PlayerVitalsChangedEvent
        {
            CurrentHealth = _currentHealth,
            MaxHealth = _maxHealth,
            CurrentPower = _currentPower,
            MaxPower = _maxPower
        });
    }

    /// <summary>
    /// Handles the logic for regenerating stamina over time.
    /// </summary>
    private void HandleStaminaRegeneration()
    {
        if (_isStaminaRegenPaused) return;
        // If stamina is full, do nothing.
        if (_currentStamina >= _maxStamina)
        {
            return;
        }

        // If we are still in the delay period after using stamina, increment the timer.
        if (_staminaRegenTimer < _staminaRegenDelay)
        {
            _staminaRegenTimer += Time.deltaTime;
        }
        else // Otherwise, if the delay is over, regenerate stamina.
        {
            _currentStamina += _staminaRegenRate * Time.deltaTime;
            _currentStamina = Mathf.Clamp(_currentStamina, 0, _maxStamina);
            OnStatsChanged?.Invoke();
            BroadcastVitals();
        }
    }

    /// <summary>
    /// A coroutine that makes the player invincible and flashes their mesh for a set duration.
    /// </summary>
    private IEnumerator InvincibilityCoroutine()
    {
        _isInvincible = true;

        float timer = 0f;
        while (timer < _invincibilityDuration)
        {
            // Stop invincibility if player dies during the coroutine
            if (_currentHealth <= 0)
            {
                _meshRenderer.enabled = true;
                _isInvincible = false;
                yield break;
            }

            // Toggle mesh visibility.
            _meshRenderer.enabled = !_meshRenderer.enabled;

            yield return new WaitForSeconds(_flashInterval);
            timer += _flashInterval;
        }

        // Ensure the mesh is visible at the end of the coroutine.
        _meshRenderer.enabled = true;
        _isInvincible = false;
    }
    /// <summary>
/// Immediately cancels the player's current invincibility effect.
/// </summary>
public void CancelInvincibility()
{
    if (_invincibilityCoroutine != null)
    {
        StopCoroutine(_invincibilityCoroutine);
    }
    _meshRenderer.enabled = true; // Ensure mesh is visible
    _isInvincible = false;
}

/// <summary>
/// Temporarily pauses stamina regeneration.
/// </summary>
public void PauseStaminaRegeneration(float duration)
{
    StartCoroutine(PauseStaminaRegenCoroutine(duration));
}

private IEnumerator PauseStaminaRegenCoroutine(float duration)
{
    _isStaminaRegenPaused = true;
    yield return new WaitForSeconds(duration);
    _isStaminaRegenPaused = false;
}
}





