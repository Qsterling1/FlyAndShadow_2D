using UnityEngine;
using TMPro;
using System.Collections;
using FlyShadow.EventBus;

/// <summary>
/// Manages the end-of-level score screen with arcade-style stat display.
/// Shows session statistics and calculates final score.
/// </summary>
public class EndGameScoreScreen : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent panel for the entire score screen.")]
    [SerializeField] private GameObject _scoreScreenPanel;

    [Header("Title & Header")]
    [Tooltip("Main title text (e.g., 'MISSION COMPLETE').")]
    [SerializeField] private TextMeshProUGUI _titleText;

    [Header("Time & Distance Section")]
    [SerializeField] private TextMeshProUGUI _sessionTimeText;
    [SerializeField] private TextMeshProUGUI _distanceTraveledText;

    [Header("Collection Section")]
    [SerializeField] private TextMeshProUGUI _chiliCoinsText;
    [SerializeField] private TextMeshProUGUI _chiliCoinsScoreText;
    [SerializeField] private TextMeshProUGUI _chickenCoinsText;
    [SerializeField] private TextMeshProUGUI _chickenCoinsScoreText;
    [SerializeField] private TextMeshProUGUI _totalPickupsText;

    [Header("Performance Section")]
    [SerializeField] private TextMeshProUGUI _damageTakenText;
    [SerializeField] private TextMeshProUGUI _obstaclesAvoidedText;
    [SerializeField] private TextMeshProUGUI _complexMovementsText;
    [SerializeField] private TextMeshProUGUI _maxSpeedText;

    [Header("State Mastery Section")]
    [SerializeField] private TextMeshProUGUI _timeInRunningText;
    [SerializeField] private TextMeshProUGUI _timeInFlappingText;
    [SerializeField] private TextMeshProUGUI _timeInFlyingText;
    [SerializeField] private TextMeshProUGUI _flightStateEntriesText;

    [Header("Final Score Section")]
    [SerializeField] private TextMeshProUGUI _finalScoreText;
    [SerializeField] private TextMeshProUGUI _rankText;

    [Header("Animation Settings")]
    [Tooltip("Time in seconds to count up each stat.")]
    [SerializeField] private float _countUpDuration = 1.5f;
    [Tooltip("Delay between each stat counting up.")]
    [SerializeField] private float _delayBetweenStats = 0.3f;
    [Tooltip("Delay before starting the count-up sequence.")]
    [SerializeField] private float _initialDelay = 0.5f;
    [Tooltip("Duration for final score count-up (usually longer).")]
    [SerializeField] private float _finalScoreCountDuration = 2.5f;

    [Header("Scoring Configuration")]
    [Tooltip("Points awarded per Chili coin.")]
    [SerializeField] private int _chiliCoinValue = 10;
    [Tooltip("Points awarded per Chicken coin.")]
    [SerializeField] private int _chickenCoinValue = 100;
    [Tooltip("Points multiplier for distance traveled.")]
    [SerializeField] private float _distanceScoreMultiplier = 0.5f;
    [Tooltip("Points multiplier for time in Flight state.")]
    [SerializeField] private float _flightTimeMultiplier = 10f;
    [Tooltip("Points awarded per complex movement executed.")]
    [SerializeField] private int _complexMovementBonus = 25;
    [Tooltip("Points penalty per damage taken.")]
    [SerializeField] private int _damagePenalty = 50;

    [Header("Rank Thresholds")]
    [Tooltip("Score thresholds for different ranks.")]
    [SerializeField] private int _sRankThreshold = 10000;
    [SerializeField] private int _aRankThreshold = 7500;
    [SerializeField] private int _bRankThreshold = 5000;
    [SerializeField] private int _cRankThreshold = 2500;

    [Header("Audio Events")]
    [Tooltip("Sound to play when a stat counts up.")]
    [SerializeField] private GameEvent _onStatCountUp;
    [Tooltip("Sound to play when final score is revealed.")]
    [SerializeField] private GameEvent _onFinalScoreReveal;

    [Header("Performance-Based Titles")]
    [Tooltip("Titles for high speed performance.")]
    [SerializeField] private string[] _highSpeedTitles = { "SPEEDSTER!", "SPEED DEMON", "SONIC SHADOW", "VELOCITY KING" };
    [Tooltip("Titles for high flight time performance.")]
    [SerializeField] private string[] _highFlightTitles = { "FREE FLYER", "SKY MASTER", "TOP FLIGHT BALLER", "AIRBORNE ACE" };
    [Tooltip("Titles for high maneuver performance.")]
    [SerializeField] private string[] _highManeuverTitles = { "STUNT MASTER", "AERIAL ACE", "TRICK KING", "COMBO CRUSHER" };
    [Tooltip("Titles for high coin collection.")]
    [SerializeField] private string[] _highCoinTitles = { "COIN COLLECTOR", "TREASURE HUNTER", "MONEY MAGNET", "GOLD RUSH" };
    [Tooltip("Titles for balanced/good overall performance.")]
    [SerializeField] private string[] _balancedTitles = { "ALL-ROUNDER", "SMOOTH OPERATOR", "MISSION COMPLETE", "WELL DONE" };
    [Tooltip("Titles for low flight time (ground-bound).")]
    [SerializeField] private string[] _lowFlightTitles = { "FLAT FLYER", "COULDN'T GET OFF THE GROUND", "GROUND BOUND", "NEEDS MORE PRACTICE" };
    [Tooltip("Titles for high damage taken.")]
    [SerializeField] private string[] _highDamageTitles = { "ROUGH LANDING", "CRASH COURSE", "BUMPY RIDE", "TOOK A BEATING" };

    [Header("Rank Flavor Text")]
    [Tooltip("Flavor text for S rank.")]
    [SerializeField] private string[] _sRankFlavor = { "LEGENDARY!", "PERFECT!", "UNSTOPPABLE!", "PHENOMENAL!" };
    [Tooltip("Flavor text for A rank.")]
    [SerializeField] private string[] _aRankFlavor = { "EXCELLENT!", "OUTSTANDING!", "GREAT JOB!", "IMPRESSIVE!" };
    [Tooltip("Flavor text for B rank.")]
    [SerializeField] private string[] _bRankFlavor = { "GOOD WORK!", "SOLID!", "NOT BAD!", "KEEP IT UP!" };
    [Tooltip("Flavor text for C rank.")]
    [SerializeField] private string[] _cRankFlavor = { "DECENT", "ROOM FOR IMPROVEMENT", "KEEP TRYING", "ALMOST THERE" };
    [Tooltip("Flavor text for D rank.")]
    [SerializeField] private string[] _dRankFlavor = { "TRY AGAIN", "BETTER LUCK NEXT TIME", "KEEP PRACTICING", "DON'T GIVE UP" };

    [Header("Performance-Based Trophy Images")]
    [Tooltip("Trophy/icon sprites for high speed performance (matches _highSpeedTitles order).")]
    [SerializeField] private Sprite[] _highSpeedTrophies;
    [Tooltip("Trophy/icon sprites for high flight time performance (matches _highFlightTitles order).")]
    [SerializeField] private Sprite[] _highFlightTrophies;
    [Tooltip("Trophy/icon sprites for high maneuver performance (matches _highManeuverTitles order).")]
    [SerializeField] private Sprite[] _highManeuverTrophies;
    [Tooltip("Trophy/icon sprites for high coin collection (matches _highCoinTitles order).")]
    [SerializeField] private Sprite[] _highCoinTrophies;
    [Tooltip("Trophy/icon sprites for balanced performance (matches _balancedTitles order).")]
    [SerializeField] private Sprite[] _balancedTrophies;
    [Tooltip("Trophy/icon sprites for low flight time (matches _lowFlightTitles order).")]
    [SerializeField] private Sprite[] _lowFlightTrophies;
    [Tooltip("Trophy/icon sprites for high damage taken (matches _highDamageTitles order).")]
    [SerializeField] private Sprite[] _highDamageTrophies;

    [Header("Trophy Display")]
    [Tooltip("UI Image component to display the trophy/icon.")]
    [SerializeField] private UnityEngine.UI.Image _trophyImage;

    [Header("Enhanced Audio Events")]
    [Tooltip("Sound when screen first appears.")]
    [SerializeField] private GameEvent _onScreenAppear;
    [Tooltip("Sound when title appears.")]
    [SerializeField] private GameEvent _onTitleAppear;
    [Tooltip("Sound when trophy image appears.")]
    [SerializeField] private GameEvent _onTrophyAppear;
    [Tooltip("Sound during each stat count-up tick.")]
    [SerializeField] private GameEvent _onStatTick;
    [Tooltip("Sound when a stat completes counting.")]
    [SerializeField] private GameEvent _onStatComplete;
    [Tooltip("Sound when final score starts counting.")]
    [SerializeField] private GameEvent _onFinalScoreStart;
    [Tooltip("Sound during final score count-up tick.")]
    [SerializeField] private GameEvent _onFinalScoreTick;
    [Tooltip("Sound when rank appears (positive ranks: S, A, B).")]
    [SerializeField] private GameEvent _onRankPositive;
    [Tooltip("Sound when rank appears (negative ranks: C, D).")]
    [SerializeField] private GameEvent _onRankNegative;

    // Private fields
    private bool _isDisplaying = false;
    private Coroutine _animationRoutine;

    private void Awake()
    {
        Debug.Log("[EndGameScoreScreen] Awake - Subscribing to GameStateChangedEvent");
        EventManager.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnDestroy()
    {
        Debug.Log("[EndGameScoreScreen] OnDestroy - Unsubscribing from GameStateChangedEvent");
        EventManager.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void Start()
    {
        if (_scoreScreenPanel != null)
        {
            _scoreScreenPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Handles game state changes to display score screen on level complete.
    /// </summary>
    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        Debug.Log($"[EndGameScoreScreen] OnGameStateChanged - State: {e.State}");
        if (e.State == GameState.LevelComplete || e.State == GameState.DemoComplete)
        {
            Debug.Log("[EndGameScoreScreen] Level/Demo Complete detected - Calling DisplayScoreScreen()");
            DisplayScoreScreen();
        }
        else
        {
            HideScoreScreen();
        }
    }

    /// <summary>
    /// Displays the score screen with animated stat count-up.
    /// </summary>
    public void DisplayScoreScreen()
    {
        Debug.Log("[EndGameScoreScreen] DisplayScoreScreen() called");

        if (_isDisplaying)
        {
            Debug.LogWarning("[EndGameScoreScreen] Already displaying, returning early");
            return;
        }

        if (SessionStatsTracker.instance == null)
        {
            Debug.LogError("EndGameScoreScreen: SessionStatsTracker instance not found!");
            return;
        }

        if (_scoreScreenPanel == null)
        {
            Debug.LogError("EndGameScoreScreen: Score screen panel reference is missing.");
            return;
        }

        Debug.Log("[EndGameScoreScreen] All checks passed, activating panel and starting animation");
        _isDisplaying = true;
        _scoreScreenPanel.SetActive(true);

        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
        }

        _animationRoutine = StartCoroutine(AnimateScoreScreen());
    }

    /// <summary>
    /// Hides the score screen.
    /// </summary>
    public void HideScoreScreen()
    {
        _isDisplaying = false;

        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
            _animationRoutine = null;
        }

        if (_scoreScreenPanel != null)
        {
            _scoreScreenPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Main coroutine that animates the entire score screen sequence.
    /// </summary>
    private IEnumerator AnimateScoreScreen()
    {
        SessionStatsTracker stats = SessionStatsTracker.instance;

        Debug.Log("=== [EndGameScoreScreen] AnimateScoreScreen Started ===");
        Debug.Log($"[EndGameScoreScreen] SessionTime: {stats.SessionTime}");
        Debug.Log($"[EndGameScoreScreen] DistanceTraveled: {stats.DistanceTraveled}");
        Debug.Log($"[EndGameScoreScreen] ChiliCoins: {stats.ChiliCoinsCollected}");
        Debug.Log($"[EndGameScoreScreen] ChickenCoins: {stats.ChickenCoinsCollected}");
        Debug.Log($"[EndGameScoreScreen] DamageTaken: {stats.DamageTaken}");
        Debug.Log($"[EndGameScoreScreen] ObstaclesAvoided: {stats.ObstaclesAvoided}");
        Debug.Log($"[EndGameScoreScreen] TimeInRunningState: {stats.TimeInRunningState}");
        Debug.Log($"[EndGameScoreScreen] TimeInFlappingState: {stats.TimeInFlappingState}");
        Debug.Log($"[EndGameScoreScreen] TimeInFlyingState: {stats.TimeInFlyingState}");
        Debug.Log($"[EndGameScoreScreen] MaxSpeedAchieved: {stats.MaxSpeedAchieved}");
        Debug.Log($"[EndGameScoreScreen] TotalPickupsCollected: {stats.TotalPickupsCollected}");
        Debug.Log($"[EndGameScoreScreen] ComplexMovements: {stats.GetTotalComplexMovements()}");
        Debug.Log("=======================================================");

        // Play screen appear sound
        _onScreenAppear?.Raise();

        yield return new WaitForSecondsRealtime(_initialDelay);

        // Generate and display performance-based title and trophy
        var (performanceTitle, performanceTrophy) = GeneratePerformanceTitleAndTrophy(stats);

        if (_titleText != null)
        {
            _titleText.text = performanceTitle;
        }
        _onTitleAppear?.Raise();
        Debug.Log($"[EndGameScoreScreen] Generated title: {performanceTitle}");

        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        // Display trophy image if available
        if (_trophyImage != null && performanceTrophy != null)
        {
            _trophyImage.sprite = performanceTrophy;
            _trophyImage.enabled = true;
            _onTrophyAppear?.Raise();
            Debug.Log($"[EndGameScoreScreen] Displayed trophy: {performanceTrophy.name}");
        }

        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        yield return StartCoroutine(AnimateText(_sessionTimeText, "Session Time: " + stats.GetFormattedSessionTime(), _countUpDuration));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        yield return StartCoroutine(AnimateFloatValue(_distanceTraveledText, 0f, stats.DistanceTraveled, "m", _countUpDuration, "Distance: ", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        int chiliCoins = stats.ChiliCoinsCollected;
        int chiliScore = chiliCoins * _chiliCoinValue;
        yield return StartCoroutine(AnimateIntValue(_chiliCoinsText, 0, chiliCoins, string.Empty, _countUpDuration, "Chili Coins: ", true));
        yield return StartCoroutine(AnimateIntValue(_chiliCoinsScoreText, 0, chiliScore, " pts", _countUpDuration * 0.5f, "", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        int chickenCoins = stats.ChickenCoinsCollected;
        int chickenScore = chickenCoins * _chickenCoinValue;
        yield return StartCoroutine(AnimateIntValue(_chickenCoinsText, 0, chickenCoins, string.Empty, _countUpDuration, "Chicken Coins: ", true));
        yield return StartCoroutine(AnimateIntValue(_chickenCoinsScoreText, 0, chickenScore, " pts", _countUpDuration * 0.5f, "", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        yield return StartCoroutine(AnimateIntValue(_totalPickupsText, 0, stats.TotalPickupsCollected, string.Empty, _countUpDuration, "Total Pickups: ", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        yield return StartCoroutine(AnimateIntValue(_damageTakenText, 0, stats.DamageTaken, string.Empty, _countUpDuration, "Damage Taken: ", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        yield return StartCoroutine(AnimateIntValue(_obstaclesAvoidedText, 0, stats.ObstaclesAvoided, string.Empty, _countUpDuration, "Obstacles Avoided: ", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        int complexMovements = stats.GetTotalComplexMovements();
        yield return StartCoroutine(AnimateIntValue(_complexMovementsText, 0, complexMovements, string.Empty, _countUpDuration, "Complex Movements: ", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        yield return StartCoroutine(AnimateFloatValue(_maxSpeedText, 0f, stats.MaxSpeedAchieved, " u/s", _countUpDuration, "Max Speed: ", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        yield return StartCoroutine(AnimateFloatValue(_timeInRunningText, 0f, stats.TimeInRunningState, "s", _countUpDuration, "Running Time: ", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        yield return StartCoroutine(AnimateFloatValue(_timeInFlappingText, 0f, stats.TimeInFlappingState, "s", _countUpDuration, "Flapping Time: ", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        yield return StartCoroutine(AnimateFloatValue(_timeInFlyingText, 0f, stats.TimeInFlyingState, "s", _countUpDuration, "Flying Time: ", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats);

        yield return StartCoroutine(AnimateIntValue(_flightStateEntriesText, 0, stats.FlyingStateEntries, " times", _countUpDuration, "Flight Entries: ", true));
        _onStatComplete?.Raise();
        yield return new WaitForSecondsRealtime(_delayBetweenStats * 2f);

        int finalScore = CalculateFinalScore(stats);

        _onFinalScoreStart?.Raise();
        yield return StartCoroutine(AnimateIntValue(_finalScoreText, 0, finalScore, string.Empty, _finalScoreCountDuration, "", true, true));
        _onFinalScoreReveal?.Raise();

        string rank = GetRank(finalScore);
        string rankFlavor = GetRankFlavorText(rank);
        if (_rankText != null)
        {
            _rankText.text = "RANK: " + rank + " - " + rankFlavor;
        }

        // Play rank sound based on performance (positive for S/A/B, negative for C/D)
        if (rank == "S" || rank == "A" || rank == "B")
        {
            _onRankPositive?.Raise();
        }
        else
        {
            _onRankNegative?.Raise();
        }

        Debug.Log($"[EndGameScoreScreen] Rank: {rank}, Flavor: {rankFlavor}");

        _animationRoutine = null;
        _isDisplaying = false;
    }

    /// <summary>
    /// Animates a text field from start to end value (integer).
    /// </summary>
    private IEnumerator AnimateIntValue(TextMeshProUGUI textField, int start, int end, string suffix, float duration, string prefix = "", bool playTickSound = false, bool isFinalScore = false)
    {
        if (textField == null)
        {
            yield break;
        }

        float elapsed = 0f;
        int previousValue = start;
        float tickInterval = 0.05f; // Play tick sound every 0.05 seconds
        float nextTickTime = tickInterval;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int currentValue = Mathf.RoundToInt(Mathf.Lerp(start, end, t));
            textField.text = prefix + currentValue.ToString() + suffix;

            // Play tick sound when value changes and enough time has passed
            if (playTickSound && currentValue != previousValue && elapsed >= nextTickTime)
            {
                if (isFinalScore)
                {
                    _onFinalScoreTick?.Raise();
                }
                else
                {
                    _onStatTick?.Raise();
                }
                nextTickTime = elapsed + tickInterval;
                previousValue = currentValue;
            }

            yield return null;
        }

        textField.text = prefix + end.ToString() + suffix;
    }

    /// <summary>
    /// Animates a text field from start to end value (float).
    /// </summary>
    private IEnumerator AnimateFloatValue(TextMeshProUGUI textField, float start, float end, string suffix, float duration, string prefix = "", bool playTickSound = false)
    {
        if (textField == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float tickInterval = 0.05f; // Play tick sound every 0.05 seconds
        float nextTickTime = tickInterval;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentValue = Mathf.Lerp(start, end, t);
            textField.text = prefix + currentValue.ToString("F1") + suffix;

            // Play tick sound at intervals
            if (playTickSound && elapsed >= nextTickTime)
            {
                _onStatTick?.Raise();
                nextTickTime = elapsed + tickInterval;
            }

            yield return null;
        }

        textField.text = prefix + end.ToString("F1") + suffix;
    }

    /// <summary>
    /// Animates text appearing (for formatted strings like time).
    /// </summary>
    private IEnumerator AnimateText(TextMeshProUGUI textField, string finalText, float duration)
    {
        if (textField == null)
        {
            yield break;
        }

        textField.text = string.Empty;
        yield return new WaitForSecondsRealtime(duration * 0.5f);
        textField.text = finalText;
    }

    /// <summary>
    /// Calculates the final score based on session statistics.
    /// </summary>
    private int CalculateFinalScore(SessionStatsTracker stats)
    {
        int score = 0;

        score += stats.ChiliCoinsCollected * _chiliCoinValue;
        score += stats.ChickenCoinsCollected * _chickenCoinValue;
        score += Mathf.RoundToInt(stats.DistanceTraveled * _distanceScoreMultiplier);
        score += Mathf.RoundToInt(stats.TimeInFlyingState * _flightTimeMultiplier);
        score += stats.GetTotalComplexMovements() * _complexMovementBonus;
        score -= stats.DamageTaken * _damagePenalty;

        return Mathf.Max(0, score);
    }

    /// <summary>
    /// Determines the rank based on final score.
    /// </summary>
    private string GetRank(int score)
    {
        if (score >= _sRankThreshold) return "S";
        if (score >= _aRankThreshold) return "A";
        if (score >= _bRankThreshold) return "B";
        if (score >= _cRankThreshold) return "C";
        return "D";
    }

    /// <summary>
    /// Generates a performance-based title and trophy by analyzing session statistics.
    /// Returns both the title string and the corresponding trophy sprite.
    /// </summary>
    private (string title, Sprite trophy) GeneratePerformanceTitleAndTrophy(SessionStatsTracker stats)
    {
        // Calculate performance metrics
        float totalSessionTime = Mathf.Max(1f, stats.SessionTime); // Avoid division by zero
        float flightPercentage = (stats.TimeInFlyingState / totalSessionTime) * 100f;
        float avgSpeed = stats.DistanceTraveled / totalSessionTime;
        int totalCoins = stats.ChiliCoinsCollected + (stats.ChickenCoinsCollected * 10);
        int totalManeuvers = stats.GetTotalComplexMovements();

        // Determine dominant performance category
        string[] selectedTitleArray;
        Sprite[] selectedTrophyArray;

        // Negative titles first (if performance is poor)
        if (flightPercentage < 10f && stats.TimeInRunningState > stats.SessionTime * 0.8f)
        {
            selectedTitleArray = _lowFlightTitles;
            selectedTrophyArray = _lowFlightTrophies;
        }
        else if (stats.DamageTaken > 5)
        {
            selectedTitleArray = _highDamageTitles;
            selectedTrophyArray = _highDamageTrophies;
        }
        // Positive titles based on dominant stat
        else if (avgSpeed > 15f && stats.MaxSpeedAchieved > 20f)
        {
            selectedTitleArray = _highSpeedTitles;
            selectedTrophyArray = _highSpeedTrophies;
        }
        else if (flightPercentage > 60f && stats.FlyingStateEntries > 3)
        {
            selectedTitleArray = _highFlightTitles;
            selectedTrophyArray = _highFlightTrophies;
        }
        else if (totalManeuvers > 5)
        {
            selectedTitleArray = _highManeuverTitles;
            selectedTrophyArray = _highManeuverTrophies;
        }
        else if (totalCoins > 50)
        {
            selectedTitleArray = _highCoinTitles;
            selectedTrophyArray = _highCoinTrophies;
        }
        else
        {
            selectedTitleArray = _balancedTitles;
            selectedTrophyArray = _balancedTrophies;
        }

        // Randomly select from the chosen category
        string title = "MISSION COMPLETE";
        Sprite trophy = null;

        if (selectedTitleArray != null && selectedTitleArray.Length > 0)
        {
            int randomIndex = Random.Range(0, selectedTitleArray.Length);
            title = selectedTitleArray[randomIndex];

            // Get corresponding trophy if available
            if (selectedTrophyArray != null && randomIndex < selectedTrophyArray.Length)
            {
                trophy = selectedTrophyArray[randomIndex];
            }
        }

        return (title, trophy);
    }

    /// <summary>
    /// Gets random flavor text for the given rank.
    /// </summary>
    private string GetRankFlavorText(string rank)
    {
        string[] flavorArray;

        switch (rank)
        {
            case "S":
                flavorArray = _sRankFlavor;
                break;
            case "A":
                flavorArray = _aRankFlavor;
                break;
            case "B":
                flavorArray = _bRankFlavor;
                break;
            case "C":
                flavorArray = _cRankFlavor;
                break;
            case "D":
                flavorArray = _dRankFlavor;
                break;
            default:
                return "";
        }

        if (flavorArray != null && flavorArray.Length > 0)
        {
            int randomIndex = Random.Range(0, flavorArray.Length);
            return flavorArray[randomIndex];
        }

        return "";
    }

    /// <summary>
    /// Public method to manually trigger score screen (for testing or button callbacks).
    /// </summary>
    public void ShowScoreScreen()
    {
        DisplayScoreScreen();
    }
}
