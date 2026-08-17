using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace ShadowInfection.UI.RoomLobby
{
    internal sealed class MirrorRoomLobbyPresenter
    {
        private readonly RoomLobbyView view;
        private readonly float refreshIntervalSeconds;

        private readonly List<PlayerRowVm> players = new List<PlayerRowVm>(16);
        private readonly Dictionary<uint, string> nameCache = new Dictionary<uint, string>(16);

        private float nextRefreshTime;
        private bool enabled;

        private NetworkRoomPlayer cachedLocalRoomPlayer;
        private int cachedHash;

        public MirrorRoomLobbyPresenter(RoomLobbyView view, float refreshIntervalSeconds)
        {
            this.view = view;
            this.refreshIntervalSeconds = Math.Max(0.05f, refreshIntervalSeconds);
            view.ReadyButtonClicked += OnReadyButtonClicked;
        }

        public void SetEnabled(bool value)
        {
            enabled = value;
        }

        public void Tick(float unscaledTime)
        {
            if (!enabled || unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = unscaledTime + refreshIntervalSeconds;

            var roomManager = NetworkManager.singleton as NetworkRoomManager;
            if (roomManager == null)
            {
                view.SetVisible(false);
                view.SetSubtitle("No room manager found");
                view.SetReadyButtonEnabled(false);
                return;
            }

            if (!Utils.IsSceneActive(roomManager.RoomScene))
            {
                view.SetVisible(false);
                return;
            }

            view.SetVisible(true);

            cachedLocalRoomPlayer = FindLocalRoomPlayer(roomManager);

            var canToggleReady = NetworkClient.active
                && cachedLocalRoomPlayer != null
                && cachedLocalRoomPlayer.isLocalPlayer;
            view.SetReadyButtonEnabled(canToggleReady);
            view.SetLocalReadyState(cachedLocalRoomPlayer != null && cachedLocalRoomPlayer.readyToBegin);

            var snapshot = BuildSnapshot(roomManager, cachedLocalRoomPlayer);
            var readyCount = snapshot.Count(p => p.ready);
            view.SetSubtitle($"{snapshot.Count} player(s) · {readyCount} ready");

            var snapshotHash = ComputeSnapshotHash(snapshot);
            if (snapshotHash == cachedHash)
                return;

            cachedHash = snapshotHash;
            players.Clear();
            players.AddRange(snapshot);
            view.SetPlayers(players);
        }

        private void OnReadyButtonClicked()
        {
            var roomManager = NetworkManager.singleton as NetworkRoomManager;
            if (roomManager == null)
                return;

            var localRoomPlayer = cachedLocalRoomPlayer ?? FindLocalRoomPlayer(roomManager);
            if (localRoomPlayer == null || !NetworkClient.active || !localRoomPlayer.isLocalPlayer)
                return;

            localRoomPlayer.CmdChangeReadyState(!localRoomPlayer.readyToBegin);
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

        private List<PlayerRowVm> BuildSnapshot(NetworkRoomManager roomManager, NetworkRoomPlayer local)
        {
            var list = new List<PlayerRowVm>(roomManager.roomSlots.Count);

            foreach (var player in roomManager.roomSlots)
            {
                if (player == null)
                    continue;

                var netId = player.netId;
                var index = player.index;
                var ready = player.readyToBegin;
                var isLocal = local != null && ReferenceEquals(player, local);

                if (!nameCache.TryGetValue(netId, out var displayName) || string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = TryResolveDisplayName(player);
                    nameCache[netId] = displayName;
                }

                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = $"Player {index + 1}";

                list.Add(new PlayerRowVm(netId, index, displayName, ready, isLocal));
            }

            list.Sort((a, b) => a.index.CompareTo(b.index));
            return list;
        }

        private static string TryResolveDisplayName(NetworkRoomPlayer player)
        {
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

        private static int ComputeSnapshotHash(List<PlayerRowVm> snapshot)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < snapshot.Count; i++)
                {
                    hash = (hash * 31) + (int)snapshot[i].netId;
                    hash = (hash * 31) + snapshot[i].index;
                    hash = (hash * 31) + (snapshot[i].ready ? 1 : 0);
                    hash = (hash * 31) + (snapshot[i].isLocal ? 1 : 0);
                    hash = (hash * 31) + (snapshot[i].displayName?.GetHashCode() ?? 0);
                }

                return hash;
            }
        }
    }
}
