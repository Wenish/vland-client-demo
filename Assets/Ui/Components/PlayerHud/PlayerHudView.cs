using System;
using System.Collections.Generic;
using Cysharp.Text;
using ShadowInfection.UI.Nameplates;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.PlayerHud
{
    internal enum PlayerHudAbilitySlot
    {
        Passive,
        BaseAttack,
        Normal1,
        Normal2,
        Normal3,
        Ultimate
    }

    internal readonly struct AbilitySlotVm
    {
        public readonly bool HasSkill;
        public readonly float CooldownRemaining;
        public readonly float CooldownProgress;
        public readonly bool IsRecastAvailable;
        public readonly float RecastRemaining;
        public readonly Texture2D Icon;
        public readonly string Tooltip;
        public readonly string ActivationKey;

        public AbilitySlotVm(
            bool hasSkill,
            float cooldownRemaining,
            float cooldownProgress,
            bool isRecastAvailable,
            float recastRemaining,
            Texture2D icon,
            string tooltip,
            string activationKey = "")
        {
            HasSkill = hasSkill;
            CooldownRemaining = cooldownRemaining;
            CooldownProgress = cooldownProgress;
            IsRecastAvailable = isRecastAvailable;
            RecastRemaining = recastRemaining;
            Icon = icon;
            Tooltip = tooltip;
            ActivationKey = activationKey ?? string.Empty;
        }

        public static AbilitySlotVm Empty =>
            new AbilitySlotVm(false, 0f, 0f, false, 0f, null, string.Empty);
    }

    internal readonly struct PlayerHudInfoLineVm
    {
        public readonly string Text;
        public readonly float Opacity;
        public readonly bool IsError;

        public PlayerHudInfoLineVm(string text, float opacity, bool isError)
        {
            Text = text ?? string.Empty;
            Opacity = opacity;
            IsError = isError;
        }
    }

    internal sealed class PlayerHudView
    {
        private readonly Label labelWave;
        private readonly Label labelRoundStarted;
        private readonly Label labelGold;
        private readonly Label labelCompletionPercent;
        private readonly HudStatItem statScore;
        private readonly HudStatItem statKills;
        private readonly HudStatItem statTimer;
        private readonly VisualElement roundInfos;
        private readonly VisualElement roundBox;
        private readonly VisualElement goldContainer;
        private readonly VisualElement roundCompletionFill;
        private readonly VisualElement playerVitalsContainer;
        private readonly VisualElement playerHealthContainer;
        private readonly VisualElement playerHealthFill;
        private readonly Label labelPlayerHealthValue;
        private readonly VisualElement playerShieldContainer;
        private readonly VisualElement playerShieldFill;
        private readonly Label labelPlayerShieldValue;
        private readonly AbilityCooldownElement skillPassive;
        private readonly AbilityCooldownElement baseAttack;
        private readonly AbilityCooldownElement skillNormal1;
        private readonly AbilityCooldownElement skillNormal2;
        private readonly AbilityCooldownElement skillNormal3;
        private readonly AbilityCooldownElement skillUltimate;
        private readonly Button loadoutButton;
        private readonly CastBar playerCastbar;
        private readonly VisualElement playerStatsContainer;
        private readonly Label labelStatAttackPower;
        private readonly Label labelStatAbilityPower;
        private readonly Label labelStatAttackSpeed;
        private readonly Label labelStatMovementSpeed;
        private readonly Label labelStatDamageReduction;
        private readonly Label labelStatArmor;
        private readonly Label labelStatMagicResist;
        private readonly Label labelStatCritChance;
        private readonly VisualElement infoFeed;
        private readonly List<Label> infoLabels = new();
        private readonly VisualElement characterHudRoot;
        private readonly TargetFrameView targetFrame;

        public event Action LoadoutButtonClicked;

        public TargetFrameView TargetFrame => targetFrame;

        public PlayerHudView(VisualElement root)
        {
            root.pickingMode = PickingMode.Ignore;
            UiGameplayInputGuard.Apply(root);

            labelWave = root.Q<Label>("labelWave");
            labelRoundStarted = root.Q<Label>("labelRoundStarted");
            labelGold = root.Q<Label>("labelGold");
            labelCompletionPercent = root.Q<Label>("labelCompletionPercent");
            statScore = root.Q<HudStatItem>("statScore");
            statKills = root.Q<HudStatItem>("statKills");
            statTimer = root.Q<HudStatItem>("statTimer");
            roundInfos = root.Q<VisualElement>("roundInfos");
            roundBox = root.Q<VisualElement>("roundBox");
            goldContainer = root.Q<VisualElement>("goldContainer");
            roundCompletionFill = root.Q<VisualElement>("roundCompletionFill");
            characterHudRoot = root.Q<VisualElement>("characterHudRoot");
            playerVitalsContainer = root.Q<VisualElement>("playerVitalsContainer");
            playerHealthContainer = root.Q<VisualElement>("playerHealthContainer");
            playerHealthFill = root.Q<VisualElement>("playerHealthFill");
            labelPlayerHealthValue = root.Q<Label>("labelPlayerHealthValue");
            playerShieldContainer = root.Q<VisualElement>("playerShieldContainer");
            playerShieldFill = root.Q<VisualElement>("playerShieldFill");
            labelPlayerShieldValue = root.Q<Label>("labelPlayerShieldValue");
            skillPassive = root.Q<AbilityCooldownElement>("skillPassive");
            baseAttack = root.Q<AbilityCooldownElement>("baseAttack");
            skillNormal1 = root.Q<AbilityCooldownElement>("skillNormal1");
            skillNormal2 = root.Q<AbilityCooldownElement>("skillNormal2");
            skillNormal3 = root.Q<AbilityCooldownElement>("skillNormal3");
            skillUltimate = root.Q<AbilityCooldownElement>("skillUltimate");
            loadoutButton = root.Q<OrnateButton>("loadoutButton") ?? root.Q<Button>("loadoutButton");
            playerCastbar = root.Q<CastBar>("playerCastbar");
            playerStatsContainer = root.Q<VisualElement>("playerStatsContainer");
            labelStatAttackPower = root.Q<Label>("labelStatAttackPower");
            labelStatAbilityPower = root.Q<Label>("labelStatAbilityPower");
            labelStatAttackSpeed = root.Q<Label>("labelStatAttackSpeed");
            labelStatMovementSpeed = root.Q<Label>("labelStatMovementSpeed");
            labelStatDamageReduction = root.Q<Label>("labelStatDamageReduction");
            labelStatArmor = root.Q<Label>("labelStatArmor");
            labelStatMagicResist = root.Q<Label>("labelStatMagicResist");
            labelStatCritChance = root.Q<Label>("labelStatCritChance");
            infoFeed = root.Q<VisualElement>("playerInfoFeed");
            targetFrame = new TargetFrameView(root);

            SetPickingIgnoreRecursive(root.Q<VisualElement>("roundInfos"));
            SetPickingIgnoreRecursive(root.Q<Label>("labelRoundStarted")?.parent);
            SetPickingIgnoreRecursive(root.Q<CastBar>("playerCastbar"));
            SetPickingIgnoreRecursive(root.Q(className: "si-hud-bottom"));
            SetPickingIgnoreRecursive(infoFeed);

            if (loadoutButton != null)
            {
                loadoutButton.pickingMode = PickingMode.Position;
                UiPointerState.RegisterBlockingElement(loadoutButton);
                loadoutButton.clicked += () => LoadoutButtonClicked?.Invoke();
            }

            Reset();
            SetCharacterHudVisible(false);
        }

        public void SetCharacterHudVisible(bool visible)
        {
            if (characterHudRoot != null)
                characterHudRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (!visible)
                HideCastBar();
        }

        public void SetLoadoutButtonVisible(bool visible)
        {
            if (loadoutButton == null)
                return;

            loadoutButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Reset()
        {
            if (labelWave != null)
                labelWave.text = string.Empty;
            if (labelRoundStarted != null)
            {
                labelRoundStarted.text = string.Empty;
                labelRoundStarted.style.opacity = 0f;
            }

            SetGold(0);
            SetPersonalScore(0);
            SetPersonalKills(0);
            SetMatchTimer(0);
            SetRoundProgress(0, 0f);
            SetZombieGameInfoVisible(false);
            SetMatchWidgetsVisible(false);
            HidePlayerVitals();
            HidePlayerStats();
            SetCharacterHudVisible(false);
            SetAbilitySlot(PlayerHudAbilitySlot.Passive, AbilitySlotVm.Empty);
            SetAbilitySlot(PlayerHudAbilitySlot.BaseAttack, AbilitySlotVm.Empty);
            SetAbilitySlot(PlayerHudAbilitySlot.Normal1, AbilitySlotVm.Empty);
            SetAbilitySlot(PlayerHudAbilitySlot.Normal2, AbilitySlotVm.Empty);
            SetAbilitySlot(PlayerHudAbilitySlot.Normal3, AbilitySlotVm.Empty);
            SetAbilitySlot(PlayerHudAbilitySlot.Ultimate, AbilitySlotVm.Empty);
            ResetCastBar();
            HideCastBar();
            targetFrame?.Hide();
            ClearInfoLines();
            if (playerCastbar != null)
                playerCastbar.style.opacity = 1f;
        }

        public void SetGold(int gold)
        {
            if (labelGold == null)
                return;

            labelGold.text = ZString.Format("{0}", gold);
        }

        public void SetMatchWidgetsVisible(bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (roundInfos != null)
                roundInfos.style.display = display;
            if (goldContainer != null)
                goldContainer.style.display = display;
        }

        public void SetZombieGameInfoVisible(bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (statScore != null)
                statScore.style.display = display;
            if (statKills != null)
                statKills.style.display = display;
            if (roundBox != null)
                roundBox.style.display = display;
        }

        public void SetPersonalScore(int score)
        {
            if (statScore != null)
                statScore.Value = ZString.Format("{0}", Mathf.Max(0, score));
        }

        public void SetPersonalKills(int kills)
        {
            if (statKills != null)
                statKills.Value = ZString.Format("{0}", Mathf.Max(0, kills));
        }

        public void SetMatchTimer(int elapsedSeconds)
        {
            if (statTimer == null)
                return;

            var minutes = elapsedSeconds / 60;
            var seconds = elapsedSeconds % 60;
            statTimer.Value = ZString.Format("{0:00}:{1:00}", minutes, seconds);
        }

        public void SetRoundProgress(int waveNumber, float percentKilled)
        {
            if (labelWave != null)
                labelWave.text = waveNumber > 0 ? ZString.Format("Round {0}", waveNumber) : "Round";

            if (labelCompletionPercent != null)
                labelCompletionPercent.text = ZString.Format("{0:0}%", percentKilled);

            if (roundCompletionFill != null)
                roundCompletionFill.style.width = Length.Percent(Mathf.Clamp(percentKilled, 0f, 100f));
        }

        public void SetRoundStartedText(int waveNumber)
        {
            if (labelRoundStarted == null)
                return;

            labelRoundStarted.text = ZString.Format("Round\n{0}", waveNumber);
        }

        public void SetRoundStartedOpacity(float opacity)
        {
            if (labelRoundStarted == null)
                return;

            labelRoundStarted.style.opacity = opacity;
        }

        public void SetHealth(int current, int max)
        {
            if (labelPlayerHealthValue != null)
                labelPlayerHealthValue.text = ZString.Format("{0} / {1}", Mathf.Max(0, current), Mathf.Max(0, max));

            SetBarFill(playerHealthFill, current, max);
            if (playerHealthContainer != null)
                playerHealthContainer.style.display = max > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetShield(int current, int max)
        {
            if (labelPlayerShieldValue != null)
                labelPlayerShieldValue.text = ZString.Format("{0} / {1}", Mathf.Max(0, current), Mathf.Max(0, max));

            SetBarFill(playerShieldFill, current, max);
            if (playerShieldContainer != null)
                playerShieldContainer.style.display = max > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ShowPlayerVitals()
        {
            if (playerVitalsContainer != null)
                playerVitalsContainer.style.display = DisplayStyle.Flex;
        }

        public void HidePlayerVitals()
        {
            if (playerVitalsContainer != null)
                playerVitalsContainer.style.display = DisplayStyle.None;

            SetHealth(0, 0);
            SetShield(0, 0);
        }

        public void ShowPlayerStats()
        {
            if (playerStatsContainer != null)
                playerStatsContainer.style.display = DisplayStyle.Flex;
        }

        public void HidePlayerStats()
        {
            if (playerStatsContainer != null)
                playerStatsContainer.style.display = DisplayStyle.None;
        }

        public void SetStat(StatType statType, float value)
        {
            switch (statType)
            {
                case StatType.AttackPower:
                    if (labelStatAttackPower != null)
                        labelStatAttackPower.text = ZString.Format("ATK: {0:0}", value);
                    break;
                case StatType.AbilityPower:
                    if (labelStatAbilityPower != null)
                        labelStatAbilityPower.text = ZString.Format("AP: {0:0}", value);
                    break;
                case StatType.AttackSpeed:
                    if (labelStatAttackSpeed != null)
                        labelStatAttackSpeed.text = ZString.Format("AS: {0:0.00}x", value);
                    break;
                case StatType.MovementSpeed:
                    if (labelStatMovementSpeed != null)
                        labelStatMovementSpeed.text = ZString.Format("SPD: {0:0.0}", value);
                    break;
                case StatType.DamageReduction:
                    if (labelStatDamageReduction != null)
                        labelStatDamageReduction.text = ZString.Format("DR: {0:0}%", value);
                    break;
                case StatType.Armor:
                    if (labelStatArmor != null)
                        labelStatArmor.text = ZString.Format("ARM: {0:0}", value);
                    break;
                case StatType.MagicResist:
                    if (labelStatMagicResist != null)
                        labelStatMagicResist.text = ZString.Format("MR: {0:0}", value);
                    break;
                case StatType.CritChance:
                    if (labelStatCritChance != null)
                        labelStatCritChance.text = ZString.Format("CRIT: {0:0}%", value);
                    break;
            }
        }

        public void SetAbilitySlot(PlayerHudAbilitySlot slot, in AbilitySlotVm vm)
        {
            var element = GetAbilityElement(slot);
            if (element == null)
                return;

            if (!vm.HasSkill)
            {
                ResetAbility(element);
                return;
            }

            element.CooldownRemaining = vm.CooldownRemaining;
            element.CooldownProgress = vm.CooldownProgress;
            element.IsRecastAvailable = vm.IsRecastAvailable;
            element.RecastRemaining = vm.RecastRemaining;
            element.IconTexture = vm.Icon;
            element.TooltipText = vm.Tooltip ?? string.Empty;
            element.ActivationKey = vm.ActivationKey ?? string.Empty;
        }

        public void ShowCastBar()
        {
            if (playerCastbar != null)
                playerCastbar.style.display = DisplayStyle.Flex;
        }

        public void HideCastBar()
        {
            if (playerCastbar != null)
                playerCastbar.style.display = DisplayStyle.None;
        }

        public void SetCastBarOpacity(float opacity)
        {
            if (playerCastbar != null)
                playerCastbar.style.opacity = opacity;
        }

        public void SetCastBarProgress(float progress)
        {
            if (playerCastbar != null)
                playerCastbar.Progress = progress;
        }

        public void SetCastBarTime(string text)
        {
            if (playerCastbar != null)
                playerCastbar.TextTime = text ?? string.Empty;
        }

        public void SetCastBarName(string text)
        {
            if (playerCastbar != null)
                playerCastbar.TextName = text ?? string.Empty;
        }

        public void SetCastBarIcon(Texture2D icon)
        {
            if (playerCastbar != null)
                playerCastbar.IconTexture = icon;
        }

        public void SetCastBarFeedback(Color color, bool visible)
        {
            if (playerCastbar == null)
                return;

            playerCastbar.SetFeedbackColor(color);
            playerCastbar.ShowFeedback(visible);
        }

        public void ResetCastBar()
        {
            SetCastBarTime(string.Empty);
            SetCastBarName(string.Empty);
            SetCastBarFeedback(Color.clear, false);
        }

        public void ClearInfoLines()
        {
            SetInfoLines(null);
        }

        public void SetInfoLines(IReadOnlyList<PlayerHudInfoLineVm> lines)
        {
            if (infoFeed == null)
                return;

            var count = lines != null ? lines.Count : 0;
            EnsureInfoLabelCount(count);

            for (var i = 0; i < infoLabels.Count; i++)
            {
                var label = infoLabels[i];
                if (i < count)
                {
                    label.text = lines[i].Text;
                    label.style.opacity = lines[i].Opacity;
                    label.EnableInClassList("si-player-info-feed__line--error", lines[i].IsError);
                    label.style.display = DisplayStyle.Flex;
                }
                else
                {
                    label.text = string.Empty;
                    label.EnableInClassList("si-player-info-feed__line--error", false);
                    label.style.display = DisplayStyle.None;
                }
            }
        }

        private void EnsureInfoLabelCount(int count)
        {
            while (infoLabels.Count < count)
            {
                var label = new Label();
                label.AddToClassList("si-player-info-feed__line");
                label.pickingMode = PickingMode.Ignore;
                infoFeed.Add(label);
                infoLabels.Add(label);
            }
        }

        private AbilityCooldownElement GetAbilityElement(PlayerHudAbilitySlot slot)
        {
            return slot switch
            {
                PlayerHudAbilitySlot.Passive => skillPassive,
                PlayerHudAbilitySlot.BaseAttack => baseAttack,
                PlayerHudAbilitySlot.Normal1 => skillNormal1,
                PlayerHudAbilitySlot.Normal2 => skillNormal2,
                PlayerHudAbilitySlot.Normal3 => skillNormal3,
                PlayerHudAbilitySlot.Ultimate => skillUltimate,
                _ => null
            };
        }

        private static void ResetAbility(AbilityCooldownElement element)
        {
            element.CooldownRemaining = 0f;
            element.CooldownProgress = 0f;
            element.IsRecastAvailable = false;
            element.RecastRemaining = 0f;
            element.IconTexture = null;
            element.TooltipText = string.Empty;
            element.ActivationKey = string.Empty;
        }

        private static void SetBarFill(VisualElement fillElement, int current, int max)
        {
            if (fillElement == null)
                return;

            var percent = max <= 0 ? 0f : Mathf.Clamp01((float)current / max) * 100f;
            fillElement.style.width = Length.Percent(percent);
        }

        private static void SetPickingIgnoreRecursive(VisualElement element)
        {
            if (element == null)
                return;

            if (element is AbilityCooldownElement or OrnateButton or BuffIconElement)
                return;

            element.pickingMode = PickingMode.Ignore;
            foreach (var child in element.Children())
                SetPickingIgnoreRecursive(child);
        }
    }
}
