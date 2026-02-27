using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CastleSiegeHudPresenter : MonoBehaviour
{
    [SerializeField] private CastleSiegeClientStateSync stateSync;
    [SerializeField] private bool hideWhenClientInactive = true;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private Label _phaseLabel;
    private Label _timerLabel;
    private Label _teamLabel;
    private Label _aliveTeamsLabel;
    private Label _winnerLabel;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        _phaseLabel = _root.Q<Label>("LabelPhase");
        _timerLabel = _root.Q<Label>("LabelTimer");
        _teamLabel = _root.Q<Label>("LabelTeam");
        _aliveTeamsLabel = _root.Q<Label>("LabelAliveTeams");
        _winnerLabel = _root.Q<Label>("LabelWinner");

        if (stateSync == null)
        {
            stateSync = GetComponent<CastleSiegeClientStateSync>();
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

    private void Render(CastleSiegeClientStateSync.Snapshot snapshot)
    {
        if (_phaseLabel == null || _timerLabel == null || _teamLabel == null || _aliveTeamsLabel == null || _winnerLabel == null)
        {
            return;
        }

        _phaseLabel.text = $"Phase: {ToDisplayName(snapshot.Phase)}";

        bool showTimer = snapshot.PhaseRemainingSeconds > 0f;
        _timerLabel.style.display = showTimer ? DisplayStyle.Flex : DisplayStyle.None;
        if (showTimer)
        {
            _timerLabel.text = $"Timer: {snapshot.PhaseRemainingSeconds:0.0}s";
        }

        if (snapshot.LocalTeamId >= 0)
        {
            _teamLabel.text = snapshot.LocalTeamEliminated
                ? $"Your Team: {snapshot.LocalTeamId} (Eliminated)"
                : $"Your Team: {snapshot.LocalTeamId}";
        }
        else
        {
            _teamLabel.text = "Your Team: -";
        }

        _aliveTeamsLabel.text = $"Alive Teams: {snapshot.AliveTeams}/{Mathf.Max(0, snapshot.TeamCount)}";

        bool showWinner = snapshot.Phase == CastleSiegeManager.MatchPhase.MatchEnded && snapshot.WinnerTeamId >= 0;
        _winnerLabel.style.display = showWinner ? DisplayStyle.Flex : DisplayStyle.None;
        if (showWinner)
        {
            _winnerLabel.text = $"Winner: Team {snapshot.WinnerTeamId}";
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
