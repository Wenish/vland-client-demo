using UnityEngine;

namespace ShadowInfection.Skirmish
{
    public interface ISkirmishHost
    {
        bool IsServer { get; }
        bool IsServerOnly { get; }
        bool MatchEnded { get; set; }
        int MatchWinnerTeam { get; set; }
        int CurrentRound { get; }
        SkirmishGameManager.RoundState CurrentRoundState { get; }
        int TeamCount { get; }
        int TargetRoundWins { get; }
        float PreRoundCountdownSeconds { get; }
        float PostRoundDelaySeconds { get; }
        Transform GetTeamSpawn(int teamId);

        bool TryGetAssignedTeam(int connectionId, out int teamId);
        void SetAssignedTeam(int connectionId, int teamId);
        void RemoveAssignedTeam(int connectionId);
        System.Collections.Generic.IEnumerable<int> AssignedConnectionIds { get; }
        System.Collections.Generic.IEnumerable<int> AssignedTeamIds { get; }

        void SetCurrentRound(int value);
        void SetRoundState(SkirmishGameManager.RoundState value);
        void SetCountdownRemaining(float value);
        void SetLastRoundResult(int winnerTeam, bool isDraw);
        void IncrementRoundResolutionSequence();
        int RoundResolutionSequence { get; }
        void RaiseRoundEnded(int winnerTeam, bool isDraw);
        void RaiseMatchEnded(int winnerTeam);
        void ServerStartMatchLifecycle();
        void ServerEndMatchLifecycle(int winnerTeamId);
        int GetTeamRoundWins(int teamId);
        void AddTeamRoundWin(int teamId);
        void LogTeamScores();
    }
}
