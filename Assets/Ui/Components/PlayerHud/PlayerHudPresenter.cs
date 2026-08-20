using System;
using System.Threading;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using LitMotion;
using MessagePipe;
using Mirror;
using MyGame.Events;
using R3;
using UnityEngine;

namespace ShadowInfection.UI.PlayerHud
{
    internal sealed class PlayerHudPresenter
    {
        private static readonly StatType[] TrackedStats =
        {
            StatType.AttackPower,
            StatType.AbilityPower,
            StatType.AttackSpeed,
            StatType.MovementSpeed,
            StatType.DamageReduction,
            StatType.Armor,
            StatType.MagicResist,
            StatType.CritChance
        };

        private readonly PlayerHudSettings settings;
        private readonly ISubscriber<PlayerGoldChangedEvent> goldChanged;
        private readonly ISubscriber<WaveStartedEvent> waveStarted;
        private readonly ISubscriber<WaveProgressChangedEvent> waveProgress;
        private readonly ISubscriber<MyPlayerUnitSpawnedEvent> myUnitSpawned;
        private readonly ISubscriber<PlayerHudInfoMessageEvent> infoMessages;
        private readonly PlayerHudCastBarDriver castBarDriver;
        private readonly PlayerHudInfoFeedDriver infoFeedDriver;

        private PlayerHudView view;
        private R3.DisposableBag subscriptions;
        private CancellationToken destroyToken;
        private CancellationTokenSource bannerCts;
        private MotionHandle goldHandle;
        private MotionHandle bannerHandle;
        private bool enabled;
        private bool matchWidgetsVisible;
        private float matchStartTime;
        private int lastElapsedSeconds = -1;
        private int displayedGold;

        private UnitController playerUnit;
        private WeaponController weaponController;
        private SkillSystem skillSystem;
        private StatSystem statSystem;
        private SkillData lastPassiveData;
        private SkillData lastNormal1Data;
        private SkillData lastNormal2Data;
        private SkillData lastNormal3Data;
        private SkillData lastUltimateData;
        private WeaponData lastWeaponData;
        private string passiveTooltip = string.Empty;
        private string normal1Tooltip = string.Empty;
        private string normal2Tooltip = string.Empty;
        private string normal3Tooltip = string.Empty;
        private string ultimateTooltip = string.Empty;
        private string weaponTooltip = string.Empty;

        public PlayerHudPresenter(
            PlayerHudSettings settings,
            ISubscriber<PlayerGoldChangedEvent> goldChanged,
            ISubscriber<WaveStartedEvent> waveStarted,
            ISubscriber<WaveProgressChangedEvent> waveProgress,
            ISubscriber<MyPlayerUnitSpawnedEvent> myUnitSpawned,
            ISubscriber<PlayerHudInfoMessageEvent> infoMessages)
        {
            this.settings = settings;
            this.goldChanged = goldChanged;
            this.waveStarted = waveStarted;
            this.waveProgress = waveProgress;
            this.myUnitSpawned = myUnitSpawned;
            this.infoMessages = infoMessages;
            castBarDriver = new PlayerHudCastBarDriver(settings, ResolveSkillIcon);
            infoFeedDriver = new PlayerHudInfoFeedDriver(settings);
        }

        public void Bind(PlayerHudView nextView, Color successColor, CancellationToken token)
        {
            Unbind();
            view = nextView;
            destroyToken = token;
            matchStartTime = Time.time;
            lastElapsedSeconds = -1;
            displayedGold = 0;
            matchWidgetsVisible = false;
            view.Reset();
            TickMatchWidgetsVisibility();
            castBarDriver.Bind(view, successColor, destroyToken);
            infoFeedDriver.Bind(view);

            subscriptions.Add(goldChanged.Subscribe(OnGoldChanged));
            subscriptions.Add(waveStarted.Subscribe(OnWaveStarted));
            subscriptions.Add(waveProgress.Subscribe(OnWaveProgressChanged));
            subscriptions.Add(myUnitSpawned.Subscribe(OnMyPlayerUnitSpawned));
            subscriptions.Add(infoMessages.Subscribe(OnInfoMessage));
            subscriptions.Add(
                Observable.EveryUpdate(UnityFrameProvider.Update, destroyToken)
                    .Subscribe(_ =>
                    {
                        if (view == null)
                            return;

                        infoFeedDriver.Tick();
                        if (!enabled)
                            return;

                        TickMatchWidgetsVisibility();
                        TickCooldowns();
                        if (matchWidgetsVisible)
                            TickMatchTimer(Time.time);
                    }));
        }

        public void Unbind()
        {
            enabled = false;
            CancelBanner();
            goldHandle.TryCancel();
            bannerHandle.TryCancel();
            UnbindPlayerUnit();
            castBarDriver.Unbind();
            infoFeedDriver.Unbind();
            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            view = null;
        }

        public void SetEnabled(bool value)
        {
            enabled = value;
        }

        private void TickMatchWidgetsVisibility()
        {
            var visible = !IsInRoomLobby();
            if (visible == matchWidgetsVisible)
                return;

            SetMatchWidgetsVisible(visible);
        }

        private void SetMatchWidgetsVisible(bool visible)
        {
            matchWidgetsVisible = visible;
            view?.SetMatchWidgetsVisible(visible);
        }

        private static bool IsInRoomLobby()
        {
            return NetworkManager.singleton is NetworkRoomManager room
                && !string.IsNullOrWhiteSpace(room.RoomScene)
                && Utils.IsSceneActive(room.RoomScene);
        }

        private void TickMatchTimer(float time)
        {
            var elapsed = Mathf.Max(0, Mathf.FloorToInt(time - matchStartTime));
            if (elapsed == lastElapsedSeconds)
                return;

            lastElapsedSeconds = elapsed;
            view.SetMatchTimer(elapsed);
        }

        private void TickCooldowns()
        {
            if (playerUnit == null)
                return;

            if (weaponController != null && weaponController.weaponData != null)
            {
                if (!ReferenceEquals(lastWeaponData, weaponController.weaponData))
                {
                    lastWeaponData = weaponController.weaponData;
                    weaponTooltip = BuildWeaponTooltip(lastWeaponData);
                }

                view.SetAbilitySlot(PlayerHudAbilitySlot.BaseAttack, new AbilitySlotVm(
                    true,
                    weaponController.AttackCooldownRemaining,
                    weaponController.AttackCooldownProgress,
                    false,
                    0f,
                    lastWeaponData.iconTexture,
                    weaponTooltip));
            }
            else
            {
                view.SetAbilitySlot(PlayerHudAbilitySlot.BaseAttack, AbilitySlotVm.Empty);
            }

            if (skillSystem == null)
                return;

            view.SetAbilitySlot(PlayerHudAbilitySlot.Passive, BuildSkillVm(
                skillSystem.GetSkill(SkillSlotType.Passive, 0),
                ref lastPassiveData,
                ref passiveTooltip));
            view.SetAbilitySlot(PlayerHudAbilitySlot.Normal1, BuildSkillVm(
                skillSystem.GetSkill(SkillSlotType.Normal, 0),
                ref lastNormal1Data,
                ref normal1Tooltip));
            view.SetAbilitySlot(PlayerHudAbilitySlot.Normal2, BuildSkillVm(
                skillSystem.GetSkill(SkillSlotType.Normal, 1),
                ref lastNormal2Data,
                ref normal2Tooltip));
            view.SetAbilitySlot(PlayerHudAbilitySlot.Normal3, BuildSkillVm(
                skillSystem.GetSkill(SkillSlotType.Normal, 2),
                ref lastNormal3Data,
                ref normal3Tooltip));
            view.SetAbilitySlot(PlayerHudAbilitySlot.Ultimate, BuildSkillVm(
                skillSystem.GetSkill(SkillSlotType.Ultimate, 0),
                ref lastUltimateData,
                ref ultimateTooltip));
        }

        private static AbilitySlotVm BuildSkillVm(
            NetworkedSkillInstance skill,
            ref SkillData cachedData,
            ref string cachedTooltip)
        {
            if (skill == null)
            {
                cachedData = null;
                cachedTooltip = string.Empty;
                return AbilitySlotVm.Empty;
            }

            if (!ReferenceEquals(cachedData, skill.skillData))
            {
                cachedData = skill.skillData;
                cachedTooltip = cachedData != null ? BuildSkillTooltip(cachedData) : string.Empty;
            }

            if (cachedData == null)
                return AbilitySlotVm.Empty;

            return new AbilitySlotVm(
                true,
                skill.CooldownRemaining,
                skill.CooldownProgress,
                skill.IsRecastWindowOpen,
                skill.RecastWindowRemaining,
                cachedData.iconTexture,
                cachedTooltip);
        }

        private void OnInfoMessage(PlayerHudInfoMessageEvent message)
        {
            if (view == null)
                return;

            infoFeedDriver.Enqueue(message);
        }

        private void OnGoldChanged(PlayerGoldChangedEvent playerGoldChangedEvent)
        {
            if (view == null || !playerGoldChangedEvent.Player.isLocalPlayer)
                return;

            TweenGold(playerGoldChangedEvent.NewGoldAmount);
        }

        private void TweenGold(int targetGold)
        {
            var from = displayedGold;
            goldHandle.TryCancel();

            if (view == null)
                return;

            if (Mathf.Approximately(settings.GoldTweenSeconds, 0f) || from == targetGold)
            {
                displayedGold = targetGold;
                view.SetGold(targetGold);
                return;
            }

            goldHandle = LMotion.Create((float)from, (float)targetGold, settings.GoldTweenSeconds)
                .WithEase(Ease.InOutCubic)
                .Bind(this, static (value, presenter) =>
                {
                    presenter.displayedGold = Mathf.RoundToInt(value);
                    presenter.view?.SetGold(presenter.displayedGold);
                });
        }

        private void OnWaveStarted(WaveStartedEvent waveStartedEvent)
        {
            if (view == null)
                return;

            view.SetRoundProgress(waveStartedEvent.WaveNumber, 0f);
            view.SetRoundStartedText(waveStartedEvent.WaveNumber);
            PlayBanner().Forget();
        }

        private void OnWaveProgressChanged(WaveProgressChangedEvent waveProgressChangedEvent)
        {
            view?.SetRoundProgress(waveProgressChangedEvent.WaveNumber, waveProgressChangedEvent.PercentKilled);
        }

        private async UniTaskVoid PlayBanner()
        {
            CancelBanner();
            bannerCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            var ct = bannerCts.Token;
            var hud = view;
            if (hud == null)
                return;

            try
            {
                hud.SetRoundStartedOpacity(0f);
                bannerHandle = LMotion.Create(0f, 1f, settings.BannerFadeInSeconds)
                    .Bind(hud, static (value, target) => target.SetRoundStartedOpacity(value));
                await bannerHandle.ToUniTask(ct);

                var hold = settings.BannerHoldSeconds - settings.BannerFadeInSeconds;
                if (hold > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(hold), cancellationToken: ct);

                bannerHandle = LMotion.Create(1f, 0f, settings.BannerFadeOutSeconds)
                    .Bind(hud, static (value, target) => target.SetRoundStartedOpacity(value));
                await bannerHandle.ToUniTask(ct);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
        {
            UnbindPlayerUnit();
            UiCursorRefresh.SetGameplayPointerEnabled(true);

            playerUnit = myPlayerUnitSpawnedEvent.PlayerCharacter;
            if (playerUnit == null || view == null)
                return;

            weaponController = playerUnit.GetComponent<WeaponController>();
            skillSystem = playerUnit.GetComponent<SkillSystem>();
            var actionState = playerUnit.GetComponent<UnitActionState>();
            castBarDriver.SetActionState(actionState);

            playerUnit.OnWeaponChange += OnWeaponChange;
            playerUnit.OnHealthChange += HandleHealthChanged;
            playerUnit.OnShieldChange += HandleShieldChanged;
            playerUnit.OnActionInterrupted += HandleActionInterrupted;
            OnWeaponChange(playerUnit);

            view.ShowPlayerVitals();
            RefreshVitals();

            statSystem = playerUnit.unitMediator != null ? playerUnit.unitMediator.Stats : null;
            if (statSystem != null)
            {
                statSystem.OnStatChanged += HandleStatChanged;
                RefreshAllStats();
                view.ShowPlayerStats();
            }

            var localPlayer = FindLocalPlayer();
            if (localPlayer != null)
                TweenGold(localPlayer.Gold);
        }

        private void UnbindPlayerUnit()
        {
            UiCursorRefresh.SetGameplayPointerEnabled(false);

            if (playerUnit != null)
            {
                playerUnit.OnWeaponChange -= OnWeaponChange;
                playerUnit.OnHealthChange -= HandleHealthChanged;
                playerUnit.OnShieldChange -= HandleShieldChanged;
                playerUnit.OnActionInterrupted -= HandleActionInterrupted;
            }

            if (statSystem != null)
                statSystem.OnStatChanged -= HandleStatChanged;

            castBarDriver.SetActionState(null);
            playerUnit = null;
            weaponController = null;
            skillSystem = null;
            statSystem = null;
            lastPassiveData = null;
            lastNormal1Data = null;
            lastNormal2Data = null;
            lastNormal3Data = null;
            lastUltimateData = null;
            lastWeaponData = null;

            if (view == null)
                return;

            view.SetAbilitySlot(PlayerHudAbilitySlot.Passive, AbilitySlotVm.Empty);
            view.SetAbilitySlot(PlayerHudAbilitySlot.BaseAttack, AbilitySlotVm.Empty);
            view.SetAbilitySlot(PlayerHudAbilitySlot.Normal1, AbilitySlotVm.Empty);
            view.SetAbilitySlot(PlayerHudAbilitySlot.Normal2, AbilitySlotVm.Empty);
            view.SetAbilitySlot(PlayerHudAbilitySlot.Normal3, AbilitySlotVm.Empty);
            view.SetAbilitySlot(PlayerHudAbilitySlot.Ultimate, AbilitySlotVm.Empty);
            view.HidePlayerVitals();
            view.HidePlayerStats();
        }

        private void RefreshVitals()
        {
            if (playerUnit == null)
            {
                view?.HidePlayerVitals();
                return;
            }

            view.ShowPlayerVitals();
            view.SetHealth(playerUnit.health, playerUnit.maxHealth);
            view.SetShield(playerUnit.shield, playerUnit.maxShield);
        }

        private void HandleHealthChanged((int current, int max) health)
        {
            view?.SetHealth(health.current, health.max);
        }

        private void HandleShieldChanged((int current, int max) shield)
        {
            view?.SetShield(shield.current, shield.max);
        }

        private void HandleActionInterrupted(
            (UnitController unitController, UnitActionState.ActionStateData interruptedAction) data)
        {
            castBarDriver.HandleInterrupted(data.unitController, playerUnit);
        }

        private void OnWeaponChange(UnitController unitController)
        {
            lastWeaponData = null;
        }

        private void HandleStatChanged(StatType statType)
        {
            if (statSystem == null || view == null)
                return;

            view.SetStat(statType, statSystem.GetStat(statType));
        }

        private void RefreshAllStats()
        {
            if (statSystem == null || view == null)
                return;

            for (var i = 0; i < TrackedStats.Length; i++)
                view.SetStat(TrackedStats[i], statSystem.GetStat(TrackedStats[i]));
        }

        private void CancelBanner()
        {
            bannerHandle.TryCancel();
            if (bannerCts == null)
                return;

            bannerCts.Cancel();
            bannerCts.Dispose();
            bannerCts = null;
        }

        private static Texture2D ResolveSkillIcon(string skillName)
        {
            var database = DatabaseManager.Instance;
            if (database == null || database.skillDatabase == null)
                return null;

            return database.skillDatabase.GetSkillByName(skillName)?.iconTexture;
        }

        private static PlayerController FindLocalPlayer()
        {
            var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i] != null && players[i].isLocalPlayer)
                    return players[i];
            }

            return null;
        }

        private static string BuildSkillTooltip(SkillData skillData)
        {
            using var sb = ZString.CreateStringBuilder();
            sb.Append("<size=20><b>");
            sb.Append(skillData.skillName);
            sb.Append("</b></size>\n<size=16><color=#cccccc>Type: ");
            sb.Append(skillData.skillType.ToString());
            sb.Append("</color></size>\n<size=16><color=#cccccc>Required Weapon: ");
            sb.Append(skillData.GetRequiredWeaponLabel());
            sb.Append("</color></size>");
            if (skillData.cooldown != 0)
            {
                sb.Append("\n<size=16><color=#cccccc>Cooldown: ");
                sb.Append(skillData.cooldown);
                sb.Append("s</color></size>");
            }

            sb.Append("\n\n<size=16>");
            sb.Append(skillData.description);
            sb.Append("</size>");
            return sb.ToString();
        }

        private static string BuildWeaponTooltip(WeaponData weaponData)
        {
            using var sb = ZString.CreateStringBuilder();
            sb.Append("<size=20><b>");
            sb.Append(weaponData.weaponName);
            sb.Append("</b></size>\n<color=#cccccc><size=16>Type: ");
            sb.Append(weaponData.weaponType.ToString());
            sb.Append("</size>\n<size=16>Damage: +");
            sb.Append(weaponData.attackPower);
            sb.Append("</size>\n<size=16>Range: ");
            sb.Append(weaponData.attackRange);
            sb.Append("</size></color>");
            return sb.ToString();
        }
    }
}
