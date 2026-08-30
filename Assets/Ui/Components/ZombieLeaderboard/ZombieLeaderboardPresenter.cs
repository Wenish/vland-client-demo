using System;
using System.Collections.Generic;
using System.Threading;
using MessagePipe;
using MyGame.Events;
using R3;
using ShadowInfection.Input;
using ShadowInfection.UI.ZombieMatch;
using UnityEngine.InputSystem;

namespace ShadowInfection.UI.ZombieLeaderboard
{
    internal sealed class ZombieLeaderboardPresenter
    {
        private readonly IZombieMatchUiSession session;
        private readonly ISubscriber<ZombieLeaderboardChangedEvent> leaderboardChanged;
        private readonly ISubscriber<ZombieGameOverEvent> gameOver;
        private readonly IInputReader input;

        private ZombieLeaderboardView view;
        private R3.DisposableBag subscriptions;
        private IReadOnlyList<ZombieLeaderboardRow> rows = Array.Empty<ZombieLeaderboardRow>();
        private bool hasSnapshot;
        private bool isVisible;
        private bool isGameOver;

        public ZombieLeaderboardPresenter(
            IZombieMatchUiSession session,
            ISubscriber<ZombieLeaderboardChangedEvent> leaderboardChanged,
            ISubscriber<ZombieGameOverEvent> gameOver,
            IInputReader input)
        {
            this.session = session;
            this.leaderboardChanged = leaderboardChanged;
            this.gameOver = gameOver;
            this.input = input;
        }

        public void Bind(ZombieLeaderboardView nextView, CancellationToken token)
        {
            Unbind();
            view = nextView;
            if (view == null)
                return;

            TryPullSnapshot();
            ApplyVisibility();
            if (isVisible)
                view.SetRows(rows);

            subscriptions.Add(leaderboardChanged.Subscribe(OnLeaderboardChanged));
            subscriptions.Add(gameOver.Subscribe(OnGameOver));
            subscriptions.Add(
                Observable.EveryUpdate(UnityFrameProvider.Update, token)
                    .Subscribe(_ =>
                    {
                        if (!hasSnapshot)
                            TryPullSnapshot();

                        TickInput();
                    }));
        }

        public void Unbind()
        {
            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            view = null;
            hasSnapshot = false;
            isVisible = false;
            isGameOver = false;
            rows = Array.Empty<ZombieLeaderboardRow>();
        }

        private void TryPullSnapshot()
        {
            if (session == null || !session.TryGetSnapshot(out var snapshot))
                return;

            hasSnapshot = true;
            isGameOver = snapshot.IsGameOver;
            rows = snapshot.Entries;
            if (isGameOver)
                isVisible = true;

            ApplyVisibility();
            if (isVisible)
                view?.SetRows(rows);
        }

        private void OnLeaderboardChanged(ZombieLeaderboardChangedEvent evt)
        {
            hasSnapshot = true;
            rows = evt != null ? evt.Entries : Array.Empty<ZombieLeaderboardRow>();
            if (isVisible)
                view?.SetRows(rows);
        }

        private void OnGameOver(ZombieGameOverEvent evt)
        {
            hasSnapshot = true;
            isGameOver = evt != null && evt.IsGameOver;
            isVisible = isGameOver;
            ApplyVisibility();
            if (isVisible)
                view?.SetRows(rows);
        }

        private void TickInput()
        {
            if (input == null || !input.WasPressed(PlayerActionId.Leaderboard))
                return;

            isVisible = !isVisible;
            ApplyVisibility();
            if (isVisible)
                view?.SetRows(rows);
        }

        private void ApplyVisibility()
        {
            view?.SetVisible(isVisible);
        }
    }
}
