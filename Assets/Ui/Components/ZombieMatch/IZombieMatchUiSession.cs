using System;
using System.Collections.Generic;
using MyGame.Events;

namespace ShadowInfection.UI.ZombieMatch
{
    internal readonly struct ZombieMatchUiSnapshot
    {
        public readonly bool IsGameOver;
        public readonly bool IsAutoReturnActive;
        public readonly float CountdownSeconds;
        public readonly int Wave;
        public readonly float KillPercent;
        public readonly IReadOnlyList<ZombieLeaderboardRow> Entries;

        public ZombieMatchUiSnapshot(
            bool isGameOver,
            bool isAutoReturnActive,
            float countdownSeconds,
            int wave,
            float killPercent,
            IReadOnlyList<ZombieLeaderboardRow> entries)
        {
            IsGameOver = isGameOver;
            IsAutoReturnActive = isAutoReturnActive;
            CountdownSeconds = countdownSeconds;
            Wave = wave;
            KillPercent = killPercent;
            Entries = entries ?? Array.Empty<ZombieLeaderboardRow>();
        }
    }

    internal interface IZombieMatchUiSession
    {
        bool TryGetSnapshot(out ZombieMatchUiSnapshot snapshot);
    }
}
