using System;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.InGameMenu
{
    internal sealed class InGameMenuView
    {
        private readonly VisualElement root;
        private readonly Button endMatchButton;
        private readonly Button leaveServerButton;
        private readonly Button stopServerButton;
        private readonly Button exitGameButton;
        private readonly Button returnToGameButton;

        public event Action EndMatchClicked;
        public event Action LeaveServerClicked;
        public event Action StopServerClicked;
        public event Action ExitGameClicked;
        public event Action ReturnToGameClicked;

        public VisualElement Root => root;

        public bool IsVisible => root != null && root.style.display == DisplayStyle.Flex;

        public InGameMenuView(VisualElement documentRoot)
        {
            root = documentRoot.Q<VisualElement>("game-menu");
            if (root != null)
            {
                root.pickingMode = PickingMode.Position;
                UiGameplayInputGuard.Apply(root);
            }

            endMatchButton = root?.Q<Button>("buttonEndMatch");
            leaveServerButton = root?.Q<Button>("buttonLeaveServer");
            stopServerButton = root?.Q<Button>("buttonStopServer");
            exitGameButton = root?.Q<Button>("buttonExitGame");
            returnToGameButton = root?.Q<Button>("buttonReturnToGame");

            if (endMatchButton != null)
                endMatchButton.clicked += () => EndMatchClicked?.Invoke();
            if (leaveServerButton != null)
                leaveServerButton.clicked += () => LeaveServerClicked?.Invoke();
            if (stopServerButton != null)
                stopServerButton.clicked += () => StopServerClicked?.Invoke();
            if (exitGameButton != null)
                exitGameButton.clicked += () => ExitGameClicked?.Invoke();
            if (returnToGameButton != null)
                returnToGameButton.clicked += () => ReturnToGameClicked?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            if (root == null)
                return;

            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetEndMatchVisible(bool visible) => SetButtonVisible(endMatchButton, visible);

        public void SetLeaveServerVisible(bool visible) => SetButtonVisible(leaveServerButton, visible);

        public void SetStopServerVisible(bool visible) => SetButtonVisible(stopServerButton, visible);

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button == null)
                return;

            button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
