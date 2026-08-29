using System;
using System.Threading;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.UI.Nameplates
{
    internal sealed class UnitNameplateBinding : IDisposable
    {
        private static readonly UiBuffData[] NoBuffs = Array.Empty<UiBuffData>();

        private readonly NameplateLayerSettings settings;
        private readonly IGameDatabases databases;
        private readonly ITeamColorService teamColors;
        private readonly Action<UnitNameplateBinding> onChanged;
        private readonly UnitNameplateVisibilityState visibility = new();

        private UnitController unit;
        private UnitActionState actionState;
        private UnitNameplateCastBarDriver castBarDriver;
        private UnitNameplateBuffDriver buffDriver;
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
        public float HeadAnchorHeight { get; private set; }

        public void Attach(UnitController nextUnit, UnitNameplateElement element, CancellationToken destroyToken)
        {
            Detach();

            unit = nextUnit;
            Element = element;
            lifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);

            actionState = unit.GetComponent<UnitActionState>();
            HeadAnchorHeight = NameplatePositionResolver.ComputeHeadAnchorHeight(
                unit,
                settings.HeadWorldOffset);

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

            buffDriver = new UnitNameplateBuffDriver(databases, NotifyChanged);
            buffDriver.Bind(unit.GetComponent<UnitNetworkBuffs>());

            NotifyChanged();
        }

        public void Detach()
        {
            castBarDriver?.Unbind();
            castBarDriver = null;

            buffDriver?.Unbind();
            buffDriver = null;

            if (unit != null)
            {
                unit.OnHealthChange -= HandleHealthChange;
                unit.OnShieldChange -= HandleShieldChange;
                unit.OnNameChanged -= HandleNameChanged;
                unit.OnTeamChanged -= HandleTeamChanged;
                unit.OnDied -= HandleDied;
                unit.OnRevive -= HandleRevive;
            }

            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
            lifetimeCts = null;

            unit = null;
            actionState = null;
            Element = null;
            HeadAnchorHeight = 0f;
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

            if (buffDriver != null && buffDriver.Tick())
                changed = true;

            if (changed)
                NotifyChanged();
        }

        public UnitNameplateSnapshot BuildSnapshot()
        {
            var healthFill = maxHealth > 0 ? displayedHealth / maxHealth : 0f;
            var shieldFill = maxShield > 0 ? displayedShield / maxShield : 0f;
            var buffs = buffDriver != null ? buffDriver.Buffs : NoBuffs;

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
                buffs);
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

        private void NotifyChanged()
        {
            if (!disposed)
                onChanged(this);
        }
    }
}
