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
        public float InfoMessageDurationSeconds { get; }
        public float InfoMessageFadeSeconds { get; }
        public int InfoMessageMaxVisible { get; }

        public PlayerHudSettings(
            float goldTweenSeconds,
            float bannerFadeInSeconds,
            float bannerHoldSeconds,
            float bannerFadeOutSeconds,
            float castBarInterruptFadeSeconds,
            float castBarSuccessFadeSeconds,
            float infoMessageDurationSeconds,
            float infoMessageFadeSeconds,
            int infoMessageMaxVisible)
        {
            GoldTweenSeconds = Math.Max(0f, goldTweenSeconds);
            BannerFadeInSeconds = Math.Max(0f, bannerFadeInSeconds);
            BannerHoldSeconds = Math.Max(0f, bannerHoldSeconds);
            BannerFadeOutSeconds = Math.Max(0f, bannerFadeOutSeconds);
            CastBarInterruptFadeSeconds = Math.Max(0f, castBarInterruptFadeSeconds);
            CastBarSuccessFadeSeconds = Math.Max(0f, castBarSuccessFadeSeconds);
            InfoMessageDurationSeconds = Math.Max(0.1f, infoMessageDurationSeconds);
            InfoMessageFadeSeconds = Math.Max(0f, infoMessageFadeSeconds);
            InfoMessageMaxVisible = Math.Max(1, infoMessageMaxVisible);
        }
    }
}
