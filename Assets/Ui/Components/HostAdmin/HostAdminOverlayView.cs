using System;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.HostAdmin
{
    internal sealed class HostAdminOverlayView
    {
        private readonly VisualElement root;
        private readonly Label managerLabel;
        private readonly Label teamSwitchingLabel;
        private readonly Button lockButton;
        private readonly Button unlockButton;

        public event Action LockClicked;
        public event Action UnlockClicked;

        public VisualElement Root => root;

        public HostAdminOverlayView(VisualElement documentRoot)
        {
            root = documentRoot;
            if (root != null)
                UiGameplayInputGuard.Apply(root);

            managerLabel = root?.Q<Label>("LabelManager");
            teamSwitchingLabel = root?.Q<Label>("LabelTeamSwitching");
            lockButton = root?.Q<Button>("ButtonLockTeamSwitching");
            unlockButton = root?.Q<Button>("ButtonUnlockTeamSwitching");

            if (lockButton != null)
                lockButton.clicked += () => LockClicked?.Invoke();
            if (unlockButton != null)
                unlockButton.clicked += () => UnlockClicked?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            if (root == null)
                return;

            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void RenderMissingManager()
        {
            if (managerLabel != null)
                managerLabel.text = "Manager: Not Found";
            if (teamSwitchingLabel != null)
                teamSwitchingLabel.text = "Team Switching: -";
            lockButton?.SetEnabled(false);
            unlockButton?.SetEnabled(false);
        }

        public void Render(string managerTypeName, bool teamSelectionLocked)
        {
            if (managerLabel != null)
                managerLabel.text = $"Manager: {managerTypeName}";
            if (teamSwitchingLabel != null)
                teamSwitchingLabel.text = teamSelectionLocked
                    ? "Team Switching: Locked"
                    : "Team Switching: Unlocked";

            lockButton?.SetEnabled(!teamSelectionLocked);
            unlockButton?.SetEnabled(teamSelectionLocked);
        }
    }
}
