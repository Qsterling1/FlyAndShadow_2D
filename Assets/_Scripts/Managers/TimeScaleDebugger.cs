using UnityEngine;

public static class TimeScaleDebugger
{
    public static float timeScale
    {
        get
        {
            // This now correctly gets the REAL time scale directly from Unity.
            return UnityEngine.Time.timeScale;
        }
        set
        {
            // This checks the REAL time scale to see if the value is changing.
            if (UnityEngine.Time.timeScale != value)
            {
                // This sets the REAL time scale.
                UnityEngine.Time.timeScale = value;

                // If the time scale was just set to 0, sound the alarm!
                if (value == 0)
                {
                    Debug.LogError("ALERT: Time.timeScale was just set to 0! The editor will now pause.");
                    Debug.Break(); // This will pause the Unity Editor.
                }
            }
        }
    }
}