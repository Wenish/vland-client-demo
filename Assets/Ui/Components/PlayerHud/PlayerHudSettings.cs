using System;

namespace ShadowInfection.UI.PlayerHud
{
    internal sealed class PlayerHudSettings
    {
        public float GoldTweenSeconds { get; }
        public float BannerFadeInSeconds { get; }
        public float BannerHoldSeconds { get; }
        public float BannerFadeOutSeconds { get; }
        public float CastBarInterruptFadeSeconds { get; }
        public float CastBarSuccessFadeSeconds { get; }

        public PlayerHudSettings(
            float goldTweenSeconds,
            float bannerFadeInSeconds,
            float bannerHoldSeconds,
            float bannerFadeOutSeconds,
            float castBarInterruptFadeSeconds,
            float castBarSuccessFadeSeconds)
        {
            GoldTweenSeconds = Math.Max(0f, goldTweenSeconds);
            BannerFadeInSeconds = Math.Max(0f, bannerFadeInSeconds);
            BannerHoldSeconds = Math.Max(0f, bannerHoldSeconds);
            BannerFadeOutSeconds = Math.Max(0f, bannerFadeOutSeconds);
            CastBarInterruptFadeSeconds = Math.Max(0f, castBarInterruptFadeSeconds);
            CastBarSuccessFadeSeconds = Math.Max(0f, castBarSuccessFadeSeconds);
        }
    }
}
