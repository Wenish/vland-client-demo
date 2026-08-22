namespace ShadowInfection.Match
{
    public sealed class MatchGameManagerUiSession : IMatchUiSession
    {
        private readonly MatchGameManagerBase manager;

        public MatchGameManagerUiSession(MatchGameManagerBase manager)
        {
            this.manager = manager;
        }

        public bool TryGetSnapshot(out MatchUiSnapshot snapshot)
        {
            if (manager == null)
            {
                snapshot = default;
                return false;
            }

            snapshot = new MatchUiSnapshot(
                manager.GetType().Name,
                manager.TeamSelectionLocked,
                manager.TeamCount,
                manager.LifecycleState,
                manager.ReturnToLobbyCountdownRemaining,
                manager.LifecycleWinnerTeamId);
            return true;
        }
    }
}
