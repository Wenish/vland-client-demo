using System.Collections.Generic;
using UnityEngine;

namespace ShadowInfection
{
    public sealed class TeamColorService : ITeamColorService
    {
        private const float GoldenRatioConjugate = 0.61803398875f;

        private readonly TeamColorTable table;
        private readonly Dictionary<int, Color> assignedColors = new();

        public TeamColorService(TeamColorTable table)
        {
            this.table = table;
        }

        public Color GetColorForTeam(int teamId)
        {
            if (assignedColors.TryGetValue(teamId, out var color))
                return color;

            if (table != null && table.TryGetPredefined(teamId, out color))
            {
                assignedColors[teamId] = color;
                return color;
            }

            color = GenerateColorFromTeamId(teamId);
            assignedColors[teamId] = color;
            return color;
        }

        private static Color GenerateColorFromTeamId(int teamId)
        {
            var hue = Mathf.Abs(teamId * GoldenRatioConjugate) % 1f;
            return Color.HSVToRGB(hue, 0.7f, 0.9f);
        }
    }
}
