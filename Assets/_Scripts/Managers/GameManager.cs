using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using FlyShadow.EventBus;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Defines the possible high-level states of the game.
/// </summary>
public enum GameState
{
    MainMenu,
    LevelStarting,
    Playing,
    Paused,
    LevelComplete,
    DemoComplete,
    GameOver
}

/// <summary>
/// Defines the available game modes for play sessions.
/// </summary>
public enum GameMode
{
    StoryMode,      // Traditional level with win conditions
    EndlessMode     // Survival mode - play until death
}

/// <summary>
/// A persistent singleton that manages the overall game state, scene transitions,
/// and coordination between other manager scripts.
/// </summary>
public class GameManager : MonoBehaviour
{
    // === Singleton Instance ===
    public static GameManager instance;

    // === Events ===
    // Other scripts (like UIManager) can subscribe to this to know when the state changes.
    public event Action<GameState> OnStateChanged;

    // === Public Properties ===
    public GameState CurrentState { get; private set; }
    public GameMode CurrentGameMode { get; private set; } = GameMode.StoryMode;

    // === Inspector Fields ===
    [Header("Game Mode Configuration")]
    [Tooltip("The current game mode. Story Mode has win conditions, Endless Mode runs until death.")]
    [SerializeField] private GameMode _currentGameModeForInspector = GameMode.StoryMode;

    [Header("State Debugging")]
    [Tooltip("The current state of the game. Read-only for debugging.")]
    [SerializeField] private GameState _currentStateForInspector;
    private Transform _playerTransform;
    private float _distanceTraveled;
    [Header("State & Debugging")]
    [Tooltip("The state the game will start in. Change this for testing different scenes.")]
    [SerializeField] private GameState _initialState = GameState.MainMenu;
    [Tooltip("If true, the game will start paused immediately.")]
    [SerializeField] private bool _pauseOnStart = false;
    [Tooltip("Allows for slow-motion or fast-forward testing.")]
    [Range(0f, 5f)]
    [SerializeField] private float _debugTimeScale = 1f;

    [Header("Scene Management")]
    [Tooltip("The name of the main menu scene file.")]
    [SerializeField] private string _mainMenuSceneName;
    [Tooltip("A list of all playable level scene names, in order.")]
    [SerializeField] private List<string> _levelSceneNames;

    [Header("Level Win/Loss Conditions")]
    [Tooltip("The condition that must be met to complete the level.")]
    [SerializeField] private WinConditionType _winCondition;
    [Tooltip("The value associated with the win condition (e.g., distance in meters).")]
    [SerializeField] private float _winConditionValue;

    [Header("Global Difficulty & Scaling")]
    [Tooltip("The player's speed at the start of a run.")]
    [SerializeField] private float _initialPlayerSpeed = 5f;
    [Tooltip("The maximum speed the player can reach.")]
    [SerializeField] private float _maxPlayerSpeed = 20f;
    [Tooltip("How many units of speed are added per second.")]
    [SerializeField] private float _speedIncreaseRate = 0.1f;

    [Header("Scoring")]
    [SerializeField] private int _chiliCoinScore = 10;
    [SerializeField] private int _chickenCoinScore = 100;
    [SerializeField] private float _distanceScoreMultiplier = 0.5f;

    [Header("AI Direction")]
    [SerializeField] private bool _allowShadowSpawning = true;
    [Tooltip("The Shadow's primary behavior in this level.")]
    [SerializeField] private ShadowIntent _shadowIntentForLevel = ShadowIntent.Hostile;
    [SerializeField] private float _shadowMinSpawnCooldown = 15f;
    [SerializeField] private float _shadowMaxSpawnCooldown = 30f;

    [Header("Achievements")]
    [SerializeField] private List<Achievement> _gameAchievements;



    [Header("Game Over Event")]
    [Tooltip("Event raised when the player's health reaches zero.")]
    [SerializeField] private GameEvent _onPlayerDied;

[Header("Community & Social Links")]
    [SerializeField] private string _discordUrl;
    [SerializeField] private string _twitterUrl;
    [SerializeField] private string _websiteUrl;

    // === Unity Methods ===
    private void Awake()
    {
        // Standard Singleton setup with DontDestroyOnLoad.
        if (instance == null)
        {
            instance = this;
            // Ensure we're at root level before calling DontDestroyOnLoad
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (_onPlayerDied != null)
        {
            _onPlayerDied.RegisterListener(PlayerDied);
        }
    }

    private void OnDisable()
    {
        if (_onPlayerDied != null)
        {
            _onPlayerDied.UnregisterListener(PlayerDied);
        }
    }

    private void PlayerDied()
    {
        // In Endless Mode, death shows the scoreboard (LevelComplete)
        // In Story Mode, death shows the Game Over screen
        if (CurrentGameMode == GameMode.EndlessMode)
        {
            Debug.Log("[GameManager] Player died in Endless Mode - Showing scoreboard");
            ChangeState(GameState.LevelComplete);
        }
        else
        {
            Debug.Log("[GameManager] Player died in Story Mode - Game Over");
            ChangeState(GameState.GameOver);
        }
    }

    private void Start()
    {
        // Set the time scale for debugging purposes.
        Time.timeScale = _debugTimeScale;

        // Set the initial state based on the Inspector setting.
        ChangeState(_initialState);

        // If the "Pause On Start" flag is checked, immediately pause the game.
        if (_pauseOnStart)
        {
            ChangeState(GameState.Paused);
        }
    }

    private void Update()
    {
        // Listen for the Escape key to toggle pause.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
            else if (CurrentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }
        if (CurrentState == GameState.Playing && _playerTransform != null)
        {
            // Track distance based on the player's X position.
            _distanceTraveled = _playerTransform.position.x;

            // Only check win conditions in Story Mode
            if (CurrentGameMode == GameMode.StoryMode)
            {
                // Check for the distance win condition.
                if (_winCondition == WinConditionType.DistanceReached && _distanceTraveled >= _winConditionValue)
                {
                    ChangeState(GameState.LevelComplete);
                }
            }
        }
    }

    // === Public Methods ===

    /// <summary>
    /// The central method for changing the game's state. Fires an event
    /// that other systems can listen to.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    public void ChangeState(GameState newState)
    {
        // Don't re-trigger the same state's logic.
        if (newState == CurrentState) return;

        CurrentState = newState;
        _currentStateForInspector = CurrentState; // Update the Inspector for easy debugging.

        // Use a switch to handle the logic for entering each new state.
        switch (newState)
        {
            case GameState.MainMenu:
                HandleMainMenu();
                break;
            case GameState.LevelStarting:
                HandleLevelStarting();
                break;
            case GameState.Playing:
                HandlePlaying();
                break;
            case GameState.Paused:
                HandlePaused();
                break;
            case GameState.LevelComplete:
                HandleLevelComplete();
                break;
            case GameState.DemoComplete:
                HandleDemoComplete();
                break;
            case GameState.GameOver:
                HandleGameOver();
                break;
        }

        EventManager.Publish(new GameStateChangedEvent { State = newState });

        // Notify any listeners that the state has changed.
        OnStateChanged?.Invoke(newState);
    }

    // === State Handlers ===

    // These methods will contain the logic for what happens when we enter a state.

    private void HandleMainMenu()
    {
        Debug.Log("GameManager: Entered MainMenu state.");
        // Later: Load the Main Menu scene using _mainMenuSceneName.
    }

    private void HandleLevelStarting()
    {
        Debug.Log("GameManager: Entered LevelStarting state.");
        Debug.Log("[GameManager] Starting LevelStarting coroutine (uses UNSCALED time)...");
        // Later: Show "3-2-1-Go!", play intro animation, etc.
        // For now, we'll just automatically transition to Playing after a delay.
        StartCoroutine(LevelStartingCoroutine());
    }

    private System.Collections.IEnumerator LevelStartingCoroutine()
    {
        // Use WaitForSecondsRealtime to avoid Time.timeScale issues
        yield return new WaitForSecondsRealtime(1f);
        Debug.Log("[GameManager] LevelStarting coroutine complete - Transitioning to Playing state");
        ChangeState(GameState.Playing);
    }

    private void HandlePlaying()
    {
        Debug.Log("GameManager: Entered Playing state.");
        Debug.Log($"[GameManager] Setting Time.timeScale to {_debugTimeScale}");
        // Unpause the game.
        Time.timeScale = _debugTimeScale; // Use debug value in case it's not 1.

        if (PlayerController.instance != null) _playerTransform = PlayerController.instance.transform;
        _distanceTraveled = 0f;
        Debug.Log("[GameManager] Playing state setup complete");
    }

    private void HandlePaused()
    {
        Debug.Log("GameManager: Entered Paused state.");
        // Pause the game.
        Time.timeScale = 0f;
    }

    private void HandleLevelComplete()
    {
        Debug.Log("GameManager: Entered LevelComplete state.");
        Time.timeScale = 0f; // Pause the game
    }
    private void HandleDemoComplete()
    {
        Debug.Log("GameManager: Entered DemoComplete state.");
        // Later: Show final score screen, submit total score to Steam leaderboard.
    }

    private void HandleGameOver()
    {
        Debug.Log("GameManager: Entered GameOver state.");
        Time.timeScale = 0f; // Pause the game
    }

    /// <summary>
    /// Sets the game mode for the next play session.
    /// Must be called before LoadLevelByIndex().
    /// </summary>
    /// <param name="mode">The game mode to activate.</param>
    public void SetGameMode(GameMode mode)
    {
        CurrentGameMode = mode;
        _currentGameModeForInspector = mode;

        Debug.Log($"[GameManager] Game mode set to: {mode}");

        // Publish event for other systems to react
        EventManager.Publish(new GameModeChangedEvent { Mode = mode });
    }

    public void LoadLevelByIndex(int levelIndex)
    {
        // Check if the requested level index is valid.
        if (levelIndex >= 0 && levelIndex < _levelSceneNames.Count)
        {
            // Load the scene using the name from the list.
            SceneManager.LoadScene(_levelSceneNames[levelIndex]);
            // Change the state to LevelStarting, which will then transition to Playing.
            ChangeState(GameState.LevelStarting);
        }
        else
        {
            Debug.LogError("GameManager: Invalid level index requested: " + levelIndex);
        }
    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("GameManager: Quitting application.");
        Application.Quit();

        // This part is for quitting the game when testing in the Unity Editor.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

   public void ReloadLevel()
    {
        TimeScaleDebugger.timeScale = 1f; // IMPORTANT: Unpause before loading
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        ChangeState(GameState.LevelStarting); // <-- ADD THIS LINE
    }
    public void ReturnToMenu()
    {
        Time.timeScale = 1f; // IMPORTANT: Unpause before loading
        SceneManager.LoadScene(_mainMenuSceneName);
        ChangeState(GameState.MainMenu);
    }


    // These definitions live outside the GameManager class so they can be easily accessed by other scripts.

    public enum WinConditionType { DistanceReached, TimeSurvived }

    public enum ShadowIntent { Hostile, Helpful, NeutralRacer }

    [System.Serializable]
    public struct Achievement
    {
        public string achievementName;
        [Tooltip("A description of how to earn the achievement.")]
        public string description;
        public Sprite badgeIcon;
        public bool isUnlocked;
    }
}

