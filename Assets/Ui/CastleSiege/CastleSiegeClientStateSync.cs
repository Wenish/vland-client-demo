using System;
using System.Collections.Generic;
using Mirror;
using MyGame.Events;
using UnityEngine;

[DisallowMultipleComponent]
public class CastleSiegeClientStateSync : MonoBehaviour
{
    [Serializable]
    public struct Snapshot
    {
        public CastleSiegeManager.MatchPhase Phase;
        public float PhaseRemainingSeconds;
        public bool TeamSelectionLocked;
        public float ReturnToLobbyCountdownRemaining;
        public int WinnerTeamId;
        public int TeamCount;
        public int AliveTeams;
        public int LocalTeamId;
        public bool LocalTeamEliminated;
        public List<bool> EliminatedTeams;
    }

    [SerializeField, Min(0.05f)] private float pollIntervalSeconds = 0.2f;
    [SerializeField] private bool onlyWhenClientActive = true;

    public event Action<Snapshot> OnStateChanged = delegate { };

    public Snapshot CurrentSnapshot { get; private set; }

    private CastleSiegeManager _manager;
    private UnitController _localUnit;
    private float _nextPollAt;
    private string _lastSignature;

    private void OnEnable()
    {
        _lastSignature = null;
        _nextPollAt = 0f;

        TryBindManager();
        SubscribeLocalUnitEvent();
        PublishIfChanged(force: true);
    }

    private void OnDisable()
    {
        UnbindManager();
        UnsubscribeLocalUnitEvent();
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

    private void SubscribeLocalUnitEvent()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Subscribe<MyPlayerUnitSpawnedEvent>(HandleMyPlayerUnitSpawned);
        }
    }

    private void UnsubscribeLocalUnitEvent()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(HandleMyPlayerUnitSpawned);
        }
    }

    private void HandleMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent evt)
    {
        _localUnit = evt.PlayerCharacter;
        PublishIfChanged(force: false);
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

    private void TryBindManager()
    {
        if (_manager != null) return;

        _manager = CastleSiegeManager.Instance;
        if (_manager == null) return;

        _manager.OnPlayerJoined += HandleManagerStateChanged;
        _manager.OnPlayerLeft += HandleManagerStateChanged;
        _manager.OnUnitDied += HandleManagerUnitDied;
        _manager.OnLordSpawned += HandleManagerIntChanged;
        _manager.OnTeamEliminated += HandleManagerIntChanged;
        _manager.OnMatchWinner += HandleManagerIntChanged;
    }

    private void UnbindManager()
    {
        if (_manager == null) return;

        _manager.OnPlayerJoined -= HandleManagerStateChanged;
        _manager.OnPlayerLeft -= HandleManagerStateChanged;
        _manager.OnUnitDied -= HandleManagerUnitDied;
        _manager.OnLordSpawned -= HandleManagerIntChanged;
        _manager.OnTeamEliminated -= HandleManagerIntChanged;
        _manager.OnMatchWinner -= HandleManagerIntChanged;

        _manager = null;
    }

    private void HandleManagerStateChanged()
    {
        PublishIfChanged(force: false);
    }

    private void HandleManagerIntChanged(int _)
    {
        PublishIfChanged(force: false);
    }

    private void HandleManagerUnitDied(UnitController _)
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
                Phase = CastleSiegeManager.MatchPhase.Setup,
                PhaseRemainingSeconds = 0f,
                TeamSelectionLocked = false,
                ReturnToLobbyCountdownRemaining = 0f,
                WinnerTeamId = -1,
                TeamCount = 0,
                AliveTeams = 0,
                LocalTeamId = -1,
                LocalTeamEliminated = false,
                EliminatedTeams = new List<bool>()
            };
        }

        int teamCount = _manager.TeamCount;
        var eliminated = new List<bool>(teamCount);
        int aliveTeams = 0;

        for (int i = 0; i < teamCount; i++)
        {
            bool isEliminated = _manager.IsTeamEliminated(i);
            eliminated.Add(isEliminated);
            if (!isEliminated)
            {
                aliveTeams++;
            }
        }

        int localTeamId = _localUnit != null ? _localUnit.team : -1;
        bool localTeamEliminated = localTeamId >= 0 && localTeamId < eliminated.Count && eliminated[localTeamId];

        return new Snapshot
        {
            Phase = _manager.CurrentPhase,
            PhaseRemainingSeconds = _manager.PhaseRemainingSeconds,
            TeamSelectionLocked = _manager.TeamSelectionLocked,
            ReturnToLobbyCountdownRemaining = _manager.ReturnToLobbyCountdownRemaining,
            WinnerTeamId = _manager.WinnerTeamId,
            TeamCount = teamCount,
            AliveTeams = aliveTeams,
            LocalTeamId = localTeamId,
            LocalTeamEliminated = localTeamEliminated,
            EliminatedTeams = eliminated
        };
    }

    private static string BuildSignature(Snapshot snapshot)
    {
        string eliminatedSignature = snapshot.EliminatedTeams == null
            ? string.Empty
            : string.Join(",", snapshot.EliminatedTeams);

        return $"{(int)snapshot.Phase}|{snapshot.PhaseRemainingSeconds:F1}|{snapshot.TeamSelectionLocked}|{snapshot.ReturnToLobbyCountdownRemaining:F1}|{snapshot.WinnerTeamId}|{snapshot.TeamCount}|{snapshot.AliveTeams}|{snapshot.LocalTeamId}|{snapshot.LocalTeamEliminated}|{eliminatedSignature}";
    }
}
