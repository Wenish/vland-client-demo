using System;
using UnityEngine;

namespace ShadowInfection
{
    [CreateAssetMenu(menuName = "Game/Team Color Table", fileName = "TeamColorTable")]
    public sealed class TeamColorTable : ScriptableObject
    {
        [SerializeField, ColorUsage(true, true)]
        private Color[] predefinedColors = Array.Empty<Color>();

        public bool TryGetPredefined(int teamId, out Color color)
        {
            if (predefinedColors != null && teamId >= 0 && teamId < predefinedColors.Length)
            {
                color = predefinedColors[teamId];
                return true;
            }

            color = default;
            return false;
        }
    }
}
