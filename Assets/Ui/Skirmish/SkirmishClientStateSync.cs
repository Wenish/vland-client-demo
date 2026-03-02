using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
public class SkirmishClientStateSync : MonoBehaviour
{
    [Serializable]
    public struct Snapshot
    {
        public int Round;
        public int TargetRoundWins;
        public int TeamCount;
        public bool TeamSelectionLocked;
        public SkirmishGameManager.RoundState RoundState;
        public float CountdownRemaining;
        public float ReturnToLobbyCountdownRemaining;
        public bool MatchEnded;
        public int MatchWinnerTeam;
        public int LocalTeamId;
        public List<int> TeamScores;
    }

    [SerializeField, Min(0.05f)] private float pollIntervalSeconds = 0.2f;
    [SerializeField] private bool onlyWhenClientActive = true;

    public event Action<Snapshot> OnStateChanged = delegate { };

    public Snapshot CurrentSnapshot { get; private set; }

    private SkirmishGameManager _manager;
    private UnitController _localUnit;
    private float _nextPollAt;
    private string _lastSignature;

    private void OnEnable()
    {
        _lastSignature = null;
        _nextPollAt = 0f;
        TryBindManager();
        PublishIfChanged(force: true);
    }

    private void OnDisable()
    {
        UnbindManager();
    }

    private void Update()
    {
        if (onlyWhenClientActive && !NetworkClient.active)
        {
            return;
        }

        if (_manager == null)
        {
            TryBindManager();
        }

        if (_localUnit == null)
        {
            TryResolveLocalUnitFallback();
        }

        if (Time.unscaledTime < _nextPollAt)
        {
            return;
        }

        _nextPollAt = Time.unscaledTime + pollIntervalSeconds;
        PublishIfChanged(force: false);
    }

    private void TryBindManager()
    {
        if (_manager != null) return;

        _manager = SkirmishGameManager.Instance;
        if (_manager == null) return;

        _manager.OnRoundChanged += HandleManagerStateChanged;
        _manager.OnRoundStateChanged += HandleManagerStateChanged;
        _manager.OnCountdownChanged += HandleManagerStateChanged;
        _manager.OnRoundEnded += HandleManagerStateChanged;
        _manager.OnMatchEnded += HandleManagerStateChanged;
        _manager.OnTeamSelectionLockChanged += HandleManagerStateChanged;
        _manager.OnReturnToLobbyCountdownChanged += HandleManagerStateChanged;
    }

    private void UnbindManager()
    {
        if (_manager == null) return;

        _manager.OnRoundChanged -= HandleManagerStateChanged;
        _manager.OnRoundStateChanged -= HandleManagerStateChanged;
        _manager.OnCountdownChanged -= HandleManagerStateChanged;
        _manager.OnRoundEnded -= HandleManagerStateChanged;
        _manager.OnMatchEnded -= HandleManagerStateChanged;
        _manager.OnTeamSelectionLockChanged -= HandleManagerStateChanged;
        _manager.OnReturnToLobbyCountdownChanged -= HandleManagerStateChanged;
        _manager = null;
    }

    private void TryResolveLocalUnitFallback()
    {
        var inputs = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach (var input in inputs)
        {
            if (!input.isLocalPlayer || input.myUnit == null)
            {
                continue;
            }

            _localUnit = input.myUnit.GetComponent<UnitController>();
            if (_localUnit != null)
            {
                return;
            }
        }
    }

    private void HandleManagerStateChanged(int _)
    {
        PublishIfChanged(force: false);
    }

    private void HandleManagerStateChanged(float _)
    {
        PublishIfChanged(force: false);
    }

    private void HandleManagerStateChanged(bool _)
    {
        PublishIfChanged(force: false);
    }

    private void HandleManagerStateChanged(SkirmishGameManager.RoundState _)
    {
        PublishIfChanged(force: false);
    }

    private void HandleManagerStateChanged((int winnerTeam, bool isDraw) _)
    {
        PublishIfChanged(force: false);
    }

    private void PublishIfChanged(bool force)
    {
        var snapshot = BuildSnapshot();
        var signature = BuildSignature(snapshot);

        if (!force && signature == _lastSignature)
        {
            return;
        }

        _lastSignature = signature;
        CurrentSnapshot = snapshot;
        OnStateChanged(snapshot);
    }

    private Snapshot BuildSnapshot()
    {
        if (_manager == null)
        {
            return new Snapshot
            {
                Round = 0,
                TargetRoundWins = 0,
                TeamCount = 0,
                TeamSelectionLocked = false,
                RoundState = SkirmishGameManager.RoundState.WaitingToStart,
                CountdownRemaining = 0f,
                ReturnToLobbyCountdownRemaining = 0f,
                MatchEnded = false,
                MatchWinnerTeam = -1,
                LocalTeamId = -1,
                TeamScores = new List<int>()
            };
        }

        int teamCount = Mathf.Max(0, _manager.TeamCount);
        var teamScores = new List<int>(teamCount);
        for (int teamId = 0; teamId < teamCount; teamId++)
        {
            teamScores.Add(_manager.GetTeamRoundWins(teamId));
        }

        return new Snapshot
        {
            Round = _manager.CurrentRound,
            TargetRoundWins = _manager.TargetRoundWins,
            TeamCount = _manager.TeamCount,
            TeamSelectionLocked = _manager.TeamSelectionLocked,
            RoundState = _manager.CurrentRoundState,
            CountdownRemaining = _manager.CountdownRemaining,
            ReturnToLobbyCountdownRemaining = _manager.ReturnToLobbyCountdownRemaining,
            MatchEnded = _manager.MatchEnded,
            MatchWinnerTeam = _manager.MatchWinnerTeam,
            LocalTeamId = _localUnit != null ? _localUnit.team : -1,
            TeamScores = teamScores
        };
    }

    private string BuildSignature(Snapshot snapshot)
    {
        string scoreSignature = snapshot.TeamScores == null
            ? string.Empty
            : string.Join(",", snapshot.TeamScores);

        return $"{snapshot.Round}|{snapshot.TargetRoundWins}|{snapshot.TeamCount}|{snapshot.TeamSelectionLocked}|{(int)snapshot.RoundState}|{snapshot.CountdownRemaining:F1}|{snapshot.ReturnToLobbyCountdownRemaining:F1}|{snapshot.MatchEnded}|{snapshot.MatchWinnerTeam}|{snapshot.LocalTeamId}|{scoreSignature}";
    }
}
