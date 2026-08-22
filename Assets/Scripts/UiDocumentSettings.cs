using System.Collections.Generic;
using ShadowInfection.DI;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class UiDocumentSettings : MonoBehaviour
{
    public AudioMixer audioMixer;
    private UIDocument uiDocument;
    private ApplicationSettings settings;

    private int audioDefaultValue = 100;

    private TextField nicknameField;

    private Toggle toggleAudio;

    private SliderInt sliderAudioMaster;
    private SliderInt sliderAudioMusic;
    private SliderInt sliderAudioSfx;
    private SliderInt silderAudioUi;
    private SliderInt sliderAudioVoice;
    private SliderInt sliderAudioAmbient;

    private Toggle toggleWindowFullscreen;

    private DropdownField dropdownFieldResolution;

    private Button buttonResetSettings;

    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found on this GameObject.");
            return;
        }
        // Get the root VisualElement
        VisualElement root = uiDocument.rootVisualElement;

        // Find settings elements by name
        toggleAudio = root.Q<Toggle>("ToggleAudio");
        sliderAudioMaster = root.Q<SliderInt>("SliderAudioMaster");
        sliderAudioMusic = root.Q<SliderInt>("SliderAudioMusic");
        sliderAudioSfx = root.Q<SliderInt>("SliderAudioSfx");
        silderAudioUi = root.Q<SliderInt>("SliderAudioUi");
        sliderAudioVoice = root.Q<SliderInt>("SliderAudioVoice");
        sliderAudioAmbient = root.Q<SliderInt>("SliderAudioAmbient");
        toggleWindowFullscreen = root.Q<Toggle>("ToggleWindowFullscreen");
        dropdownFieldResolution = root.Q<DropdownField>("DropdownFieldResolution");
        buttonResetSettings = root.Q<Button>("ButtonResetSettings");
        nicknameField = root.Q<TextField>("TextFieldNickname");
    }

    private EventCallback<ChangeEvent<int>> masterCallback;
    private EventCallback<ChangeEvent<int>> musicCallback;
    private EventCallback<ChangeEvent<int>> sfxCallback;
    private EventCallback<ChangeEvent<int>> uiCallback;
    private EventCallback<ChangeEvent<int>> voiceCallback;
    private EventCallback<ChangeEvent<int>> ambientCallback;
    private EventCallback<ChangeEvent<bool>> audioToggleCallback;
    private EventCallback<ChangeEvent<bool>> windowFullscreenToggleCallback;
    private EventCallback<ChangeEvent<string>> resolutionDropdownCallback;
    private EventCallback<ChangeEvent<string>> nicknameFieldCallback;

    void OnEnable()
    {
        settings = GameServices.Settings;
        if (settings == null)
            return;

        // Initialize callbacks with proper ApplicationSettings methods
        masterCallback = evt => settings.SetMasterVolume(evt.newValue);
        musicCallback = evt => settings.SetMusicVolume(evt.newValue);
        sfxCallback = evt => settings.SetSfxVolume(evt.newValue);
        uiCallback = evt => settings.SetUiVolume(evt.newValue);
        voiceCallback = evt => settings.SetVoiceVolume(evt.newValue);
        ambientCallback = evt => settings.SetAmbientVolume(evt.newValue);
        audioToggleCallback = evt => settings.SetAudioEnabled(evt.newValue);
        windowFullscreenToggleCallback = evt => settings.SetWindowedFullscreenEnabled(evt.newValue);
        resolutionDropdownCallback = evt => settings.SetResolutionIndex(dropdownFieldResolution.index);
        nicknameFieldCallback = evt => settings.SetNickname(evt.newValue);

        // Load current settings and initialize UI
        LoadAndApplyCurrentSettings();

        // Register callbacks
        if (sliderAudioMaster != null)
            sliderAudioMaster.RegisterValueChangedCallback(masterCallback);
        if (sliderAudioMusic != null)
            sliderAudioMusic.RegisterValueChangedCallback(musicCallback);
        if (sliderAudioSfx != null)
            sliderAudioSfx.RegisterValueChangedCallback(sfxCallback);
        if (silderAudioUi != null)
            silderAudioUi.RegisterValueChangedCallback(uiCallback);
        if (sliderAudioVoice != null)
            sliderAudioVoice.RegisterValueChangedCallback(voiceCallback);
        if (sliderAudioAmbient != null)
            sliderAudioAmbient.RegisterValueChangedCallback(ambientCallback);

        if (toggleAudio != null)
            toggleAudio.RegisterValueChangedCallback(audioToggleCallback);

        if (toggleWindowFullscreen != null)
            toggleWindowFullscreen.RegisterValueChangedCallback(windowFullscreenToggleCallback);

        if (buttonResetSettings != null)
            buttonResetSettings.clicked += OnButtonResetSettings;

        if (dropdownFieldResolution != null)
            dropdownFieldResolution.RegisterValueChangedCallback(resolutionDropdownCallback);

        if (nicknameField != null)
            nicknameField.RegisterValueChangedCallback(nicknameFieldCallback);
    }

    void OnDisable()
    {
        sliderAudioMaster?.UnregisterValueChangedCallback(masterCallback);
        sliderAudioMusic?.UnregisterValueChangedCallback(musicCallback);
        sliderAudioSfx?.UnregisterValueChangedCallback(sfxCallback);
        silderAudioUi?.UnregisterValueChangedCallback(uiCallback);
        sliderAudioVoice?.UnregisterValueChangedCallback(voiceCallback);
        sliderAudioAmbient?.UnregisterValueChangedCallback(ambientCallback);
        toggleAudio?.UnregisterValueChangedCallback(audioToggleCallback);
        toggleWindowFullscreen?.UnregisterValueChangedCallback(windowFullscreenToggleCallback);
        dropdownFieldResolution?.UnregisterValueChangedCallback(resolutionDropdownCallback);
        buttonResetSettings.clicked -= OnButtonResetSettings;
        nicknameField?.UnregisterValueChangedCallback(nicknameFieldCallback);
    }

    void Start()
    {
        // Ensure ApplicationSettings is ready and apply current settings to UI
        if (settings != null)
        {
            LoadAndApplyCurrentSettings();
        }
    }


    private void LoadAndApplyCurrentSettings()
    {
        if (settings == null)
        {
            settings = GameServices.Settings;
            if (settings == null)
                return;
        }

        // Set toggle value
        if (toggleAudio != null)
            toggleAudio.SetValueWithoutNotify(settings.IsAudioEnabled);

        // Set slider values without triggering callbacks
        if (sliderAudioMaster != null)
            sliderAudioMaster.SetValueWithoutNotify(settings.AudioMasterVolume);
        if (sliderAudioMusic != null)
            sliderAudioMusic.SetValueWithoutNotify(settings.AudioMusicVolume);
        if (sliderAudioSfx != null)
            sliderAudioSfx.SetValueWithoutNotify(settings.AudioSfxVolume);
        if (silderAudioUi != null)
            silderAudioUi.SetValueWithoutNotify(settings.AudioUiVolume);
        if (sliderAudioVoice != null)
            sliderAudioVoice.SetValueWithoutNotify(settings.AudioVoiceVolume);
        if (sliderAudioAmbient != null)
            sliderAudioAmbient.SetValueWithoutNotify(settings.AudioAmbientVolume);

        // Set windowed fullscreen toggle
        if (toggleWindowFullscreen != null)
            toggleWindowFullscreen.SetValueWithoutNotify(settings.IsWindowedFullscreenEnabled);

        // Set resolution dropdown
        if (dropdownFieldResolution != null)
        {
            dropdownFieldResolution.choices = GetResolutionChoices();
            dropdownFieldResolution.index = settings.SelectedResolutionIndex;
        }
        // Set nickname field
        if (nicknameField != null)
            nicknameField.SetValueWithoutNotify(settings.Nickname);

    }

    private List<string> GetResolutionChoices()
    {
        List<string> choices = new List<string>();
        foreach (Resolution res in Screen.resolutions)
        {
            choices.Add($"{res.width} x {res.height} @ {(int)res.refreshRateRatio.value}Hz");
        }
        return choices;
    }

    void OnButtonResetSettings()
    {
        Debug.Log("Reset Settings button clicked");
        
        if (settings == null)
        {
            settings = GameServices.Settings;
            if (settings == null)
                return;
        }

        // Reset all audio settings to default values
        settings.SetAudioEnabled(true);
        settings.SetMasterVolume(audioDefaultValue);
        settings.SetMusicVolume(audioDefaultValue);
        settings.SetSfxVolume(audioDefaultValue);
        settings.SetUiVolume(audioDefaultValue);
        settings.SetVoiceVolume(audioDefaultValue);
        settings.SetAmbientVolume(audioDefaultValue);

        // Reset windowed fullscreen setting
        settings.SetResolutionIndex(settings.GetDefaultResolutionIndex());

        // Somehow reseting window fullscreeen with reesolution before bugs out
        // settings.SetWindowedFullscreenEnabled(true);

        // Update UI elements to reflect the reset values
        LoadAndApplyCurrentSettings();
        
        buttonResetSettings?.Blur();
    }
}
