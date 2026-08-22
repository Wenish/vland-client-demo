using System;
using System.Collections.Generic;
using System.Linq;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.CastleSiege
{
    public sealed class CastleSiegeObjectiveService : IDisposable
    {
        private readonly ICastleSiegeHost host;
        private readonly Dictionary<int, UnitController> lordByTeamId = new Dictionary<int, UnitController>();
        private readonly Dictionary<int, Action> lordDeathHandlersByTeamId = new Dictionary<int, Action>();

        public CastleSiegeObjectiveService(ICastleSiegeHost host)
        {
            this.host = host;
        }

        public UnitController GetAliveEnemyLord(int requesterTeamId, Vector3 fromPosition)
        {
            UnitController bestLord = null;
            float bestDistanceSqr = float.MaxValue;

            foreach (var pair in lordByTeamId)
            {
                if (pair.Key == requesterTeamId || host.IsTeamEliminated(pair.Key))
                    continue;

                var lord = pair.Value;
                if (lord == null || lord.IsDead)
                    continue;

                float sqrDistance = (lord.transform.position - fromPosition).sqrMagnitude;
                if (sqrDistance >= bestDistanceSqr)
                    continue;

                bestDistanceSqr = sqrDistance;
                bestLord = lord;
            }

            return bestLord;
        }

        public void SpawnLordsOnce()
        {
            if (host.LordsSpawned)
                return;

            host.LordsSpawned = true;

            foreach (var pair in host.TeamConfigs)
            {
                int teamId = pair.Key;
                var teamConfig = pair.Value;

                Vector3 spawnPosition = teamConfig.LordSpawn.Position;
                if (!host.TryFindSpawnPosition(spawnPosition, out Vector3 resolvedPosition))
                    resolvedPosition = spawnPosition;

                var units = GameServices.Units;
                if (units == null)
                {
                    UnityEngine.Debug.LogError("[CastleSiegeManager] Unit spawner is missing. Cannot spawn lord.");
                    continue;
                }

                var lordObject = units.Spawn(teamConfig.LordUnit, resolvedPosition, teamConfig.LordSpawn.Rotation, true);
                if (lordObject == null)
                {
                    UnityEngine.Debug.LogError($"[CastleSiegeManager] Failed to spawn lord for team {teamId}.");
                    continue;
                }

                var lordController = lordObject.GetComponent<UnitController>();
                if (lordController == null)
                {
                    UnityEngine.Debug.LogError($"[CastleSiegeManager] Spawned lord has no UnitController for team {teamId}.");
                    continue;
                }

                lordController.SetTeam(teamId);
                Action onLordDied = () => HandleLordDied(teamId, lordController);
                lordController.OnDied += onLordDied;
                lordByTeamId[teamId] = lordController;
                lordDeathHandlersByTeamId[teamId] = onLordDied;
                host.RaiseLordSpawned(teamId);
            }
        }

        public void CheckVictory()
        {
            if (host.CurrentPhase == CastleSiegeManager.MatchPhase.MatchEnded)
                return;

            var aliveTeams = host.TeamConfigs.Keys
                .Where(teamId => !host.IsTeamEliminated(teamId))
                .OrderBy(teamId => teamId)
                .ToList();
            if (aliveTeams.Count != 1)
                return;

            host.WinnerTeamId = aliveTeams[0];
            host.SetPhase(CastleSiegeManager.MatchPhase.MatchEnded);
            host.ServerEndMatchLifecycle(host.WinnerTeamId);
            host.RaiseMatchWinner(host.WinnerTeamId);
        }

        public void Dispose()
        {
            foreach (var pair in lordByTeamId)
            {
                if (pair.Value == null)
                    continue;

                if (lordDeathHandlersByTeamId.TryGetValue(pair.Key, out Action handler))
                    pair.Value.OnDied -= handler;
            }

            lordByTeamId.Clear();
            lordDeathHandlersByTeamId.Clear();
        }

        private void HandleLordDied(int teamId, UnitController lordController)
        {
            host.RaiseUnitDied(lordController);
            EliminateTeam(teamId);
        }

        private void EliminateTeam(int teamId)
        {
            if (host.IsTeamEliminated(teamId))
                return;

            host.MarkTeamEliminated(teamId);
            host.CancelRespawnsForTeam(teamId);

            if (GameServices.PlayerUnits != null)
            {
                foreach (var playerUnit in GameServices.PlayerUnits.playerUnits)
                {
                    if (playerUnit.Unit == null)
                        continue;
                    if (!host.TryGetAssignedTeam(playerUnit.ConnectionId, out int assignedTeam) || assignedTeam != teamId)
                        continue;

                    var playerController = playerUnit.Unit.GetComponent<UnitController>();
                    if (playerController != null && !playerController.IsDead)
                        playerController.SetHealth(0);
                }
            }

            host.RaiseTeamEliminated(teamId);
            CheckVictory();
        }
    }
}
