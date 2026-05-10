using UnityEngine;

/// <summary>
/// Designer-authored audio definition that maps a logical sound name to clip data.
/// </summary>
[CreateAssetMenu(fileName = "Audio Cue", menuName = "FlyShadow/Audio Cue")]
public class AudioCue : ScriptableObject
{
    public string Name;
    public AudioClip Clip;
    [Range(0f, 1f)]
    public float Volume = 1f;
    [Range(0.1f, 3f)]
    public float Pitch = 1f;
    public bool Loop;
}
