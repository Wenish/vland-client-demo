using System.Threading;
using MessagePipe;
using Mirror;
using MyGame.Events;
using R3;
using ShadowInfection.UI.ZombieMatch;

namespace ShadowInfection.UI.ZombieGameOver
{
    internal sealed class ZombieGameOverPresenter
    {
        private readonly IZombieMatchUiSession session;
        private readonly IZombieMatchCommands commands;
        private readonly ISubscriber<ZombieGameOverEvent> gameOver;
        private readonly ISubscriber<ZombieReturnToLobbyCountdownEvent> countdown;

        private ZombieGameOverView view;
        private R3.DisposableBag subscriptions;
        private bool hasSnapshot;
        private bool isGameOver;
        private bool isAutoReturn;
        private float countdownSeconds;

        public ZombieGameOverPresenter(
            IZombieMatchUiSession session,
            IZombieMatchCommands commands,
            ISubscriber<ZombieGameOverEvent> gameOver,
            ISubscriber<ZombieReturnToLobbyCountdownEvent> countdown)
        {
            this.session = session;
            this.commands = commands;
            this.gameOver = gameOver;
            this.countdown = countdown;
        }

        public void Bind(ZombieGameOverView nextView, CancellationToken token)
        {
            Unbind();
            view = nextView;
            if (view == null)
                return;

            view.ReturnToLobbyClicked += OnReturnToLobbyClicked;
            TryPullSnapshot();
            Render();

            subscriptions.Add(gameOver.Subscribe(OnGameOver));
            subscriptions.Add(countdown.Subscribe(OnCountdown));
            subscriptions.Add(
                Observable.EveryUpdate(UnityFrameProvider.Update, token)
                    .Subscribe(_ =>
                    {
                        if (!hasSnapshot)
                            TryPullSnapshot();
                    }));
        }

        public void Unbind()
        {
            if (view != null)
                view.ReturnToLobbyClicked -= OnReturnToLobbyClicked;

            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            view = null;
            hasSnapshot = false;
            isGameOver = false;
            isAutoReturn = false;
            countdownSeconds = 0f;
        }

        private void TryPullSnapshot()
        {
            if (session == null || !session.TryGetSnapshot(out var snapshot))
                return;

            hasSnapshot = true;
            isGameOver = snapshot.IsGameOver;
            isAutoReturn = snapshot.IsAutoReturnActive;
            countdownSeconds = snapshot.CountdownSeconds;
            Render();
        }

        private void OnGameOver(ZombieGameOverEvent evt)
        {
            hasSnapshot = true;
            isGameOver = evt != null && evt.IsGameOver;
            if (!isGameOver)
            {
                isAutoReturn = false;
                countdownSeconds = 0f;
            }

            Render();
        }

        private void OnCountdown(ZombieReturnToLobbyCountdownEvent evt)
        {
            hasSnapshot = true;
            if (evt != null)
            {
                isAutoReturn = evt.IsAutoReturnActive;
                countdownSeconds = evt.CountdownSeconds;
            }

            Render();
        }

        private void OnReturnToLobbyClicked()
        {
            commands?.TryReturnToLobby();
        }

        private void Render()
        {
            if (view == null)
                return;

            view.SetPanelVisible(isGameOver);
            view.SetCountdownVisible(isGameOver && isAutoReturn, countdownSeconds);
            view.SetReturnButtonVisible(NetworkServer.active && isGameOver);
        }
    }
}
