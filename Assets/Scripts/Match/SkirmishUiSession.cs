using System;

namespace ShadowInfection.Match
{
    public sealed class SkirmishUiSession : ISkirmishUiSession
    {
        private readonly SkirmishGameManager manager;

        public SkirmishUiSession(SkirmishGameManager manager)
        {
            this.manager = manager;
        }

        public bool TryGetSnapshot(out SkirmishUiSnapshot snapshot)
        {
            if (manager == null)
            {
                snapshot = default;
                return false;
            }

            int teamCount = Math.Max(0, manager.TeamCount);
            var teamScores = new int[teamCount];
            for (int teamId = 0; teamId < teamCount; teamId++)
                teamScores[teamId] = manager.GetTeamRoundWins(teamId);

            snapshot = new SkirmishUiSnapshot(
                manager.CurrentRound,
                manager.TargetRoundWins,
                manager.TeamCount,
                manager.TeamSelectionLocked,
                manager.CurrentRoundState,
                manager.CountdownRemaining,
                manager.ReturnToLobbyCountdownRemaining,
                manager.MatchEnded,
                manager.MatchWinnerTeam,
                LocalMatchPlayer.ResolveLocalTeamId(),
                teamScores);
            return true;
        }
    }
}
