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
///
/// Each instance is registered with the component that owns it. When the owner is destroyed
/// the controls are disabled and disposed here, so a script that forgets to do it in
/// OnDisable/OnDestroy cannot leave enabled actions behind (the generated wrapper reports
/// those as leaks when it is garbage collected).
/// </summary>
public static class GameInput
{
    public const string BindingOverridesKey = "inputBindingOverrides";

    struct Entry
    {
        public UnityEngine.Object owner;
        public GameControls controls;
    }

    static readonly List<Entry> liveControls = new List<Entry>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        liveControls.Clear();
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    static void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene) => ReleaseDestroyedOwners();

    /// <summary>Creates a new, enabled set of controls owned by <paramref name="owner"/>, with the saved binding overrides applied.</summary>
    public static GameControls CreateControls(UnityEngine.Object owner)
    {
        var controls = new GameControls();

        string overrides = PlayerPrefs.GetString(BindingOverridesKey, string.Empty);
        if (!string.IsNullOrEmpty(overrides))
            controls.asset.LoadBindingOverridesFromJson(overrides);

        ReleaseDestroyedOwners();
        liveControls.Add(new Entry { owner = owner, controls = controls });

        controls.Enable();
        return controls;
    }

    /// <summary>Saves the overrides of <paramref name="source"/> and copies them to every live instance.</summary>
    public static void SaveAndBroadcastOverrides(InputActionAsset source)
    {
        string overrides = source.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(BindingOverridesKey, overrides);
        PlayerPrefs.Save();

        foreach (Entry entry in liveControls)
        {
            InputActionAsset asset = entry.controls.asset;
            if (asset != null && asset != source)
            {
                asset.RemoveAllBindingOverrides();
                asset.LoadBindingOverridesFromJson(overrides);
            }
        }
    }

    /// <summary>Removes every saved override and resets all live instances to the default bindings.</summary>
    public static void ResetOverrides()
    {
        PlayerPrefs.DeleteKey(BindingOverridesKey);

        foreach (Entry entry in liveControls)
        {
            if (entry.controls.asset != null)
                entry.controls.asset.RemoveAllBindingOverrides();
        }
    }

    /// <summary>Disables and disposes the controls whose owner no longer exists.</summary>
    public static void ReleaseDestroyedOwners()
    {
        for (int i = liveControls.Count - 1; i >= 0; i--)
        {
            Entry entry = liveControls[i];
            if (entry.owner != null)
                continue;

            liveControls.RemoveAt(i);
            InputActionAsset asset = entry.controls.asset;
            if (asset == null)
                continue;   // Already disposed by its owner

            asset.Disable();
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(asset);
            else
                UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}
