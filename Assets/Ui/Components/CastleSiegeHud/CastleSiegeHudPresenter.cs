using System.Threading;
using MessagePipe;
using Mirror;
using MyGame.Events;
using R3;
using ShadowInfection.Match;

namespace ShadowInfection.UI.CastleSiegeHud
{
    internal sealed class CastleSiegeHudPresenter
    {
        private readonly ICastleSiegeUiSession session;
        private readonly IMatchCommands commands;
        private readonly ISubscriber<MatchLifecycleStateChangedEvent> lifecycleChanged;
        private readonly ISubscriber<MatchTeamSelectionLockChangedEvent> teamLockChanged;
        private readonly ISubscriber<MatchEndedEvent> matchEnded;
        private readonly ISubscriber<ReturnToLobbyCountdownEvent> lobbyCountdown;
        private readonly ISubscriber<MatchPlayerTeamAssignedEvent> teamAssigned;
        private readonly ISubscriber<MyPlayerUnitSpawnedEvent> myUnitSpawned;

        private CastleSiegeHudView view;
        private R3.DisposableBag subscriptions;
        private bool hideWhenClientInactive;
        private string lastSignature;

        public CastleSiegeHudPresenter(
            ICastleSiegeUiSession session,
            IMatchCommands commands,
            ISubscriber<MatchLifecycleStateChangedEvent> lifecycleChanged,
            ISubscriber<MatchTeamSelectionLockChangedEvent> teamLockChanged,
            ISubscriber<MatchEndedEvent> matchEnded,
            ISubscriber<ReturnToLobbyCountdownEvent> lobbyCountdown,
            ISubscriber<MatchPlayerTeamAssignedEvent> teamAssigned,
            ISubscriber<MyPlayerUnitSpawnedEvent> myUnitSpawned)
        {
            this.session = session;
            this.commands = commands;
            this.lifecycleChanged = lifecycleChanged;
            this.teamLockChanged = teamLockChanged;
            this.matchEnded = matchEnded;
            this.lobbyCountdown = lobbyCountdown;
            this.teamAssigned = teamAssigned;
            this.myUnitSpawned = myUnitSpawned;
        }

        public void Bind(CastleSiegeHudView nextView, bool hideWhenInactive, CancellationToken token)
        {
            Unbind();
            view = nextView;
            hideWhenClientInactive = hideWhenInactive;
            if (view == null)
                return;

            view.TeamChosen += OnTeamChosen;
            Render(force: true);

            subscriptions.Add(lifecycleChanged.Subscribe(_ => Render(force: true)));
            subscriptions.Add(teamLockChanged.Subscribe(_ => Render(force: true)));
            subscriptions.Add(matchEnded.Subscribe(_ => Render(force: true)));
            subscriptions.Add(lobbyCountdown.Subscribe(_ => Render(force: false)));
            subscriptions.Add(teamAssigned.Subscribe(_ => Render(force: true)));
            subscriptions.Add(myUnitSpawned.Subscribe(_ => Render(force: true)));
            subscriptions.Add(
                Observable.EveryUpdate(UnityFrameProvider.Update, token)
                    .Subscribe(_ => Render(force: false)));
        }

        public void Unbind()
        {
            if (view != null)
                view.TeamChosen -= OnTeamChosen;

            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            view = null;
            lastSignature = null;
        }

        private void OnTeamChosen(int teamId)
        {
            commands.TryChooseLocalTeam(teamId);
        }

        private void Render(bool force)
        {
            if (view == null)
                return;

            bool isVisible = !hideWhenClientInactive || NetworkClient.active;
            view.SetVisible(isVisible);
            if (!isVisible)
                return;

            if (!session.TryGetSnapshot(out var snapshot))
                return;

            if (!force && snapshot.Signature == lastSignature)
                return;

            lastSignature = snapshot.Signature;
            view.Render(snapshot);
        }
    }
}
