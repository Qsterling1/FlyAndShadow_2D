// AudioEmitter.cs (Corrected Version)

using UnityEngine;

/// <summary>
/// A component that tells the AudioManager to play a specific Audio Cue.
/// </summary>
public class AudioEmitter : MonoBehaviour
{
    [Header("Sound Definition")]
    [Tooltip("The Audio Cue asset this emitter will request to be played.")]
    [SerializeField] private AudioCue _audioCue;

    [Header("Configuration")]
    [Tooltip("If checked, the sound will play as soon as the object is enabled.")]
    [SerializeField] private bool _playOnStart = false;

    private void Start()
    {
        if (_playOnStart)
        {
            Play();
        }
    }

    /// <summary>
    /// Asks the AudioManager to play the assigned Audio Cue.
    /// </summary>
    public void Play()
    {
        if (_audioCue == null) return;
        
        // Ask the central AudioManager to play the sound.
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySound(_audioCue);
        }
    }
}