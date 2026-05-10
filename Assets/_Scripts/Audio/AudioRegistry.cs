using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Registry for mapping gameplay events to audio playback definitions.
/// </summary>
[CreateAssetMenu(fileName = "Audio Registry", menuName = "Fly & Shadow/Audio/Audio Registry")]
public class AudioRegistry : ScriptableObject
{
    [SerializeField]
    [Tooltip("List of bindings defining how events should trigger audio playback.")]
    private List<Binding> _bindings = new List<Binding>();

    /// <summary>
    /// Returns the configured set of bindings.
    /// </summary>
    public IReadOnlyList<Binding> Bindings => _bindings;

    private void OnValidate()
    {
        if (_bindings == null || _bindings.Count == 0)
        {
            return;
        }

        var duplicates = new HashSet<GameEvent>();
        var seen = new HashSet<GameEvent>();

        for (int i = 0; i < _bindings.Count; i++)
        {
            var binding = _bindings[i];
            if (binding == null)
            {
                continue;
            }

            var trigger = binding.Trigger;
            if (trigger == null)
            {
                continue;
            }

            if (!seen.Add(trigger))
            {
                duplicates.Add(trigger);
            }
        }

        if (duplicates.Count > 0)
        {
            var duplicateNames = string.Join(", ", duplicates.Select(d => d != null ? d.name : "<null>"));
#if UNITY_EDITOR
            Debug.LogWarning($"AudioRegistry '{name}' has duplicate triggers: {duplicateNames}", this);
#else
            Debug.LogWarning($"AudioRegistry '{name}' has duplicate triggers: {duplicateNames}");
#endif
        }
    }

    [Serializable]
    public class Binding
    {
        [SerializeField]
        [Tooltip("Event that triggers this audio binding.")]
        private GameEvent _trigger;

        [SerializeField]
        [Tooltip("Collection of variations that can be used when the event fires.")]
        private AudioVariation[] _variations = Array.Empty<AudioVariation>();

        [SerializeField]
        [Tooltip("If true, the audio will route through the music channel instead of the SFX pool.")]
        private bool _isMusic;

        [SerializeField]
        [Tooltip("Determines how the AudioManager selects a variation when this binding is triggered.")]
        private PlaybackStrategyType _playbackStrategy = PlaybackStrategyType.FirstValid;

        /// <summary>
        /// Event that will trigger audio playback.
        /// </summary>
        public GameEvent Trigger => _trigger;

        /// <summary>
        /// The available audio variations for this binding.
        /// </summary>
        public AudioVariation[] Variations => _variations;

        /// <summary>
        /// True when the binding should route the selected cue through the music source.
        /// </summary>
        public bool IsMusic => _isMusic;

        /// <summary>
        /// Strategy used by the AudioManager to select a variation.
        /// </summary>
        public PlaybackStrategyType PlaybackStrategy => _playbackStrategy;
    }
}

/// <summary>
/// Defines a single audio variation that can be played for a binding.
/// </summary>
[Serializable]
public class AudioVariation
{
    [SerializeField]
    [Tooltip("Audio cue to play when this variation is selected.")]
    private AudioCue _cue;

    [SerializeField]
    [Tooltip("Relative weight used by weighted playback strategies.")]
    private float _weight = 1f;

    /// <summary>
    /// Audio cue referenced by this variation.
    /// </summary>
    public AudioCue Cue => _cue;

    /// <summary>
    /// Weight value used for weighted random selection.
    /// </summary>
    public float Weight => Mathf.Max(0f, _weight);
}

/// <summary>
/// Supported audio playback strategies for registry bindings.
/// </summary>
public enum PlaybackStrategyType
{
    FirstValid = 0,
    Sequential = 1,
    WeightedRandom = 2
}
