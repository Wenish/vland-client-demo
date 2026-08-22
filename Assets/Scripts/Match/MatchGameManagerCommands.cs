using Mirror;

namespace ShadowInfection.Match
{
    public sealed class MatchGameManagerCommands : IMatchCommands
    {
        private readonly MatchGameManagerBase manager;

        public MatchGameManagerCommands(MatchGameManagerBase manager)
        {
            this.manager = manager;
        }

        public bool TryReturnToLobby()
        {
            if (!NetworkServer.active || manager == null)
                return false;

            return manager.ServerTryReturnToLobby();
        }

        public bool TryLockTeamSwitching()
        {
            if (!NetworkServer.active || manager == null)
                return false;

            manager.ServerLockTeamSwitching();
            return true;
        }

        public bool TryUnlockTeamSwitching()
        {
            if (!NetworkServer.active || manager == null)
                return false;

            manager.ServerUnlockTeamSwitching();
            return true;
        }

        public bool TryChooseLocalTeam(int teamId)
        {
            return LocalMatchPlayer.TryChooseLocalTeam(teamId);
        }
    }
}
