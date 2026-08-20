using System;

namespace ShadowInfection.UI.RoomLobby
{
    internal sealed class RoomLobbySettings
    {
        public float RefreshIntervalSeconds { get; }

        public RoomLobbySettings(float refreshIntervalSeconds)
        {
            RefreshIntervalSeconds = Math.Max(0.05f, refreshIntervalSeconds);
        }
    }
}
