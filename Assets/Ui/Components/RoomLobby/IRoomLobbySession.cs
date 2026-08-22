using System.Collections.Generic;

namespace ShadowInfection.UI.RoomLobby
{
    internal readonly struct RoomLobbyState
    {
        public readonly bool IsInRoomScene;
        public readonly bool CanToggleReady;
        public readonly bool LocalIsReady;
        public readonly bool HasSelectedCharacter;
        public readonly bool CanEditCharacter;
        public readonly int ReadyCount;
        public readonly IReadOnlyList<PlayerRowVm> Players;
        public readonly IReadOnlyList<CharacterRowVm> Characters;
        public readonly bool CanCreateCharacter;

        public RoomLobbyState(
            bool isInRoomScene,
            bool canToggleReady,
            bool localIsReady,
            bool hasSelectedCharacter,
            bool canEditCharacter,
            int readyCount,
            IReadOnlyList<PlayerRowVm> players,
            IReadOnlyList<CharacterRowVm> characters,
            bool canCreateCharacter)
        {
            IsInRoomScene = isInRoomScene;
            CanToggleReady = canToggleReady;
            LocalIsReady = localIsReady;
            HasSelectedCharacter = hasSelectedCharacter;
            CanEditCharacter = canEditCharacter;
            ReadyCount = readyCount;
            Players = players;
            Characters = characters;
            CanCreateCharacter = canCreateCharacter;
        }
    }

    internal interface IRoomLobbySession
    {
        bool TryGetState(out RoomLobbyState state);

        void ToggleLocalReady();

        bool SelectCharacter(string characterId);

        bool CreateCharacter(string name, CharacterGender gender);

        bool DeleteCharacter(string characterId);
    }
}
