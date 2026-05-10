using UnityEngine;
using System; // Required for the 'Action' event type.
using FlyShadow.EventBus;

/// <summary>
/// A singleton manager that tracks all player currency (Chili and Chicken coins).
/// It handles the logic for collecting and exchanging coins and provides an event
/// for the UI to listen to.
/// </summary>
public class CoinCounter : MonoBehaviour
{
    // === Singleton Instance ===
    public static CoinCounter instance;

    // === Events ===
    // The UI will subscribe to this event to know when to update the coin display.
    public event Action OnCoinsChanged;

    // === Public Properties ===
    public int ChiliCoins { get; private set; }
    public int ChickenCoins { get; private set; }

    // === Inspector Fields ===
    [Header("Exchange Rate")]
    [Tooltip("How many Chili Coins are required to automatically exchange for 1 Chicken Coin.")]
    [SerializeField] private int _chiliPerChicken = 10;

    [Header("Live Debugging Values")]
    [SerializeField] private int _chiliCoinsForInspector;
    [SerializeField] private int _chickenCoinsForInspector;

    // === Unity Methods ===
    private void Awake()
    {
        // Standard Singleton setup.
        if (instance == null)
        {
            instance = this;
            EventManager.Subscribe<CurrencyModifiedEvent>(OnCurrencyModified);
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
            EventManager.Unsubscribe<CurrencyModifiedEvent>(OnCurrencyModified);
        }
    }

    private void Start()
    {
        // Trigger the event once at the start to ensure the UI shows the initial '0' values.
        OnCoinsChanged?.Invoke();
        UpdateInspectorValues();
    }

    // === Public Methods ===

    /// <summary>
    /// Modifies the Chili Coin total by a given amount (can be positive or negative).
    /// </summary>
    /// <param name="amount">The amount to add or subtract.</param>
    public void ModifyChili(int amount)
    {
        ApplyCurrencyDelta(amount);
    }

    /// <summary>
    /// Modifies the Chicken Coin total by a given amount (can be positive or negative).
    /// </summary>
    /// <param name="amount">The amount to add or subtract.</param>
    public void ModifyChicken(int amount)
    {
        ChickenCoins = Mathf.Max(0, ChickenCoins + amount);
        OnCoinsChanged?.Invoke();
        UpdateInspectorValues();
    }

    // === Private Methods ===

    private void OnCurrencyModified(CurrencyModifiedEvent payload)
    {
        if (payload.Amount == 0)
        {
            return;
        }

        ApplyCurrencyDelta(payload.Amount);
    }

    private void ApplyCurrencyDelta(int amount)
    {
        if (amount > 0)
        {
            AddCurrency(amount);
        }
        else
        {
            RemoveCurrency(-amount);
        }

        OnCoinsChanged?.Invoke();
        UpdateInspectorValues();
    }

    private void AddCurrency(int amount)
    {
        ChiliCoins += amount;

        if (_chiliPerChicken <= 0)
        {
            return;
        }

        while (ChiliCoins >= _chiliPerChicken)
        {
            ChiliCoins -= _chiliPerChicken;
            ChickenCoins += 1;
        }
    }

    private void RemoveCurrency(int amount)
    {
        // First consume Chili coins.
        int chiliToUse = Mathf.Min(amount, ChiliCoins);
        ChiliCoins -= chiliToUse;
        amount -= chiliToUse;

        if (amount <= 0)
        {
            return;
        }

        if (_chiliPerChicken <= 0 || ChickenCoins <= 0)
        {
            ChiliCoins = 0;
            ChickenCoins = Mathf.Max(0, ChickenCoins);
            return;
        }

        // Convert chicken coins into chili to cover the deficit.
        int chiliPerChicken = Mathf.Max(1, _chiliPerChicken);
        int chickensNeeded = Mathf.CeilToInt(amount / (float)chiliPerChicken);
        int chickensUsed = Mathf.Min(chickensNeeded, ChickenCoins);
        ChickenCoins -= chickensUsed;
        ChiliCoins += chickensUsed * chiliPerChicken;

        ChiliCoins = Mathf.Max(0, ChiliCoins - amount);
    }

    private void UpdateInspectorValues()
    {
        _chiliCoinsForInspector = ChiliCoins;
        _chickenCoinsForInspector = ChickenCoins;
    }
}
