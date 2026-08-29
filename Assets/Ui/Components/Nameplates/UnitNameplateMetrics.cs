namespace ShadowInfection.UI.Nameplates
{
    internal static class UnitNameplateMetrics
    {
        public const float BarWidth = 140f;
        public const float HealthHeight = 18f;
        public const float ShieldHeight = 6f;
        public const float ShieldHealthGap = 2f;
        public const float VitalsBorder = 2f;
        public const float BuffSize = 28f;
        public const float CastWidth = 140f;
        public const float CastHeight = 12f;
        public const float CastIconSize = 12f;
        public const float CastIconMargin = 2f;
        public const float CastMarginTop = 2f;
        public const float CastTrackWidth = CastWidth - CastIconSize - CastIconMargin;
        public const float NameFontSize = 12f;
        public const float BarHeight = HealthHeight;
        public const float EstimatedPlateHeight =
            VitalsBorder * 2f + ShieldHeight + ShieldHealthGap + HealthHeight + CastMarginTop + CastHeight;
        public const float MaxLayoutWidth = 640f;
        public const float MaxLayoutHeight = 200f;
    }
}
