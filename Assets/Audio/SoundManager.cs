using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("References")]
    [SerializeField] private GameObject audioSourcePrefab;
    [SerializeField] private SoundDatabase soundDatabase;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    /// <summary>
    /// Play a sound by its name as defined in the SoundDatabase.
    /// </summary>
    /// <param name="soundName">Name of the sound in SoundDatabase</param>
    /// <param name="position">Optional world position (null = global 2D sound)</param>
    /// <param name="parent">Optional parent GameObject to attach the sound to</param>
    public void PlaySound(string soundName, Vector3? position = null, Transform parent = null, float pitchOffset = 0f)
    {
        SoundData soundData = soundDatabase.GetSound(soundName);
        if (soundData == null)
        {
            Debug.LogWarning($"[SoundManager] Sound '{soundName}' not found in SoundDatabase.");
            return;
        }

        PlaySound(soundData, position, parent, pitchOffset);
    }

    /// <summary>
    /// Play a SoundData object directly.
    /// </summary>
    /// <param name="soundData">The SoundData asset to play</param>
    /// <param name="position">Optional world position (null = global 2D sound)</param>
    /// <param name="parent">Optional parent GameObject to attach the sound to</param>
    public void PlaySound(SoundData soundData, Vector3? position = null, Transform parent = null, float pitchOffset = 0f)
    {
        if (soundData == null || soundData.clip == null)
        {
            Debug.LogWarning("[SoundManager] Invalid SoundData or missing AudioClip.");
            return;
        }

        Vector3 spawnPosition = position ?? Vector3.zero;
        GameObject go = Instantiate(audioSourcePrefab, spawnPosition, Quaternion.identity);
        
        // Attach to parent if provided
        if (parent != null)
        {
            go.transform.SetParent(parent);
        }
        
        AudioSource source = go.GetComponent<AudioSource>();

        if (source == null)
        {
            Debug.LogError("[SoundManager] AudioSource prefab is missing an AudioSource component.");
            Destroy(go);
            return;
        }

        source.clip = soundData.clip;
        source.outputAudioMixerGroup = soundData.mixerGroup;
        source.volume = soundData.volume;
        source.pitch = soundData.pitch + pitchOffset;
        source.spatialBlend = soundData.isSpatial ? 1f : 0f;

        source.Play();
        Destroy(go, soundData.clip.length / source.pitch); // Cleanup
    }
}
