using System.Threading;
using MessagePipe;
using Mirror;
using MyGame.Events.Ui;
using R3;
using ShadowInfection.UI.Session;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ShadowInfection.UI.InGameMenu
{
    internal sealed class InGameMenuPresenter
    {
        private readonly ISessionFlowCommands sessionCommands;
        private readonly IPublisher<RequestCloseVendorWindowEvent> closeVendor;
        private readonly ISubscriber<VendorWindowVisibilityChangedEvent> vendorVisibility;

        private InGameMenuView view;
        private R3.DisposableBag subscriptions;
        private bool vendorIsOpen;
        private bool lastStopServerVisible;
        private bool lastEndMatchVisible;
        private bool lastLeaveServerVisible;
        private bool hasRoleSnapshot;

        public InGameMenuPresenter(
            ISessionFlowCommands sessionCommands,
            IPublisher<RequestCloseVendorWindowEvent> closeVendor,
            ISubscriber<VendorWindowVisibilityChangedEvent> vendorVisibility)
        {
            this.sessionCommands = sessionCommands;
            this.closeVendor = closeVendor;
            this.vendorVisibility = vendorVisibility;
        }

        public void Bind(InGameMenuView nextView, CancellationToken token)
        {
            Unbind();
            view = nextView;
            if (view == null)
                return;

            view.EndMatchClicked += OnEndMatchClicked;
            view.LeaveServerClicked += OnLeaveServerClicked;
            view.StopServerClicked += OnStopServerClicked;
            view.ExitGameClicked += OnExitGameClicked;
            view.ReturnToGameClicked += CloseMenu;

            RefreshRoleButtons();
            subscriptions.Add(vendorVisibility.Subscribe(OnVendorVisibilityChanged));
            subscriptions.Add(
                Observable.EveryUpdate(UnityFrameProvider.Update, token)
                    .Subscribe(_ =>
                    {
                        RefreshRoleButtons();
                        TickEscape();
                    }));
        }

        public void Unbind()
        {
            if (view != null)
            {
                view.EndMatchClicked -= OnEndMatchClicked;
                view.LeaveServerClicked -= OnLeaveServerClicked;
                view.StopServerClicked -= OnStopServerClicked;
                view.ExitGameClicked -= OnExitGameClicked;
                view.ReturnToGameClicked -= CloseMenu;
            }

            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            view = null;
            vendorIsOpen = false;
            hasRoleSnapshot = false;
        }

        private void TickEscape()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            if (vendorIsOpen)
            {
                closeVendor.Publish(new RequestCloseVendorWindowEvent());
                return;
            }

            if (view != null && view.IsVisible)
                CloseMenu();
            else
                OpenMenu();
        }

        private void OpenMenu()
        {
            closeVendor.Publish(new RequestCloseVendorWindowEvent());
            view?.SetVisible(true);
        }

        private void CloseMenu()
        {
            view?.SetVisible(false);
        }

        private void OnVendorVisibilityChanged(VendorWindowVisibilityChangedEvent evt)
        {
            vendorIsOpen = evt != null && evt.IsOpen;
        }

        private void RefreshRoleButtons()
        {
            if (view == null)
                return;

            var isServer = NetworkServer.active;
            var isInLobby = SceneManager.GetActiveScene().name == SceneNames.Lobby;
            var isOnlyClient = NetworkClient.isConnected && !NetworkServer.active;
            var stopServerVisible = isServer;
            var endMatchVisible = isServer && !isInLobby;
            var leaveServerVisible = isOnlyClient;

            if (hasRoleSnapshot
                && stopServerVisible == lastStopServerVisible
                && endMatchVisible == lastEndMatchVisible
                && leaveServerVisible == lastLeaveServerVisible)
            {
                return;
            }

            hasRoleSnapshot = true;
            lastStopServerVisible = stopServerVisible;
            lastEndMatchVisible = endMatchVisible;
            lastLeaveServerVisible = leaveServerVisible;
            view.SetStopServerVisible(stopServerVisible);
            view.SetEndMatchVisible(endMatchVisible);
            view.SetLeaveServerVisible(leaveServerVisible);
        }

        private void OnEndMatchClicked()
        {
            CloseMenu();
            sessionCommands.TryEndMatch();
        }

        private void OnLeaveServerClicked()
        {
            sessionCommands.LeaveClient();
            CloseMenu();
        }

        private void OnStopServerClicked()
        {
            sessionCommands.StopHost();
            CloseMenu();
        }

        private void OnExitGameClicked()
        {
            sessionCommands.ExitGame();
        }
    }
}
