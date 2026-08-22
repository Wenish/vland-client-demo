namespace ShadowInfection.Match
{
    public readonly struct CastleSiegeUiSnapshot
    {
        public readonly CastleSiegeManager.MatchPhase Phase;
        public readonly float PhaseRemainingSeconds;
        public readonly bool TeamSelectionLocked;
        public readonly float ReturnToLobbyCountdownRemaining;
        public readonly int WinnerTeamId;
        public readonly int TeamCount;
        public readonly int AliveTeams;
        public readonly int LocalTeamId;
        public readonly bool LocalTeamEliminated;

        public CastleSiegeUiSnapshot(
            CastleSiegeManager.MatchPhase phase,
            float phaseRemainingSeconds,
            bool teamSelectionLocked,
            float returnToLobbyCountdownRemaining,
            int winnerTeamId,
            int teamCount,
            int aliveTeams,
            int localTeamId,
            bool localTeamEliminated)
        {
            Phase = phase;
            PhaseRemainingSeconds = phaseRemainingSeconds;
            TeamSelectionLocked = teamSelectionLocked;
            ReturnToLobbyCountdownRemaining = returnToLobbyCountdownRemaining;
            WinnerTeamId = winnerTeamId;
            TeamCount = teamCount;
            AliveTeams = aliveTeams;
            LocalTeamId = localTeamId;
            LocalTeamEliminated = localTeamEliminated;
        }

        public string Signature =>
            $"{(int)Phase}|{PhaseRemainingSeconds:F1}|{TeamSelectionLocked}|{ReturnToLobbyCountdownRemaining:F1}|{WinnerTeamId}|{TeamCount}|{AliveTeams}|{LocalTeamId}|{LocalTeamEliminated}";
    }

    public interface ICastleSiegeUiSession
    {
        bool TryGetSnapshot(out CastleSiegeUiSnapshot snapshot);
    }
}
