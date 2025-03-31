using System.Collections;
using MyGame.Events;
using UnityEngine;
using UnityEngine.UIElements;

public class UiDocumentZombieIngameController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private Label _labelWave;
    private Label _labelRoundStarted;
    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _labelWave = _uiDocument.rootVisualElement.Q<Label>(name: "labelWave");
        _labelRoundStarted = _uiDocument.rootVisualElement.Q<Label>(name: "labelRoundStarted");
        _labelWave.text = "";
        _labelRoundStarted.text = "";
    }

    void Start()
    {
        EventManager.Instance.Subscribe<WaveStartedEvent>(OnWaveStartedEvent);
    }

    void OnDestroy()
    {
        EventManager.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStartedEvent);
    }

    void OnWaveStartedEvent(WaveStartedEvent waveStartedEvent)
    {
        Debug.Log($"Wave {waveStartedEvent.WaveNumber} started with {waveStartedEvent.TotalZombies} zombies.");
        _labelWave.text = waveStartedEvent.WaveNumber.ToString();
        _labelRoundStarted.text = $"Round\n{waveStartedEvent.WaveNumber}";
        _labelRoundStarted.style.opacity = 1f;
        StartCoroutine(FadeText(_labelRoundStarted, 0f, 1f, 1f)); // Fade in
        StartCoroutine(WaitAndFadeOut(_labelRoundStarted, 1f, 3f)); // Wait and fade out
    }

    IEnumerator FadeText(Label textLabel, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            textLabel.style.opacity = newAlpha;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        textLabel.style.opacity = endAlpha;
    }

    IEnumerator WaitAndFadeOut(Label textLabel, float waitTime, float fadeDuration)
    {
        yield return new WaitForSeconds(waitTime);
        float elapsedTime = 0f;
        float startAlpha = textLabel.style.opacity.value;
        float endAlpha = 0f;

        while (elapsedTime < fadeDuration)
        {
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);
            textLabel.style.opacity = newAlpha;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        textLabel.style.opacity = endAlpha;
    }
}
