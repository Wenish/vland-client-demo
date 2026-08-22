namespace ShadowInfection.Match
{
    public sealed class CastleSiegeUiSession : ICastleSiegeUiSession
    {
        private readonly CastleSiegeManager manager;

        public CastleSiegeUiSession(CastleSiegeManager manager)
        {
            this.manager = manager;
        }

        public bool TryGetSnapshot(out CastleSiegeUiSnapshot snapshot)
        {
            if (manager == null)
            {
                snapshot = default;
                return false;
            }

            int teamCount = manager.TeamCount;
            int aliveTeams = 0;
            for (int i = 0; i < teamCount; i++)
            {
                if (!manager.IsTeamEliminated(i))
                    aliveTeams++;
            }

            int localTeamId = LocalMatchPlayer.ResolveLocalTeamId();
            bool localTeamEliminated = localTeamId >= 0 && manager.IsTeamEliminated(localTeamId);

            snapshot = new CastleSiegeUiSnapshot(
                manager.CurrentPhase,
                manager.PhaseRemainingSeconds,
                manager.TeamSelectionLocked,
                manager.ReturnToLobbyCountdownRemaining,
                manager.WinnerTeamId,
                teamCount,
                aliveTeams,
                localTeamId,
                localTeamEliminated);
            return true;
        }
    }
}
