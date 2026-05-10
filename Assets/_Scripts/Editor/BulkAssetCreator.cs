
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class BulkAssetCreator
{
    private const string GameEventPath = "Assets/_Scripts/GameEvents";
    private const string AudioCuePath = "Assets/_Audio/AudioCues";

    [MenuItem("Tools/Audio/Create Events and Cues")]
    public static void CreateEventsAndCues()
    {
        // Define game events that match our existing audio cues - simplified and essential only
        var coreGameEvents = new List<string>
        {
            // Core Movement
            "Jump",              // Uses existing Jump.asset
            "Land",              // Uses existing Land.asset
            "Flap",              // Uses existing Flap.asset for wing movement
            
            // Collectibles (keeping specific pickup types for variety)
            "ChickenCoinPickup", // Uses existing ChickenCoin_Pickup_Cue.asset
            "ChiliCoinPickup",   // Uses existing ChiliCoin_Pickup_Cue.asset
            
            // Effects & Status
            "Hurt",              // Uses existing Hurt.asset
            "PlayerDie",         // Critical for game over sequence
            "ObstacleHit",       // Uses existing ObsicalHit1.asset
            "ButtonClick",       // Uses existing ButtonClick.asset for UI
            "PhaseShift",        // Uses existing SmallPhaseShift sounds
            
            // Music System (keeping all tracks for variety)
            "MenuTheme",         // Uses existing Theme.asset
            "GameplayTrack1",    // Uses existing Track1.asset
            "GameplayTrack2",    // Uses existing Track2.asset
            "GameplayTrack3",    // Uses existing Track3.asset
            "GameOverTheme",     // Uses existing EndGameMusic.asset
            "BlackYallTheme"     // Uses existing ImBlackYall.asset - special theme
        };

        // Create audio cues that match our game events
        var audioSuffix = "Sfx"; // Suffix for audio cue names
        var finalGameEventNames = coreGameEvents;
        var finalAudioCueNames = coreGameEvents.Select(name => name + audioSuffix).ToList();

        // Create the assets
        CreateAssets<GameEvent>(finalGameEventNames, GameEventPath);
        CreateAssets<AudioCue>(finalAudioCueNames, AudioCuePath);

        // Refresh the asset database to show the new assets
        AssetDatabase.Refresh();
    }

    private static void CreateAssets<T>(List<string> assetNames, string path) where T : ScriptableObject
    {
        // Create the target directory if it doesn't exist
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"Created directory: {path}");
        }

        int createdCount = 0;
        foreach (string assetName in assetNames)
        {
            string fullPath = Path.Combine(path, $"{assetName}.asset");

            // Check if the asset already exists
            if (File.Exists(fullPath))
            {
                continue;
            }

            // Create an instance of the ScriptableObject
            T asset = ScriptableObject.CreateInstance<T>();

            // Create the asset in the database
            AssetDatabase.CreateAsset(asset, fullPath);
            createdCount++;
        }

        // Log a summary
        if (createdCount > 0)
        {
            Debug.Log($"Successfully created {createdCount} new {typeof(T).Name} assets in {path}.");
        }
        else
        {
            Debug.Log($"All {typeof(T).Name} assets already exist in {path}. No new assets were created.");
        }
    }
}
#endif
