using System.Collections;
using MyGame.Events;
using UnityEngine;
using UnityEngine.UIElements;

public class UiDocumentZombieIngameController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private Label _labelWave;
    private Label _labelRoundStarted;
    private Label _labelGold;
    private Coroutine _goldCoroutine;
    private AbilityCooldownElement _baseAttack;

    [SerializeField]
    private UnitController _myPlayerUnitController;
    private WeaponController _myPlayerUnitWeaponController;

    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _labelWave = _uiDocument.rootVisualElement.Q<Label>(name: "labelWave");
        _labelRoundStarted = _uiDocument.rootVisualElement.Q<Label>(name: "labelRoundStarted");
        _labelGold = _uiDocument.rootVisualElement.Q<Label>(name: "labelGold");
        _labelWave.text = "";
        _labelRoundStarted.text = "";
        _labelGold.text = "Gold: 0";
        _baseAttack = _uiDocument.rootVisualElement.Q<AbilityCooldownElement>(name: "baseAttack");
        _baseAttack.CooldownRemaining = 0f;
        _baseAttack.CooldownProgress = 0f;
    }

    void Start()
    {
        EventManager.Instance.Subscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
        EventManager.Instance.Subscribe<WaveStartedEvent>(OnWaveStartedEvent);
        EventManager.Instance.Subscribe<PlayerGoldChangedEvent>(OnPlayerGoldChangedEvent);
    }

    void OnDestroy()
    {
        EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
        EventManager.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStartedEvent);
        EventManager.Instance.Unsubscribe<PlayerGoldChangedEvent>(OnPlayerGoldChangedEvent);
    }

    void Update()
    {
        SyncAttackCooldown();
    }

    void SyncAttackCooldown()
    {
        if (_myPlayerUnitController == null) return;
        if (_myPlayerUnitWeaponController == null) return;

        _baseAttack.CooldownRemaining = _myPlayerUnitWeaponController.AttackCooldownRemaining;
        _baseAttack.CooldownProgress = _myPlayerUnitWeaponController.AttackCooldownProgress;
    }

    void OnPlayerGoldChangedEvent(PlayerGoldChangedEvent playerGoldChangedEvent)
    {
        var isLocalPlayer = playerGoldChangedEvent.Player.isLocalPlayer;
        if (!isLocalPlayer) return; // Ignore if not local player

        SetGoldText(playerGoldChangedEvent.NewGoldAmount);
    }

    void SetGoldText(int gold)
    {
        int currentGold = int.Parse(_labelGold.text.Replace("Gold: ", ""));
        if (currentGold != gold)
        {
            _labelGold.text = $"Gold: {gold}";
        }
        if (_goldCoroutine != null)
        {
            StopCoroutine(_goldCoroutine);
        }
        _goldCoroutine = StartCoroutine(CountGoldCoroutine(currentGold, gold));
    }

    void OnWaveStartedEvent(WaveStartedEvent waveStartedEvent)
    {
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

    private IEnumerator CountGoldCoroutine(int currentGold, int targetGold)
    {
        float duration = 1f;
        float elapsed = 0f;
        float startGold = currentGold;
        float endGold = targetGold;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smoothstep easing
            t = t * t * (3f - 2f * t);

            float currentGoldF = Mathf.Lerp(startGold, endGold, t);
            int displayGold = Mathf.RoundToInt(currentGoldF);
            _labelGold.text = $"Gold: {displayGold}";

            yield return null; // Wait one frame
        }

        // Ensure final value is exactly correct
        _labelGold.text = $"Gold: {targetGold}";
    }

    private void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
    {
        _myPlayerUnitController = myPlayerUnitSpawnedEvent.PlayerCharacter;
        _myPlayerUnitWeaponController = myPlayerUnitSpawnedEvent.PlayerCharacter.GetComponent<WeaponController>();
        SetGoldText(myPlayerUnitSpawnedEvent.Player.Gold);
        OnWeaponChange(_myPlayerUnitController);
        _myPlayerUnitController.OnWeaponChange += OnWeaponChange;
    }

    private void OnWeaponChange(UnitController unitController)
    {
        _baseAttack.IconTexture = _myPlayerUnitWeaponController.weaponData.iconTexture;
    }
}
