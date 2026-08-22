using UnityEngine;

namespace ShadowInfection
{
    public interface ITeamColorService
    {
        Color GetColorForTeam(int teamId);
    }
}
