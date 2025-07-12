using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class UiDocumentSettings : MonoBehaviour
{
    public AudioMixer audioMixer;
    private UIDocument uiDocument;

    private int audioDefaultValue = 100;

    private Toggle toggleAudio;

    private SliderInt sliderAudioMaster;
    private SliderInt sliderAudioMusic;
    private SliderInt sliderAudioSfx;
    private SliderInt silderAudioUi;
    private SliderInt sliderAudioVoice;
    private SliderInt sliderAudioAmbient;

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
        buttonResetSettings = root.Q<Button>("ButtonResetSettings");
    }

    private EventCallback<ChangeEvent<int>> masterCallback;
    private EventCallback<ChangeEvent<int>> musicCallback;
    private EventCallback<ChangeEvent<int>> sfxCallback;
    private EventCallback<ChangeEvent<int>> uiCallback;
    private EventCallback<ChangeEvent<int>> voiceCallback;
    private EventCallback<ChangeEvent<int>> ambientCallback;
    private EventCallback<ChangeEvent<bool>> audioToggleCallback;

    void OnEnable()
    {
        // Initialize callbacks with proper ApplicationSettings methods
        masterCallback = evt => ApplicationSettings.Instance.SetMasterVolume(evt.newValue);
        musicCallback = evt => ApplicationSettings.Instance.SetMusicVolume(evt.newValue);
        sfxCallback = evt => ApplicationSettings.Instance.SetSfxVolume(evt.newValue);
        uiCallback = evt => ApplicationSettings.Instance.SetUiVolume(evt.newValue);
        voiceCallback = evt => ApplicationSettings.Instance.SetVoiceVolume(evt.newValue);
        ambientCallback = evt => ApplicationSettings.Instance.SetAmbientVolume(evt.newValue);
        audioToggleCallback = evt => ApplicationSettings.Instance.SetAudioEnabled(evt.newValue);
        
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

        if (buttonResetSettings != null)
            buttonResetSettings.clicked += OnButtonResetSettings;
    }

    void OnDisable()
    {
        sliderAudioMaster?.UnregisterValueChangedCallback(masterCallback);
        sliderAudioMusic?.UnregisterValueChangedCallback(musicCallback);
        sliderAudioSfx?.UnregisterValueChangedCallback(sfxCallback);
        silderAudioUi?.UnregisterValueChangedCallback(uiCallback);
        sliderAudioVoice?.UnregisterValueChangedCallback(voiceCallback);
        sliderAudioAmbient?.UnregisterValueChangedCallback(ambientCallback);

        if (toggleAudio != null)
            toggleAudio.UnregisterValueChangedCallback(audioToggleCallback);

        if (buttonResetSettings != null)
            buttonResetSettings.clicked -= OnButtonResetSettings;
    }

    void Start()
    {
        // Ensure ApplicationSettings is ready and apply current settings to UI
        if (ApplicationSettings.Instance != null)
        {
            LoadAndApplyCurrentSettings();
        }
    }

    private void LoadAndApplyCurrentSettings()
    {
        if (ApplicationSettings.Instance == null) return;

        // Set toggle value
        if (toggleAudio != null)
            toggleAudio.SetValueWithoutNotify(ApplicationSettings.Instance.IsAudioEnabled);

        // Set slider values without triggering callbacks
        if (sliderAudioMaster != null)
            sliderAudioMaster.SetValueWithoutNotify(ApplicationSettings.Instance.AudioMasterVolume);
        if (sliderAudioMusic != null)
            sliderAudioMusic.SetValueWithoutNotify(ApplicationSettings.Instance.AudioMusicVolume);
        if (sliderAudioSfx != null)
            sliderAudioSfx.SetValueWithoutNotify(ApplicationSettings.Instance.AudioSfxVolume);
        if (silderAudioUi != null)
            silderAudioUi.SetValueWithoutNotify(ApplicationSettings.Instance.AudioUiVolume);
        if (sliderAudioVoice != null)
            sliderAudioVoice.SetValueWithoutNotify(ApplicationSettings.Instance.AudioVoiceVolume);
        if (sliderAudioAmbient != null)
            sliderAudioAmbient.SetValueWithoutNotify(ApplicationSettings.Instance.AudioAmbientVolume);
    }

    void OnButtonResetSettings()
    {
        Debug.Log("Reset Settings button clicked");
        
        if (ApplicationSettings.Instance == null) return;

        // Reset all audio settings to default values
        ApplicationSettings.Instance.SetAudioEnabled(true);
        ApplicationSettings.Instance.SetMasterVolume(audioDefaultValue);
        ApplicationSettings.Instance.SetMusicVolume(audioDefaultValue);
        ApplicationSettings.Instance.SetSfxVolume(audioDefaultValue);
        ApplicationSettings.Instance.SetUiVolume(audioDefaultValue);
        ApplicationSettings.Instance.SetVoiceVolume(audioDefaultValue);
        ApplicationSettings.Instance.SetAmbientVolume(audioDefaultValue);

        // Update UI elements to reflect the reset values
        LoadAndApplyCurrentSettings();
        
        buttonResetSettings?.Blur();
    }
}
