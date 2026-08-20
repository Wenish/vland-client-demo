using System;
using System.Collections.Generic;

namespace ShadowInfection.UI.RoomLobby
{
    internal sealed class MirrorRoomLobbyPresenter
    {
        private readonly RoomLobbySettings settings;
        private readonly IRoomLobbySession session;

        private RoomLobbyView view;
        private float nextRefreshTime;
        private bool enabled;
        private int cachedHash;

        public MirrorRoomLobbyPresenter(RoomLobbySettings settings, IRoomLobbySession session)
        {
            this.settings = settings;
            this.session = session;
        }

        public void Bind(RoomLobbyView nextView)
        {
            if (view != null)
                view.ReadyButtonClicked -= OnReadyButtonClicked;

            view = nextView;

            if (view != null)
                view.ReadyButtonClicked += OnReadyButtonClicked;
        }

        public void Unbind()
        {
            Bind(null);
        }

        public void SetEnabled(bool value)
        {
            enabled = value;
        }

        public void Tick(float unscaledTime)
        {
            if (!enabled || view == null || unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = unscaledTime + settings.RefreshIntervalSeconds;

            if (!session.TryGetState(out var state))
            {
                view.SetVisible(false);
                view.SetSubtitle("No room manager found");
                view.SetReadyButtonEnabled(false);
                return;
            }

            if (!state.IsInRoomScene)
            {
                view.SetVisible(false);
                return;
            }

            view.SetVisible(true);
            view.SetReadyButtonEnabled(state.CanToggleReady);
            view.SetLocalReadyState(state.LocalIsReady);
            view.SetSubtitle($"{state.Players.Count} player(s) · {state.ReadyCount} ready");

            var snapshotHash = ComputeSnapshotHash(state.Players);
            if (snapshotHash == cachedHash)
                return;

            cachedHash = snapshotHash;
            view.SetPlayers(state.Players);
        }

        private void OnReadyButtonClicked()
        {
            session.ToggleLocalReady();
        }

        private static int ComputeSnapshotHash(IReadOnlyList<PlayerRowVm> snapshot)
        {
            if (snapshot == null)
                return 0;

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
