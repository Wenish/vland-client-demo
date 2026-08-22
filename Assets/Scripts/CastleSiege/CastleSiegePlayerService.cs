using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirror;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.CastleSiege
{
    public sealed class CastleSiegePlayerService : IDisposable
    {
        private readonly ICastleSiegeHost host;
        private readonly Dictionary<int, GameObject> trackedPlayerUnitsByConnection = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, Action> playerDeathHandlersByConnection = new Dictionary<int, Action>();
        private readonly Dictionary<int, CancellationTokenSource> respawnCtsByConnection = new Dictionary<int, CancellationTokenSource>();

        public CastleSiegePlayerService(ICastleSiegeHost host)
        {
            this.host = host;
        }

        public void SyncPlayersAndTeams()
        {
            if (GameServices.PlayerUnits == null)
                return;

            var activeConnectionIds = new HashSet<int>();

            foreach (var playerUnit in GameServices.PlayerUnits.playerUnits)
            {
                if (playerUnit.Unit == null)
                    continue;

                int connectionId = playerUnit.ConnectionId;
                activeConnectionIds.Add(connectionId);

                bool isNewAssignment = !host.TryGetAssignedTeam(connectionId, out int assignedTeamId);
                if (isNewAssignment)
                {
                    assignedTeamId = AssignTeamForConnection();
                    host.SetAssignedTeam(connectionId, assignedTeamId);
                    host.RaisePlayerJoined();
                }

                var unitController = playerUnit.Unit.GetComponent<UnitController>();
                if (unitController == null)
                    continue;

                bool isNewUnit = !trackedPlayerUnitsByConnection.TryGetValue(connectionId, out GameObject trackedUnit)
                    || trackedUnit != playerUnit.Unit;

                unitController.SetTeam(assignedTeamId);
                EnsurePlayerDeathSubscription(connectionId, playerUnit.Unit, unitController);

                if (host.IsTeamEliminated(assignedTeamId))
                {
                    if (!unitController.IsDead)
                        unitController.SetHealth(0);
                    continue;
                }

                if ((isNewAssignment || isNewUnit) && host.TeamConfigs.ContainsKey(assignedTeamId))
                {
                    var spawn = GetNearestTeamPlayerSpawn(assignedTeamId, unitController.transform.position);
                    RespawnPlayerAtSpawn(unitController, spawn);
                }
            }

            HandleDisconnectedPlayers(activeConnectionIds);
        }

        public void RespawnAllActivePlayersToTeamSpawns()
        {
            if (GameServices.PlayerUnits == null)
                return;

            foreach (var playerUnit in GameServices.PlayerUnits.playerUnits)
            {
                if (playerUnit.Unit == null)
                    continue;

                if (!host.TryGetAssignedTeam(playerUnit.ConnectionId, out int assignedTeamId))
                    continue;

                if (host.IsTeamEliminated(assignedTeamId))
                    continue;

                var unitController = playerUnit.Unit.GetComponent<UnitController>();
                if (unitController == null)
                    continue;

                var spawn = GetNearestTeamPlayerSpawn(assignedTeamId, unitController.transform.position);
                RespawnPlayerAtSpawn(unitController, spawn);
            }
        }

        public void RespawnAssignedPlayer(int connectionId, int teamId)
        {
            if (GameServices.PlayerUnits == null || !host.TeamConfigs.ContainsKey(teamId))
                return;

            for (int i = 0; i < GameServices.PlayerUnits.playerUnits.Count; i++)
            {
                var playerUnit = GameServices.PlayerUnits.playerUnits[i];
                if (playerUnit.ConnectionId != connectionId || playerUnit.Unit == null)
                    continue;

                var unitController = playerUnit.Unit.GetComponent<UnitController>();
                if (unitController == null)
                    return;

                var spawn = GetNearestTeamPlayerSpawn(teamId, unitController.transform.position);
                RespawnPlayerAtSpawn(unitController, spawn);
                return;
            }
        }

        public void CancelRespawnsForTeam(int teamId)
        {
            var toCancel = new List<int>();
            foreach (int connectionId in respawnCtsByConnection.Keys)
            {
                if (host.TryGetAssignedTeam(connectionId, out int assignedTeam) && assignedTeam == teamId)
                    toCancel.Add(connectionId);
            }

            for (int i = 0; i < toCancel.Count; i++)
                CancelRespawn(toCancel[i]);
        }

        public void Dispose()
        {
            foreach (var pair in respawnCtsByConnection)
                pair.Value?.Cancel();
            respawnCtsByConnection.Clear();

            foreach (var pair in trackedPlayerUnitsByConnection)
            {
                if (pair.Value == null)
                    continue;

                var controller = pair.Value.GetComponent<UnitController>();
                if (controller == null)
                    continue;

                if (playerDeathHandlersByConnection.TryGetValue(pair.Key, out Action handler))
                    controller.OnDied -= handler;
            }

            trackedPlayerUnitsByConnection.Clear();
            playerDeathHandlersByConnection.Clear();
        }

        private void HandleDisconnectedPlayers(HashSet<int> activeConnectionIds)
        {
            var disconnected = host.AssignedConnectionIds
                .Where(id => !activeConnectionIds.Contains(id))
                .ToList();

            foreach (int connectionId in disconnected)
            {
                host.RemoveAssignedTeam(connectionId);
                trackedPlayerUnitsByConnection.Remove(connectionId);
                playerDeathHandlersByConnection.Remove(connectionId);
                CancelRespawn(connectionId);
                host.RaisePlayerLeft();
            }
        }

        private void EnsurePlayerDeathSubscription(int connectionId, GameObject playerUnitObject, UnitController unitController)
        {
            if (trackedPlayerUnitsByConnection.TryGetValue(connectionId, out GameObject trackedUnit)
                && trackedUnit == playerUnitObject)
            {
                return;
            }

            if (trackedPlayerUnitsByConnection.TryGetValue(connectionId, out GameObject oldTracked) && oldTracked != null)
            {
                var oldController = oldTracked.GetComponent<UnitController>();
                if (oldController != null && playerDeathHandlersByConnection.TryGetValue(connectionId, out Action oldHandler))
                    oldController.OnDied -= oldHandler;
            }

            Action onDiedHandler = () => HandlePlayerUnitDied(connectionId, unitController);
            unitController.OnDied += onDiedHandler;
            trackedPlayerUnitsByConnection[connectionId] = playerUnitObject;
            playerDeathHandlersByConnection[connectionId] = onDiedHandler;
        }

        private void HandlePlayerUnitDied(int connectionId, UnitController unitController)
        {
            host.RaiseUnitDied(unitController);

            if (!host.TryGetAssignedTeam(connectionId, out int teamId))
                return;

            if (host.IsTeamEliminated(teamId) || host.CurrentPhase == CastleSiegeManager.MatchPhase.MatchEnded)
                return;

            StartRespawn(connectionId, unitController.transform.position);
        }

        private void StartRespawn(int connectionId, Vector3 deathPosition)
        {
            CancelRespawn(connectionId);
            var cts = new CancellationTokenSource();
            respawnCtsByConnection[connectionId] = cts;
            RespawnAfterDelayAsync(connectionId, deathPosition, cts.Token).Forget();
        }

        private void CancelRespawn(int connectionId)
        {
            if (!respawnCtsByConnection.TryGetValue(connectionId, out var cts))
                return;

            cts.Cancel();
            cts.Dispose();
            respawnCtsByConnection.Remove(connectionId);
        }

        private async UniTaskVoid RespawnAfterDelayAsync(int connectionId, Vector3 deathPosition, CancellationToken ct)
        {
            try
            {
                float delay = ComputeRespawnSeconds();
                if (delay > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);

                if (!host.TryGetAssignedTeam(connectionId, out int teamId) || host.IsTeamEliminated(teamId))
                    return;

                var playerUnit = GameServices.PlayerUnits.playerUnits.FirstOrDefault(unit => unit.ConnectionId == connectionId);
                if (playerUnit.Unit == null)
                    return;

                var controller = playerUnit.Unit.GetComponent<UnitController>();
                if (controller == null)
                    return;

                var spawn = GetNearestTeamPlayerSpawn(teamId, deathPosition);
                RespawnPlayerAtSpawn(controller, spawn);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (respawnCtsByConnection.TryGetValue(connectionId, out var existing) && existing.Token == ct)
                {
                    existing.Dispose();
                    respawnCtsByConnection.Remove(connectionId);
                }
            }
        }

        private float ComputeRespawnSeconds()
        {
            var mapConfig = host.MapConfig;
            float matchMinutes = 0f;
            if (host.InGameStartServerTime > 0d)
                matchMinutes = Mathf.Max(0f, (float)((NetworkTime.time - host.InGameStartServerTime) / 60d));

            float respawn = mapConfig.BaseRespawnSeconds + Mathf.Floor(matchMinutes) * mapConfig.ExtraRespawnPerMinute;
            return Mathf.Clamp(respawn, mapConfig.MinRespawnSeconds, mapConfig.MaxRespawnSeconds);
        }

        private int AssignTeamForConnection()
        {
            var population = new Dictionary<int, int>();
            foreach (int teamId in host.TeamConfigs.Keys)
            {
                if (host.IsTeamEliminated(teamId))
                    continue;
                population[teamId] = 0;
            }

            foreach (int connectionId in host.AssignedConnectionIds)
            {
                if (!host.TryGetAssignedTeam(connectionId, out int assignedTeam))
                    continue;
                if (!population.ContainsKey(assignedTeam))
                    continue;
                population[assignedTeam]++;
            }

            if (population.Count == 0)
                return 0;

            int minimum = population.Values.Min();
            return population
                .Where(pair => pair.Value == minimum)
                .OrderBy(pair => pair.Key)
                .First()
                .Key;
        }

        private CastleSiegeMapConfig.SpawnPointData GetNearestTeamPlayerSpawn(int teamId, Vector3 deathPosition)
        {
            var spawnPoints = host.TeamConfigs[teamId].PlayerSpawnPoints;
            int selectedIndex = 0;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < spawnPoints.Count; i++)
            {
                float sqrDistance = (spawnPoints[i].Position - deathPosition).sqrMagnitude;
                if (sqrDistance < closestDistance)
                {
                    closestDistance = sqrDistance;
                    selectedIndex = i;
                }
            }

            return spawnPoints[selectedIndex];
        }

        private void RespawnPlayerAtSpawn(UnitController unitController, CastleSiegeMapConfig.SpawnPointData spawnPoint)
        {
            Vector3 targetPosition = spawnPoint.Position;
            if (!host.TryFindSpawnPosition(spawnPoint.Position, out targetPosition))
                targetPosition = spawnPoint.Position;

            unitController.InterruptAction();
            unitController.SetHealth(unitController.maxHealth);
            unitController.SetShield(unitController.maxShield);

            var transformToMove = unitController.transform;
            if (unitController.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = targetPosition;
                rb.rotation = spawnPoint.Rotation;
            }

            transformToMove.SetPositionAndRotation(targetPosition, spawnPoint.Rotation);
        }
    }
}
