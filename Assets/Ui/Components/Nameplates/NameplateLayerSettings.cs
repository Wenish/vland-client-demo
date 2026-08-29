using UnityEngine;

namespace ShadowInfection.UI.Nameplates
{
    public sealed class NameplateLayerSettings
    {
        public NameplateLayerSettings(
            float healthLerpSeconds,
            float headWorldOffset,
            float screenOffsetPixels,
            Color localPlayerHealthColor)
        {
            HealthLerpSeconds = healthLerpSeconds;
            HeadWorldOffset = headWorldOffset;
            ScreenOffsetPixels = screenOffsetPixels;
            LocalPlayerHealthColor = localPlayerHealthColor;
        }

        public float HealthLerpSeconds { get; }
        public float HeadWorldOffset { get; }
        public float ScreenOffsetPixels { get; }
        public Color LocalPlayerHealthColor { get; }
    }
}
