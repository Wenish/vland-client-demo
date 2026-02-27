using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CastleSiegeManager : NetworkBehaviour
{
    public static CastleSiegeManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private CastleSiegeMapConfig mapConfig;

    [Header("Match Start")]
    [SerializeField] private bool autoStartWhenMinPlayersReached = true;
    [SerializeField, Min(1)] private int minPlayersToStart = 2;

    [Header("Spawn Validation")]
    [SerializeField, Min(0f)] private float spawnCollisionCheckRadius = 0.65f;

    [Header("Gizmos")]
    [SerializeField] private bool drawSpawnGizmos = true;
    [SerializeField, Min(0.05f)] private float lordSpawnGizmoRadius = 0.65f;
    [SerializeField, Min(0.05f)] private float playerSpawnGizmoRadius = 0.35f;
    [SerializeField, Min(0.05f)] private float spawnFacingGizmoLength = 1.5f;

    [SyncVar(hook = nameof(HookOnPhaseChanged))]
    public MatchPhase CurrentPhase = MatchPhase.Setup;

    [SyncVar(hook = nameof(HookOnPhaseRemainingChanged))]
    public float PhaseRemainingSeconds = 0f;

    [SyncVar(hook = nameof(HookOnWinnerChanged))]
    public int WinnerTeamId = -1;

    public event Action<MatchPhase> OnMatchPhaseChanged = delegate { };
    public event Action OnPlayerJoined = delegate { };
    public event Action OnPlayerLeft = delegate { };
    public event Action<UnitController> OnUnitDied = delegate { };
    public event Action<int> OnLordSpawned = delegate { };
    public event Action<int> OnTeamEliminated = delegate { };
    public event Action<int> OnMatchWinner = delegate { };

    private readonly Dictionary<int, int> _connectionTeamAssignments = new Dictionary<int, int>();
    private readonly Dictionary<int, GameObject> _trackedPlayerUnitsByConnection = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, Action> _playerDeathHandlersByConnection = new Dictionary<int, Action>();
    private readonly Dictionary<int, Coroutine> _respawnCoroutinesByConnection = new Dictionary<int, Coroutine>();

    private readonly Dictionary<int, UnitController> _lordByTeamId = new Dictionary<int, UnitController>();
    private readonly Dictionary<int, Action> _lordDeathHandlersByTeamId = new Dictionary<int, Action>();
    private readonly HashSet<int> _eliminatedTeams = new HashSet<int>();
    private readonly SyncList<byte> _teamEliminatedFlags = new SyncList<byte>();

    private readonly Dictionary<int, CastleSiegeMapConfig.TeamConfig> _teamConfigByTeamId = new Dictionary<int, CastleSiegeMapConfig.TeamConfig>();

    private Coroutine _matchLoop;
    private double _inGameStartServerTime = -1d;
    private bool _lordsSpawned = false;

    public bool IsInGame => CurrentPhase == MatchPhase.InGame;
    public int TeamCount => mapConfig != null ? Mathf.Max(0, mapConfig.TeamCount) : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (!ValidateMapConfig())
        {
            SetPhase(MatchPhase.MatchEnded);
            return;
        }

        InitializeTeamConfigLookup();
        InitializeTeamEliminationFlags();

        if (_matchLoop != null)
        {
            StopCoroutine(_matchLoop);
        }

        _matchLoop = StartCoroutine(ServerMatchLoop());
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        CleanupAllServerState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawSpawnGizmos || mapConfig == null || mapConfig.Teams == null)
        {
            return;
        }

        int teamCount = Mathf.Max(1, mapConfig.TeamCount);
        foreach (var teamConfig in mapConfig.Teams)
        {
            if (teamConfig == null)
            {
                continue;
            }

            Color teamColor = GetTeamGizmoColor(teamConfig.TeamId, teamCount);

            DrawSpawnGizmo(teamConfig.LordSpawn, lordSpawnGizmoRadius, teamColor);

#if UNITY_EDITOR
            Handles.color = teamColor;
            Handles.Label(teamConfig.LordSpawn.Position + Vector3.up * (lordSpawnGizmoRadius + 0.2f), $"Team {teamConfig.TeamId} Lord");
#endif

            if (teamConfig.PlayerSpawnPoints == null)
            {
                continue;
            }

            for (int i = 0; i < teamConfig.PlayerSpawnPoints.Count; i++)
            {
                var spawn = teamConfig.PlayerSpawnPoints[i];
                DrawSpawnGizmo(spawn, playerSpawnGizmoRadius, teamColor);

#if UNITY_EDITOR
                Handles.color = teamColor;
                Handles.Label(spawn.Position + Vector3.up * (playerSpawnGizmoRadius + 0.15f), $"Team {teamConfig.TeamId} Spawn {i + 1}");
#endif
            }
        }
    }

    private void DrawSpawnGizmo(CastleSiegeMapConfig.SpawnPointData spawnPoint, float radius, Color color)
    {
        Quaternion rotation = spawnPoint.Rotation;
        Vector3 position = spawnPoint.Position;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(position, radius);
        Gizmos.DrawSphere(position, radius * 0.15f);

        Vector3 forward = rotation * Vector3.forward;
        Vector3 end = position + forward * spawnFacingGizmoLength;
        Gizmos.DrawLine(position, end);

        Vector3 rightWing = Quaternion.Euler(0f, 150f, 0f) * forward;
        Vector3 leftWing = Quaternion.Euler(0f, -150f, 0f) * forward;
        float wingLength = spawnFacingGizmoLength * 0.2f;
        Gizmos.DrawLine(end, end + rightWing * wingLength);
        Gizmos.DrawLine(end, end + leftWing * wingLength);
    }

    private Color GetTeamGizmoColor(int teamId, int teamCount)
    {
        float hue = Mathf.Repeat((teamId / (float)Mathf.Max(1, teamCount)) + 0.08f, 1f);
        return Color.HSVToRGB(hue, 0.8f, 1f);
    }

    [Server]
    public void ServerForceStartMatch()
    {
        if (CurrentPhase != MatchPhase.Setup)
        {
            return;
        }

        autoStartWhenMinPlayersReached = true;
        minPlayersToStart = 1;
    }

    [Server]
    private IEnumerator ServerMatchLoop()
    {
        SetPhase(MatchPhase.Setup);
        WinnerTeamId = -1;
        _inGameStartServerTime = -1d;
        _lordsSpawned = false;

        while (isServer && CurrentPhase == MatchPhase.Setup)
        {
            SyncPlayersAndTeams();
            if (CanLeaveSetup())
            {
                break;
            }

            yield return null;
        }

        if (!isServer) yield break;

        SetPhase(MatchPhase.Warmup);
        yield return RunPhaseTimer(mapConfig.WarmupSeconds);

        if (!isServer) yield break;

        SetPhase(MatchPhase.Countdown);
        yield return RunPhaseTimer(mapConfig.StartCountdownSeconds);

        if (!isServer) yield break;

        TransitionToInGame();

        while (isServer && CurrentPhase == MatchPhase.InGame)
        {
            SyncPlayersAndTeams();
            CheckVictoryCondition();
            yield return null;
        }
    }

    [Server]
    private bool CanLeaveSetup()
    {
        if (!autoStartWhenMinPlayersReached)
        {
            return false;
        }

        if (PlayerUnitsManager.Instance == null)
        {
            return false;
        }

        int connectedPlayers = PlayerUnitsManager.Instance.playerUnits.Count(unit => unit.Unit != null);
        return connectedPlayers >= Mathf.Max(1, minPlayersToStart);
    }

    [Server]
    private IEnumerator RunPhaseTimer(float durationSeconds)
    {
        float endTime = Time.time + Mathf.Max(0f, durationSeconds);
        while (isServer && Time.time < endTime)
        {
            SyncPlayersAndTeams();
            PhaseRemainingSeconds = Mathf.Max(0f, endTime - Time.time);
            yield return null;
        }

        PhaseRemainingSeconds = 0f;
    }

    [Server]
    private void TransitionToInGame()
    {
        SetPhase(MatchPhase.InGame);
        _inGameStartServerTime = NetworkTime.time;
        PhaseRemainingSeconds = 0f;

        SpawnLordsOnce();
        SyncPlayersAndTeams();
    }

    [Server]
    private void SyncPlayersAndTeams()
    {
        if (PlayerUnitsManager.Instance == null)
        {
            return;
        }

        var activeConnectionIds = new HashSet<int>();

        foreach (var playerUnit in PlayerUnitsManager.Instance.playerUnits)
        {
            if (playerUnit.Unit == null)
            {
                continue;
            }

            int connectionId = playerUnit.ConnectionId;
            activeConnectionIds.Add(connectionId);

            if (!_connectionTeamAssignments.TryGetValue(connectionId, out int assignedTeamId))
            {
                assignedTeamId = AssignTeamForConnection();
                _connectionTeamAssignments[connectionId] = assignedTeamId;
                OnPlayerJoined();
            }

            var unitController = playerUnit.Unit.GetComponent<UnitController>();
            if (unitController == null)
            {
                continue;
            }

            unitController.SetTeam(assignedTeamId);
            EnsurePlayerDeathSubscription(connectionId, playerUnit.Unit, unitController);

            if (_eliminatedTeams.Contains(assignedTeamId))
            {
                if (!unitController.IsDead)
                {
                    unitController.SetHealth(0);
                }
                continue;
            }
            
        }

        HandleDisconnectedPlayers(activeConnectionIds);
    }

    [Server]
    private void HandleDisconnectedPlayers(HashSet<int> activeConnectionIds)
    {
        var disconnected = _connectionTeamAssignments.Keys
            .Where(id => !activeConnectionIds.Contains(id))
            .ToList();

        foreach (int connectionId in disconnected)
        {
            _connectionTeamAssignments.Remove(connectionId);
            _trackedPlayerUnitsByConnection.Remove(connectionId);
            _playerDeathHandlersByConnection.Remove(connectionId);

            if (_respawnCoroutinesByConnection.TryGetValue(connectionId, out Coroutine respawnCoroutine))
            {
                StopCoroutine(respawnCoroutine);
                _respawnCoroutinesByConnection.Remove(connectionId);
            }

            OnPlayerLeft();
        }
    }

    [Server]
    private void EnsurePlayerDeathSubscription(int connectionId, GameObject playerUnitObject, UnitController unitController)
    {
        if (_trackedPlayerUnitsByConnection.TryGetValue(connectionId, out GameObject trackedUnit) && trackedUnit == playerUnitObject)
        {
            return;
        }

        if (_trackedPlayerUnitsByConnection.TryGetValue(connectionId, out GameObject oldTracked) && oldTracked != null)
        {
            var oldController = oldTracked.GetComponent<UnitController>();
            if (oldController != null && _playerDeathHandlersByConnection.TryGetValue(connectionId, out Action oldHandler))
            {
                oldController.OnDied -= oldHandler;
            }
        }

        Action onDiedHandler = () => HandlePlayerUnitDied(connectionId, unitController);
        unitController.OnDied += onDiedHandler;

        _trackedPlayerUnitsByConnection[connectionId] = playerUnitObject;
        _playerDeathHandlersByConnection[connectionId] = onDiedHandler;
    }

    [Server]
    private void HandlePlayerUnitDied(int connectionId, UnitController unitController)
    {
        OnUnitDied(unitController);

        if (!_connectionTeamAssignments.TryGetValue(connectionId, out int teamId))
        {
            return;
        }

        if (_eliminatedTeams.Contains(teamId))
        {
            return;
        }

        if (CurrentPhase == MatchPhase.MatchEnded)
        {
            return;
        }

        Vector3 deathPosition = unitController.transform.position;

        if (_respawnCoroutinesByConnection.TryGetValue(connectionId, out Coroutine oldCoroutine))
        {
            StopCoroutine(oldCoroutine);
        }

        _respawnCoroutinesByConnection[connectionId] = StartCoroutine(RespawnPlayerAfterDelay(connectionId, deathPosition));
    }

    [Server]
    private IEnumerator RespawnPlayerAfterDelay(int connectionId, Vector3 deathPosition)
    {
        float delay = ComputeRespawnSeconds();
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (!_connectionTeamAssignments.TryGetValue(connectionId, out int teamId))
        {
            _respawnCoroutinesByConnection.Remove(connectionId);
            yield break;
        }

        if (_eliminatedTeams.Contains(teamId))
        {
            _respawnCoroutinesByConnection.Remove(connectionId);
            yield break;
        }

        var playerUnit = PlayerUnitsManager.Instance.playerUnits.FirstOrDefault(unit => unit.ConnectionId == connectionId);
        if (playerUnit.Unit == null)
        {
            _respawnCoroutinesByConnection.Remove(connectionId);
            yield break;
        }

        var controller = playerUnit.Unit.GetComponent<UnitController>();
        if (controller == null)
        {
            _respawnCoroutinesByConnection.Remove(connectionId);
            yield break;
        }

        var spawn = GetNearestTeamPlayerSpawn(teamId, deathPosition);
        RespawnPlayerAtSpawn(controller, spawn);
        _respawnCoroutinesByConnection.Remove(connectionId);
    }

    [Server]
    private float ComputeRespawnSeconds()
    {
        float matchMinutes = 0f;
        if (_inGameStartServerTime > 0d)
        {
            matchMinutes = Mathf.Max(0f, (float)((NetworkTime.time - _inGameStartServerTime) / 60d));
        }

        float respawn = mapConfig.BaseRespawnSeconds + Mathf.Floor(matchMinutes) * mapConfig.ExtraRespawnPerMinute;
        return Mathf.Clamp(respawn, mapConfig.MinRespawnSeconds, mapConfig.MaxRespawnSeconds);
    }

    [Server]
    private int AssignTeamForConnection()
    {
        var population = new Dictionary<int, int>();
        foreach (int teamId in _teamConfigByTeamId.Keys)
        {
            if (_eliminatedTeams.Contains(teamId)) continue;
            population[teamId] = 0;
        }

        foreach (int assignedTeam in _connectionTeamAssignments.Values)
        {
            if (!population.ContainsKey(assignedTeam)) continue;
            population[assignedTeam]++;
        }

        if (population.Count == 0)
        {
            return 0;
        }

        int minimum = population.Values.Min();
        return population
            .Where(pair => pair.Value == minimum)
            .OrderBy(pair => pair.Key)
            .First()
            .Key;
    }

    [Server]
    private void SpawnLordsOnce()
    {
        if (_lordsSpawned)
        {
            return;
        }

        _lordsSpawned = true;

        foreach (var pair in _teamConfigByTeamId)
        {
            int teamId = pair.Key;
            var teamConfig = pair.Value;

            Vector3 spawnPosition = teamConfig.LordSpawn.Position;
            if (!TryFindSpawnPosition(spawnPosition, out Vector3 resolvedPosition))
            {
                resolvedPosition = spawnPosition;
            }

            var lordObject = UnitSpawner.Instance.Spawn(teamConfig.LordUnit, resolvedPosition, teamConfig.LordSpawn.Rotation, true);
            if (lordObject == null)
            {
                Debug.LogError($"[CastleSiegeManager] Failed to spawn lord for team {teamId}.", this);
                continue;
            }

            var lordController = lordObject.GetComponent<UnitController>();
            if (lordController == null)
            {
                Debug.LogError($"[CastleSiegeManager] Spawned lord has no UnitController for team {teamId}.", this);
                continue;
            }

            lordController.SetTeam(teamId);

            Action onLordDied = () => HandleLordDied(teamId, lordController);
            lordController.OnDied += onLordDied;

            _lordByTeamId[teamId] = lordController;
            _lordDeathHandlersByTeamId[teamId] = onLordDied;

            OnLordSpawned(teamId);
        }
    }

    [Server]
    private void HandleLordDied(int teamId, UnitController lordController)
    {
        OnUnitDied(lordController);
        EliminateTeam(teamId);
    }

    [Server]
    private void EliminateTeam(int teamId)
    {
        if (_eliminatedTeams.Contains(teamId))
        {
            return;
        }

        _eliminatedTeams.Add(teamId);
        if (teamId >= 0 && teamId < _teamEliminatedFlags.Count)
        {
            _teamEliminatedFlags[teamId] = 1;
        }

        if (PlayerUnitsManager.Instance != null)
        {
            foreach (var playerUnit in PlayerUnitsManager.Instance.playerUnits)
            {
                if (playerUnit.Unit == null) continue;
                if (!_connectionTeamAssignments.TryGetValue(playerUnit.ConnectionId, out int assignedTeam)) continue;
                if (assignedTeam != teamId) continue;

                if (_respawnCoroutinesByConnection.TryGetValue(playerUnit.ConnectionId, out Coroutine respawnCoroutine))
                {
                    StopCoroutine(respawnCoroutine);
                    _respawnCoroutinesByConnection.Remove(playerUnit.ConnectionId);
                }

                var playerController = playerUnit.Unit.GetComponent<UnitController>();
                if (playerController != null && !playerController.IsDead)
                {
                    playerController.SetHealth(0);
                }
            }
        }

        OnTeamEliminated(teamId);
        CheckVictoryCondition();
    }

    [Server]
    private void CheckVictoryCondition()
    {
        if (CurrentPhase == MatchPhase.MatchEnded)
        {
            return;
        }

        var aliveTeams = _teamConfigByTeamId.Keys.Where(teamId => !_eliminatedTeams.Contains(teamId)).OrderBy(teamId => teamId).ToList();
        if (aliveTeams.Count != 1)
        {
            return;
        }

        WinnerTeamId = aliveTeams[0];
        SetPhase(MatchPhase.MatchEnded);
        OnMatchWinner(WinnerTeamId);
    }

    private void SetPhase(MatchPhase newPhase)
    {
        if (CurrentPhase == newPhase)
        {
            return;
        }

        CurrentPhase = newPhase;

        if (NetworkServer.active && (newPhase == MatchPhase.Warmup || newPhase == MatchPhase.Countdown))
        {
            RespawnAllActivePlayersToTeamSpawns();
        }

        if (NetworkServer.active)
        {
            OnMatchPhaseChanged(newPhase);
        }
    }

    [Server]
    private void RespawnAllActivePlayersToTeamSpawns()
    {
        if (PlayerUnitsManager.Instance == null)
        {
            return;
        }

        foreach (var playerUnit in PlayerUnitsManager.Instance.playerUnits)
        {
            if (playerUnit.Unit == null)
            {
                continue;
            }

            if (!_connectionTeamAssignments.TryGetValue(playerUnit.ConnectionId, out int assignedTeamId))
            {
                continue;
            }

            if (_eliminatedTeams.Contains(assignedTeamId))
            {
                continue;
            }

            var unitController = playerUnit.Unit.GetComponent<UnitController>();
            if (unitController == null)
            {
                continue;
            }

            var spawn = GetNearestTeamPlayerSpawn(assignedTeamId, unitController.transform.position);
            RespawnPlayerAtSpawn(unitController, spawn);
        }
    }

    [Server]
    private CastleSiegeMapConfig.SpawnPointData GetNearestTeamPlayerSpawn(int teamId, Vector3 deathPosition)
    {
        var teamConfig = _teamConfigByTeamId[teamId];
        var spawnPoints = teamConfig.PlayerSpawnPoints;

        int selectedIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            float sqrDistance = (spawnPoints[i].Position - deathPosition).sqrMagnitude;
            if (sqrDistance < closestDistance)
            {
                closestDistance = sqrDistance;
                selectedIndex = i;
            }
        }

        return spawnPoints[selectedIndex];
    }

    [Server]
    private void RespawnPlayerAtSpawn(UnitController unitController, CastleSiegeMapConfig.SpawnPointData spawnPoint)
    {
        Vector3 basePosition = spawnPoint.Position;
        Vector3 targetPosition = basePosition;

        if (!TryFindSpawnPosition(basePosition, out targetPosition))
        {
            targetPosition = basePosition;
        }

        unitController.InterruptAction();
        unitController.SetHealth(unitController.maxHealth);
        unitController.SetShield(unitController.maxShield);

        var transformToMove = unitController.transform;
        if (unitController.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = targetPosition;
            rb.rotation = spawnPoint.Rotation;
        }

        transformToMove.SetPositionAndRotation(targetPosition, spawnPoint.Rotation);
    }

    [Server]
    private bool TryFindSpawnPosition(Vector3 basePosition, out Vector3 validPosition)
    {
        for (int attempt = 0; attempt < mapConfig.SpawnOffsetMaxAttempts; attempt++)
        {
            Vector3 candidate = basePosition;
            if (attempt > 0)
            {
                float radius = mapConfig.SpawnOffsetRadiusStart + attempt * mapConfig.SpawnOffsetRadiusStep;
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                candidate = basePosition + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }

            if (!TryValidateCandidate(ref candidate))
            {
                continue;
            }

            validPosition = candidate;
            return true;
        }

        validPosition = basePosition;
        return false;
    }

    [Server]
    private bool TryValidateCandidate(ref Vector3 candidate)
    {
        bool blocked = Physics.CheckSphere(candidate, spawnCollisionCheckRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (blocked)
        {
            return false;
        }

        if (mapConfig.RequireWalkable)
        {
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                return false;
            }

            candidate = hit.position;
        }

        return true;
    }

    [Server]
    private bool ValidateMapConfig()
    {
        if (mapConfig == null)
        {
            Debug.LogError("[CastleSiegeManager] MapConfig is missing. Match cannot start.", this);
            return false;
        }

        if (!mapConfig.Validate(out string errorMessage))
        {
            Debug.LogError($"[CastleSiegeManager] MapConfig invalid. Match cannot start. Reason: {errorMessage}", this);
            return false;
        }

        return true;
    }

    [Server]
    private void InitializeTeamConfigLookup()
    {
        _teamConfigByTeamId.Clear();

        foreach (var teamConfig in mapConfig.Teams)
        {
            _teamConfigByTeamId[teamConfig.TeamId] = teamConfig;
        }
    }

    [Server]
    private void InitializeTeamEliminationFlags()
    {
        _teamEliminatedFlags.Clear();
        _eliminatedTeams.Clear();
        for (int i = 0; i < mapConfig.TeamCount; i++)
        {
            _teamEliminatedFlags.Add(0);
        }
    }

    [Server]
    private void CleanupAllServerState()
    {
        if (_matchLoop != null)
        {
            StopCoroutine(_matchLoop);
            _matchLoop = null;
        }

        foreach (var pair in _respawnCoroutinesByConnection)
        {
            if (pair.Value != null)
            {
                StopCoroutine(pair.Value);
            }
        }

        _respawnCoroutinesByConnection.Clear();

        foreach (var pair in _trackedPlayerUnitsByConnection)
        {
            if (pair.Value == null) continue;
            var controller = pair.Value.GetComponent<UnitController>();
            if (controller == null) continue;
            if (_playerDeathHandlersByConnection.TryGetValue(pair.Key, out Action handler))
            {
                controller.OnDied -= handler;
            }
        }

        foreach (var pair in _lordByTeamId)
        {
            if (pair.Value == null) continue;
            if (_lordDeathHandlersByTeamId.TryGetValue(pair.Key, out Action handler))
            {
                pair.Value.OnDied -= handler;
            }
        }

        _connectionTeamAssignments.Clear();
        _trackedPlayerUnitsByConnection.Clear();
        _playerDeathHandlersByConnection.Clear();
        _lordByTeamId.Clear();
        _lordDeathHandlersByTeamId.Clear();
        _eliminatedTeams.Clear();
        _teamConfigByTeamId.Clear();
        _teamEliminatedFlags.Clear();
    }

    private void HookOnPhaseChanged(MatchPhase oldValue, MatchPhase newValue)
    {
        if (NetworkServer.active)
        {
            return;
        }

        OnMatchPhaseChanged(newValue);
    }

    private void HookOnPhaseRemainingChanged(float oldValue, float newValue)
    {
    }

    private void HookOnWinnerChanged(int oldValue, int newValue)
    {
    }

    public bool IsTeamEliminated(int teamId)
    {
        if (teamId < 0 || teamId >= _teamEliminatedFlags.Count)
        {
            return false;
        }

        return _teamEliminatedFlags[teamId] == 1;
    }

    public enum MatchPhase : byte
    {
        Setup = 0,
        Warmup = 1,
        Countdown = 2,
        InGame = 3,
        MatchEnded = 4
    }
}