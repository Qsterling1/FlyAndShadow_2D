
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// An Editor Tool to automatically find, create, and wire GameEvent and AudioCue assets
/// into a central AudioManager prefab.
/// </summary>
public class AudioWiringTool : EditorWindow
{
    // Constants for asset paths
    private const string GameEventPath = "Assets/_Scripts/GameEvents";
    private const string AudioCuePath = "Assets/_Audio/AudioCues";
    private const string ReportPath = "Docs/reports";
    private const string AudioManagerPrefabPath = "Assets/_Art/_Prefabs/Managers/AudioManager.prefab";

    // Tool options
    private bool _isDryRun = true;
    private bool _createMissingAssets = true;
    private bool _overwriteExistingBindings = false;

    // Scroll position for the results
    private Vector2 _scrollPosition;
    private string _reportContent = "Run the tool to generate a report.";

    [MenuItem("Tools/Audio/Audio Wiring Tool")]
    public static void ShowWindow()
    {
        GetWindow<AudioWiringTool>("Audio Wiring Tool");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Audio Event & Cue Wiring Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool scans for GameEvents and AudioCues, creates missing pairs, and wires them to the AudioManager prefab.",
            MessageType.Info);

        // --- OPTIONS ---
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        _isDryRun = EditorGUILayout.Toggle(new GUIContent("Dry Run", "Simulate the run and generate a report without applying changes."), _isDryRun);
        _createMissingAssets = EditorGUILayout.Toggle(new GUIContent("Create Missing Assets", "If a GameEvent or AudioCue is found without a matching pair, create the missing asset."), _createMissingAssets);
        _overwriteExistingBindings = EditorGUILayout.Toggle(new GUIContent("Overwrite Existing Bindings", "If a binding for a GameEvent already exists in the AudioManager, overwrite it. Otherwise, skips."), _overwriteExistingBindings);

        // --- ACTIONS ---
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        if (GUILayout.Button(_isDryRun ? "Run Dry Run" : "Run and Apply Changes"))
        {
            Execute();
        }

        // --- REPORT PREVIEW ---
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Report Preview", EditorStyles.boldLabel);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
        EditorGUILayout.TextArea(_reportContent, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Main execution logic for the tool.
    /// </summary>
    private void Execute()
    {
        var reportBuilder = new StringBuilder();
        var createdAssets = new List<string>();
        var updatedBindings = new List<string>();
        var updatedMusicFields = new List<string>();

        // --- 1. Scan for all existing assets ---
        var gameEvents = FindAssets<GameEvent>(GameEventPath);
        var audioCues = FindAssets<AudioCue>(AudioCuePath);

        var allNames = gameEvents.Keys.Union(audioCues.Keys).ToHashSet();

        // --- 2. Create missing assets if enabled ---
        if (_createMissingAssets)
        {
            foreach (var name in allNames)
            {
                // Create missing GameEvent
                if (!gameEvents.ContainsKey(name))
                {
                    var newEvent = CreateAsset<GameEvent>(GameEventPath, name);
                    gameEvents[name] = newEvent;
                    createdAssets.Add($"GameEvent: {name}");
                }

                // Create missing AudioCue
                if (!audioCues.ContainsKey(name))
                {
                    var newCue = CreateAsset<AudioCue>(AudioCuePath, name);
                    audioCues[name] = newCue;
                    createdAssets.Add($"AudioCue: {name}");
                }
            }
        }

        // --- 3. Find or create the AudioManager prefab ---
        var audioManager = FindOrCreateAudioManager();
        if (audioManager == null)
        {
            Debug.LogError("AudioWiringTool: Could not find or create AudioManager prefab. Aborting.");
            return;
        }

        // --- 4. Wire the assets if not a dry run ---
        if (!_isDryRun)
        {
            var serializedManager = new SerializedObject(audioManager);
            Undo.RecordObject(audioManager, "Audio Wiring Tool Changes");

            // a) Wire music fields
            WireMusicField(serializedManager, "_mainMenuMusicCue", audioCues, "MusicMainMenu", updatedMusicFields);
            WireMusicField(serializedManager, "_gameplayMusicCue", audioCues, "MusicGameplay", updatedMusicFields);
            WireMusicField(serializedManager, "_levelCompleteMusicCue", audioCues, "MusicLevelComplete", updatedMusicFields);
            WireMusicField(serializedManager, "_gameOverMusicCue", audioCues, "MusicGameOver", updatedMusicFields);

            // b) Wire event-driven list
            var eventListProp = serializedManager.FindProperty("_eventDrivenAudio");
            var matchedNames = gameEvents.Keys.Intersect(audioCues.Keys).ToList();

            foreach (var name in matchedNames)
            {
                var gameEvent = gameEvents[name];
                var audioCue = audioCues[name];
                bool isMusic = name.StartsWith("Music");

                int existingIndex = -1;
                for (int i = 0; i < eventListProp.arraySize; i++)
                {
                    var prop = eventListProp.GetArrayElementAtIndex(i);
                    var eventProp = prop.FindPropertyRelative("GameEvent");
                    if (eventProp.objectReferenceValue == gameEvent)
                    {
                        existingIndex = i;
                        break;
                    }
                }

                if (existingIndex != -1) // Found existing
                {
                    if (_overwriteExistingBindings)
                    {
                        var prop = eventListProp.GetArrayElementAtIndex(existingIndex);
                        prop.FindPropertyRelative("Cue").objectReferenceValue = audioCue;
                        prop.FindPropertyRelative("PlayAsMusic").boolValue = isMusic;
                        updatedBindings.Add($"{name} -> {name} (Overwritten)");
                    }
                }
                else // Not found, add new
                {
                    eventListProp.InsertArrayElementAtIndex(eventListProp.arraySize);
                    var newProp = eventListProp.GetArrayElementAtIndex(eventListProp.arraySize - 1);
                    newProp.FindPropertyRelative("GameEvent").objectReferenceValue = gameEvent;
                    newProp.FindPropertyRelative("Cue").objectReferenceValue = audioCue;
                    newProp.FindPropertyRelative("PlayAsMusic").boolValue = isMusic;
                    updatedBindings.Add($"{name} -> {name} (New)");
                }
            }

            serializedManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(audioManager);
            AssetDatabase.SaveAssets();
        }

        // --- 5. Generate and save the report ---
        GenerateReport(reportBuilder, allNames, gameEvents, audioCues, createdAssets, updatedBindings, updatedMusicFields);
        _reportContent = reportBuilder.ToString();
        SaveReportToFile(_reportContent);

        // --- 6. Log summary ---
        string summary = _isDryRun ? "Dry run complete." : "Run complete. Changes applied.";
        Debug.Log($"AudioWiringTool: {summary} See report at {ReportPath}/audio_wiring_report.md for details.");
    }

    /// <summary>
    /// Finds assets of a given type in a specific folder and returns a name-to-asset dictionary.
    /// </summary>
    private Dictionary<string, T> FindAssets<T>(string path) where T : ScriptableObject
    {
        var assets = new Dictionary<string, T>();
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { path });
        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null && !assets.ContainsKey(asset.name))
            {
                assets.Add(asset.name, asset);
            }
        }
        return assets;
    }

    /// <summary>
    /// Creates a ScriptableObject asset at the specified path.
    /// </summary>
    private T CreateAsset<T>(string path, string name) where T : ScriptableObject
    {
        if (!_isDryRun)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var asset = CreateInstance<T>();
            var assetPath = Path.Combine(path, $"{name}.asset");
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }
        // In a dry run, return a temporary instance to allow the report to be generated.
        return CreateInstance<T>();
    }

    /// <summary>
    /// Finds the AudioManager instance in the current scene.
    /// </summary>
    private AudioManager FindOrCreateAudioManager()
    {
        // First try to find an existing AudioManager
        var manager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        if (manager != null)
        {
            return manager;
        }

        // Look for a PersistentSystems instance that can create one
        var persistentSystems = UnityEngine.Object.FindFirstObjectByType<PersistentSystems>();
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

        Debug.LogError("No AudioManager found in scene and no PersistentSystems available to create one. Please ensure PersistentSystems prefab is in the scene.");
        return null;
    }

    /// <summary>
    /// Wires a single music field on the AudioManager.
    /// </summary>
    private void WireMusicField(SerializedObject serializedManager, string fieldName, Dictionary<string, AudioCue> audioCues, string cueName, List<string> updatedFields)
    {
        if (audioCues.TryGetValue(cueName, out var cue))
        {
            var prop = serializedManager.FindProperty(fieldName);
            if (prop.objectReferenceValue != cue)
            {
                prop.objectReferenceValue = cue;
                updatedFields.Add($"{fieldName} -> {cueName}");
            }
        }
    }

    /// <summary>
    /// Generates the final markdown report content.
    /// </summary>
    private void GenerateReport(StringBuilder sb, HashSet<string> allNames, Dictionary<string, GameEvent> gameEvents, Dictionary<string, AudioCue> audioCues, List<string> created, List<string> updated, List<string> musicFields)
    {
        sb.AppendLine("# Audio Wiring Report");
        sb.AppendLine($"*Generated on: {System.DateTime.Now}*");
        sb.AppendLine($"*Mode: {(_isDryRun ? "Dry Run" : "Applied Changes")}*");
        sb.AppendLine();

        if (created.Any())
        {
            sb.AppendLine("## Created Assets");
            created.ForEach(a => sb.AppendLine($"- {a}"));
            sb.AppendLine();
        }

        if (musicFields.Any())
        {
            sb.AppendLine("## Updated Music Fields on AudioManager");
            musicFields.ForEach(a => sb.AppendLine($"- {a}"));
            sb.AppendLine();
        }

        if (updated.Any())
        {
            sb.AppendLine("## Wired/Updated Event Bindings on AudioManager");
            updated.ForEach(a => sb.AppendLine($"- {a}"));
            sb.AppendLine();
        }

        var unmatchedEvents = gameEvents.Keys.Except(audioCues.Keys).ToList();
        var unmatchedCues = audioCues.Keys.Except(gameEvents.Keys).ToList();

        if (unmatchedEvents.Any() || unmatchedCues.Any())
        {
            sb.AppendLine("## TODO: Unmatched Assets");
            sb.AppendLine("The following assets do not have a matching pair. Consider creating the missing asset or renaming.");
            if (unmatchedEvents.Any())
            {
                sb.AppendLine("### GameEvents without an AudioCue:");
                unmatchedEvents.ForEach(name => sb.AppendLine($"- {name}"));
            }
            if (unmatchedCues.Any())
            {
                sb.AppendLine("### AudioCues without a GameEvent:");
                unmatchedCues.ForEach(name => sb.AppendLine($"- {name}"));
            }
        }

        if (!created.Any() && !updated.Any() && !musicFields.Any())
        {
            sb.AppendLine("## Summary");
            sb.AppendLine("No changes were made. All assets appear to be correctly created and wired according to the tool's logic.");
        }
    }

    /// <summary>
    /// Saves the report string to a markdown file.
    /// </summary>
    private void SaveReportToFile(string content)
    {
        if (!Directory.Exists(ReportPath))
        {
            Directory.CreateDirectory(ReportPath);
        }
        File.WriteAllText(Path.Combine(ReportPath, "audio_wiring_report.md"), content);
    }
}
#endif



