using UnityEngine;

public static class ModePrefs
{
    const string KEY = "GameMode"; // 0=ColorClimb, 1=SpeedRun
    public static bool IsSpeedRun
    {
        get => PlayerPrefs.GetInt(KEY, 0) == 1;
        set { PlayerPrefs.SetInt(KEY, value ? 1 : 0); PlayerPrefs.Save(); }
    }
}

