#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AudioWiringRunner
{
    private const string MenuPath = "Tools/Audio/Wire Events";
    private static readonly string[] GameEventSearchFolders = { "Assets/_Scripts/GameEvents" };
    private static readonly string[] AudioCueSearchFolders = { "Assets/_Audio/AudioCues" };

    [MenuItem(MenuPath)]
    private static void WireAudio()
    {
        try
        {
            WiringSummary summary = Execute();
            EditorUtility.DisplayDialog("Audio Wiring", summary.DialogMessage, "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Audio wiring failed: {ex}");
            EditorUtility.DisplayDialog("Audio Wiring", $"Failed: {ex.Message}", "OK");
        }
    }

    private static WiringSummary Execute()
    {
        Dictionary<string, GameEvent> gameEventMap = LoadAssets<GameEvent>(GameEventSearchFolders, "GameEvent");
        Dictionary<string, AudioCue> audioCueMap = LoadAssets<AudioCue>(AudioCueSearchFolders, "AudioCue");

        List<string> eventsWithoutCue = gameEventMap.Keys
            .Where(name => !audioCueMap.ContainsKey(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        List<string> cuesWithoutEvent = audioCueMap.Keys
            .Where(name => !gameEventMap.ContainsKey(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        AudioManager audioManager = EnsureAudioManagerInScene();
        if (audioManager == null)
        {
            throw new InvalidOperationException("No AudioManager found in the current scene or via prefabs.");
        }

        SerializedObject serializedManager = new SerializedObject(audioManager);
        Undo.RecordObject(audioManager, "Wire Audio Events");

        SerializedProperty eventListProperty = FindEventListProperty(serializedManager);
        if (eventListProperty == null)
        {
            throw new InvalidOperationException("Unable to locate the event-driven audio list on AudioManager.");
        }

        int added = 0;
        int updated = 0;

        Dictionary<string, int> existingIndices = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < eventListProperty.arraySize; i++)
        {
            SerializedProperty element = eventListProperty.GetArrayElementAtIndex(i);
            SerializedProperty eventProp = element.FindPropertyRelative("GameEvent");
            GameEvent existingEvent = eventProp?.objectReferenceValue as GameEvent;
            if (existingEvent == null)
            {
                continue;
            }

            if (!existingIndices.ContainsKey(existingEvent.name))
            {
                existingIndices.Add(existingEvent.name, i);
            }
        }

        foreach (KeyValuePair<string, GameEvent> pair in gameEventMap)
        {
            if (!audioCueMap.TryGetValue(pair.Key, out AudioCue cue))
            {
                continue;
            }

            GameEvent eventAsset = pair.Value;

            if (existingIndices.TryGetValue(pair.Key, out int index))
            {
                SerializedProperty element = eventListProperty.GetArrayElementAtIndex(index);
                bool changed = false;

                SerializedProperty eventProp = element.FindPropertyRelative("GameEvent");
                if (eventProp != null && eventProp.objectReferenceValue != eventAsset)
                {
                    eventProp.objectReferenceValue = eventAsset;
                    changed = true;
                }

                SerializedProperty cueProp = element.FindPropertyRelative("Cue");
                if (cueProp != null && cueProp.objectReferenceValue != cue)
                {
                    cueProp.objectReferenceValue = cue;
                    changed = true;
                }

                if (changed)
                {
                    updated++;
                }
            }
            else
            {
                int newIndex = eventListProperty.arraySize;
                eventListProperty.arraySize++;
                SerializedProperty element = eventListProperty.GetArrayElementAtIndex(newIndex);

                SerializedProperty eventProp = element.FindPropertyRelative("GameEvent");
                if (eventProp != null)
                {
                    eventProp.objectReferenceValue = eventAsset;
                }

                SerializedProperty cueProp = element.FindPropertyRelative("Cue");
                if (cueProp != null)
                {
                    cueProp.objectReferenceValue = cue;
                }

                SerializedProperty playAsMusicProp = element.FindPropertyRelative("PlayAsMusic");
                if (playAsMusicProp != null)
                {
                    playAsMusicProp.boolValue = false;
                }

                added++;
            }
        }

        int musicAssignments = 0;
        musicAssignments += AssignMusicCue(serializedManager, "_mainMenuMusicCue", "MainMenuMusic", audioCueMap);
        musicAssignments += AssignMusicCue(serializedManager, "_gameplayMusicCue", "GameplayMusic", audioCueMap);
        musicAssignments += AssignMusicCue(serializedManager, "_levelCompleteMusicCue", "LevelCompleteMusic", audioCueMap);
        musicAssignments += AssignMusicCue(serializedManager, "_gameOverMusicCue", "GameOverMusic", audioCueMap);

        serializedManager.ApplyModifiedProperties();
        EditorUtility.SetDirty(audioManager);
        AssetDatabase.SaveAssets();

        WiringSummary summary = new WiringSummary
        {
            Added = added,
            Updated = updated,
            MusicAssignments = musicAssignments,
            EventsWithoutCue = eventsWithoutCue,
            CuesWithoutEvent = cuesWithoutEvent
        };

        Debug.Log(summary.ToLogString());
        return summary;
    }

    private static SerializedProperty FindEventListProperty(SerializedObject so)
    {
        SerializedProperty direct = so.FindProperty("_eventDrivenAudio");
        if (direct != null && direct.isArray)
        {
            return direct;
        }

        SerializedProperty iterator = so.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (!iterator.isArray || iterator.propertyType != SerializedPropertyType.Generic)
            {
                continue;
            }

            SerializedProperty element = iterator.arraySize > 0 ? iterator.GetArrayElementAtIndex(0) : null;
            if (element == null)
            {
                continue;
            }

            SerializedProperty eventProp = element.FindPropertyRelative("GameEvent");
            SerializedProperty cueProp = element.FindPropertyRelative("Cue");
            if (eventProp != null && cueProp != null)
            {
                return so.FindProperty(iterator.propertyPath);
            }
        }

        return null;
    }

    private static int AssignMusicCue(SerializedObject so, string propertyName, string cueName, Dictionary<string, AudioCue> cueMap)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue != null)
        {
            return 0;
        }

        if (!cueMap.TryGetValue(cueName, out AudioCue cue) || cue == null)
        {
            return 0;
        }

        property.objectReferenceValue = cue;
        return 1;
    }

    private static AudioManager EnsureAudioManagerInScene()
    {
        // First try to find an existing AudioManager
        AudioManager manager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        if (manager != null)
        {
            return manager;
        }

        // Look for a PersistentSystems instance that can create one
        PersistentSystems persistentSystems = UnityEngine.Object.FindFirstObjectByType<PersistentSystems>();
        if (persistentSystems != null)
        {
            // Force an update frame to let PersistentSystems create its managers
            UnityEditor.EditorApplication.Step();
            manager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
            if (manager != null)
            {
                return manager;
            }
        }

        Debug.LogError("No AudioManager found in scene and no PersistentSystems available to create one.");
        return null;
    }

    private static Dictionary<string, T> LoadAssets<T>(IEnumerable<string> searchFolders, string typeLabel) where T : UnityEngine.Object
    {
        Dictionary<string, T> result = new Dictionary<string, T>(StringComparer.Ordinal);

        List<string> validFolders = new List<string>();
        foreach (string folder in searchFolders)
        {
            if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
            {
                validFolders.Add(folder);
            }
        }

        if (validFolders.Count == 0)
        {
            return result;
        }

        string[] guids = AssetDatabase.FindAssets($"t:{typeLabel}", validFolders.ToArray());
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                continue;
            }

            if (!result.ContainsKey(asset.name))
            {
                result.Add(asset.name, asset);
            }
        }

        return result;
    }

    private struct WiringSummary
    {
        public int Added;
        public int Updated;
        public int MusicAssignments;
        public List<string> EventsWithoutCue;
        public List<string> CuesWithoutEvent;

        public string DialogMessage =>
            $"Added {Added} pair(s), updated {Updated}, music slots assigned {MusicAssignments}.\n" +
            $"Events without cues: {FormatList(EventsWithoutCue)}\nCues without events: {FormatList(CuesWithoutEvent)}";

        public string ToLogString()
        {
            return "Audio Wiring Summary -> " + DialogMessage.Replace("\n", " | ");
        }

        private static string FormatList(List<string> list)
        {
            if (list == null || list.Count == 0)
            {
                return "(none)";
            }

            return string.Join(", ", list);
        }
    }
}
#endif
