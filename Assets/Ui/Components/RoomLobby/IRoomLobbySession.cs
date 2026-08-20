using System.Collections.Generic;

namespace ShadowInfection.UI.RoomLobby
{
    internal readonly struct RoomLobbyState
    {
        public readonly bool IsInRoomScene;
        public readonly bool CanToggleReady;
        public readonly bool LocalIsReady;
        public readonly int ReadyCount;
        public readonly IReadOnlyList<PlayerRowVm> Players;

        public RoomLobbyState(
            bool isInRoomScene,
            bool canToggleReady,
            bool localIsReady,
            int readyCount,
            IReadOnlyList<PlayerRowVm> players)
        {
            IsInRoomScene = isInRoomScene;
            CanToggleReady = canToggleReady;
            LocalIsReady = localIsReady;
            ReadyCount = readyCount;
            Players = players;
        }
    }

    internal interface IRoomLobbySession
    {
        bool TryGetState(out RoomLobbyState state);

        void ToggleLocalReady();
    }
}
