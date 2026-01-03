using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.RoomLobby
{
    internal sealed class MirrorRoomLobbyPresenter
    {
        private readonly RoomLobbyView view;
        private readonly float refreshIntervalSeconds;

        private readonly List<RoomLobbyView.PlayerRowVm> players = new List<RoomLobbyView.PlayerRowVm>(16);
        private readonly Dictionary<uint, string> nameCache = new Dictionary<uint, string>(16);

        private float nextRefreshTime;
        private bool enabled;

        private NetworkRoomPlayer cachedLocalRoomPlayer;
        private bool cachedLocalReady;
        private int cachedHash;

        public MirrorRoomLobbyPresenter(RoomLobbyView view, float refreshIntervalSeconds)
        {
            this.view = view;
            this.refreshIntervalSeconds = Math.Max(0.05f, refreshIntervalSeconds);

            view.ReadyButtonClicked += OnReadyButtonClicked;

            view.BindPlayerList(MakePlayerRow, BindPlayerRow, players);

            nextRefreshTime = 0;
        }

        public void SetEnabled(bool value)
        {
            enabled = value;
        }

        public void Tick(float unscaledTime)
        {
            if (!enabled)
                return;

            if (unscaledTime < nextRefreshTime)
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

            bool canToggleReady = NetworkClient.active && cachedLocalRoomPlayer != null && cachedLocalRoomPlayer.isLocalPlayer;
            view.SetReadyButtonEnabled(canToggleReady);

            cachedLocalReady = cachedLocalRoomPlayer != null && cachedLocalRoomPlayer.readyToBegin;
            view.SetLocalReadyState(cachedLocalReady);

            var snapshot = BuildSnapshot(roomManager, cachedLocalRoomPlayer);
            int snapshotHash = ComputeSnapshotHash(snapshot);

            int readyCount = snapshot.Count(p => p.ready);
            view.SetSubtitle($"{snapshot.Count} player(s) · {readyCount} ready");

            if (snapshotHash == cachedHash)
                return;

            cachedHash = snapshotHash;
            players.Clear();
            players.AddRange(snapshot);
            view.RefreshPlayers();
        }

        private void OnReadyButtonClicked()
        {
            var roomManager = NetworkManager.singleton as NetworkRoomManager;
            if (roomManager == null)
                return;

            var localRoomPlayer = cachedLocalRoomPlayer ?? FindLocalRoomPlayer(roomManager);
            if (localRoomPlayer == null)
                return;

            if (!NetworkClient.active || !localRoomPlayer.isLocalPlayer)
                return;

            localRoomPlayer.CmdChangeReadyState(!localRoomPlayer.readyToBegin);
        }

        private static NetworkRoomPlayer FindLocalRoomPlayer(NetworkRoomManager roomManager)
        {
            if (!NetworkClient.active)
                return null;

            // NetworkClient.localPlayer should be the room player while in the room scene.
            if (NetworkClient.localPlayer != null)
                return NetworkClient.localPlayer.GetComponent<NetworkRoomPlayer>();

            // Fallback: search room slots for an isLocalPlayer room player.
            foreach (var player in roomManager.roomSlots)
            {
                if (player != null && player.isLocalPlayer)
                    return player;
            }

            return null;
        }

        private List<RoomLobbyView.PlayerRowVm> BuildSnapshot(NetworkRoomManager roomManager, NetworkRoomPlayer local)
        {
            var list = new List<RoomLobbyView.PlayerRowVm>(roomManager.roomSlots.Count);

            foreach (var player in roomManager.roomSlots)
            {
                if (player == null)
                    continue;

                uint netId = player.netId;
                int index = player.index;
                bool ready = player.readyToBegin;
                bool isLocal = local != null && ReferenceEquals(player, local);

                string displayName = null;
                nameCache.TryGetValue(netId, out displayName);

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = TryResolveDisplayName(player);
                    nameCache[netId] = displayName;
                }

                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = $"Player {index + 1}";

                list.Add(new RoomLobbyView.PlayerRowVm(netId, index, displayName, ready, isLocal));
            }

            list.Sort((a, b) => a.index.CompareTo(b.index));
            return list;
        }

        private static string TryResolveDisplayName(NetworkRoomPlayer player)
        {
            // Prefer any common string field/property on any component.
            // This keeps the UI decoupled from game-specific player metadata.
            var components = player.GetComponents<UnityEngine.Component>();
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

        private static int ComputeSnapshotHash(List<RoomLobbyView.PlayerRowVm> snapshot)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < snapshot.Count; i++)
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

        private static VisualElement MakePlayerRow()
        {
            var row = new VisualElement();
            row.AddToClassList("room-lobby__row");

            var left = new VisualElement();
            left.AddToClassList("room-lobby__row-left");

            var dot = new VisualElement { name = "dot" };
            dot.AddToClassList("room-lobby__dot");

            var name = new Label { name = "name" };
            name.AddToClassList("room-lobby__name");

            var tag = new Label { name = "tag" };
            tag.AddToClassList("room-lobby__tag");

            left.Add(dot);
            left.Add(name);
            left.Add(tag);

            var state = new Label { name = "state" };
            state.AddToClassList("room-lobby__state");

            row.Add(left);
            row.Add(state);
            return row;
        }

        private static void BindPlayerRow(VisualElement element, int index)
        {
            if (element == null)
                return;

            var ctx = element.userData as BindContext;
            if (ctx == null)
            {
                ctx = new BindContext(element);
                element.userData = ctx;
            }

            if (ctx.ItemsSource == null || index < 0 || index >= ctx.ItemsSource.Count)
                return;

            var vm = ctx.ItemsSource[index];

            ctx.Row.RemoveFromClassList("room-lobby__row--local");
            if (vm.isLocal)
                ctx.Row.AddToClassList("room-lobby__row--local");

            ctx.Name.text = vm.displayName;
            ctx.Tag.text = vm.isLocal ? "(You)" : string.Empty;

            ctx.Dot.RemoveFromClassList("room-lobby__dot--ready");
            if (vm.ready)
                ctx.Dot.AddToClassList("room-lobby__dot--ready");

            ctx.State.RemoveFromClassList("room-lobby__state--ready");
            if (vm.ready)
                ctx.State.AddToClassList("room-lobby__state--ready");

            ctx.State.text = vm.ready ? "Ready" : "Not Ready";
        }

        private sealed class BindContext
        {
            public readonly VisualElement Row;
            public readonly VisualElement Dot;
            public readonly Label Name;
            public readonly Label Tag;
            public readonly Label State;
            public readonly IList<RoomLobbyView.PlayerRowVm> ItemsSource;

            public BindContext(VisualElement row)
            {
                Row = row;
                Dot = row.Q<VisualElement>("dot");
                Name = row.Q<Label>("name");
                Tag = row.Q<Label>("tag");
                State = row.Q<Label>("state");

                // ListView doesn't pass itemsSource into bindItem; stash it from the ListView.
                var listView = row.GetFirstAncestorOfType<ListView>();
                ItemsSource = listView?.itemsSource as IList<RoomLobbyView.PlayerRowVm>;
            }
        }
    }
}
