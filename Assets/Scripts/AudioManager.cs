using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource musicSource;
    public AudioSource uiSfxSource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject); // Verhindert Duplikate
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayUISound(AudioClip clip)
    {
        uiSfxSource.PlayOneShot(clip);
    }

    public void PlayMusic(AudioClip music, bool loop = true)
    {
        if (musicSource.clip != music)
        {
            musicSource.clip = music;
            musicSource.loop = loop;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
}
