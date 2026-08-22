using System.Collections.Generic;
using System.Linq;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.Skirmish
{
    public sealed class SkirmishPlayerService
    {
        private readonly ISkirmishHost host;

        public SkirmishPlayerService(ISkirmishHost host)
        {
            this.host = host;
        }

        public void AssignTeamsToNewPlayers()
        {
            if (GameServices.PlayerUnits == null)
                return;

            var activeConnectionIds = new HashSet<int>();

            foreach (var playerUnit in GameServices.PlayerUnits.playerUnits)
            {
                if (playerUnit.Unit == null)
                    continue;

                activeConnectionIds.Add(playerUnit.ConnectionId);
                if (host.TryGetAssignedTeam(playerUnit.ConnectionId, out _))
                    continue;

                int assignedTeam = FindLeastPopulatedTeam();
                host.SetAssignedTeam(playerUnit.ConnectionId, assignedTeam);

                var unitController = playerUnit.Unit.GetComponent<UnitController>();
                if (unitController == null)
                    continue;

                unitController.SetTeam(assignedTeam);
                TeleportToTeamSpawn(playerUnit.Unit, unitController, assignedTeam);
            }

            var removedConnections = host.AssignedConnectionIds
                .Where(connectionId => !activeConnectionIds.Contains(connectionId))
                .ToList();

            foreach (var connectionId in removedConnections)
                host.RemoveAssignedTeam(connectionId);
        }

        public void ReviveAndTeleportAllPlayersToTeamSpawns()
        {
            if (GameServices.PlayerUnits == null)
                return;

            int teamCount = host.TeamCount;
            foreach (var playerUnit in GameServices.PlayerUnits.playerUnits)
            {
                if (playerUnit.Unit == null)
                    continue;

                var unitController = playerUnit.Unit.GetComponent<UnitController>();
                if (unitController == null)
                    continue;

                if (!host.TryGetAssignedTeam(playerUnit.ConnectionId, out int teamId))
                    teamId = Mathf.Clamp(unitController.team, 0, teamCount - 1);

                TeleportToTeamSpawn(playerUnit.Unit, unitController, teamId);
            }
        }

        public void TeleportAssignedPlayer(int connectionId, int teamId)
        {
            if (GameServices.PlayerUnits == null)
                return;

            for (int i = 0; i < GameServices.PlayerUnits.playerUnits.Count; i++)
            {
                var playerUnit = GameServices.PlayerUnits.playerUnits[i];
                if (playerUnit.ConnectionId != connectionId || playerUnit.Unit == null)
                    continue;

                var unitController = playerUnit.Unit.GetComponent<UnitController>();
                if (unitController == null)
                    return;

                TeleportToTeamSpawn(playerUnit.Unit, unitController, teamId);
                return;
            }
        }

        private int FindLeastPopulatedTeam()
        {
            int teamCount = host.TeamCount;
            var teamPopulation = new int[teamCount];

            foreach (int assignedTeam in host.AssignedTeamIds)
            {
                if (assignedTeam < 0 || assignedTeam >= teamCount)
                    continue;
                teamPopulation[assignedTeam]++;
            }

            int selectedTeam = 0;
            int lowestCount = teamPopulation[0];
            for (int team = 1; team < teamCount; team++)
            {
                if (teamPopulation[team] < lowestCount)
                {
                    lowestCount = teamPopulation[team];
                    selectedTeam = team;
                }
            }

            return selectedTeam;
        }

        private void TeleportToTeamSpawn(GameObject unitObject, UnitController unitController, int teamId)
        {
            int clampedTeamId = Mathf.Clamp(teamId, 0, host.TeamCount - 1);
            var spawn = host.GetTeamSpawn(clampedTeamId);
            if (spawn == null)
            {
                UnityEngine.Debug.LogError($"[SkirmishGameManager] Team spawn for team {clampedTeamId} is missing.");
                return;
            }

            unitController.SetTeam(clampedTeamId);
            unitController.InterruptAction();
            unitController.SetHealth(unitController.maxHealth);
            unitController.SetShield(unitController.maxShield);

            if (unitObject.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = spawn.position;
                rb.rotation = spawn.rotation;
            }

            unitObject.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        }
    }
}
