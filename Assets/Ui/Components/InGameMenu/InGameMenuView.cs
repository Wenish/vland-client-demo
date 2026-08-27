using System;
using ShadowInfection.UI.SettingsPanel;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.InGameMenu
{
    internal sealed class InGameMenuView
    {
        private readonly VisualElement root;
        private readonly VisualElement settingsOverlay;
        private readonly Button endMatchButton;
        private readonly Button leaveServerButton;
        private readonly Button stopServerButton;
        private readonly Button exitGameButton;
        private readonly Button settingsButton;
        private readonly Button closeSettingsButton;
        private readonly Button returnToGameButton;
        private bool modalInputPushed;

        public event Action EndMatchClicked;
        public event Action LeaveServerClicked;
        public event Action StopServerClicked;
        public event Action ExitGameClicked;
        public event Action SettingsClicked;
        public event Action SettingsCloseClicked;
        public event Action ReturnToGameClicked;

        public VisualElement Root => root;
        public SettingsPanelView SettingsPanel { get; }

        public bool IsVisible => root != null && root.style.display == DisplayStyle.Flex;

        public bool IsSettingsOverlayVisible =>
            settingsOverlay != null && settingsOverlay.style.display == DisplayStyle.Flex;

        public InGameMenuView(VisualElement documentRoot)
        {
            root = documentRoot.Q<VisualElement>("game-menu");
            if (root != null)
            {
                root.pickingMode = PickingMode.Position;
                UiGameplayInputGuard.Apply(root);
            }

            settingsOverlay = documentRoot.Q<VisualElement>("settings-overlay");
            if (settingsOverlay != null)
            {
                settingsOverlay.pickingMode = PickingMode.Position;
                UiGameplayInputGuard.Apply(settingsOverlay);
            }

            SettingsPanel = settingsOverlay != null
                ? new SettingsPanelView(settingsOverlay)
                : null;
            SettingsPanel?.SetEmbeddedInOverlay(true);

            endMatchButton = root?.Q<Button>("buttonEndMatch");
            leaveServerButton = root?.Q<Button>("buttonLeaveServer");
            stopServerButton = root?.Q<Button>("buttonStopServer");
            exitGameButton = root?.Q<Button>("buttonExitGame");
            settingsButton = root?.Q<Button>("buttonSettings");
            closeSettingsButton = settingsOverlay?.Q<Button>("buttonCloseSettings");
            returnToGameButton = root?.Q<Button>("buttonReturnToGame");

            if (endMatchButton != null)
                endMatchButton.clicked += () => EndMatchClicked?.Invoke();
            if (leaveServerButton != null)
                leaveServerButton.clicked += () => LeaveServerClicked?.Invoke();
            if (stopServerButton != null)
                stopServerButton.clicked += () => StopServerClicked?.Invoke();
            if (exitGameButton != null)
                exitGameButton.clicked += () => ExitGameClicked?.Invoke();
            if (settingsButton != null)
                settingsButton.clicked += () => SettingsClicked?.Invoke();
            if (closeSettingsButton != null)
                closeSettingsButton.clicked += () => SettingsCloseClicked?.Invoke();
            if (returnToGameButton != null)
                returnToGameButton.clicked += () => ReturnToGameClicked?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            if (root == null)
                return;

            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                SetSettingsOverlayVisible(false);
            else
                RefreshModalInputBlock();
        }

        public void SetSettingsOverlayVisible(bool visible)
        {
            if (settingsOverlay == null)
                return;

            settingsOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshModalInputBlock();
        }

        public void ReleaseModalInputBlock()
        {
            if (!modalInputPushed)
                return;

            UiModalInputBlock.Pop();
            modalInputPushed = false;
        }

        public void SetEndMatchVisible(bool visible) => SetButtonVisible(endMatchButton, visible);

        public void SetLeaveServerVisible(bool visible) => SetButtonVisible(leaveServerButton, visible);

        public void SetStopServerVisible(bool visible) => SetButtonVisible(stopServerButton, visible);

        private void RefreshModalInputBlock()
        {
            var shouldBlock = IsVisible || IsSettingsOverlayVisible;
            if (shouldBlock == modalInputPushed)
                return;

            if (shouldBlock)
            {
                PlayerInput.CancelLocalGameplayInput();
                UiModalInputBlock.Push();
            }
            else
            {
                UiModalInputBlock.Pop();
            }

            modalInputPushed = shouldBlock;
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button == null)
                return;

            button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
