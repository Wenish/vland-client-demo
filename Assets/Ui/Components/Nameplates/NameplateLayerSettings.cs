using UnityEngine;

namespace ShadowInfection.UI.Nameplates
{
    [CreateAssetMenu(
        fileName = "NameplateLayerSettings",
        menuName = "Game/UI/Nameplate Layer Settings")]
    public sealed class NameplateLayerSettings : ScriptableObject
    {
        [SerializeField]
        [Min(0.01f)]
        private float healthLerpSeconds = 0.2f;

        [SerializeField]
        [Min(0f)]
        private float headWorldOffset = 0.15f;

        [SerializeField]
        [Min(0f)]
        private float screenOffsetPixels = 12f;

        [SerializeField]
        private Color localPlayerHealthColor = new Color(0f, 0.6509804f, 0.24313727f, 1f);

        public float HealthLerpSeconds => healthLerpSeconds;
        public float HeadWorldOffset => headWorldOffset;
        public float ScreenOffsetPixels => screenOffsetPixels;
        public Color LocalPlayerHealthColor => localPlayerHealthColor;
    }
}
