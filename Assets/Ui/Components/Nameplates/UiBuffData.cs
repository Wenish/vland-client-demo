using System;
using UnityEngine;

namespace ShadowInfection.UI.Nameplates
{
    [Serializable]
    public sealed class UiBuffData
    {
        public string InstanceId;
        public string BuffId;
        public Texture2D IconTexture;
        public int StackCount = 1;
        public float Duration = Mathf.Infinity;
        public float TimeRemaining = Mathf.Infinity;
        public float NormalizedRemaining => Duration <= 0f ? 0f : Mathf.Clamp01(TimeRemaining / Duration);
    }
}
