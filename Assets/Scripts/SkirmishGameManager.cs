using UnityEngine;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SkirmishGameManager : NetworkBehaviour
{
    public static SkirmishGameManager Instance { get; private set; }

    [Header("Team Spawns")]
    [Tooltip("Each index represents a team. Spawn 0 = Team 0, Spawn 1 = Team 1, etc.")]
    [SerializeField] private List<Transform> teamSpawns = new List<Transform>();

    [Header("Round Timing")]
    [SerializeField, Min(0f)] private float preRoundCountdownSeconds = 5f;
    [SerializeField, Min(0f)] private float postRoundDelaySeconds = 3f;

    [Header("Match Rules")]
    [SerializeField, Min(1)] private int targetRoundWins = 10;
    [SerializeField, Min(0f)] private float returnToLobbyDelaySeconds = 10f;

    [SyncVar(hook = nameof(HookOnRoundNumberChanged))]
    public int CurrentRound = 0;

    [SyncVar(hook = nameof(HookOnRoundStateChanged))]
    public RoundState CurrentRoundState = RoundState.WaitingToStart;

    [SyncVar(hook = nameof(HookOnCountdownChanged))]
    public float CountdownRemaining = 0f;

    [SyncVar(hook = nameof(HookOnMatchEndedChanged))]
    public bool MatchEnded = false;

    [SyncVar(hook = nameof(HookOnMatchWinnerTeamChanged))]
    public int MatchWinnerTeam = -1;

    public event Action<int> OnRoundChanged = delegate { };
    public event Action<RoundState> OnRoundStateChanged = delegate { };
    public event Action<float> OnCountdownChanged = delegate { };
    public event Action<(int winnerTeam, bool isDraw)> OnRoundEnded = delegate { };
    public event Action<int> OnMatchEnded = delegate { };

    private readonly Dictionary<int, int> _connectionTeamAssignments = new Dictionary<int, int>();
    private readonly SyncDictionary<int, int> _teamRoundWins = new SyncDictionary<int, int>();
    private Coroutine _roundLoopCoroutine;
    private Coroutine _returnToLobbyCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple instances of SkirmishGameManager detected. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _teamRoundWins.Clear();

        if (teamSpawns == null || teamSpawns.Count == 0)
        {
            Debug.LogError("[SkirmishGameManager] No team spawns assigned. Skirmish cannot start.", this);
            return;
        }

        AssignTeamsToNewPlayers();

        if (_roundLoopCoroutine != null)
        {
            StopCoroutine(_roundLoopCoroutine);
        }
        _roundLoopCoroutine = StartCoroutine(RoundLoop());
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (_roundLoopCoroutine != null)
        {
            StopCoroutine(_roundLoopCoroutine);
            _roundLoopCoroutine = null;
        }

        if (_returnToLobbyCoroutine != null)
        {
            StopCoroutine(_returnToLobbyCoroutine);
            _returnToLobbyCoroutine = null;
        }

        _connectionTeamAssignments.Clear();
        _teamRoundWins.Clear();
    }

    private IEnumerator RoundLoop()
    {
        yield return WaitForFirstPlayerUnit();

        while (isServer && !MatchEnded)
        {
            CurrentRound++;

            AssignTeamsToNewPlayers();
            ReviveAndTeleportAllPlayersToTeamSpawns();

            CurrentRoundState = RoundState.PreRoundCountdown;
            yield return RunCountdown(preRoundCountdownSeconds);

            CurrentRoundState = RoundState.InRound;

            bool roundResolved = false;
            while (isServer && !MatchEnded && !roundResolved)
            {
                AssignTeamsToNewPlayers();

                var roundOutcome = GetRoundOutcome();
                if (roundOutcome.HasValue)
                {
                    var (winnerTeam, isDraw) = roundOutcome.Value;
                    HandleRoundEnd(winnerTeam, isDraw);
                    roundResolved = true;

                    if (MatchEnded)
                    {
                        yield break;
                    }
                }

                yield return null;
            }

            CurrentRoundState = RoundState.PostRoundDelay;
            yield return RunCountdown(postRoundDelaySeconds);
        }
    }

    [Server]
    private IEnumerator WaitForFirstPlayerUnit()
    {
        while (isServer)
        {
            if (PlayerUnitsManager.Instance != null)
            {
                bool hasAnyPlayerUnit = PlayerUnitsManager.Instance.playerUnits
                    .Any(playerUnit => playerUnit.Unit != null);

                if (hasAnyPlayerUnit)
                {
                    yield break;
                }
            }

            yield return null;
        }
    }

    [Server]
    private void AssignTeamsToNewPlayers()
    {
        if (PlayerUnitsManager.Instance == null) return;

        var activeConnectionIds = new HashSet<int>();

        foreach (var playerUnit in PlayerUnitsManager.Instance.playerUnits)
        {
            if (playerUnit.Unit == null) continue;

            activeConnectionIds.Add(playerUnit.ConnectionId);

            if (_connectionTeamAssignments.ContainsKey(playerUnit.ConnectionId))
            {
                continue;
            }

            int assignedTeam = FindLeastPopulatedTeam();
            _connectionTeamAssignments[playerUnit.ConnectionId] = assignedTeam;

            var unitController = playerUnit.Unit.GetComponent<UnitController>();
            if (unitController != null)
            {
                unitController.SetTeam(assignedTeam);
            }
        }

        var removedConnections = _connectionTeamAssignments.Keys
            .Where(connectionId => !activeConnectionIds.Contains(connectionId))
            .ToList();

        foreach (var connectionId in removedConnections)
        {
            _connectionTeamAssignments.Remove(connectionId);
        }
    }

    [Server]
    private int FindLeastPopulatedTeam()
    {
        int teamCount = teamSpawns.Count;
        var teamPopulation = new int[teamCount];

        foreach (var assignedTeam in _connectionTeamAssignments.Values)
        {
            if (assignedTeam < 0 || assignedTeam >= teamCount) continue;
            teamPopulation[assignedTeam]++;
        }

        int selectedTeam = 0;
        int lowestCount = teamPopulation[0];
        for (int team = 1; team < teamCount; team++)
        {
            if (teamPopulation[team] < lowestCount)
            {
                lowestCount = teamPopulation[team];
                selectedTeam = team;
            }
        }

        return selectedTeam;
    }

    [Server]
    private void ReviveAndTeleportAllPlayersToTeamSpawns()
    {
        if (PlayerUnitsManager.Instance == null) return;

        foreach (var playerUnit in PlayerUnitsManager.Instance.playerUnits)
        {
            if (playerUnit.Unit == null) continue;

            var unitController = playerUnit.Unit.GetComponent<UnitController>();
            if (unitController == null) continue;

            if (!_connectionTeamAssignments.TryGetValue(playerUnit.ConnectionId, out int teamId))
            {
                teamId = Mathf.Clamp(unitController.team, 0, teamSpawns.Count - 1);
            }

            teamId = Mathf.Clamp(teamId, 0, teamSpawns.Count - 1);
            var spawn = teamSpawns[teamId];
            if (spawn == null)
            {
                Debug.LogError($"[SkirmishGameManager] Team spawn for team {teamId} is missing.", this);
                continue;
            }

            unitController.SetTeam(teamId);
            unitController.InterruptAction();
            unitController.SetHealth(unitController.maxHealth);
            unitController.SetShield(unitController.maxShield);

            if (playerUnit.Unit.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = spawn.position;
                rb.rotation = spawn.rotation;

                var unitTransform = playerUnit.Unit.transform;
                unitTransform.SetPositionAndRotation(spawn.position, spawn.rotation);
            }
            else
            {
                var unitTransform = playerUnit.Unit.transform;
                unitTransform.SetPositionAndRotation(spawn.position, spawn.rotation);
            }
        }
    }

    [Server]
    private (int winnerTeam, bool isDraw)? GetRoundOutcome()
    {
        if (PlayerUnitsManager.Instance == null) return null;

        var aliveTeams = new HashSet<int>();

        foreach (var playerUnit in PlayerUnitsManager.Instance.playerUnits)
        {
            if (playerUnit.Unit == null) continue;

            var unitController = playerUnit.Unit.GetComponent<UnitController>();
            if (unitController == null) continue;
            if (unitController.unitType != UnitType.Player) continue;
            if (unitController.IsDead) continue;

            int teamId = Mathf.Clamp(unitController.team, 0, teamSpawns.Count - 1);
            aliveTeams.Add(teamId);

            if (aliveTeams.Count > 1)
            {
                return null;
            }
        }

        if (aliveTeams.Count == 1)
        {
            int winningTeam = aliveTeams.First();
            return (winningTeam, false);
        }

        return (-1, true);
    }

    [Server]
    private void HandleRoundEnd(int winnerTeam, bool isDraw)
    {
        CurrentRoundState = RoundState.RoundEnded;
        OnRoundEnded((winnerTeam, isDraw));

        if (isDraw)
        {
            return;
        }

        if (!_teamRoundWins.ContainsKey(winnerTeam))
        {
            _teamRoundWins[winnerTeam] = 0;
        }

        _teamRoundWins[winnerTeam]++;
        LogTeamScores();

        if (_teamRoundWins[winnerTeam] >= targetRoundWins)
        {
            MatchEnded = true;
            MatchWinnerTeam = winnerTeam;
            CurrentRoundState = RoundState.MatchEnded;
            OnMatchEnded(winnerTeam);

            if (_returnToLobbyCoroutine == null)
            {
                _returnToLobbyCoroutine = StartCoroutine(ReturnToLobbyAfterDelay());
            }
        }
    }

    [Server]
    private IEnumerator ReturnToLobbyAfterDelay()
    {
        float delay = Mathf.Max(0f, returnToLobbyDelaySeconds);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (NetworkManager.singleton is NetworkRoomManager roomManager)
        {
            roomManager.ServerChangeScene(roomManager.RoomScene);
        }
        else
        {
            Debug.LogWarning("[SkirmishGameManager] NetworkManager is not a NetworkRoomManager. Unable to return to room scene.", this);
        }

        _returnToLobbyCoroutine = null;
    }

    [Server]
    private IEnumerator RunCountdown(float durationSeconds)
    {
        float endTime = Time.time + Mathf.Max(0f, durationSeconds);

        while (isServer && Time.time < endTime)
        {
            CountdownRemaining = Mathf.Max(0f, endTime - Time.time);
            yield return null;
        }

        CountdownRemaining = 0f;
    }

    public int GetTeamRoundWins(int teamId)
    {
        return _teamRoundWins.TryGetValue(teamId, out int wins) ? wins : 0;
    }

    [Server]
    private void LogTeamScores()
    {
        if (teamSpawns == null || teamSpawns.Count == 0)
        {
            return;
        }

        var scoreEntries = new List<string>(teamSpawns.Count);
        for (int teamId = 0; teamId < teamSpawns.Count; teamId++)
        {
            scoreEntries.Add($"Team {teamId}: {GetTeamRoundWins(teamId)}");
        }

        Debug.Log($"[SkirmishGameManager] Score Update -> {string.Join(" | ", scoreEntries)}", this);
    }

    private void HookOnRoundNumberChanged(int oldValue, int newValue)
    {
        OnRoundChanged(newValue);
    }

    private void HookOnRoundStateChanged(RoundState oldValue, RoundState newValue)
    {
        OnRoundStateChanged(newValue);
    }

    private void HookOnCountdownChanged(float oldValue, float newValue)
    {
        OnCountdownChanged(newValue);
    }

    private void HookOnMatchEndedChanged(bool oldValue, bool newValue)
    {
        if (!newValue) return;

        if (MatchWinnerTeam >= 0)
        {
            OnMatchEnded(MatchWinnerTeam);
        }
    }

    private void HookOnMatchWinnerTeamChanged(int oldValue, int newValue)
    {
        if (newValue < 0) return;
        if (!MatchEnded) return;
        OnMatchEnded(newValue);
    }

    public enum RoundState : byte
    {
        WaitingToStart = 0,
        PreRoundCountdown = 1,
        InRound = 2,
        RoundEnded = 3,
        PostRoundDelay = 4,
        MatchEnded = 5
    }
}