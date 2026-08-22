using System;

namespace ShadowInfection.Match
{
    public readonly struct SkirmishUiSnapshot
    {
        public readonly int Round;
        public readonly int TargetRoundWins;
        public readonly int TeamCount;
        public readonly bool TeamSelectionLocked;
        public readonly SkirmishGameManager.RoundState RoundState;
        public readonly float CountdownRemaining;
        public readonly float ReturnToLobbyCountdownRemaining;
        public readonly bool MatchEnded;
        public readonly int MatchWinnerTeam;
        public readonly int LocalTeamId;
        public readonly int[] TeamScores;

        public SkirmishUiSnapshot(
            int round,
            int targetRoundWins,
            int teamCount,
            bool teamSelectionLocked,
            SkirmishGameManager.RoundState roundState,
            float countdownRemaining,
            float returnToLobbyCountdownRemaining,
            bool matchEnded,
            int matchWinnerTeam,
            int localTeamId,
            int[] teamScores)
        {
            Round = round;
            TargetRoundWins = targetRoundWins;
            TeamCount = teamCount;
            TeamSelectionLocked = teamSelectionLocked;
            RoundState = roundState;
            CountdownRemaining = countdownRemaining;
            ReturnToLobbyCountdownRemaining = returnToLobbyCountdownRemaining;
            MatchEnded = matchEnded;
            MatchWinnerTeam = matchWinnerTeam;
            LocalTeamId = localTeamId;
            TeamScores = teamScores ?? Array.Empty<int>();
        }

        public string Signature
        {
            get
            {
                string scores = TeamScores.Length == 0
                    ? string.Empty
                    : string.Join(",", TeamScores);
                return $"{Round}|{TargetRoundWins}|{TeamCount}|{TeamSelectionLocked}|{(int)RoundState}|{CountdownRemaining:F1}|{ReturnToLobbyCountdownRemaining:F1}|{MatchEnded}|{MatchWinnerTeam}|{LocalTeamId}|{scores}";
            }
        }
    }

    public interface ISkirmishUiSession
    {
        bool TryGetSnapshot(out SkirmishUiSnapshot snapshot);
    }
}
