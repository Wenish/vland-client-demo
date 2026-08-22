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
    }
}
