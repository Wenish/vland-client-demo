using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.Zombie
{
    public sealed class ZombieLeaderboardService
    {
        public const int KillPointsReward = 25;

        private readonly SyncList<ZombieGameManager.ZombieLeaderboardEntry> entries;

        public ZombieLeaderboardService(SyncList<ZombieGameManager.ZombieLeaderboardEntry> entries)
        {
            this.entries = entries;
        }

        public void Reset()
        {
            entries.Clear();
            ReconcileConnectivity();
        }

        public void ReconcileConnectivity()
        {
            var playerUnits = GameServices.PlayerUnits;
            if (playerUnits == null)
                return;

            var activeHumanConnectionIds = new HashSet<int>();
            for (int i = 0; i < playerUnits.playerUnits.Count; i++)
            {
                var playerUnit = playerUnits.playerUnits[i];
                if (playerUnit.ConnectionId < 0 || playerUnit.Unit == null)
                    continue;

                activeHumanConnectionIds.Add(playerUnit.ConnectionId);
                EnsureEntry(playerUnit.ConnectionId, playerUnit.Unit.GetComponent<UnitController>(), true);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var row = entries[i];
                bool isConnected = activeHumanConnectionIds.Contains(row.ConnectionId);
                if (row.IsConnected == isConnected)
                    continue;

                row.IsConnected = isConnected;
                entries[i] = row;
            }

            Sort();
        }

        public void OnPlayerSpawned(int connectionId, UnitController unit)
        {
            EnsureEntry(connectionId, unit, true);
        }

        public void CreditDamage(UnitController attacker, int appliedDamage)
        {
            if (attacker == null || attacker.unitType != UnitType.Player || appliedDamage <= 0)
                return;

            if (!TryGetConnectionId(attacker, out int connectionId))
                return;

            if (!TryGetIndex(connectionId, out int rowIndex))
                return;

            var row = entries[rowIndex];
            row.Points += appliedDamage;
            row.PlayerName = ResolveDisplayName(connectionId, attacker, row.PlayerName);
            row.IsConnected = true;
            entries[rowIndex] = row;
            Sort();
        }

        public void CreditGold(UnitController player, int goldAmount)
        {
            if (player == null || player.unitType != UnitType.Player || goldAmount <= 0)
                return;

            if (!TryGetConnectionId(player, out int connectionId))
                return;

            if (!TryGetIndex(connectionId, out int rowIndex))
                return;

            var row = entries[rowIndex];
            row.GoldGathered += goldAmount;
            row.PlayerName = ResolveDisplayName(connectionId, player, row.PlayerName);
            row.IsConnected = true;
            entries[rowIndex] = row;
            Sort();
        }

        public void CreditKill(UnitController killer)
        {
            if (killer == null || killer.unitType != UnitType.Player)
                return;

            if (!TryGetConnectionId(killer, out int connectionId))
                return;

            if (!TryGetIndex(connectionId, out int rowIndex))
                return;

            var row = entries[rowIndex];
            row.Kills += 1;
            row.Points += KillPointsReward;
            row.PlayerName = ResolveDisplayName(connectionId, killer, row.PlayerName);
            row.IsConnected = true;
            entries[rowIndex] = row;
            Sort();
        }

        public void CreditDeath(UnitController deadUnit)
        {
            if (deadUnit == null || deadUnit.unitType != UnitType.Player)
                return;

            if (!TryGetConnectionId(deadUnit, out int connectionId))
                return;

            if (!TryGetIndex(connectionId, out int rowIndex))
                return;

            var row = entries[rowIndex];
            row.Deaths += 1;
            row.PlayerName = ResolveDisplayName(connectionId, deadUnit, row.PlayerName);
            row.IsConnected = true;
            entries[rowIndex] = row;
            Sort();
        }

        public void EnsureEntry(int connectionId, UnitController playerUnit, bool isConnected)
        {
            if (connectionId < 0)
                return;

            string resolvedName = playerUnit != null
                ? ResolveDisplayName(connectionId, playerUnit, $"Player {connectionId}")
                : null;

            for (int i = 0; i < entries.Count; i++)
            {
                var current = entries[i];
                if (current.ConnectionId != connectionId)
                    continue;

                bool changed = false;
                if (!string.IsNullOrWhiteSpace(resolvedName)
                    && !string.Equals(current.PlayerName, resolvedName, StringComparison.Ordinal))
                {
                    current.PlayerName = resolvedName;
                    changed = true;
                }

                if (current.IsConnected != isConnected)
                {
                    current.IsConnected = isConnected;
                    changed = true;
                }

                if (changed)
                    entries[i] = current;
                return;
            }

            entries.Add(new ZombieGameManager.ZombieLeaderboardEntry
            {
                ConnectionId = connectionId,
                PlayerName = ResolveDisplayName(connectionId, playerUnit, $"Player {connectionId}"),
                Points = 0,
                Kills = 0,
                Deaths = 0,
                GoldGathered = 0,
                IsConnected = isConnected
            });
            Sort();
        }

        private bool TryGetIndex(int connectionId, out int index)
        {
            EnsureEntry(connectionId, null, false);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].ConnectionId != connectionId)
                    continue;

                index = i;
                return true;
            }

            index = -1;
            return false;
        }

        private static bool TryGetConnectionId(UnitController playerUnit, out int connectionId)
        {
            connectionId = default;
            var playerUnits = GameServices.PlayerUnits;
            if (playerUnit == null || playerUnits == null)
                return false;

            for (int i = 0; i < playerUnits.playerUnits.Count; i++)
            {
                var playerUnitEntry = playerUnits.playerUnits[i];
                if (playerUnitEntry.ConnectionId < 0 || playerUnitEntry.Unit == null)
                    continue;

                if (playerUnitEntry.Unit != playerUnit.gameObject)
                    continue;

                connectionId = playerUnitEntry.ConnectionId;
                return true;
            }

            return false;
        }

        private void Sort()
        {
            if (entries.Count <= 1)
                return;

            var orderedRows = entries
                .OrderByDescending(entry => entry.Points)
                .ThenByDescending(entry => entry.Kills)
                .ThenBy(entry => entry.Deaths)
                .ThenBy(entry => entry.ConnectionId)
                .ToList();

            for (int i = 0; i < orderedRows.Count; i++)
            {
                var lhs = orderedRows[i];
                var rhs = entries[i];
                bool same = lhs.ConnectionId == rhs.ConnectionId
                    && lhs.Points == rhs.Points
                    && lhs.Kills == rhs.Kills
                    && lhs.Deaths == rhs.Deaths
                    && lhs.GoldGathered == rhs.GoldGathered
                    && lhs.IsConnected == rhs.IsConnected
                    && string.Equals(lhs.PlayerName, rhs.PlayerName, StringComparison.Ordinal);
                if (same)
                    continue;

                entries.Clear();
                for (int j = 0; j < orderedRows.Count; j++)
                    entries.Add(orderedRows[j]);
                return;
            }
        }

        private static string ResolveDisplayName(int connectionId, UnitController playerUnit, string fallback)
        {
            if (playerUnit != null && !string.IsNullOrWhiteSpace(playerUnit.unitName))
                return playerUnit.unitName;

            if (!string.IsNullOrWhiteSpace(fallback))
                return fallback;

            return $"Player {connectionId}";
        }
    }
}
