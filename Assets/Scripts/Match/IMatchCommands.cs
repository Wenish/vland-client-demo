namespace ShadowInfection.Match
{
    public interface IMatchCommands
    {
        bool TryReturnToLobby();
        bool TryLockTeamSwitching();
        bool TryUnlockTeamSwitching();
        bool TryChooseLocalTeam(int teamId);
    }
}
