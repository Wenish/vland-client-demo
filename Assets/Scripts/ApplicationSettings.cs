using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class ApplicationSettings : MonoBehaviour
{
    public static ApplicationSettings Instance;

    public string Nickname { get; set; } = "";
    public bool IsAudioEnabled { get; set; } = true;
    public int AudioMasterVolume { get; set; } = 100;
    public int AudioMusicVolume { get; set; } = 100;
    public int AudioSfxVolume { get; set; } = 100;
    public int AudioUiVolume { get; set; } = 100;
    public int AudioVoiceVolume { get; set; } = 100;
    public int AudioAmbientVolume { get; set; } = 100;

    public bool IsWindowedFullscreenEnabled { get; set; } = true;
    public int SelectedResolutionIndex { get; set; } = 0;

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
        ApplyResolutionSettings();
    }

    void Start()
    {
        LoadSettings();
        ApplyAudioSettings();
        ApplyWindowedFullscreenSettings();
        ApplyResolutionSettings();
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetString("Nickname", Nickname);
        PlayerPrefs.SetInt("AudioEnabled", IsAudioEnabled ? 1 : 0);
        PlayerPrefs.SetInt("AudioMasterVolume", AudioMasterVolume);
        PlayerPrefs.SetInt("AudioMusicVolume", AudioMusicVolume);
        PlayerPrefs.SetInt("AudioSfxVolume", AudioSfxVolume);
        PlayerPrefs.SetInt("AudioUiVolume", AudioUiVolume);
        PlayerPrefs.SetInt("AudioVoiceVolume", AudioVoiceVolume);
        PlayerPrefs.SetInt("AudioAmbientVolume", AudioAmbientVolume);

        PlayerPrefs.SetInt("WindowedFullscreenEnabled", IsWindowedFullscreenEnabled ? 1 : 0);
        PlayerPrefs.SetInt("SelectedResolutionIndex", SelectedResolutionIndex);
        Debug.Log($"[ApplicationSettings] Saving Resolution Index: {SelectedResolutionIndex}");



        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        Nickname = PlayerPrefs.GetString("Nickname", "");
        IsAudioEnabled = PlayerPrefs.GetInt("AudioEnabled", 1) == 1;
        AudioMasterVolume = PlayerPrefs.GetInt("AudioMasterVolume", 100);
        AudioMusicVolume = PlayerPrefs.GetInt("AudioMusicVolume", 100);
        AudioSfxVolume = PlayerPrefs.GetInt("AudioSfxVolume", 100);
        AudioUiVolume = PlayerPrefs.GetInt("AudioUiVolume", 100);
        AudioVoiceVolume = PlayerPrefs.GetInt("AudioVoiceVolume", 100);
        AudioAmbientVolume = PlayerPrefs.GetInt("AudioAmbientVolume", 100);
        IsWindowedFullscreenEnabled = PlayerPrefs.GetInt("WindowedFullscreenEnabled", 1) == 1;
        SelectedResolutionIndex = PlayerPrefs.GetInt("SelectedResolutionIndex", GetDefaultResolutionIndex());
    }

    public int GetDefaultResolutionIndex()
    {
        Resolution current = Screen.currentResolution;
        Resolution[] available = Screen.resolutions;

        // First, try to find exact match (resolution + refresh rate)
        for (int i = 0; i < available.Length; i++)
        {
            if (available[i].width == current.width &&
                available[i].height == current.height &&
                available[i].refreshRateRatio.value == current.refreshRateRatio.value)
            {
                return i;
            }
        }

        // Second, try to find resolution match (ignore refresh rate)
        for (int i = 0; i < available.Length; i++)
        {
            if (available[i].width == current.width &&
                available[i].height == current.height)
            {
                return i;
            }
        }

        // Fallback: find closest resolution by total pixel count
        int closestIndex = 0;
        int currentPixels = current.width * current.height;
        int closestPixelDiff = int.MaxValue;

        for (int i = 0; i < available.Length; i++)
        {
            int availablePixels = available[i].width * available[i].height;
            int pixelDiff = Mathf.Abs(availablePixels - currentPixels);

            if (pixelDiff < closestPixelDiff)
            {
                closestPixelDiff = pixelDiff;
                closestIndex = i;
            }
        }

        Debug.LogWarning($"[ApplicationSettings] Could not find exact resolution match for {current.width}x{current.height}@{current.refreshRateRatio.value}Hz. Using closest match: {available[closestIndex].width}x{available[closestIndex].height}@{available[closestIndex].refreshRateRatio.value}Hz");
        return closestIndex;
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
        }
        else
        {
            AudioListener.volume = 1f;
        }
    }

    private void ApplyWindowedFullscreenSettings()
    {
        if (IsWindowedFullscreenEnabled)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }

    public void ApplyResolutionSettings()
    {
        Resolution[] resolutions = Screen.resolutions;
        if (resolutions.Length == 0 || SelectedResolutionIndex < 0 || SelectedResolutionIndex >= resolutions.Length)
        {
            Debug.LogWarning($"[ApplicationSettings] Invalid resolution index: {SelectedResolutionIndex}, available: {resolutions.Length}");
            return;
        }

        Resolution selected = resolutions[SelectedResolutionIndex];

        Screen.SetResolution(selected.width, selected.height, Screen.fullScreenMode);
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

    public void SetResolutionIndex(int index)
    {
        SelectedResolutionIndex = Mathf.Clamp(index, 0, Screen.resolutions.Length - 1);
        ApplyResolutionSettings();
        SaveSettings();
    }

    public void SetNickname(string nickname)
    {
        Nickname = nickname;
        SaveSettings();
    }

}