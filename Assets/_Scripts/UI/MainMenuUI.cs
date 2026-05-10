using UnityEngine;

/// <summary>
/// Handles button clicks and other UI events in the Main Menu scene.
/// It communicates the player's choices to the GameManager.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    /// <summary>
    /// This function is called when the "Start Game" button is clicked.
    /// It tells the GameManager to load the first level.
    /// </summary>
    public void OnStartGamePressed()
    {
        // We call the GameManager, which is in charge of loading scenes.
        // We pass '0' to load the first level in its list.
        if (GameManager.instance != null)
        {
            GameManager.instance.LoadLevelByIndex(0);
        }
    }

    /// <summary>
    /// This function is called when the "Quit Game" button is clicked.
    /// </summary>
    public void OnQuitGamePressed()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.QuitGame();
        }
    }

    /// <summary>
    /// Called when the "Story Mode" button is clicked.
    /// Starts the game in Story Mode with traditional win conditions.
    /// </summary>
    public void OnStoryModeButtonPressed()
    {
        if (GameManager.instance != null)
        {
            Debug.Log("[MainMenuUI] Story Mode selected");
            GameManager.instance.SetGameMode(GameMode.StoryMode);
            GameManager.instance.LoadLevelByIndex(0);
        }
        else
        {
            Debug.LogError("[MainMenuUI] GameManager instance not found!");
        }
    }

    /// <summary>
    /// Called when the "Endless Mode" button is clicked.
    /// Starts the game in Endless Mode - play until death.
    /// </summary>
    public void OnEndlessModeButtonPressed()
    {
        if (GameManager.instance != null)
        {
            Debug.Log("[MainMenuUI] Endless Mode selected");
            GameManager.instance.SetGameMode(GameMode.EndlessMode);
            GameManager.instance.LoadLevelByIndex(0);
        }
        else
        {
            Debug.LogError("[MainMenuUI] GameManager instance not found!");
        }
    }
}