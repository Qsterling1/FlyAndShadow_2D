using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using FlyShadow.EventBus;

[System.Serializable]
public class PowerPhase
{
    public string phaseName = "New Phase";
    [Tooltip("The base sprite for the meter during this phase.")]
    public Sprite meterBaseSprite;
    [Tooltip("The color of the meter's fill during this phase.")]
    public Color meterFillColor = Color.white;
    [Tooltip("The TOTAL number of black feathers required to COMPLETE this phase.")]
   public int powerRequiredToComplete;
}

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("State Panels")]
    [Tooltip("Parent canvas group visible during normal gameplay.")]
    [SerializeField] private GameObject _hudGroup; // The parent of HealthBar, StaminaBar etc.
    [Tooltip("UI container displayed while the game is paused.")]
    [SerializeField] private GameObject _pausePanel;
    [Tooltip("Panel shown when the player runs out of health.")]
    [SerializeField] private GameObject _gameOverPanel;
    [Tooltip("Panel shown after the level is completed.")]
    [SerializeField] private GameObject _levelCompletePanel;
    [Header("Master Feature Toggles")]
    [SerializeField] private bool _showHealthBar = true;
    [SerializeField] private bool _showStaminaBar = true;
    [SerializeField] private bool _showPowerMeter = true;
    [SerializeField] private bool _enableFeedbackEffects = true;

    [Header("Component References")]
    [SerializeField] private GameObject _healthBarGroup;
    [SerializeField] private GameObject _staminaBarGroup;
    [SerializeField] private GameObject _powerMeterGroup;

    [Header("Health Bar")]
    [SerializeField] private Image _healthBarFill;

    [Header("Stamina Bar")]
    [SerializeField] private Image _staminaBarFill;
    [SerializeField] private Animator _staminaBarAnimator;

    [Header("Power Meter")]
    [SerializeField] private List<PowerPhase> _powerPhases;
    [SerializeField] private Image _powerMeterBase;
    [SerializeField] private Image _powerMeterFill;
    [SerializeField] private Image _fullPowerOverlay;
    [SerializeField] private ParticleSystem _phaseShiftParticles;

    [Header("Currency Display")]
    [SerializeField] private GameObject _tempCoinDisplayParent;
    [SerializeField] private TextMeshProUGUI _chiliCoinCountText;
    [SerializeField] private TextMeshProUGUI _chickenCoinCountText;

    [Header("Coin Counter Visibility Settings")]
    [Tooltip("How long (in seconds) the coin counter stays visible after a coin is collected.\n\n" +
             "• Set to 0 or negative = DISABLED (never appears)\n" +
             "• Set to a positive number (e.g., 3.0) = TIMED (appears for that many seconds)\n" +
             "• Set to Infinity = INDEFINITE (appears and stays on screen permanently)")]
    [SerializeField] private float _coinDisplayDuration = 3f;

    [Header("Stats Display (USB)")]
    [SerializeField] private GameObject _statsUSBParent;
    [SerializeField] private TextMeshProUGUI _statsUSBText;
    [SerializeField] private Animator _statsUSBAnimator;

    [Header("Feedback Effect Settings")]
    [SerializeField] private Color _staminaFailColor = Color.yellow;
    [SerializeField] private float _staminaFailDuration = 0.5f;
    [SerializeField] private Color _negativeFeedbackColor = Color.white;
    [SerializeField] private float _negativeFeedbackDuration = 0.5f;

    private Color _staminaDefaultColor;
    private int _currentPhaseIndex = 0;
    private Coroutine _activeStaminaFailCoroutine;
    private Coroutine _activeNegativeFeedbackCoroutine;
    private Coroutine _activeCoinDisplayCoroutine;
    private Coroutine _activeStatsUSBCoroutine;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (PlayerStats.instance != null) PlayerStats.instance.OnStatsChanged += UpdateHealthAndStaminaBars;
        EventManager.Subscribe<PlayerPowerChangedEvent>(OnPlayerPowerChanged);
        EventManager.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        if (CoinCounter.instance != null) CoinCounter.instance.OnCoinsChanged += HandleCoinUpdate;
    }

    private void OnDisable()
    {
        if (PlayerStats.instance != null) PlayerStats.instance.OnStatsChanged -= UpdateHealthAndStaminaBars;
        EventManager.Unsubscribe<PlayerPowerChangedEvent>(OnPlayerPowerChanged);
        EventManager.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        if (CoinCounter.instance != null) CoinCounter.instance.OnCoinsChanged -= HandleCoinUpdate;
    }

    private void OnPlayerPowerChanged(PlayerPowerChangedEvent payload)
    {
        UpdatePowerMeter(payload.CurrentPower);
    }

    private void Start()
    {
        _healthBarGroup.SetActive(_showHealthBar);
        _staminaBarGroup.SetActive(_showStaminaBar);
        _powerMeterGroup.SetActive(_showPowerMeter);

        if (_staminaBarFill != null) _staminaDefaultColor = _staminaBarFill.color;

        _statsUSBParent.SetActive(false);
        _tempCoinDisplayParent.SetActive(false);
        if (_pausePanel != null) _pausePanel.SetActive(false);
        if (_fullPowerOverlay != null) _fullPowerOverlay.gameObject.SetActive(false);

        InitializePowerMeter();
        UpdateHealthAndStaminaBars();
    }

    private void InitializePowerMeter()
    {
        if (!_showPowerMeter || _powerPhases.Count == 0) return;

        _currentPhaseIndex = 0;
        PowerPhase initialPhase = _powerPhases[0];
        _powerMeterBase.sprite = initialPhase.meterBaseSprite;
        _powerMeterFill.color = initialPhase.meterFillColor;
    }

    private void UpdateHealthAndStaminaBars()
    {
        if (PlayerStats.instance == null) return;

        if (_showHealthBar) _healthBarFill.fillAmount = (float)PlayerStats.instance.CurrentHealth / PlayerStats.instance.MaxHealth;

        if (_showStaminaBar)
        {
            _staminaBarFill.fillAmount = PlayerStats.instance.CurrentStamina / PlayerStats.instance.MaxStamina;
            bool isStaminaFull = PlayerStats.instance.CurrentStamina >= PlayerStats.instance.MaxStamina;
            if (_enableFeedbackEffects) _staminaBarAnimator.SetBool("isPulsing", isStaminaFull);
        }
    }

    private void UpdatePowerMeter(int currentPower)
    {
        if (!_showPowerMeter || _powerPhases.Count == 0) return;

        if (_currentPhaseIndex < _powerPhases.Count - 1 && currentPower >= _powerPhases[_currentPhaseIndex].powerRequiredToComplete)
        {
            _currentPhaseIndex++;
            TriggerPhaseShift();
        }
        // NEW: handle downshift when feathers (power) drop
        while (_currentPhaseIndex > 0 &&
       currentPower < _powerPhases[_currentPhaseIndex - 1].powerRequiredToComplete)
        {
            _currentPhaseIndex--;
            TriggerPhaseShift();
        }


        float powerRequiredForThisPhase = _powerPhases[_currentPhaseIndex].powerRequiredToComplete;
        float powerRequiredForLastPhase = (_currentPhaseIndex > 0) ? _powerPhases[_currentPhaseIndex - 1].powerRequiredToComplete : 0;
        float powerInCurrentPhase = currentPower - powerRequiredForLastPhase;
        float phaseDuration = powerRequiredForThisPhase - powerRequiredForLastPhase;
        _powerMeterFill.fillAmount = (phaseDuration > 0) ? Mathf.Clamp01(powerInCurrentPhase / phaseDuration) : 1f;
        bool isFinalPhaseComplete = _currentPhaseIndex >= _powerPhases.Count - 1 && _powerMeterFill.fillAmount >= 1f;
        _fullPowerOverlay.gameObject.SetActive(isFinalPhaseComplete);
    }

    private void TriggerPhaseShift()
    {
        PowerPhase newPhase = _powerPhases[_currentPhaseIndex];
        _powerMeterBase.sprite = newPhase.meterBaseSprite;
        _powerMeterFill.color = newPhase.meterFillColor;

        if (_enableFeedbackEffects && _phaseShiftParticles != null) _phaseShiftParticles.Play();

        ShowStatsUSBMessage(newPhase.phaseName + " Unlocked!", 4f);
    }

    private void HandleCoinUpdate()
    {
#if UNITY_EDITOR
        Debug.Log("--- CHECKPOINT 3: UIManager.cs heard the event from CoinCounter and is updating the UI.");
#endif
        if (_activeCoinDisplayCoroutine != null) StopCoroutine(_activeCoinDisplayCoroutine);
        _activeCoinDisplayCoroutine = StartCoroutine(ShowCoinUpdate_Coroutine());
    }

    private IEnumerator ShowCoinUpdate_Coroutine()
    {
        // Update the coin count text
        _chiliCoinCountText.text = CoinCounter.instance.ChiliCoins.ToString();
        _chickenCoinCountText.text = CoinCounter.instance.ChickenCoins.ToString();

        // DISABLED Mode: Duration is 0 or negative
        if (_coinDisplayDuration <= 0f)
        {
            _tempCoinDisplayParent.SetActive(false);
            yield break;
        }

        // Show the coin counter
        _tempCoinDisplayParent.SetActive(true);

        // INDEFINITE Mode: Duration is Infinity
        if (float.IsInfinity(_coinDisplayDuration))
        {
            // Stay on screen permanently - coroutine ends, display stays active
            yield break;
        }

        // TIMED Mode: Duration is a positive number
        yield return new WaitForSeconds(_coinDisplayDuration);
        _tempCoinDisplayParent.SetActive(false);
    }

    public void TriggerStaminaFailEffect()
    {
        if (!_enableFeedbackEffects) return;
        if (_activeStaminaFailCoroutine != null) StopCoroutine(_activeStaminaFailCoroutine);
        _activeStaminaFailCoroutine = StartCoroutine(StaminaFail_Coroutine());
    }

    private IEnumerator StaminaFail_Coroutine()
    {
        _staminaBarFill.color = _staminaFailColor;
        _staminaBarAnimator.SetTrigger("Shake");
        yield return new WaitForSeconds(_staminaFailDuration);
        _staminaBarFill.color = _staminaDefaultColor;
    }

    public void TriggerNegativeFeedbackFlash()
    {
        if (!_enableFeedbackEffects) return;
        if (_activeNegativeFeedbackCoroutine != null) StopCoroutine(_activeNegativeFeedbackCoroutine);
        _activeNegativeFeedbackCoroutine = StartCoroutine(NegativeFeedbackFlash_Coroutine());
    }

    private IEnumerator NegativeFeedbackFlash_Coroutine()
    {
        _powerMeterBase.color = _negativeFeedbackColor;
        yield return new WaitForSeconds(_negativeFeedbackDuration);
        _powerMeterBase.color = Color.white;
    }

    public void ShowStatsUSBMessage(string message, float duration)
    {
        if (_activeStatsUSBCoroutine != null) StopCoroutine(_activeStatsUSBCoroutine);
        _activeStatsUSBCoroutine = StartCoroutine(ShowStatsUSBMessage_Coroutine(message, duration));
    }

    private IEnumerator ShowStatsUSBMessage_Coroutine(string message, float duration)
    {
        _statsUSBParent.SetActive(true);
        _statsUSBText.text = message;
        if (_enableFeedbackEffects) _statsUSBAnimator.SetTrigger("Buzz");
        yield return new WaitForSeconds(duration);
        _statsUSBParent.SetActive(false);
    }

    private void OnGameStateChanged(GameStateChangedEvent payload)
    {
        HandleGameStateChanged(payload.State);
    }

    private void HandleGameStateChanged(GameState newState)
    {
        SetPanelActive(_hudGroup, newState == GameState.Playing);
        SetPanelActive(_pausePanel, newState == GameState.Paused);
        SetPanelActive(_gameOverPanel, newState == GameState.GameOver);
        bool levelFinished = newState == GameState.LevelComplete || newState == GameState.DemoComplete;
        SetPanelActive(_levelCompletePanel, levelFinished);

        bool shouldPause = newState == GameState.GameOver || newState == GameState.Paused || levelFinished;
        Time.timeScale = shouldPause ? 0f : 1f;
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null && panel.activeSelf != isActive)
        {
            panel.SetActive(isActive);
        }
    }

    public void OnRestartButtonPressed()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.ReloadLevel();
        }
    }

    public void OnMainMenuButtonPressed()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.ReturnToMenu();
        }
    }

    public void OnQuitGamePressed()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.QuitGame();
        }
    }
}

