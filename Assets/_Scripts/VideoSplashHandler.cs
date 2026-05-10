using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the splash screen video. Plays the video after an initial start delay,
/// and then loads a specified scene after the video finishes with an end delay.
/// Attach this to a GameObject with a VideoPlayer where "Play On Awake" is disabled.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class VideoSplashHandler : MonoBehaviour
{
    [Tooltip("The exact name of the scene to load after the splash video.")]
    [SerializeField] private string _sceneToLoad;

    [Tooltip("The delay in seconds before the video starts playing.")]
    [SerializeField] private float _startDelayInSeconds = 1.0f;

    [Tooltip("The delay in seconds after the video finishes before the scene loads.")]
    [SerializeField] private float _endDelayInSeconds = 2.0f;

    private VideoPlayer _videoPlayer;

    private void Awake()
    {
        // Get a reference to the VideoPlayer component.
        _videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Start()
    {
        // Invoke the video playback method after the specified start delay.
        Invoke(nameof(StartVideoPlayback), _startDelayInSeconds);
    }

    /// <summary>
    /// Subscribes to the video end event and starts playback.
    /// </summary>
    private void StartVideoPlayback()
    {
        // Subscribe to the event that fires when the video is finished.
        _videoPlayer.loopPointReached += OnVideoFinished;
        // Manually start the video playback.
        _videoPlayer.Play();
    }

    /// <summary>
    /// Called by the VideoPlayer's loopPointReached event when the video is complete.
    /// </summary>
    private void OnVideoFinished(VideoPlayer source)
    {
        // Invoke the scene loading method after the specified end delay.
        Invoke(nameof(LoadNextScene), _endDelayInSeconds);
    }

    /// <summary>
    /// Loads the specified scene.
    /// </summary>
    private void LoadNextScene()
    {
        SceneManager.LoadScene(_sceneToLoad);
    }

    private void OnDestroy()
    {
        // It's good practice to unsubscribe from events when the object is destroyed.
        if (_videoPlayer != null)
        {
            _videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}