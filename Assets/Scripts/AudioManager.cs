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
    public float musicFadeDuration = 3.0f;
    
    [Range(0f, 1f)]
    public float musicMaxVolume = 1.0f;

    public void PlayMusic(AudioClip music, bool loop = true)
    {
        if (music == null)
            return;

        bool isSameClip = musicSource.clip == music;
        bool isPlayingSameClip = isSameClip && musicSource.isPlaying;

        if (isPlayingSameClip && musicFadeCoroutine == null)
            return; // nothing to do

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        // If a different clip is currently playing, fade it out then fade in the new one.
        if (musicSource.isPlaying && !isSameClip)
        {
            musicFadeCoroutine = StartCoroutine(FadeOutAndInMusic(music, loop));
        }
        else
        {
            // Either nothing is playing or same clip is requested but not playing -> just fade in
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

    public AudioClip GetCurrentMusicPlaying()
    {
        return musicSource.isPlaying ? musicSource.clip : null;
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

    // Fades out the currently playing music (if any) and then fades in the provided clip.
    private System.Collections.IEnumerator FadeOutAndInMusic(AudioClip newClip, bool loop)
    {
        // Fade out current
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float t = 0f;
            if (musicFadeDuration <= 0f)
            {
                musicSource.volume = 0f;
            }
            else
            {
                while (t < musicFadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    musicSource.volume = Mathf.Lerp(startVolume, 0f, t / musicFadeDuration);
                    yield return null;
                }
                musicSource.volume = 0f;
            }
            musicSource.Stop();
        }

        // Fade in new
        float targetVolume = musicMaxVolume;
        musicSource.volume = 0f;
        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.Play();
        float tIn = 0f;
        if (musicFadeDuration <= 0f)
        {
            musicSource.volume = targetVolume;
        }
        else
        {
            while (tIn < musicFadeDuration)
            {
                tIn += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, targetVolume, tIn / musicFadeDuration);
                yield return null;
            }
            musicSource.volume = targetVolume;
        }
        musicFadeCoroutine = null;
    }
}
