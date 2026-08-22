using System;
using System.Collections.Generic;
using ShadowInfection.Match;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.SkirmishHud
{
    internal sealed class SkirmishHudView
    {
        private readonly VisualElement root;
        private readonly Label phaseLabel;
        private readonly Label roundLabel;
        private readonly Label pointsToWinLabel;
        private readonly Label countdownLabel;
        private readonly Label teamLabel;
        private readonly Label lobbyReturnLabel;
        private readonly VisualElement scoresContainer;
        private readonly VisualElement teamSelectionContainer;
        private readonly List<Label> teamScoreLabels = new List<Label>();
        private readonly List<Button> teamSelectionButtons = new List<Button>();

        public event Action<int> TeamChosen;

        public VisualElement Root => root;

        public SkirmishHudView(VisualElement documentRoot)
        {
            root = documentRoot;
            if (root != null)
                UiGameplayInputGuard.Apply(root);

            phaseLabel = root?.Q<Label>("LabelPhase");
            roundLabel = root?.Q<Label>("LabelRound");
            pointsToWinLabel = root?.Q<Label>("LabelPointsToWin");
            countdownLabel = root?.Q<Label>("LabelCountdown");
            teamLabel = root?.Q<Label>("LabelTeam");
            lobbyReturnLabel = root?.Q<Label>("LabelLobbyReturn");
            scoresContainer = root?.Q<VisualElement>("ScoresContainer");
            teamSelectionContainer = root?.Q<VisualElement>("TeamSelectionContainer");
        }

        public void SetVisible(bool visible)
        {
            if (root == null)
                return;

            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Render(SkirmishUiSnapshot snapshot)
        {
            if (phaseLabel == null || roundLabel == null || pointsToWinLabel == null || countdownLabel == null || scoresContainer == null)
                return;

            phaseLabel.text = $"Phase: {ToDisplayName(snapshot.RoundState)}";
            roundLabel.text = $"Round: {Mathf.Max(0, snapshot.Round)}";
            pointsToWinLabel.text = $"Points to win: {Mathf.Max(0, snapshot.TargetRoundWins)}";
            if (teamLabel != null)
                teamLabel.text = snapshot.LocalTeamId >= 0 ? $"Your Team: {snapshot.LocalTeamId}" : "Your Team: -";

            bool showCountdown = snapshot.CountdownRemaining > 0f;
            countdownLabel.style.display = showCountdown ? DisplayStyle.Flex : DisplayStyle.None;
            if (showCountdown)
                countdownLabel.text = $"Countdown: {snapshot.CountdownRemaining:0.0}s";

            bool showLobbyReturn = snapshot.MatchEnded;
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

            SyncScoreRows(snapshot.TeamScores);
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

        private void SyncScoreRows(int[] teamScores)
        {
            while (teamScoreLabels.Count < teamScores.Length)
            {
                var scoreLabel = new Label();
                scoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                scoreLabel.style.fontSize = 15;
                scoreLabel.style.marginBottom = 2;
                scoreLabel.style.color = Color.white;
                scoresContainer.Add(scoreLabel);
                teamScoreLabels.Add(scoreLabel);
            }

            for (int i = 0; i < teamScoreLabels.Count; i++)
            {
                bool isActive = i < teamScores.Length;
                teamScoreLabels[i].style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
                if (isActive)
                    teamScoreLabels[i].text = $"Team {i}: {teamScores[i]}";
            }
        }

        private static string ToDisplayName(SkirmishGameManager.RoundState roundState)
        {
            switch (roundState)
            {
                case SkirmishGameManager.RoundState.WaitingToStart:
                    return "Waiting";
                case SkirmishGameManager.RoundState.PreRoundCountdown:
                    return "Pre-Round";
                case SkirmishGameManager.RoundState.InRound:
                    return "In Round";
                case SkirmishGameManager.RoundState.RoundEnded:
                    return "Round Ended";
                case SkirmishGameManager.RoundState.PostRoundDelay:
                    return "Post-Round";
                case SkirmishGameManager.RoundState.MatchEnded:
                    return "Match Ended";
                default:
                    return roundState.ToString();
            }
        }
    }
}
