using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace ShadowInfection.UI.RoomLobby
{
    internal sealed class MirrorRoomLobbySession : IRoomLobbySession
    {
        private static readonly string[] DisplayNameCandidates =
        {
            "displayName",
            "DisplayName",
            "playerName",
            "PlayerName",
            "username",
            "Username",
            "nickName",
            "NickName",
            "characterName",
            "CharacterName",
        };

        private readonly Dictionary<uint, string> nameCache = new Dictionary<uint, string>(16);
        private readonly List<PlayerRowVm> players = new List<PlayerRowVm>(16);

        public bool TryGetState(out RoomLobbyState state)
        {
            var roomManager = NetworkManager.singleton as NetworkRoomManager;
            if (roomManager == null)
            {
                state = default;
                return false;
            }

            if (!Utils.IsSceneActive(roomManager.RoomScene))
            {
                state = new RoomLobbyState(
                    isInRoomScene: false,
                    canToggleReady: false,
                    localIsReady: false,
                    readyCount: 0,
                    players: Array.Empty<PlayerRowVm>());
                return true;
            }

            var local = FindLocalRoomPlayer(roomManager);
            var canToggleReady = NetworkClient.active && local != null && local.isLocalPlayer;

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
                localIsReady: local != null && local.readyToBegin,
                readyCount: readyCount,
                players: players.ToArray());
            return true;
        }

        public void ToggleLocalReady()
        {
            var roomManager = NetworkManager.singleton as NetworkRoomManager;
            if (roomManager == null || !NetworkClient.active)
                return;

            var local = FindLocalRoomPlayer(roomManager);
            if (local == null || !local.isLocalPlayer)
                return;

            local.CmdChangeReadyState(!local.readyToBegin);
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
            if (nameCache.TryGetValue(netId, out var cached) && !string.IsNullOrWhiteSpace(cached))
                return cached;

            var displayName = TryResolveDisplayName(player);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                nameCache[netId] = displayName;
                return displayName;
            }

            return $"Player {player.index + 1}";
        }

        private static string TryResolveDisplayName(NetworkRoomPlayer player)
        {
            if (player is MyNetworkRoomPlayer named && !string.IsNullOrWhiteSpace(named.nickName))
                return named.nickName;

            var components = player.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null)
                    continue;

                var type = component.GetType();
                foreach (var candidate in DisplayNameCandidates)
                {
                    var field = type.GetField(candidate);
                    if (field != null && field.FieldType == typeof(string))
                    {
                        var value = field.GetValue(component) as string;
                        if (!string.IsNullOrWhiteSpace(value))
                            return value;
                    }

                    var prop = type.GetProperty(candidate);
                    if (prop != null && prop.PropertyType == typeof(string) && prop.CanRead)
                    {
                        var value = prop.GetValue(component, null) as string;
                        if (!string.IsNullOrWhiteSpace(value))
                            return value;
                    }
                }
            }

            return null;
        }
    }
}
