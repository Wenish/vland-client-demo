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


    private Coroutine musicFadeCoroutine;
    public float musicFadeDuration = 1.0f;
    
    [Range(0f, 1f)]
    public float musicMaxVolume = 1.0f;

    public void PlayMusic(AudioClip music, bool loop = true)
    {
        if (musicSource.clip != music)
        {
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeInMusic(music, loop));
        }
    }


    public void PlayMusic()
    {
        if (!musicSource.isPlaying)
        {
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeInMusic(musicSource.clip, musicSource.loop));
        }
    }


    public void StopMusic()
    {
        if (musicSource.isPlaying)
        {
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeOutMusic());
        }
    }

    private System.Collections.IEnumerator FadeInMusic(AudioClip newClip, bool loop)
    {
        float targetVolume = musicMaxVolume;
        musicSource.volume = 0f;
        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.Play();
        float t = 0f;
        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / musicFadeDuration);
            yield return null;
        }
        musicSource.volume = targetVolume;
        musicFadeCoroutine = null;
    }

    private System.Collections.IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        float t = 0f;
        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / musicFadeDuration);
            yield return null;
        }
        musicSource.volume = 0f;
        musicSource.Stop();
        musicFadeCoroutine = null;
    }
}
