using UnityEngine;

/// <summary>
/// Player settings that gameplay reads every frame, with safe defaults for a first launch.
/// Before this class existed, gameplay read the raw PlayerPrefs keys. On a fresh install, before
/// the options menu had run, those keys were missing, so mouse sensitivity and FOV were 0.
/// </summary>
public static class GameSettings
{
    public const string SensitivityKey = "sensitivity";
    const string LegacySensitivityKey = "sensivity";   // Misspelled key used by older builds
    public const string FieldOfViewKey = "fov";

    public const float DefaultSensitivity = 2f;
    public const int DefaultFieldOfView = 90;

    public static float MouseSensitivity
    {
        get
        {
            if (PlayerPrefs.HasKey(SensitivityKey))
                return PlayerPrefs.GetFloat(SensitivityKey);
            return PlayerPrefs.GetFloat(LegacySensitivityKey, DefaultSensitivity);
        }
        set => PlayerPrefs.SetFloat(SensitivityKey, value);
    }

    public static int FieldOfView
    {
        get
        {
            int fov = PlayerPrefs.GetInt(FieldOfViewKey, DefaultFieldOfView);
            return fov > 0 ? fov : DefaultFieldOfView;
        }
        set => PlayerPrefs.SetInt(FieldOfViewKey, value);
    }
}
