using System.Collections.Generic;
using UnityEngine;

namespace ShadowInfection.UI.Nameplates
{
    public readonly struct UnitNameplateSnapshot
    {
        public UnitNameplateSnapshot(
            bool showRoot,
            bool showHealth,
            bool showShield,
            bool showName,
            bool showCastBar,
            float healthFill,
            float shieldFill,
            string unitName,
            Color healthColor,
            Texture2D castIcon,
            float castProgress,
            IReadOnlyList<UiBuffData> buffs)
        {
            ShowRoot = showRoot;
            ShowHealth = showHealth;
            ShowShield = showShield;
            ShowName = showName;
            ShowCastBar = showCastBar;
            HealthFill = healthFill;
            ShieldFill = shieldFill;
            UnitName = unitName;
            HealthColor = healthColor;
            CastIcon = castIcon;
            CastProgress = castProgress;
            Buffs = buffs;
        }

        public bool ShowRoot { get; }
        public bool ShowHealth { get; }
        public bool ShowShield { get; }
        public bool ShowName { get; }
        public bool ShowCastBar { get; }
        public float HealthFill { get; }
        public float ShieldFill { get; }
        public string UnitName { get; }
        public Color HealthColor { get; }
        public Texture2D CastIcon { get; }
        public float CastProgress { get; }
        public IReadOnlyList<UiBuffData> Buffs { get; }
    }
}
