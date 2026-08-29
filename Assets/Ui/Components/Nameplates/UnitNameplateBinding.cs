using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.UI.Nameplates
{
    internal sealed class UnitNameplateBinding : IDisposable
    {
        private readonly NameplateLayerSettings settings;
        private readonly IGameDatabases databases;
        private readonly ITeamColorService teamColors;
        private readonly Action<UnitNameplateBinding> onChanged;
        private readonly List<UiBuffData> buffs = new();
        private readonly UnitNameplateVisibilityState visibility = new();

        private UnitController unit;
        private UnitActionState actionState;
        private UnitNetworkBuffs networkBuffs;
        private UnitNameplateCastBarDriver castBarDriver;
        private CancellationTokenSource lifetimeCts;

        private bool showCastBar;
        private Texture2D castIcon;
        private float castProgress;
        private bool isLocalPlayer;
        private float displayedHealth;
        private float displayedShield;
        private float targetHealth;
        private float targetShield;
        private float healthLerpStart;
        private float shieldLerpStart;
        private float healthLerpElapsed;
        private float shieldLerpElapsed;
        private int maxHealth;
        private int maxShield;
        private Color healthColor = Color.white;
        private bool disposed;

        public UnitNameplateBinding(
            NameplateLayerSettings settings,
            IGameDatabases databases,
            ITeamColorService teamColors,
            Action<UnitNameplateBinding> onChanged)
        {
            this.settings = settings;
            this.databases = databases;
            this.teamColors = teamColors;
            this.onChanged = onChanged;
        }

        public UnitController Unit => unit;
        public UnitNameplateElement Element { get; private set; }

        public void Attach(UnitController nextUnit, UnitNameplateElement element, CancellationToken destroyToken)
        {
            Detach();

            unit = nextUnit;
            Element = element;
            lifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);

            actionState = unit.GetComponent<UnitActionState>();
            networkBuffs = unit.GetComponent<UnitNetworkBuffs>();

            maxHealth = unit.maxHealth;
            maxShield = unit.maxShield;
            targetHealth = unit.health;
            targetShield = unit.shield;
            displayedHealth = targetHealth;
            displayedShield = targetShield;

            UnitNameplateVisibilityPolicy.ApplyInit(unit, visibility);
            ApplyHealthColor();

            unit.OnHealthChange += HandleHealthChange;
            unit.OnShieldChange += HandleShieldChange;
            unit.OnNameChanged += HandleNameChanged;
            unit.OnTeamChanged += HandleTeamChanged;
            unit.OnDied += HandleDied;
            unit.OnRevive += HandleRevive;

            castBarDriver = new UnitNameplateCastBarDriver(databases, ApplyCastBar);
            castBarDriver.Bind(actionState, lifetimeCts.Token);

            if (networkBuffs != null)
            {
                networkBuffs.NetworkBuffs.OnAdd += OnBuffAdded;
                networkBuffs.NetworkBuffs.OnRemove += OnBuffRemoved;
                networkBuffs.NetworkBuffs.OnSet += OnBuffChanged;
                SeedBuffs();
            }

            NotifyChanged();
        }

        public void Detach()
        {
            castBarDriver?.Unbind();
            castBarDriver = null;

            if (unit != null)
            {
                unit.OnHealthChange -= HandleHealthChange;
                unit.OnShieldChange -= HandleShieldChange;
                unit.OnNameChanged -= HandleNameChanged;
                unit.OnTeamChanged -= HandleTeamChanged;
                unit.OnDied -= HandleDied;
                unit.OnRevive -= HandleRevive;
            }

            if (networkBuffs != null)
            {
                networkBuffs.NetworkBuffs.OnAdd -= OnBuffAdded;
                networkBuffs.NetworkBuffs.OnRemove -= OnBuffRemoved;
                networkBuffs.NetworkBuffs.OnSet -= OnBuffChanged;
            }

            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
            lifetimeCts = null;

            buffs.Clear();
            unit = null;
            networkBuffs = null;
            actionState = null;
            Element = null;
            showCastBar = false;
        }

        public void SetLocalPlayer(bool local)
        {
            isLocalPlayer = local;
            ApplyHealthColor();
            NotifyChanged();
        }

        public void Tick(float deltaTime)
        {
            if (unit == null || disposed)
                return;

            var changed = false;
            if (!Mathf.Approximately(displayedHealth, targetHealth))
            {
                healthLerpElapsed += deltaTime;
                var t = Mathf.Clamp01(healthLerpElapsed / Mathf.Max(settings.HealthLerpSeconds, 0.01f));
                displayedHealth = Mathf.Lerp(healthLerpStart, targetHealth, t);
                changed = true;
            }

            if (!Mathf.Approximately(displayedShield, targetShield))
            {
                shieldLerpElapsed += deltaTime;
                var t = Mathf.Clamp01(shieldLerpElapsed / Mathf.Max(settings.HealthLerpSeconds, 0.01f));
                displayedShield = Mathf.Lerp(shieldLerpStart, targetShield, t);
                changed = true;
            }

            if (UpdateBuffTimers())
                changed = true;

            if (changed)
                NotifyChanged();
        }

        public UnitNameplateSnapshot BuildSnapshot()
        {
            var healthFill = maxHealth > 0 ? displayedHealth / maxHealth : 0f;
            var shieldFill = maxShield > 0 ? displayedShield / maxShield : 0f;
            var orderedBuffs = buffs.OrderByDescending(b => b.TimeRemaining).ToList();

            return new UnitNameplateSnapshot(
                visibility.ShowRoot,
                visibility.ShowHealth,
                visibility.ShowShield,
                visibility.ShowName,
                showCastBar,
                healthFill,
                shieldFill,
                unit != null ? unit.unitName : string.Empty,
                healthColor,
                castIcon,
                castProgress,
                orderedBuffs);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Detach();
        }

        private void ApplyCastBar(bool visible, Texture2D icon, float progress)
        {
            showCastBar = visible;
            castIcon = icon;
            castProgress = progress;
            NotifyChanged();
        }

        private void HandleHealthChange((int current, int max) health)
        {
            maxHealth = health.max;
            healthLerpStart = displayedHealth;
            healthLerpElapsed = 0f;
            targetHealth = health.current;
            UnitNameplateVisibilityPolicy.OnHealthChange(unit, health, visibility);
            NotifyChanged();
        }

        private void HandleShieldChange((int current, int max) shield)
        {
            maxShield = shield.max;
            shieldLerpStart = displayedShield;
            shieldLerpElapsed = 0f;
            targetShield = shield.current;
            UnitNameplateVisibilityPolicy.OnShieldChange(unit, shield, visibility);
            NotifyChanged();
        }

        private void HandleNameChanged(UnitController _)
        {
            NotifyChanged();
        }

        private void HandleTeamChanged(UnitController _)
        {
            ApplyHealthColor();
            NotifyChanged();
        }

        private void HandleDied()
        {
            UnitNameplateVisibilityPolicy.OnDied(visibility);
            NotifyChanged();
        }

        private void HandleRevive()
        {
            UnitNameplateVisibilityPolicy.OnRevive(unit, visibility);
            targetHealth = unit.health;
            targetShield = unit.shield;
            displayedHealth = targetHealth;
            displayedShield = targetShield;
            NotifyChanged();
        }

        private void ApplyHealthColor()
        {
            if (unit == null)
                return;

            healthColor = isLocalPlayer
                ? settings.LocalPlayerHealthColor
                : teamColors.GetColorForTeam(unit.team);
        }

        private void SeedBuffs()
        {
            buffs.Clear();
            for (var i = 0; i < networkBuffs.NetworkBuffs.Count; i++)
            {
                var buff = networkBuffs.NetworkBuffs[i];
                if (!buff.ShowInUnitUiBuffBar)
                    continue;

                if (buffs.Any(b => b.InstanceId == buff.InstanceId))
                    continue;

                buffs.Add(CreateBuffData(buff));
            }
        }

        private void OnBuffAdded(int index)
        {
            var buff = networkBuffs.NetworkBuffs[index];
            if (!buff.ShowInUnitUiBuffBar)
                return;

            var existing = buffs.FirstOrDefault(b => b.InstanceId == buff.InstanceId);
            if (existing != null)
            {
                UpdateBuffData(existing, buff);
            }
            else
            {
                buffs.Add(CreateBuffData(buff));
            }

            NotifyChanged();
        }

        private void OnBuffRemoved(int index, UnitNetworkBuffs.NetworkBuffData oldBuff)
        {
            var buffData = buffs.FirstOrDefault(b => b.InstanceId == oldBuff.InstanceId);
            if (buffData != null)
            {
                buffs.Remove(buffData);
                NotifyChanged();
            }
        }

        private void OnBuffChanged(int index, UnitNetworkBuffs.NetworkBuffData oldBuff)
        {
            var buff = networkBuffs.NetworkBuffs[index];
            var buffData = buffs.FirstOrDefault(b => b.InstanceId == buff.InstanceId);
            if (buffData != null)
            {
                buffData.TimeRemaining = buff.Remaining;
                NotifyChanged();
            }
        }

        private bool UpdateBuffTimers()
        {
            var changed = false;
            foreach (var buffData in buffs)
            {
                if (buffData.Duration <= 0f || buffData.Duration >= Mathf.Infinity)
                    continue;

                var buff = networkBuffs.NetworkBuffs.FirstOrDefault(b => b.InstanceId == buffData.InstanceId);
                if (buff == null)
                    continue;

                if (!Mathf.Approximately(buffData.TimeRemaining, buff.Remaining))
                {
                    buffData.TimeRemaining = buff.Remaining;
                    changed = true;
                }
            }

            return changed;
        }

        private UiBuffData CreateBuffData(UnitNetworkBuffs.NetworkBuffData buff)
        {
            var isInfinite = buff.Duration == Mathf.Infinity;
            return new UiBuffData
            {
                InstanceId = buff.InstanceId,
                BuffId = buff.BuffId,
                IconTexture = databases?.Skills?.GetSkillByName(buff.SkillName)?.iconTexture,
                Duration = buff.Duration,
                TimeRemaining = isInfinite ? Mathf.Infinity : buff.Remaining
            };
        }

        private static void UpdateBuffData(UiBuffData target, UnitNetworkBuffs.NetworkBuffData buff)
        {
            var isInfinite = buff.Duration == Mathf.Infinity;
            target.BuffId = buff.BuffId;
            target.Duration = buff.Duration;
            target.TimeRemaining = isInfinite ? Mathf.Infinity : buff.Remaining;
        }

        private void NotifyChanged()
        {
            if (!disposed)
                onChanged(this);
        }
    }
}
