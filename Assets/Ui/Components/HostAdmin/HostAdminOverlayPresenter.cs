using System.Threading;
using MessagePipe;
using Mirror;
using MyGame.Events;
using R3;
using ShadowInfection.Match;

namespace ShadowInfection.UI.HostAdmin
{
    internal sealed class HostAdminOverlayPresenter
    {
        private readonly IMatchUiSession session;
        private readonly IMatchCommands commands;
        private readonly ISubscriber<MatchTeamSelectionLockChangedEvent> teamLockChanged;
        private readonly ISubscriber<MatchLifecycleStateChangedEvent> lifecycleChanged;

        private HostAdminOverlayView view;
        private R3.DisposableBag subscriptions;
        private bool hideWhenNotHost;
        private string lastSignature;

        public HostAdminOverlayPresenter(
            IMatchUiSession session,
            IMatchCommands commands,
            ISubscriber<MatchTeamSelectionLockChangedEvent> teamLockChanged,
            ISubscriber<MatchLifecycleStateChangedEvent> lifecycleChanged)
        {
            this.session = session;
            this.commands = commands;
            this.teamLockChanged = teamLockChanged;
            this.lifecycleChanged = lifecycleChanged;
        }

        public void Bind(HostAdminOverlayView nextView, bool hideWhenHostMissing, CancellationToken token)
        {
            Unbind();
            view = nextView;
            hideWhenNotHost = hideWhenHostMissing;
            if (view == null)
                return;

            view.LockClicked += OnLockClicked;
            view.UnlockClicked += OnUnlockClicked;
            Render(force: true);

            subscriptions.Add(teamLockChanged.Subscribe(_ => Render(force: true)));
            subscriptions.Add(lifecycleChanged.Subscribe(_ => Render(force: true)));
            subscriptions.Add(
                Observable.EveryUpdate(UnityFrameProvider.Update, token)
                    .Subscribe(_ => Render(force: false)));
        }

        public void Unbind()
        {
            if (view != null)
            {
                view.LockClicked -= OnLockClicked;
                view.UnlockClicked -= OnUnlockClicked;
            }

            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            view = null;
            lastSignature = null;
        }

        private void OnLockClicked()
        {
            if (!NetworkServer.active)
                return;

            commands.TryLockTeamSwitching();
            Render(force: true);
        }

        private void OnUnlockClicked()
        {
            if (!NetworkServer.active)
                return;

            commands.TryUnlockTeamSwitching();
            Render(force: true);
        }

        private void Render(bool force)
        {
            if (view == null)
                return;

            bool isHost = NetworkServer.active && NetworkClient.active;
            bool shouldShow = !hideWhenNotHost || isHost;
            view.SetVisible(shouldShow);
            if (!shouldShow)
                return;

            if (!session.TryGetSnapshot(out var snapshot))
            {
                if (force || lastSignature != "missing")
                {
                    lastSignature = "missing";
                    view.RenderMissingManager();
                }

                return;
            }

            if (!force && snapshot.Signature == lastSignature)
                return;

            lastSignature = snapshot.Signature;
            view.Render(snapshot.ManagerTypeName, snapshot.TeamSelectionLocked);
        }
    }
}
