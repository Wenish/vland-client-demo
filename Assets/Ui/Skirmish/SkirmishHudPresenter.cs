using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SkirmishHudPresenter : MonoBehaviour
{
    [SerializeField] private SkirmishClientStateSync stateSync;
    [SerializeField] private bool hideWhenClientInactive = true;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private Label _phaseLabel;
    private Label _roundLabel;
    private Label _pointsToWinLabel;
    private Label _countdownLabel;
    private VisualElement _scoresContainer;

    private readonly List<Label> _teamScoreLabels = new List<Label>();

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;
        _phaseLabel = _root.Q<Label>("LabelPhase");
        _roundLabel = _root.Q<Label>("LabelRound");
        _pointsToWinLabel = _root.Q<Label>("LabelPointsToWin");
        _countdownLabel = _root.Q<Label>("LabelCountdown");
        _scoresContainer = _root.Q<VisualElement>("ScoresContainer");

        if (stateSync == null)
        {
            stateSync = GetComponent<SkirmishClientStateSync>();
        }
    }

    private void OnEnable()
    {
        if (stateSync != null)
        {
            stateSync.OnStateChanged += Render;
            Render(stateSync.CurrentSnapshot);
        }

        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (stateSync != null)
        {
            stateSync.OnStateChanged -= Render;
        }
    }

    private void Update()
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (_root == null) return;

        bool isVisible = !hideWhenClientInactive || NetworkClient.active;
        _root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void Render(SkirmishClientStateSync.Snapshot snapshot)
    {
        if (_phaseLabel == null || _roundLabel == null || _pointsToWinLabel == null || _countdownLabel == null || _scoresContainer == null)
        {
            return;
        }

        _phaseLabel.text = $"Phase: {ToDisplayName(snapshot.RoundState)}";
        _roundLabel.text = $"Round: {Mathf.Max(0, snapshot.Round)}";
        _pointsToWinLabel.text = $"Points to win: {Mathf.Max(0, snapshot.TargetRoundWins)}";

        bool showCountdown = snapshot.CountdownRemaining > 0f;
        _countdownLabel.style.display = showCountdown ? DisplayStyle.Flex : DisplayStyle.None;
        if (showCountdown)
        {
            _countdownLabel.text = $"Countdown: {snapshot.CountdownRemaining:0.0}s";
        }

        SyncScoreRows(snapshot.TeamScores ?? new List<int>());
    }

    private void SyncScoreRows(List<int> teamScores)
    {
        while (_teamScoreLabels.Count < teamScores.Count)
        {
            var scoreLabel = new Label();
            scoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            scoreLabel.style.fontSize = 15;
            scoreLabel.style.marginBottom = 2;
            scoreLabel.style.color = Color.white;
            _scoresContainer.Add(scoreLabel);
            _teamScoreLabels.Add(scoreLabel);
        }

        for (int i = 0; i < _teamScoreLabels.Count; i++)
        {
            bool isActive = i < teamScores.Count;
            _teamScoreLabels[i].style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;

            if (isActive)
            {
                _teamScoreLabels[i].text = $"Team {i}: {teamScores[i]}";
            }
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
