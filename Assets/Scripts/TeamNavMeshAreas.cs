using UnityEngine.AI;

public static class TeamNavMeshAreas
{
    public const string DefaultTeamAreaNameFormat = "Team_{0}";

    public static int BuildAreaMaskForTeam(int teamId, int maxTeams = 16, string teamAreaNameFormat = DefaultTeamAreaNameFormat)
    {
        int mask = NavMesh.AllAreas;

        for (int i = 0; i < maxTeams; i++)
        {
            if (i == teamId)
            {
                continue;
            }

            int areaIndex = NavMesh.GetAreaFromName(string.Format(teamAreaNameFormat, i));
            if (areaIndex < 0)
            {
                continue;
            }

            mask &= ~(1 << areaIndex);
        }

        return mask;
    }
}
