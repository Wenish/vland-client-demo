using System.Collections;
using MyGame.Events;
using UnityEngine;
using UnityEngine.Rendering;

public class DeathEffectController : MonoBehaviour
{
    public UnitController MyUnit;
    public Volume postProcessVolume;

    public float fadeDuration = 0.5f;

    private Coroutine _fadeCoroutine;
    private R3.DisposableBag subscriptions;

    void Start()
    {
        GameMessages.Subscribe<MyPlayerUnitSpawnedEvent>(ref subscriptions, OnPlayerUnitSpawned);
    }

    void OnDestroy()
    {
        subscriptions.Dispose();
    }

    private void OnPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
    {
        MyUnit = myPlayerUnitSpawnedEvent.PlayerCharacter;
        MyUnit.OnDied += OnUnitDied;
        MyUnit.OnRevive += OnUnitRevive;
    }

    private void OnUnitRevive()
    {
        StartFadeCoroutine(false);
    }

    private void OnUnitDied()
    {
        StartFadeCoroutine(true);
    }

    private void StartFadeCoroutine(bool toGray)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeGrayscale(toGray));
    }

    IEnumerator FadeGrayscale(bool toGray)
    {
        float start = postProcessVolume.weight;
        float end = toGray ? 1f : 0f;
        float duration = fadeDuration;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            postProcessVolume.weight = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }

        postProcessVolume.weight = end;
        _fadeCoroutine = null;
    }
}
