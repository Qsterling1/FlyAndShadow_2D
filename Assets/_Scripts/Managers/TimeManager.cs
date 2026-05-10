using UnityEngine;
using System.Collections;

/// <summary>
/// A singleton manager to safely control Time.timeScale for effects like slow-motion.
/// This prevents multiple objects from conflicting when they try to alter game time.
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Applies a time scale modification for a specific duration.
    /// </summary>
    public void ApplyTimeDilation(float newTimeScale, float duration)
    {
        StartCoroutine(TimeDilationCoroutine(newTimeScale, duration));
    }

    private IEnumerator TimeDilationCoroutine(float newTimeScale, float duration)
    {
        float defaultTimeScale = 1.0f;

        Time.timeScale = newTimeScale;
        yield return new WaitForSecondsRealtime(duration); // Use Realtime to ignore the modified timeScale
        Time.timeScale = defaultTimeScale;
    }
}