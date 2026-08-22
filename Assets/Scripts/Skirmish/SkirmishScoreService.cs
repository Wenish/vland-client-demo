using System.Collections.Generic;
using System.Linq;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.Skirmish
{
    public sealed class SkirmishScoreService
    {
        private readonly ISkirmishHost host;

        public SkirmishScoreService(ISkirmishHost host)
        {
            this.host = host;
        }

        public (int winnerTeam, bool isDraw)? GetRoundOutcome()
        {
            if (GameServices.PlayerUnits == null)
                return null;

            var aliveTeams = new HashSet<int>();
            foreach (var playerUnit in GameServices.PlayerUnits.playerUnits)
            {
                if (playerUnit.Unit == null)
                    continue;

                var unitController = playerUnit.Unit.GetComponent<UnitController>();
                if (unitController == null || unitController.unitType != UnitType.Player || unitController.IsDead)
                    continue;

                int teamId = Mathf.Clamp(unitController.team, 0, host.TeamCount - 1);
                aliveTeams.Add(teamId);
                if (aliveTeams.Count > 1)
                    return null;
            }

            if (aliveTeams.Count == 1)
                return (aliveTeams.First(), false);

            return (-1, true);
        }

        public void HandleRoundEnd(int winnerTeam, bool isDraw)
        {
            host.SetRoundState(SkirmishGameManager.RoundState.RoundEnded);
            host.SetLastRoundResult(winnerTeam, isDraw);
            host.IncrementRoundResolutionSequence();

            if (host.IsServerOnly)
                host.RaiseRoundEnded(winnerTeam, isDraw);

            if (isDraw)
                return;

            host.AddTeamRoundWin(winnerTeam);
            host.LogTeamScores();

            if (host.GetTeamRoundWins(winnerTeam) < host.TargetRoundWins)
                return;

            host.MatchEnded = true;
            host.MatchWinnerTeam = winnerTeam;
            host.SetRoundState(SkirmishGameManager.RoundState.MatchEnded);
            host.ServerEndMatchLifecycle(winnerTeam);
            host.RaiseMatchEnded(winnerTeam);
        }
    }
}
