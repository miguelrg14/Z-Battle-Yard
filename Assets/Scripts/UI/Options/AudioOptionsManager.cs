using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Audio tab of the options menu. Each slider holds a linear volume in [0, 1] that is saved to
/// <see cref="PlayerPrefs"/> and converted to decibels for the matching exposed mixer parameter.
/// </summary>
public class AudioOptionsManager : MonoBehaviour
{
    const float MinLinearVolume = 0.0001f;    // -80 dB, the mixer's silence floor

    const string MasterKey = "masterVolume";
    const string MusicKey = "musicVolume";
    const string SfxKey = "sfxVolume";
    const string UiKey = "uiVolume";
    const string VoiceKey = "voiceVolume";

    [SerializeField] AudioMixer audioMixer;

    [Header("Option sliders")]
    [SerializeField] Slider generalVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider effectsVolumeSlider;
    [SerializeField] Slider uIVolumeSlider;
    [SerializeField] Slider dialogueVolumeSlider;

    [Header("Default linear volumes [0-1]")]
    [SerializeField] float defaultMasterVolume = 1f;
    [SerializeField] float defaultSfxVolume = 1f;
    [SerializeField] float defaultMusicVolume = 1f;
    [SerializeField] float defaultUiVolume = 1f;
    [SerializeField] float defaultVoiceVolume = 1f;

    TextMeshProUGUI generalVolumeLabel;
    TextMeshProUGUI musicVolumeLabel;
    TextMeshProUGUI effectsVolumeLabel;
    TextMeshProUGUI uiVolumeLabel;
    TextMeshProUGUI dialogueVolumeLabel;

    void Awake() => GetReferences();
    void Start() => LoadAudioSettings();

    // Called by the sliders' OnValueChanged events
    public void SetGeneralVolume(float volume)  => ApplyVolume(MasterKey, generalVolumeSlider, generalVolumeLabel);
    public void SetMusicVolume(float volume)    => ApplyVolume(MusicKey, musicVolumeSlider, musicVolumeLabel);
    public void SetSfxVolume(float volume)      => ApplyVolume(SfxKey, effectsVolumeSlider, effectsVolumeLabel);
    public void SetUiVolume(float volume)       => ApplyVolume(UiKey, uIVolumeSlider, uiVolumeLabel);
    public void SetDialogueVolume(float volume) => ApplyVolume(VoiceKey, dialogueVolumeSlider, dialogueVolumeLabel);

    public void Reset_AudioOptions()
    {
        SetSlider(MasterKey, generalVolumeSlider, generalVolumeLabel, defaultMasterVolume);
        SetSlider(MusicKey, musicVolumeSlider, musicVolumeLabel, defaultMusicVolume);
        SetSlider(SfxKey, effectsVolumeSlider, effectsVolumeLabel, defaultSfxVolume);
        SetSlider(UiKey, uIVolumeSlider, uiVolumeLabel, defaultUiVolume);
        SetSlider(VoiceKey, dialogueVolumeSlider, dialogueVolumeLabel, defaultVoiceVolume);
    }

    /// <summary>Converts a linear [0, 1] volume to decibels, clamping silence to -80 dB.</summary>
    public static float LinearToDecibels(float linear) => Mathf.Log10(Mathf.Max(linear, MinLinearVolume)) * 20f;

    void ApplyVolume(string key, Slider slider, TextMeshProUGUI label)
    {
        float linear = Mathf.Clamp01(slider.value / Mathf.Max(slider.maxValue, MinLinearVolume));
        audioMixer.SetFloat(key, LinearToDecibels(linear));
        PlayerPrefs.SetFloat(key, slider.value);
        label.text = Mathf.RoundToInt(linear * 100f) + "%";
    }

    void SetSlider(string key, Slider slider, TextMeshProUGUI label, float value)
    {
        slider.SetValueWithoutNotify(value);
        ApplyVolume(key, slider, label);
    }

    // Options are stored in PlayerPrefs so they persist between scenes and sessions
    void LoadAudioSettings()
    {
        SetSlider(MasterKey, generalVolumeSlider, generalVolumeLabel, LoadLinearVolume(MasterKey, defaultMasterVolume));
        SetSlider(MusicKey, musicVolumeSlider, musicVolumeLabel, LoadLinearVolume(MusicKey, defaultMusicVolume));
        SetSlider(SfxKey, effectsVolumeSlider, effectsVolumeLabel, LoadLinearVolume(SfxKey, defaultSfxVolume));
        SetSlider(UiKey, uIVolumeSlider, uiVolumeLabel, LoadLinearVolume(UiKey, defaultUiVolume));
        SetSlider(VoiceKey, dialogueVolumeSlider, dialogueVolumeLabel, LoadLinearVolume(VoiceKey, defaultVoiceVolume));
    }

    static float LoadLinearVolume(string key, float defaultValue)
    {
        float value = PlayerPrefs.GetFloat(key, defaultValue);

        // Older versions stored decibels (<= 0) by mistake, which muted the game on the next launch
        return value > 0f && value <= 1f ? value : defaultValue;
    }

    void GetReferences()
    {
        generalVolumeLabel = generalVolumeSlider.GetComponentInChildren<TextMeshProUGUI>();
        musicVolumeLabel = musicVolumeSlider.GetComponentInChildren<TextMeshProUGUI>();
        effectsVolumeLabel = effectsVolumeSlider.GetComponentInChildren<TextMeshProUGUI>();
        uiVolumeLabel = uIVolumeSlider.GetComponentInChildren<TextMeshProUGUI>();
        dialogueVolumeLabel = dialogueVolumeSlider.GetComponentInChildren<TextMeshProUGUI>();
    }
}
