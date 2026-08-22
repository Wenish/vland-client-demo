using System.Collections.Generic;
using UnityEngine;

namespace ShadowInfection.CastleSiege
{
    public interface ICastleSiegeHost
    {
        bool IsServer { get; }
        CastleSiegeManager.MatchPhase CurrentPhase { get; }
        CastleSiegeMapConfig MapConfig { get; }
        bool AutoStartWhenMinPlayersReached { get; }
        int MinPlayersToStart { get; }
        float SpawnCollisionCheckRadius { get; }
        double InGameStartServerTime { get; set; }
        bool LordsSpawned { get; set; }
        int WinnerTeamId { get; set; }
        float PhaseRemainingSeconds { get; set; }

        IReadOnlyDictionary<int, CastleSiegeMapConfig.TeamConfig> TeamConfigs { get; }
        bool IsTeamEliminated(int teamId);
        void MarkTeamEliminated(int teamId);
        IEnumerable<int> AssignedConnectionIds { get; }
        bool TryGetAssignedTeam(int connectionId, out int teamId);
        void SetAssignedTeam(int connectionId, int teamId);
        void RemoveAssignedTeam(int connectionId);

        void SetPhase(CastleSiegeManager.MatchPhase phase);
        void ServerStartMatchLifecycle();
        void ServerEndMatchLifecycle(int winnerTeamId);
        void RaisePlayerJoined();
        void RaisePlayerLeft();
        void RaiseUnitDied(UnitController unit);
        void RaiseLordSpawned(int teamId);
        void RaiseTeamEliminated(int teamId);
        void RaiseMatchWinner(int winnerTeamId);
        void CancelRespawnsForTeam(int teamId);
        bool TryFindSpawnPosition(Vector3 basePosition, out Vector3 validPosition);
    }
}
