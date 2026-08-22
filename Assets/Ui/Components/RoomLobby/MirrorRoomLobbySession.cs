using System;
using System.Collections.Generic;
using Mirror;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.UI.RoomLobby
{
    internal sealed class MirrorRoomLobbySession : IRoomLobbySession
    {
        private readonly Dictionary<uint, string> nameCache = new Dictionary<uint, string>(16);
        private readonly List<PlayerRowVm> players = new List<PlayerRowVm>(16);
        private readonly List<CharacterRowVm> characters = new List<CharacterRowVm>(8);

        public bool TryGetState(out RoomLobbyState state)
        {
            var roomManager = NetworkManager.singleton as NetworkRoomManager;
            if (roomManager == null)
            {
                state = default;
                return false;
            }

            BuildCharacterRows();

            if (!Utils.IsSceneActive(roomManager.RoomScene))
            {
                state = new RoomLobbyState(
                    isInRoomScene: false,
                    canToggleReady: false,
                    localIsReady: false,
                    hasSelectedCharacter: false,
                    canEditCharacter: false,
                    readyCount: 0,
                    players: Array.Empty<PlayerRowVm>(),
                    characters: characters.ToArray(),
                    canCreateCharacter: GameServices.Characters?.CanCreateCharacter() ?? false);
                return true;
            }

            var local = FindLocalRoomPlayer(roomManager) as MyNetworkRoomPlayer;
            var hasSelected = local != null && local.HasSelectedCharacter;
            var localReady = local != null && local.readyToBegin;
            var canToggleReady = NetworkClient.active
                && local != null
                && local.isLocalPlayer
                && (localReady || hasSelected);
            var canEditCharacter = NetworkClient.active
                && local != null
                && local.isLocalPlayer
                && !localReady;

            players.Clear();
            var readyCount = 0;

            foreach (var player in roomManager.roomSlots)
            {
                if (player == null)
                    continue;

                var isLocal = local != null && ReferenceEquals(player, local);
                var ready = player.readyToBegin;
                if (ready)
                    readyCount++;

                players.Add(new PlayerRowVm(
                    player.netId,
                    player.index,
                    ResolveDisplayName(player),
                    ready,
                    isLocal));
            }

            players.Sort((a, b) => a.index.CompareTo(b.index));

            state = new RoomLobbyState(
                isInRoomScene: true,
                canToggleReady: canToggleReady,
                localIsReady: localReady,
                hasSelectedCharacter: hasSelected,
                canEditCharacter: canEditCharacter,
                readyCount: readyCount,
                players: players.ToArray(),
                characters: characters.ToArray(),
                canCreateCharacter: GameServices.Characters?.CanCreateCharacter() ?? false);
            return true;
        }

        public void ToggleLocalReady()
        {
            var roomManager = NetworkManager.singleton as NetworkRoomManager;
            if (roomManager == null || !NetworkClient.active)
                return;

            var local = FindLocalRoomPlayer(roomManager) as MyNetworkRoomPlayer;
            if (local == null || !local.isLocalPlayer)
                return;

            if (!local.readyToBegin && !local.HasSelectedCharacter)
                return;

            local.CmdChangeReadyState(!local.readyToBegin);
        }

        public bool SelectCharacter(string characterId)
        {
            var roomManager = NetworkManager.singleton as NetworkRoomManager;
            if (roomManager == null || !NetworkClient.active)
                return false;

            var local = FindLocalRoomPlayer(roomManager) as MyNetworkRoomPlayer;
            if (local == null || !local.isLocalPlayer || local.readyToBegin)
                return false;

            var charactersManager = GameServices.Characters;
            if (charactersManager == null || !charactersManager.SelectActive(characterId))
                return false;

            local.RequestSelectCharacter(charactersManager.GetActive());
            return true;
        }

        public bool CreateCharacter(string name, CharacterGender gender)
        {
            var roomManager = NetworkManager.singleton as NetworkRoomManager;
            if (roomManager == null || !NetworkClient.active)
                return false;

            var local = FindLocalRoomPlayer(roomManager) as MyNetworkRoomPlayer;
            if (local == null || !local.isLocalPlayer || local.readyToBegin)
                return false;

            var charactersManager = GameServices.Characters;
            if (charactersManager == null)
                return false;

            var created = charactersManager.CreateCharacter(name, gender);
            if (created == null)
                return false;

            local.RequestSelectCharacter(created);
            return true;
        }

        public bool DeleteCharacter(string characterId)
        {
            var roomManager = NetworkManager.singleton as NetworkRoomManager;
            if (roomManager == null || !NetworkClient.active)
                return false;

            var local = FindLocalRoomPlayer(roomManager) as MyNetworkRoomPlayer;
            if (local == null || !local.isLocalPlayer || local.readyToBegin)
                return false;

            var charactersManager = GameServices.Characters;
            if (charactersManager == null)
                return false;

            bool wasLobbySelected = local.HasSelectedCharacter
                && local.selectedCharacterId == characterId;

            if (!charactersManager.DeleteCharacter(characterId))
                return false;

            if (wasLobbySelected)
                local.RequestClearCharacterSelection();

            return true;
        }

        private void BuildCharacterRows()
        {
            characters.Clear();
            var manager = GameServices.Characters;
            if (manager == null)
                return;

            var roomManager = NetworkManager.singleton as NetworkRoomManager;
            var local = roomManager != null ? FindLocalRoomPlayer(roomManager) as MyNetworkRoomPlayer : null;
            // Only highlight the character confirmed for this lobby session (synced),
            // not merely the last locally active save slot.
            var selectedId = local != null && local.HasSelectedCharacter
                ? local.selectedCharacterId
                : null;

            foreach (var character in manager.Characters)
            {
                if (character == null)
                    continue;

                characters.Add(new CharacterRowVm(
                    character.Id,
                    character.Name,
                    character.Gender == CharacterGender.Female ? "Female" : "Male",
                    character.Id == selectedId));
            }
        }

        private static NetworkRoomPlayer FindLocalRoomPlayer(NetworkRoomManager roomManager)
        {
            if (!NetworkClient.active)
                return null;

            if (NetworkClient.localPlayer != null)
                return NetworkClient.localPlayer.GetComponent<NetworkRoomPlayer>();

            foreach (var player in roomManager.roomSlots)
            {
                if (player != null && player.isLocalPlayer)
                    return player;
            }

            return null;
        }

        private string ResolveDisplayName(NetworkRoomPlayer player)
        {
            var netId = player.netId;
            if (player is MyNetworkRoomPlayer named && !string.IsNullOrWhiteSpace(named.characterName))
            {
                nameCache[netId] = named.characterName;
                return named.characterName;
            }

            if (nameCache.TryGetValue(netId, out var cached) && !string.IsNullOrWhiteSpace(cached))
                return cached;

            return $"Player {player.index + 1}";
        }
    }
}
