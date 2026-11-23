using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Creates <see cref="GameControls"/> instances and keeps their binding overrides in sync.
///
/// Every gameplay script owns its own <see cref="GameControls"/> instance, so a rebind done in
/// the options menu has to reach all of them. This class tracks the live instances, applies
/// the saved overrides to each new one and stores the overrides in <see cref="PlayerPrefs"/>
/// so they survive scene changes and restarts.
/// </summary>
public static class GameInput
{
    public const string BindingOverridesKey = "inputBindingOverrides";

    static readonly List<WeakReference<GameControls>> liveControls = new List<WeakReference<GameControls>>();

    /// <summary>Creates a new, enabled set of controls with the saved binding overrides applied.</summary>
    public static GameControls CreateControls()
    {
        var controls = new GameControls();

        string overrides = PlayerPrefs.GetString(BindingOverridesKey, string.Empty);
        if (!string.IsNullOrEmpty(overrides))
            controls.asset.LoadBindingOverridesFromJson(overrides);

        liveControls.RemoveAll(reference => !reference.TryGetTarget(out _));
        liveControls.Add(new WeakReference<GameControls>(controls));

        controls.Enable();
        return controls;
    }

    /// <summary>Saves the overrides of <paramref name="source"/> and copies them to every live instance.</summary>
    public static void SaveAndBroadcastOverrides(InputActionAsset source)
    {
        string overrides = source.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(BindingOverridesKey, overrides);
        PlayerPrefs.Save();

        foreach (var reference in liveControls)
        {
            if (reference.TryGetTarget(out var controls) && controls.asset != source)
            {
                controls.asset.RemoveAllBindingOverrides();
                controls.asset.LoadBindingOverridesFromJson(overrides);
            }
        }
    }

    /// <summary>Removes every saved override and resets all live instances to the default bindings.</summary>
    public static void ResetOverrides()
    {
        PlayerPrefs.DeleteKey(BindingOverridesKey);

        foreach (var reference in liveControls)
        {
            if (reference.TryGetTarget(out var controls))
                controls.asset.RemoveAllBindingOverrides();
        }
    }
}
