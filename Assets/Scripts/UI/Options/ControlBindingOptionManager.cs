using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Controls tab of the options menu: mouse sensitivity and keyboard/mouse key rebinding.
/// Each binding button starts an interactive rebind of its action. The new bindings are saved
/// through <see cref="GameInput"/>, so every gameplay script picks them up immediately.
/// </summary>
public class ControlBindingOptionManager : MonoBehaviour
{
    GameControls playerInput;
    InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    [Tooltip("Kept for the existing prefab reference; rebinding now uses the action of each button.")]
    [SerializeField] InputActionReference actionToRemap;

    // Mouse sensitivity (also read in game by the FPS camera through GameSettings)
    [FormerlySerializedAs("sensivitySlider")]
    [SerializeField] Slider sensitivitySlider;

    [Header("Binding buttons")]
    [SerializeField] Button menu_Button;
    [SerializeField] Button moveForward_Button;
    [SerializeField] Button moveLeft_Button;
    [SerializeField] Button moveBack_Button;
    [SerializeField] Button moveRight_Button;
    [SerializeField] Button sprint_Button;
    [SerializeField] Button lantern_Button;
    [SerializeField] Button shoot_Button;
    [SerializeField] Button reload_Button;
    [SerializeField] Button melee_Button;
    [SerializeField] Button grenade_Button;
    [SerializeField] Button interact_Button;
    [SerializeField] Button crouch_Button;
    [SerializeField] Button prone_Button;
    [SerializeField] Button seeStats_Button;

    TextMeshProUGUI sensitivityLabel;

    /// <summary>One rebindable keyboard/mouse binding and the button that shows it.</summary>
    class BindingSlot
    {
        public Button Button;
        public TextMeshProUGUI Label;
        public InputAction Action;
        public string CompositePart;    // "up", "down"... for the movement composite, null otherwise
        public int BindingIndex = -1;
    }

    readonly List<BindingSlot> slots = new List<BindingSlot>();

    void Awake()
    {
        playerInput = GameInput.CreateControls(this);
        sensitivityLabel = sensitivitySlider.GetComponentInChildren<TextMeshProUGUI>();
        CreateBindingSlots();
    }

    void Start()
    {
        LoadControlsSettings();
        RefreshBindingLabels();
    }

    void OnDestroy()
    {
        rebindingOperation?.Dispose();
        playerInput?.Disable();
        playerInput?.Dispose();
    }

    // Called by the sensitivity slider's OnValueChanged event
    public void SetSensitivity(float value)
    {
        GameSettings.MouseSensitivity = sensitivitySlider.value;
        sensitivityLabel.text = sensitivitySlider.value.ToString("F2");
    }

    /// <summary>Legacy entry point still wired to a button in the prefab; rebinding is handled per button.</summary>
    public void StartRebinding() { }

    /// <summary>Legacy entry point still wired to a button in the prefab; rebinding is handled per button.</summary>
    public void Rebind_Shoot() { }

    public void Reset_ControlOptions()
    {
        sensitivitySlider.value = GameSettings.DefaultSensitivity;
        GameSettings.MouseSensitivity = GameSettings.DefaultSensitivity;
        GameInput.ResetOverrides();
        RefreshBindingLabels();
    }

    void StartRebinding(BindingSlot slot)
    {
        if (rebindingOperation != null || slot.BindingIndex < 0)
            return;

        EventSystem.current?.SetSelectedGameObject(null);
        slot.Label.text = "Press any key";

        bool wasEnabled = slot.Action.enabled;
        slot.Action.Disable();

        rebindingOperation = slot.Action.PerformInteractiveRebinding(slot.BindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Gamepad>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(_ => FinishRebinding(slot, wasEnabled, save: true))
            .OnCancel(_ => FinishRebinding(slot, wasEnabled, save: false))
            .Start();
    }

    void FinishRebinding(BindingSlot slot, bool wasEnabled, bool save)
    {
        rebindingOperation?.Dispose();
        rebindingOperation = null;

        if (wasEnabled)
            slot.Action.Enable();

        if (save)
            GameInput.SaveAndBroadcastOverrides(playerInput.asset);

        RefreshBindingLabels();
    }

    void CreateBindingSlots()
    {
        AddSlot(menu_Button, playerInput.OnFoot.Menu);
        AddSlot(moveForward_Button, playerInput.OnFoot.Movement, "up");
        AddSlot(moveLeft_Button, playerInput.OnFoot.Movement, "left");
        AddSlot(moveBack_Button, playerInput.OnFoot.Movement, "down");
        AddSlot(moveRight_Button, playerInput.OnFoot.Movement, "right");
        AddSlot(sprint_Button, playerInput.OnFoot.Sprint);
        AddSlot(lantern_Button, playerInput.OnFoot.Lantern);
        AddSlot(shoot_Button, playerInput.Weapon.Fire1Pressed);
        AddSlot(reload_Button, playerInput.Weapon.Reload);
        AddSlot(melee_Button, playerInput.Weapon.Melee);
        AddSlot(grenade_Button, playerInput.Weapon.Grenade);
        AddSlot(interact_Button, playerInput.OnFoot.Interact);
        AddSlot(crouch_Button, playerInput.OnFoot.Crouch);
        AddSlot(prone_Button, playerInput.OnFoot.Prone);
        AddSlot(seeStats_Button, playerInput.OnFoot.SeeStats);
    }

    void AddSlot(Button button, InputAction action, string compositePart = null)
    {
        if (button == null)
            return;

        var slot = new BindingSlot
        {
            Button = button,
            Label = button.GetComponentInChildren<TextMeshProUGUI>(),
            Action = action,
            CompositePart = compositePart,
            BindingIndex = FindKeyboardMouseBinding(action, compositePart),
        };
        slots.Add(slot);
        button.onClick.AddListener(() => StartRebinding(slot));
    }

    /// <summary>Index of the keyboard/mouse binding of <paramref name="action"/> (or of one composite part).</summary>
    static int FindKeyboardMouseBinding(InputAction action, string compositePart)
    {
        var bindings = action.bindings;
        for (int i = 0; i < bindings.Count; i++)
        {
            InputBinding binding = bindings[i];
            if (binding.isComposite)
                continue;
            if (compositePart != null && (!binding.isPartOfComposite || !string.Equals(binding.name, compositePart, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (compositePart == null && binding.isPartOfComposite)
                continue;

            string path = binding.effectivePath ?? string.Empty;
            if (path.StartsWith("<Keyboard>") || path.StartsWith("<Mouse>"))
                return i;
        }
        return -1;
    }

    void RefreshBindingLabels()
    {
        foreach (var slot in slots)
        {
            if (slot.Label == null)
                continue;
            slot.Label.text = slot.BindingIndex >= 0
                ? slot.Action.GetBindingDisplayString(slot.BindingIndex)
                : slot.Action.GetBindingDisplayString();
        }
    }

    // Options are stored in PlayerPrefs so they persist between scenes and sessions
    void LoadControlsSettings()
    {
        float sensitivity = GameSettings.MouseSensitivity;
        sensitivitySlider.SetValueWithoutNotify(sensitivity);
        sensitivityLabel.text = sensitivity.ToString("F2");
    }
}
