using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class UiDocumentSettings : MonoBehaviour
{
    public AudioMixer audioMixer;
    private UIDocument uiDocument;

    private VisualElement pageSettings;

    private int audioDefaultValue = 0;

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


        pageSettings = root.Q<VisualElement>("PageSettings");

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

    void OnEnable()
    {
        masterCallback = evt => audioMixer.SetFloat("MasterVolume", evt.newValue);
        musicCallback = evt => audioMixer.SetFloat("MusicVolume", evt.newValue);
        sfxCallback = evt => audioMixer.SetFloat("SfxVolume", evt.newValue);
        uiCallback = evt => audioMixer.SetFloat("UiVolume", evt.newValue);
        voiceCallback = evt => audioMixer.SetFloat("VoiceVolume", evt.newValue);
        ambientCallback = evt => audioMixer.SetFloat("AmbientVolume", evt.newValue);

        RegisterSliderWithMixer(sliderAudioMaster, "MasterVolume", masterCallback);
        RegisterSliderWithMixer(sliderAudioMusic, "MusicVolume", musicCallback);
        RegisterSliderWithMixer(sliderAudioSfx, "SfxVolume", sfxCallback);
        RegisterSliderWithMixer(silderAudioUi, "UiVolume", uiCallback);
        RegisterSliderWithMixer(sliderAudioVoice, "VoiceVolume", voiceCallback);
        RegisterSliderWithMixer(sliderAudioAmbient, "AmbientVolume", ambientCallback);

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

        if (buttonResetSettings != null)
            buttonResetSettings.clicked -= OnButtonResetSettings;
    }

    private void RegisterSliderWithMixer(SliderInt slider, string mixerParameter, EventCallback<ChangeEvent<int>> callback)
    {
        float volume;
        if (audioMixer.GetFloat(mixerParameter, out volume))
        {
            slider.value = (int)volume;
        }

        slider.RegisterValueChangedCallback(callback);
    }
    
    void OnButtonResetSettings()
    {
        Debug.Log("Reset Settings button clicked");
        
        // Reset audio settings to default values
        audioMixer.SetFloat("MasterVolume", audioDefaultValue);
        audioMixer.SetFloat("MusicVolume", audioDefaultValue);
        audioMixer.SetFloat("SfxVolume", audioDefaultValue);
        audioMixer.SetFloat("UiVolume", audioDefaultValue);
        audioMixer.SetFloat("VoiceVolume", audioDefaultValue);
        audioMixer.SetFloat("AmbientVolume", audioDefaultValue);

        // Reset sliders to default values
        sliderAudioMaster.value = audioDefaultValue;
        sliderAudioMusic.value = audioDefaultValue;
        sliderAudioSfx.value = audioDefaultValue;
        silderAudioUi.value = audioDefaultValue;
        sliderAudioVoice.value = audioDefaultValue;
        sliderAudioAmbient.value = audioDefaultValue;
    }
}
