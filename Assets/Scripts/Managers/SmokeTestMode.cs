using System;
using UnityEngine;

/// <summary>
/// Automated smoke test used by the CI on the built game: launching the player with <c>-smoketest</c>
/// skips the main menu, starts a game and logs milestones that the workflow checks in the player log.
/// </summary>
public static class SmokeTestMode
{
    public const string Argument = "-smoketest";
    public const string LogPrefix = "[SmokeTest] ";

    static bool? enabled;

    public static bool Enabled => enabled ??= Array.IndexOf(Environment.GetCommandLineArgs(), Argument) >= 0;

    public static void Log(string message)
    {
        if (Enabled)
            Debug.Log(LogPrefix + message);
    }
}
