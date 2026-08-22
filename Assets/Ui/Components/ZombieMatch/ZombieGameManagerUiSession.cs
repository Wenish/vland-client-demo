using System;
using System.Collections.Generic;
using Mirror;
using MyGame.Events;

namespace ShadowInfection.UI.ZombieMatch
{
    internal sealed class ZombieGameManagerUiSession : IZombieMatchUiSession, IZombieMatchCommands
    {
        private readonly ZombieGameManager manager;

        public ZombieGameManagerUiSession(ZombieGameManager manager)
        {
            this.manager = manager;
        }

        public bool TryGetSnapshot(out ZombieMatchUiSnapshot snapshot)
        {
            if (manager == null)
            {
                snapshot = default;
                return false;
            }

            snapshot = new ZombieMatchUiSnapshot(
                manager.IsGameOver,
                manager.IsAutoReturnToLobbyEnabled,
                manager.ReturnToLobbyCountdownSeconds,
                manager.CurrentWave,
                manager.CurrentWaveKilledPercent,
                CopyRows(manager.LeaderboardEntries));
            return true;
        }

        public bool TryReturnToLobby()
        {
            if (!NetworkServer.active || manager == null)
                return false;

            manager.ServerReturnToLobby();
            return true;
        }

        private static ZombieLeaderboardRow[] CopyRows(
            IReadOnlyList<ZombieGameManager.ZombieLeaderboardEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return Array.Empty<ZombieLeaderboardRow>();

            var rows = new ZombieLeaderboardRow[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                rows[i] = new ZombieLeaderboardRow(
                    entry.ConnectionId,
                    entry.PlayerName,
                    entry.Points,
                    entry.Kills,
                    entry.Deaths,
                    entry.GoldGathered,
                    entry.IsConnected);
            }

            return rows;
        }
    }
}
