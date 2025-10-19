using System.Collections;
using System.Linq;
using Mirror;
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
    private AbilityCooldownElement _skillPassive;
    private AbilityCooldownElement _baseAttack;
    private AbilityCooldownElement _skillNormal1;
    private AbilityCooldownElement _skillNormal2;
    private AbilityCooldownElement _skillNormal3;
    private AbilityCooldownElement _skillUltimate;
    private CastBar _playerCastbar;


    private UnitController _myPlayerUnitController;
    private WeaponController _myPlayerUnitWeaponController;
    private SkillSystem _myPlayerUnitSkillSystem;
    private UnitActionState _myPlayerUnitActionState;
    private Coroutine _castbarCoroutine;
    private Coroutine _fadeOutCoroutine;

    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _labelWave = _uiDocument.rootVisualElement.Q<Label>(name: "labelWave");
        _labelRoundStarted = _uiDocument.rootVisualElement.Q<Label>(name: "labelRoundStarted");
        _labelGold = _uiDocument.rootVisualElement.Q<Label>(name: "labelGold");
        _labelWave.text = "";
        _labelRoundStarted.text = "";
        _labelGold.text = "Gold: 0";
        _skillPassive = _uiDocument.rootVisualElement.Q<AbilityCooldownElement>(name: "skillPassive");
        _skillPassive.CooldownRemaining = 0f;
        _skillPassive.CooldownProgress = 0f;
        _skillPassive.IconTexture = null;
        _baseAttack = _uiDocument.rootVisualElement.Q<AbilityCooldownElement>(name: "baseAttack");
        _baseAttack.CooldownRemaining = 0f;
        _baseAttack.CooldownProgress = 0f;
        _skillNormal1 = _uiDocument.rootVisualElement.Q<AbilityCooldownElement>(name: "skillNormal1");
        _skillNormal1.CooldownRemaining = 0f;
        _skillNormal1.CooldownProgress = 0f;
        _skillNormal1.IconTexture = null;
        _skillNormal2 = _uiDocument.rootVisualElement.Q<AbilityCooldownElement>(name: "skillNormal2");
        _skillNormal2.CooldownRemaining = 0f;
        _skillNormal2.CooldownProgress = 0f;
        _skillNormal2.IconTexture = null;
        _skillNormal3 = _uiDocument.rootVisualElement.Q<AbilityCooldownElement>(name: "skillNormal3");
        _skillNormal3.CooldownRemaining = 0f;
        _skillNormal3.CooldownProgress = 0f;
        _skillNormal3.IconTexture = null;
        _skillUltimate = _uiDocument.rootVisualElement.Q<AbilityCooldownElement>(name: "skillUltimate");
        _skillUltimate.CooldownRemaining = 0f;
        _skillUltimate.CooldownProgress = 0f;
        _skillUltimate.IconTexture = null;
        _playerCastbar = _uiDocument.rootVisualElement.Q<CastBar>(name: "playerCastbar");
        _playerCastbar.Progress = 0f;
        _playerCastbar.TextTime = "";
        _playerCastbar.TextName = "";
        _playerCastbar.IconTexture = null;
        HidePlayerCastbar();
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
        SyncSkillCooldown();
    }

    void SyncAttackCooldown()
    {
        if (_myPlayerUnitController == null) return;
        if (_myPlayerUnitWeaponController == null) return;

        _baseAttack.CooldownRemaining = _myPlayerUnitWeaponController.AttackCooldownRemaining;
        _baseAttack.CooldownProgress = _myPlayerUnitWeaponController.AttackCooldownProgress;
    }

    void SyncSkillCooldown()
    {
        if (_myPlayerUnitController == null) return;
        if (_myPlayerUnitSkillSystem == null) return;
        var skillPassive = _myPlayerUnitSkillSystem.GetSkill(SkillSlotType.Passive, 0);
        if (skillPassive != null)
        {
            _skillPassive.CooldownRemaining = skillPassive.CooldownRemaining;
            _skillPassive.CooldownProgress = skillPassive.CooldownProgress;
            if (skillPassive.skillData != null)
            {
                _skillPassive.IconTexture = skillPassive.skillData.iconTexture;
                _skillPassive.TooltipText = GetSkillTooltip(skillPassive.skillData);
            }
            else
            {
                ResetAbilityCooldownElement(_skillPassive);
            }
        }

        var skill1 = _myPlayerUnitSkillSystem.GetSkill(SkillSlotType.Normal, 0);
        if (skill1 != null)
        {
            _skillNormal1.CooldownRemaining = skill1.CooldownRemaining;
            _skillNormal1.CooldownProgress = skill1.CooldownProgress;
            if (skill1.skillData != null)
            {
                _skillNormal1.IconTexture = skill1.skillData.iconTexture;
                _skillNormal1.TooltipText = GetSkillTooltip(skill1.skillData);
            }
            else
            {
                ResetAbilityCooldownElement(_skillNormal1);
            }
        }

        var skill2 = _myPlayerUnitSkillSystem.GetSkill(SkillSlotType.Normal, 1);
        if (skill2 != null)
        {
            _skillNormal2.CooldownRemaining = skill2.CooldownRemaining;
            _skillNormal2.CooldownProgress = skill2.CooldownProgress;
            if (skill2.skillData != null)
            {
                _skillNormal2.IconTexture = skill2.skillData.iconTexture;
                _skillNormal2.TooltipText = GetSkillTooltip(skill2.skillData);
            }
            else
            {
                ResetAbilityCooldownElement(_skillNormal2);
            }
        }

        var skill3 = _myPlayerUnitSkillSystem.GetSkill(SkillSlotType.Normal, 2);
        if (skill3 != null)
        {
            _skillNormal3.CooldownRemaining = skill3.CooldownRemaining;
            _skillNormal3.CooldownProgress = skill3.CooldownProgress;
            if (skill3.skillData != null)
            {
                _skillNormal3.IconTexture = skill3.skillData.iconTexture;
                _skillNormal3.TooltipText = GetSkillTooltip(skill3.skillData);
            }
            else
            {
                ResetAbilityCooldownElement(_skillNormal3);
            }
        }

        var skillUltimate = _myPlayerUnitSkillSystem.GetSkill(SkillSlotType.Ultimate, 0);
        if (skillUltimate != null)
        {
            _skillUltimate.CooldownRemaining = skillUltimate.CooldownRemaining;
            _skillUltimate.CooldownProgress = skillUltimate.CooldownProgress;
            if (skillUltimate.skillData != null)
            {
                _skillUltimate.IconTexture = skillUltimate.skillData.iconTexture;
                _skillUltimate.TooltipText = GetSkillTooltip(skillUltimate.skillData);
            }
            else
            {
                ResetAbilityCooldownElement(_skillUltimate);
            }
        }

        // Reset elements if no skill
        if (skillPassive == null)
        {
            ResetAbilityCooldownElement(_skillPassive);
        }
        if (skill1 == null)
        {
            ResetAbilityCooldownElement(_skillNormal1);
        }
        if (skill2 == null)
        {
            ResetAbilityCooldownElement(_skillNormal2);
        }
        if (skill3 == null)
        {
            ResetAbilityCooldownElement(_skillNormal3);
        }
        if (skillUltimate == null)
        {
            ResetAbilityCooldownElement(_skillUltimate);
        }
    }

    void ResetAbilityCooldownElement(AbilityCooldownElement element)
    {
        element.CooldownRemaining = 0f;
        element.CooldownProgress = 0f;
        element.IconTexture = null;
        element.TooltipText = "";
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
        _myPlayerUnitSkillSystem = myPlayerUnitSpawnedEvent.PlayerCharacter.GetComponent<SkillSystem>();
        _myPlayerUnitActionState = myPlayerUnitSpawnedEvent.PlayerCharacter.GetComponent<UnitActionState>();
        OnWeaponChange(_myPlayerUnitController);
        _myPlayerUnitController.OnWeaponChange += OnWeaponChange;
        _myPlayerUnitActionState.OnActionStateChanged += HandleOnActionStateChanged;

        var localPlayerController = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).FirstOrDefault(pc => pc.isLocalPlayer);
        if (localPlayerController != null)
        {
            SetGoldText(localPlayerController.Gold);
        }
    }

    private void HandleOnActionStateChanged(UnitActionState unitActionState)
    {
        if (_myPlayerUnitActionState == null) return;
        var isCasting = unitActionState.state.type == UnitActionState.ActionType.Casting;
        var isChanneling = unitActionState.state.type == UnitActionState.ActionType.Channeling;
        var isCastingOrChanneling = isCasting || isChanneling;
        if (!isCastingOrChanneling) return;

        // Starting a new cast/channel: cancel any previous castbar coroutine and fade-out to avoid early hide
        if (_castbarCoroutine != null)
        {
            StopCoroutine(_castbarCoroutine);
            _castbarCoroutine = null;
        }
        if (_fadeOutCoroutine != null)
        {
            StopCoroutine(_fadeOutCoroutine);
            _fadeOutCoroutine = null;
        }
        _castbarCoroutine = StartCoroutine(ChangeCastbar(_myPlayerUnitActionState.state));
    }

    private IEnumerator ChangeCastbar(UnitActionState.ActionStateData actionStateData)
    {
        if (_fadeOutCoroutine != null)
        {
            StopCoroutine(_fadeOutCoroutine);
            _fadeOutCoroutine = null;
        }
        _playerCastbar.style.opacity = 1f;
        _playerCastbar.Progress = 0f;

        switch (actionStateData.type)
        {
            case UnitActionState.ActionType.Casting:
                SetSkillPlayerCastbarIcon(actionStateData.name);
                break;
            case UnitActionState.ActionType.Channeling:
                SetSkillPlayerCastbarIcon(actionStateData.name);
                break;
            default:
                _playerCastbar.IconTexture = null;
                break;
        }

        ShowPlayerCastbar();

        _playerCastbar.TextName = actionStateData.name;
        var startTime = actionStateData.startTime;
        var endTime = startTime + actionStateData.duration;
        var currentTime = NetworkTime.time;

        while (currentTime < endTime)
        {
            if (actionStateData.type == UnitActionState.ActionType.Channeling)
            {
                _playerCastbar.Progress = (float)((endTime - currentTime) / actionStateData.duration);
            }
            else
            {
                _playerCastbar.Progress = (float)((currentTime - startTime) / actionStateData.duration);
            }
            _playerCastbar.TextTime = $"{endTime - currentTime:0.0}s";
            yield return null;
            currentTime = NetworkTime.time;
        }

        _playerCastbar.Progress = 1f;
        _playerCastbar.TextTime = "0.0s";
        // Mark this castbar run as finished
        _castbarCoroutine = null;
        _fadeOutCoroutine = StartCoroutine(FadeOutPlayerCastbar(0.5f));
    }

    private void SetSkillPlayerCastbarIcon(string skillName)
    {
        var skillData = DatabaseManager.Instance.skillDatabase.GetSkillByName(skillName);
        if (skillData != null)
        {
            _playerCastbar.IconTexture = skillData.iconTexture;
        }

    }

    private IEnumerator FadeOutPlayerCastbar(float fadeDuration)
    {
        float elapsedTime = 0f;
        float startAlpha = 1f;
        float endAlpha = 0f;

        while (elapsedTime < fadeDuration)
        {
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);
            _playerCastbar.style.opacity = newAlpha;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        HidePlayerCastbar();
        _playerCastbar.style.opacity = startAlpha;
    }

    private void HidePlayerCastbar()
    {
        _playerCastbar.style.display = DisplayStyle.None;
    }

    private void ShowPlayerCastbar()
    {
        _playerCastbar.style.display = DisplayStyle.Flex;
    }

    private void OnWeaponChange(UnitController unitController)
    {
        _baseAttack.IconTexture = _myPlayerUnitWeaponController.weaponData.iconTexture;
        _baseAttack.TooltipText = GetWeaponTooltip(_myPlayerUnitWeaponController.weaponData);
    }

    private string GetSkillTooltip(SkillData skillData)
    {
        var title = $"<size=16><b>{skillData.skillName}</b></size>";
        var type = $"Type: {skillData.skillType}";
        var cooldown = $"Cooldown: {skillData.cooldown}s";
        var description = $"{skillData.description}";

        return $"{title}\n{type}\n{cooldown}\n{description}";
    }

    private string GetWeaponTooltip(WeaponData weaponData)
    {
        var title = $"<size=16><b>{weaponData.weaponName}</b></size>";
        var type = $"Type: {weaponData.weaponType}";
        var damage = $"Damage: {weaponData.attackPower}";
        var range = $"Range: {weaponData.attackRange}";

        return $"{title}\n{type}\n{damage}\n{range}";
    }
}
