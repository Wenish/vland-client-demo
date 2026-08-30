using System.Collections.Generic;
using System.Threading;
using MessagePipe;
using ShadowInfection;
using ShadowInfection.DI;
using ShadowInfection.Targeting;
using ShadowInfection.UI.Nameplates;
using UnityEngine;

namespace ShadowInfection.UI.PlayerHud
{
    internal sealed class TargetFrameDriver
    {
        private static readonly Color LocalPlayerHealthColor = new Color(0f, 0.6509804f, 0.24313727f, 1f);
        private static readonly UiBuffData[] NoBuffs = System.Array.Empty<UiBuffData>();

        private readonly IPlayerTarget playerTarget;
        private readonly ITeamColorService teamColors;
        private readonly IGameDatabases databases;
        private readonly ISubscriber<PlayerTargetChangedEvent> targetChanged;
        private readonly TargetFrameCastBarDriver castBarDriver;
        private readonly List<UiBuffData> groupedBuffs = new();
        private readonly List<UiBuffData> groupedDebuffs = new();

        private TargetFrameView view;
        private UnitController localPlayer;
        private UnitController unit;
        private UnitNameplateBuffDriver buffDriver;
        private CancellationToken destroyToken;
        private System.IDisposable targetChangedSub;

        public TargetFrameDriver(
            IPlayerTarget playerTarget,
            ITeamColorService teamColors,
            IGameDatabases databases,
            PlayerHudSettings settings,
            ISubscriber<PlayerTargetChangedEvent> targetChanged)
        {
            this.playerTarget = playerTarget;
            this.teamColors = teamColors;
            this.databases = databases;
            this.targetChanged = targetChanged;
            castBarDriver = new TargetFrameCastBarDriver(settings, ResolveSkillIcon);
        }

        public void Bind(TargetFrameView nextView, CancellationToken token)
        {
            Unbind();
            view = nextView;
            destroyToken = token;
            castBarDriver.Bind(view, destroyToken);
            targetChangedSub = targetChanged.Subscribe(OnTargetChanged);
            BindUnit(playerTarget != null ? playerTarget.Current : null);
        }

        public void SetLocalPlayer(UnitController player)
        {
            localPlayer = player;
            if (unit != null)
                RefreshVitals();
        }

        public void Tick()
        {
            if (view == null || unit == null)
                return;

            if (buffDriver != null && buffDriver.Tick())
                RefreshBuffs();
        }

        public void Unbind()
        {
            targetChangedSub?.Dispose();
            targetChangedSub = null;
            UnbindUnit();
            castBarDriver.Unbind();
            view = null;
        }

        private void OnTargetChanged(PlayerTargetChangedEvent evt)
        {
            BindUnit(evt.Current);
        }

        private void BindUnit(UnitController next)
        {
            if (view == null)
                return;

            if (next != null && next.Equals(null))
                next = null;

            if (unit == next)
            {
                if (unit == null)
                    view.Hide();
                return;
            }

            UnbindUnit();
            unit = next;
            if (unit == null)
            {
                view.Hide();
                return;
            }

            unit.OnHealthChange += HandleHealthChange;
            unit.OnShieldChange += HandleShieldChange;
            unit.OnNameChanged += HandleNameChanged;
            unit.OnTeamChanged += HandleTeamChanged;
            unit.OnActionInterrupted += HandleInterrupted;

            buffDriver = new UnitNameplateBuffDriver(databases, RefreshBuffs);
            buffDriver.Bind(unit.GetComponent<UnitNetworkBuffs>());

            castBarDriver.SetActionState(unit.GetComponent<UnitActionState>());

            view.Show();
            view.SetName(unit.unitName);
            RefreshVitals();
            RefreshBuffs();
        }

        private void UnbindUnit()
        {
            castBarDriver.SetActionState(null);
            buffDriver?.Unbind();
            buffDriver = null;

            if (unit != null && !unit.Equals(null))
            {
                unit.OnHealthChange -= HandleHealthChange;
                unit.OnShieldChange -= HandleShieldChange;
                unit.OnNameChanged -= HandleNameChanged;
                unit.OnTeamChanged -= HandleTeamChanged;
                unit.OnActionInterrupted -= HandleInterrupted;
            }

            unit = null;
        }

        private void HandleHealthChange((int current, int max) _)
        {
            RefreshVitals();
        }

        private void HandleShieldChange((int current, int max) _)
        {
            RefreshVitals();
        }

        private void HandleNameChanged(UnitController _)
        {
            if (view != null && unit != null)
                view.SetName(unit.unitName);
        }

        private void HandleTeamChanged(UnitController _)
        {
            RefreshVitals();
        }

        private void HandleInterrupted(
            (UnitController unitController, UnitActionState.ActionStateData interruptedAction) data)
        {
            castBarDriver.HandleInterrupted(data.unitController, unit);
        }

        private void RefreshVitals()
        {
            if (view == null || unit == null)
                return;

            view.SetHealth(unit.health, unit.maxHealth, ResolveHealthColor());
            view.SetShield(unit.IsDead ? 0 : unit.shield, unit.IsDead ? 0 : unit.maxShield);
        }

        private Color ResolveHealthColor()
        {
            if (unit == null)
                return Color.white;

            if (localPlayer != null && unit == localPlayer)
                return LocalPlayerHealthColor;

            return teamColors != null ? teamColors.GetColorForTeam(unit.team) : Color.white;
        }

        private void RefreshBuffs()
        {
            if (view == null)
                return;

            groupedBuffs.Clear();
            groupedDebuffs.Clear();

            var source = buffDriver != null ? buffDriver.Buffs : NoBuffs;
            for (var i = 0; i < source.Count; i++)
            {
                var data = source[i];
                if (data == null)
                    continue;

                var list = data.IsNegative ? groupedDebuffs : groupedBuffs;
                var existing = FindGrouped(list, data.BuffId);
                if (existing != null)
                {
                    existing.StackCount += Mathf.Max(1, data.StackCount);
                    if (data.TimeRemaining < existing.TimeRemaining)
                    {
                        existing.TimeRemaining = data.TimeRemaining;
                        existing.Duration = data.Duration;
                    }

                    continue;
                }

                list.Add(new UiBuffData
                {
                    InstanceId = data.InstanceId,
                    BuffId = data.BuffId,
                    DisplayName = data.DisplayName,
                    IconTexture = data.IconTexture,
                    StackCount = Mathf.Max(1, data.StackCount),
                    Duration = data.Duration,
                    TimeRemaining = data.TimeRemaining,
                    IsNegative = data.IsNegative
                });
            }

            view.SetBuffs(groupedBuffs, groupedDebuffs);
        }

        private static UiBuffData FindGrouped(List<UiBuffData> list, string buffId)
        {
            if (string.IsNullOrEmpty(buffId))
                return null;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].BuffId == buffId)
                    return list[i];
            }

            return null;
        }

        private Texture2D ResolveSkillIcon(string skillName)
        {
            if (databases == null || databases.Skills == null)
                return null;

            return databases.Skills.GetSkillByName(skillName)?.iconTexture;
        }
    }
}
