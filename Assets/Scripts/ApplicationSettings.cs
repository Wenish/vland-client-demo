using UnityEngine;
using UnityEngine.Audio;

public class ApplicationSettings : MonoBehaviour
{
    public static ApplicationSettings Instance;

    public bool IsAudioEnabled { get; set; } = true;
    public int AudioMasterVolume { get; set; } = 100;
    public int AudioMusicVolume { get; set; } = 100;
    public int AudioSfxVolume { get; set; } = 100;
    public int AudioUiVolume { get; set; } = 100;
    public int AudioVoiceVolume { get; set; } = 100;
    public int AudioAmbientVolume { get; set; } = 100;

    public bool IsWindowedFullscreenEnabled { get; set; } = true;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer; // Assign this in the inspector

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
        ApplyAudioSettings();
        ApplyWindowedFullscreenSettings();
    }

    void Start()
    {
        LoadSettings();
        ApplyAudioSettings();
        ApplyWindowedFullscreenSettings();
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("AudioEnabled", IsAudioEnabled ? 1 : 0);
        PlayerPrefs.SetInt("AudioMasterVolume", AudioMasterVolume);
        PlayerPrefs.SetInt("AudioMusicVolume", AudioMusicVolume);
        PlayerPrefs.SetInt("AudioSfxVolume", AudioSfxVolume);
        PlayerPrefs.SetInt("AudioUiVolume", AudioUiVolume);
        PlayerPrefs.SetInt("AudioVoiceVolume", AudioVoiceVolume);
        PlayerPrefs.SetInt("AudioAmbientVolume", AudioAmbientVolume);

        PlayerPrefs.SetInt("WindowedFullscreenEnabled", IsWindowedFullscreenEnabled ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        IsAudioEnabled = PlayerPrefs.GetInt("AudioEnabled", 1) == 1;
        AudioMasterVolume = PlayerPrefs.GetInt("AudioMasterVolume", 100);
        AudioMusicVolume = PlayerPrefs.GetInt("AudioMusicVolume", 100);
        AudioSfxVolume = PlayerPrefs.GetInt("AudioSfxVolume", 100);
        AudioUiVolume = PlayerPrefs.GetInt("AudioUiVolume", 100);
        AudioVoiceVolume = PlayerPrefs.GetInt("AudioVoiceVolume", 100);
        AudioAmbientVolume = PlayerPrefs.GetInt("AudioAmbientVolume", 100);
        IsWindowedFullscreenEnabled = PlayerPrefs.GetInt("WindowedFullscreenEnabled", 1) == 1;

        Debug.Log($"[ApplicationSettings] Settings loaded - Master: {AudioMasterVolume}, Music: {AudioMusicVolume}, SFX: {AudioSfxVolume}, UI: {AudioUiVolume}, Voice: {AudioVoiceVolume}, Ambient: {AudioAmbientVolume}, Enabled: {IsAudioEnabled}");
    }

    public void ApplyAudioSettings()
    {
        if (audioMixer == null)
        {
            Debug.LogError("[ApplicationSettings] AudioMixer is not assigned in ApplicationSettings!");
            return;
        }

        // Apply master volume (convert from 0-100 range to -80-0 dB range)
        float masterVolumeDb = AudioMasterVolume == 0 ? -80f : Mathf.Log10(AudioMasterVolume / 100f) * 20f;
        bool masterResult = audioMixer.SetFloat("MasterVolume", masterVolumeDb);

        // Apply individual channel volumes
        float musicVolumeDb = AudioMusicVolume == 0 ? -80f : Mathf.Log10(AudioMusicVolume / 100f) * 20f;
        bool musicResult = audioMixer.SetFloat("MusicVolume", musicVolumeDb);

        float sfxVolumeDb = AudioSfxVolume == 0 ? -80f : Mathf.Log10(AudioSfxVolume / 100f) * 20f;
        bool sfxResult = audioMixer.SetFloat("SfxVolume", sfxVolumeDb);

        float uiVolumeDb = AudioUiVolume == 0 ? -80f : Mathf.Log10(AudioUiVolume / 100f) * 20f;
        bool uiResult = audioMixer.SetFloat("UiVolume", uiVolumeDb);

        float voiceVolumeDb = AudioVoiceVolume == 0 ? -80f : Mathf.Log10(AudioVoiceVolume / 100f) * 20f;
        bool voiceResult = audioMixer.SetFloat("VoiceVolume", voiceVolumeDb);

        float ambientVolumeDb = AudioAmbientVolume == 0 ? -80f : Mathf.Log10(AudioAmbientVolume / 100f) * 20f;
        bool ambientResult = audioMixer.SetFloat("AmbientVolume", ambientVolumeDb);

        // Handle audio enabled/disabled
        if (!IsAudioEnabled)
        {
            AudioListener.volume = 0f;
            Debug.Log("[ApplicationSettings] Audio disabled via AudioListener.volume = 0");
        }
        else
        {
            AudioListener.volume = 1f;
            Debug.Log("[ApplicationSettings] Audio enabled via AudioListener.volume = 1");
        }
    }

    private void ApplyWindowedFullscreenSettings()
    {
        if (IsWindowedFullscreenEnabled)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Debug.Log("[ApplicationSettings] Windowed Fullscreen enabled");
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Debug.Log("[ApplicationSettings] Windowed Fullscreen disabled");
        }
    }

    // Convenience methods to update individual settings and apply them immediately
    public void SetMasterVolume(int volume)
    {
        AudioMasterVolume = Mathf.Clamp(volume, 0, 100);
        if (audioMixer != null)
        {
            float volumeDb = AudioMasterVolume == 0 ? -80f : Mathf.Log10(AudioMasterVolume / 100f) * 20f;
            audioMixer.SetFloat("MasterVolume", volumeDb);
        }
        SaveSettings();
    }

    public void SetMusicVolume(int volume)
    {
        AudioMusicVolume = Mathf.Clamp(volume, 0, 100);
        if (audioMixer != null)
        {
            float volumeDb = AudioMusicVolume == 0 ? -80f : Mathf.Log10(AudioMusicVolume / 100f) * 20f;
            audioMixer.SetFloat("MusicVolume", volumeDb);
        }
        SaveSettings();
    }

    public void SetSfxVolume(int volume)
    {
        AudioSfxVolume = Mathf.Clamp(volume, 0, 100);
        if (audioMixer != null)
        {
            float volumeDb = AudioSfxVolume == 0 ? -80f : Mathf.Log10(AudioSfxVolume / 100f) * 20f;
            audioMixer.SetFloat("SfxVolume", volumeDb);
        }
        SaveSettings();
    }

    public void SetUiVolume(int volume)
    {
        AudioUiVolume = Mathf.Clamp(volume, 0, 100);
        if (audioMixer != null)
        {
            float volumeDb = AudioUiVolume == 0 ? -80f : Mathf.Log10(AudioUiVolume / 100f) * 20f;
            audioMixer.SetFloat("UiVolume", volumeDb);
        }
        SaveSettings();
    }

    public void SetVoiceVolume(int volume)
    {
        AudioVoiceVolume = Mathf.Clamp(volume, 0, 100);
        if (audioMixer != null)
        {
            float volumeDb = AudioVoiceVolume == 0 ? -80f : Mathf.Log10(AudioVoiceVolume / 100f) * 20f;
            audioMixer.SetFloat("VoiceVolume", volumeDb);
        }
        SaveSettings();
    }

    public void SetAmbientVolume(int volume)
    {
        AudioAmbientVolume = Mathf.Clamp(volume, 0, 100);
        if (audioMixer != null)
        {
            float volumeDb = AudioAmbientVolume == 0 ? -80f : Mathf.Log10(AudioAmbientVolume / 100f) * 20f;
            audioMixer.SetFloat("AmbientVolume", volumeDb);
        }
        SaveSettings();
    }

    public void SetAudioEnabled(bool enabled)
    {
        IsAudioEnabled = enabled;
        ApplyAudioSettings();
        SaveSettings();
    }

    public void SetWindowedFullscreenEnabled(bool enabled)
    {
        IsWindowedFullscreenEnabled = enabled;
        ApplyWindowedFullscreenSettings();
        SaveSettings();
    }

}