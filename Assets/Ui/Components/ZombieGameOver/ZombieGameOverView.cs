using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.ZombieGameOver
{
    internal sealed class ZombieGameOverView
    {
        private readonly VisualElement root;
        private readonly Label titleLabel;
        private readonly Label countdownLabel;
        private readonly Button returnButton;

        public event Action ReturnToLobbyClicked;

        public ZombieGameOverView(VisualElement documentRoot)
        {
            root = documentRoot.Q<VisualElement>("zombieGameOverRoot");
            titleLabel = documentRoot.Q<Label>("zombieGameOverTitle");
            countdownLabel = documentRoot.Q<Label>("zombieReturnCountdownLabel");
            returnButton = documentRoot.Q<OrnateButton>("zombieReturnToLobbyButton")
                ?? documentRoot.Q<Button>("zombieReturnToLobbyButton");

            if (root != null)
                UiGameplayInputGuard.Apply(root);

            if (titleLabel != null)
                titleLabel.text = "Game Over";

            if (returnButton != null)
                returnButton.clicked += () => ReturnToLobbyClicked?.Invoke();

            SetPanelVisible(false);
            SetCountdownVisible(false, 0f);
            SetReturnButtonVisible(false);
        }

        public void SetPanelVisible(bool visible)
        {
            if (root == null)
                return;

            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetCountdownVisible(bool visible, float seconds)
        {
            if (countdownLabel == null)
                return;

            countdownLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            countdownLabel.text = $"Returning to lobby in {Mathf.CeilToInt(Mathf.Max(0f, seconds))}s";
        }

        public void SetReturnButtonVisible(bool visible)
        {
            if (returnButton == null)
                return;

            returnButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
