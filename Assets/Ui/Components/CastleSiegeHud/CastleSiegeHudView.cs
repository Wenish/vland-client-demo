using System;
using System.Collections.Generic;
using ShadowInfection.Match;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.CastleSiegeHud
{
    internal sealed class CastleSiegeHudView
    {
        private readonly VisualElement root;
        private readonly Label phaseLabel;
        private readonly Label timerLabel;
        private readonly Label teamLabel;
        private readonly Label aliveTeamsLabel;
        private readonly Label winnerLabel;
        private readonly Label lobbyReturnLabel;
        private readonly VisualElement teamSelectionContainer;
        private readonly List<Button> teamSelectionButtons = new List<Button>();

        public event Action<int> TeamChosen;

        public VisualElement Root => root;

        public CastleSiegeHudView(VisualElement documentRoot)
        {
            root = documentRoot;
            if (root != null)
                UiGameplayInputGuard.Apply(root);

            phaseLabel = root?.Q<Label>("LabelPhase");
            timerLabel = root?.Q<Label>("LabelTimer");
            teamLabel = root?.Q<Label>("LabelTeam");
            aliveTeamsLabel = root?.Q<Label>("LabelAliveTeams");
            winnerLabel = root?.Q<Label>("LabelWinner");
            lobbyReturnLabel = root?.Q<Label>("LabelLobbyReturn");
            teamSelectionContainer = root?.Q<VisualElement>("TeamSelectionContainer");
        }

        public void SetVisible(bool visible)
        {
            if (root == null)
                return;

            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Render(CastleSiegeUiSnapshot snapshot)
        {
            if (phaseLabel == null || timerLabel == null || teamLabel == null || aliveTeamsLabel == null || winnerLabel == null)
                return;

            phaseLabel.text = $"Phase: {ToDisplayName(snapshot.Phase)}";

            bool showTimer = snapshot.PhaseRemainingSeconds > 0f;
            timerLabel.style.display = showTimer ? DisplayStyle.Flex : DisplayStyle.None;
            if (showTimer)
                timerLabel.text = $"Timer: {snapshot.PhaseRemainingSeconds:0.0}s";

            if (snapshot.LocalTeamId >= 0)
            {
                teamLabel.text = snapshot.LocalTeamEliminated
                    ? $"Your Team: {snapshot.LocalTeamId} (Eliminated)"
                    : $"Your Team: {snapshot.LocalTeamId}";
            }
            else
            {
                teamLabel.text = "Your Team: -";
            }

            aliveTeamsLabel.text = $"Alive Teams: {snapshot.AliveTeams}/{Mathf.Max(0, snapshot.TeamCount)}";

            bool showWinner = snapshot.Phase == CastleSiegeManager.MatchPhase.MatchEnded && snapshot.WinnerTeamId >= 0;
            winnerLabel.style.display = showWinner ? DisplayStyle.Flex : DisplayStyle.None;
            if (showWinner)
                winnerLabel.text = $"Winner: Team {snapshot.WinnerTeamId}";

            bool showLobbyReturn = snapshot.Phase == CastleSiegeManager.MatchPhase.MatchEnded;
            if (lobbyReturnLabel != null)
            {
                lobbyReturnLabel.style.display = showLobbyReturn ? DisplayStyle.Flex : DisplayStyle.None;
                if (showLobbyReturn)
                    lobbyReturnLabel.text = $"Returning to lobby in: {Mathf.Max(0f, snapshot.ReturnToLobbyCountdownRemaining):0.0}s";
            }

            bool showTeamSelection = !snapshot.TeamSelectionLocked && snapshot.TeamCount > 0;
            if (teamSelectionContainer != null)
            {
                teamSelectionContainer.style.display = showTeamSelection ? DisplayStyle.Flex : DisplayStyle.None;
                if (showTeamSelection)
                    SyncTeamSelectionButtons(snapshot.TeamCount, snapshot.LocalTeamId);
            }
        }

        private void SyncTeamSelectionButtons(int teamCount, int localTeamId)
        {
            while (teamSelectionButtons.Count < teamCount)
            {
                int teamId = teamSelectionButtons.Count;
                var button = new OrnateButton { text = $"Join Team {teamId}" };
                button.clicked += () => TeamChosen?.Invoke(teamId);
                button.AddToClassList("si-button--compact");
                button.style.marginTop = 2;
                button.style.marginBottom = 2;
                teamSelectionContainer.Add(button);
                teamSelectionButtons.Add(button);
            }

            for (int i = 0; i < teamSelectionButtons.Count; i++)
            {
                bool isActive = i < teamCount;
                var button = teamSelectionButtons[i];
                button.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
                if (!isActive)
                    continue;

                bool isCurrent = i == localTeamId;
                button.text = isCurrent ? $"Team {i} (Selected)" : $"Join Team {i}";
                button.SetEnabled(!isCurrent);
            }
        }

        private static string ToDisplayName(CastleSiegeManager.MatchPhase phase)
        {
            switch (phase)
            {
                case CastleSiegeManager.MatchPhase.Setup:
                    return "Setup";
                case CastleSiegeManager.MatchPhase.Warmup:
                    return "Warmup";
                case CastleSiegeManager.MatchPhase.Countdown:
                    return "Countdown";
                case CastleSiegeManager.MatchPhase.InGame:
                    return "In Game";
                case CastleSiegeManager.MatchPhase.MatchEnded:
                    return "Match Ended";
                default:
                    return phase.ToString();
            }
        }
    }
}
