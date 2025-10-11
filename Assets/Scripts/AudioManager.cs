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

    // Loop-with-pause settings
    [Tooltip("Seconds to wait after a track finishes before looping again.")]
    public float loopPauseSeconds = 120f; // 2 minutes by default

    private Coroutine loopPauseCoroutine;
    private bool loopWithPauseEnabled = false;

    public void PlayMusic(AudioClip music, bool loop = true)
    {
        if (music == null)
            return;

        bool isSameClip = musicSource.clip == music;
        bool isPlayingSameClip = isSameClip && musicSource.isPlaying;

        // Update desired loop behavior (we manage looping manually)
        loopWithPauseEnabled = loop;
        musicSource.loop = false; // we handle looping with a pause manually

        if (isPlayingSameClip && musicFadeCoroutine == null)
        {
            // Potentially only the loop flag changed; ensure loop coroutine reflects that
            if (loopWithPauseEnabled)
                StartLoopWithPauseIfNeeded();
            else
                StopLoopWithPauseCoroutine();
            return; // nothing else to do
        }

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        // If a different clip is currently playing, fade it out then fade in the new one.
        if (musicSource.isPlaying && !isSameClip)
        {
            // stop any loop coroutine before switching
            StopLoopWithPauseCoroutine();
            musicFadeCoroutine = StartCoroutine(FadeOutAndInMusic(music, loop));
        }
        else
        {
            // Either nothing is playing or same clip is requested but not playing -> just fade in
            StopLoopWithPauseCoroutine();
            musicFadeCoroutine = StartCoroutine(FadeInMusic(music, loop));
        }
    }


    public void PlayMusic()
    {
        if (!musicSource.isPlaying)
        {
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);
            // use our stored desired loopWithPauseEnabled instead of AudioSource.loop
            musicFadeCoroutine = StartCoroutine(FadeInMusic(musicSource.clip, loopWithPauseEnabled));
        }
    }


    public void StopMusic()
    {
        if (musicSource.isPlaying)
        {
            // ensure loop doesn't restart while we fade out
            loopWithPauseEnabled = false;
            StopLoopWithPauseCoroutine();
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
        loopWithPauseEnabled = loop;
        float targetVolume = musicMaxVolume;
        musicSource.volume = 0f;
        musicSource.clip = newClip;
        musicSource.loop = false; // manual looping if enabled
        musicSource.Play();
        float t = 0f;
        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / musicFadeDuration);
            yield return null;
        }
        musicSource.volume = targetVolume;
        StartLoopWithPauseIfNeeded();
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
        // Ensure loop coroutine doesn't try to restart while transitioning
        StopLoopWithPauseCoroutine();
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
        loopWithPauseEnabled = loop;
        float targetVolume = musicMaxVolume;
        musicSource.volume = 0f;
        musicSource.clip = newClip;
        musicSource.loop = false; // manual looping if enabled
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
        StartLoopWithPauseIfNeeded();
        musicFadeCoroutine = null;
    }

    private void StopLoopWithPauseCoroutine()
    {
        if (loopPauseCoroutine != null)
        {
            StopCoroutine(loopPauseCoroutine);
            loopPauseCoroutine = null;
        }
    }

    private void StartLoopWithPauseIfNeeded()
    {
        if (!loopWithPauseEnabled || musicSource.clip == null)
            return;
        // Always disable built-in looping
        musicSource.loop = false;
        if (loopPauseCoroutine != null)
        {
            StopCoroutine(loopPauseCoroutine);
            loopPauseCoroutine = null;
        }
        loopPauseCoroutine = StartCoroutine(LoopWithPauseCoroutine());
    }

    private System.Collections.IEnumerator LoopWithPauseCoroutine()
    {
        // Ensure no built-in loop
        musicSource.loop = false;

        while (loopWithPauseEnabled && musicSource.clip != null)
        {
            // Wait for current playback to finish naturally
            while (loopWithPauseEnabled && musicSource.clip != null && musicSource.isPlaying)
            {
                yield return null;
            }

            if (!loopWithPauseEnabled || musicSource.clip == null)
                break;

            // Wait for the configured pause duration in real time
            float waited = 0f;
            while (loopWithPauseEnabled && musicSource.clip != null && waited < loopPauseSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!loopWithPauseEnabled || musicSource.clip == null)
                break;

            // Restart track from beginning without changing volume (respect current volume)
            musicSource.time = 0f;
            musicSource.Play();
        }

        loopPauseCoroutine = null;
    }
}
