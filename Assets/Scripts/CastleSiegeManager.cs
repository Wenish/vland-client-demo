using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using ShadowInfection.CastleSiege;
using ShadowInfection.DI;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CastleSiegeManager : MatchGameManagerBase, ICastleSiegeHost
{
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

    private readonly HashSet<int> eliminatedTeams = new HashSet<int>();
    private readonly SyncList<byte> teamEliminatedFlags = new SyncList<byte>();
    private readonly Dictionary<int, CastleSiegeMapConfig.TeamConfig> teamConfigByTeamId = new Dictionary<int, CastleSiegeMapConfig.TeamConfig>();

    private double inGameStartServerTime = -1d;
    private bool lordsSpawned;
    private bool hasRaisedMatchWinnerEvent;
    private int lastRaisedWinnerTeamId = -1;
    private CastleSiegePlayerService playerService;
    private CastleSiegeObjectiveService objectiveService;
    private CastleSiegeMatchDirector matchDirector;

    public bool IsInGame => CurrentPhase == MatchPhase.InGame;
    public override int TeamCount => mapConfig != null ? Mathf.Max(0, mapConfig.TeamCount) : 0;
    public CastleSiegeMatchDirector MatchDirector => matchDirector;
    public CastleSiegePlayerService PlayerService => playerService;
    public CastleSiegeObjectiveService ObjectiveService => objectiveService;

    bool ICastleSiegeHost.IsServer => isServer;
    CastleSiegeManager.MatchPhase ICastleSiegeHost.CurrentPhase => CurrentPhase;
    CastleSiegeMapConfig ICastleSiegeHost.MapConfig => mapConfig;
    bool ICastleSiegeHost.AutoStartWhenMinPlayersReached => autoStartWhenMinPlayersReached;
    int ICastleSiegeHost.MinPlayersToStart => minPlayersToStart;
    float ICastleSiegeHost.SpawnCollisionCheckRadius => spawnCollisionCheckRadius;
    double ICastleSiegeHost.InGameStartServerTime
    {
        get => inGameStartServerTime;
        set => inGameStartServerTime = value;
    }
    bool ICastleSiegeHost.LordsSpawned
    {
        get => lordsSpawned;
        set => lordsSpawned = value;
    }
    int ICastleSiegeHost.WinnerTeamId
    {
        get => WinnerTeamId;
        set => WinnerTeamId = value;
    }
    float ICastleSiegeHost.PhaseRemainingSeconds
    {
        get => PhaseRemainingSeconds;
        set => PhaseRemainingSeconds = value;
    }
    IReadOnlyDictionary<int, CastleSiegeMapConfig.TeamConfig> ICastleSiegeHost.TeamConfigs => teamConfigByTeamId;
    IEnumerable<int> ICastleSiegeHost.AssignedConnectionIds => ConnectionTeamAssignments.Keys;

    internal void EnsureServices()
    {
        playerService ??= new CastleSiegePlayerService(this);
        objectiveService ??= new CastleSiegeObjectiveService(this);
        matchDirector ??= new CastleSiegeMatchDirector(this, playerService, objectiveService);
    }

    private void ResetMatchWinnerEventState()
    {
        hasRaisedMatchWinnerEvent = false;
        lastRaisedWinnerTeamId = -1;
    }

    private void RaiseMatchWinnerIfNeeded(int winnerTeamId)
    {
        if (winnerTeamId < 0)
            return;

        if (hasRaisedMatchWinnerEvent && lastRaisedWinnerTeamId == winnerTeamId)
            return;

        hasRaisedMatchWinnerEvent = true;
        lastRaisedWinnerTeamId = winnerTeamId;
        OnMatchWinner(winnerTeamId);
    }

    protected override void Awake()
    {
        base.Awake();
        ResetMatchWinnerEventState();
        EnsureServices();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ResetMatchWinnerEventState();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        EnsureServices();
        EnsureBotFillManager();

        if (!ValidateMapConfig())
        {
            SetPhase(MatchPhase.MatchEnded);
            return;
        }

        ResetMatchWinnerEventState();
        ServerEnterPreMatch();
        ClearTeamAssignments();
        InitializeTeamConfigLookup();
        InitializeTeamEliminationFlags();
        matchDirector.Stop();
        matchDirector.Start();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        CleanupAllServerState();
        ClearTeamAssignments();
        ResetMatchWinnerEventState();
    }

    protected override void OnDestroy()
    {
        matchDirector?.Dispose();
        playerService?.Dispose();
        objectiveService?.Dispose();
        base.OnDestroy();
    }

    [Server]
    public void ServerForceStartMatch()
    {
        if (CurrentPhase != MatchPhase.Setup)
            return;

        autoStartWhenMinPlayersReached = true;
        minPlayersToStart = 1;
    }

    [Server]
    public UnitController ServerGetAliveEnemyLordForTeam(int requesterTeamId, Vector3 fromPosition)
    {
        EnsureServices();
        return objectiveService.GetAliveEnemyLord(requesterTeamId, fromPosition);
    }

    public bool IsTeamEliminated(int teamId)
    {
        if (teamId < 0 || teamId >= teamEliminatedFlags.Count)
            return false;

        return teamEliminatedFlags[teamId] == 1;
    }

    [Server]
    protected override void OnServerPlayerTeamAssigned(int connectionId, int teamId)
    {
        base.OnServerPlayerTeamAssigned(connectionId, teamId);
        playerService?.RespawnAssignedPlayer(connectionId, teamId);
    }

    bool ICastleSiegeHost.IsTeamEliminated(int teamId)
    {
        return eliminatedTeams.Contains(teamId) || IsTeamEliminated(teamId);
    }

    void ICastleSiegeHost.MarkTeamEliminated(int teamId)
    {
        eliminatedTeams.Add(teamId);
        if (teamId >= 0 && teamId < teamEliminatedFlags.Count)
            teamEliminatedFlags[teamId] = 1;
    }

    bool ICastleSiegeHost.TryGetAssignedTeam(int connectionId, out int teamId)
    {
        return ConnectionTeamAssignments.TryGetValue(connectionId, out teamId);
    }

    void ICastleSiegeHost.SetAssignedTeam(int connectionId, int teamId)
    {
        ConnectionTeamAssignments[connectionId] = teamId;
    }

    void ICastleSiegeHost.RemoveAssignedTeam(int connectionId)
    {
        ConnectionTeamAssignments.Remove(connectionId);
    }

    void ICastleSiegeHost.SetPhase(MatchPhase phase) => SetPhase(phase);
    void ICastleSiegeHost.ServerStartMatchLifecycle() => ServerStartMatchLifecycle();
    void ICastleSiegeHost.ServerEndMatchLifecycle(int winnerTeamId) => ServerEndMatchLifecycle(winnerTeamId);
    void ICastleSiegeHost.RaisePlayerJoined() => OnPlayerJoined();
    void ICastleSiegeHost.RaisePlayerLeft() => OnPlayerLeft();
    void ICastleSiegeHost.RaiseUnitDied(UnitController unit) => OnUnitDied(unit);
    void ICastleSiegeHost.RaiseLordSpawned(int teamId) => OnLordSpawned(teamId);
    void ICastleSiegeHost.RaiseTeamEliminated(int teamId) => OnTeamEliminated(teamId);
    void ICastleSiegeHost.RaiseMatchWinner(int winnerTeamId) => RaiseMatchWinnerIfNeeded(winnerTeamId);
    void ICastleSiegeHost.CancelRespawnsForTeam(int teamId) => playerService?.CancelRespawnsForTeam(teamId);
    bool ICastleSiegeHost.TryFindSpawnPosition(Vector3 basePosition, out Vector3 validPosition) => TryFindSpawnPosition(basePosition, out validPosition);

    private void SetPhase(MatchPhase newPhase)
    {
        if (CurrentPhase == newPhase)
            return;

        CurrentPhase = newPhase;

        if (NetworkServer.active && (newPhase == MatchPhase.Warmup || newPhase == MatchPhase.Countdown))
            playerService?.RespawnAllActivePlayersToTeamSpawns();

        if (NetworkServer.active)
            OnMatchPhaseChanged(newPhase);
    }

    [Server]
    private void EnsureBotFillManager()
    {
        var botFill = GetComponent<PvpBotFillManager>();
        if (botFill == null)
            botFill = gameObject.AddComponent<PvpBotFillManager>();

        int teamCount = Mathf.Max(1, TeamCount);
        int targetPlayers = Mathf.Max(6, teamCount * 3);
        botFill.ServerConfigure(targetPlayers, Mathf.Max(0, targetPlayers - 1), "Player");
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
        teamConfigByTeamId.Clear();
        foreach (var teamConfig in mapConfig.Teams)
            teamConfigByTeamId[teamConfig.TeamId] = teamConfig;
    }

    [Server]
    private void InitializeTeamEliminationFlags()
    {
        teamEliminatedFlags.Clear();
        eliminatedTeams.Clear();
        for (int i = 0; i < mapConfig.TeamCount; i++)
            teamEliminatedFlags.Add(0);
    }

    [Server]
    private void CleanupAllServerState()
    {
        matchDirector?.Stop();
        playerService?.Dispose();
        objectiveService?.Dispose();
        teamConfigByTeamId.Clear();
        eliminatedTeams.Clear();
        teamEliminatedFlags.Clear();
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
                continue;

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
            return false;

        if (mapConfig.RequireWalkable)
        {
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                return false;

            candidate = hit.position;
        }

        return true;
    }

    private void HookOnPhaseChanged(MatchPhase oldValue, MatchPhase newValue)
    {
        if (NetworkServer.active)
            return;

        OnMatchPhaseChanged(newValue);
    }

    private void HookOnPhaseRemainingChanged(float oldValue, float newValue)
    {
    }

    private void HookOnWinnerChanged(int oldValue, int newValue)
    {
        RaiseMatchWinnerIfNeeded(newValue);
    }

    private void OnDrawGizmos()
    {
        if (!drawSpawnGizmos || mapConfig == null || mapConfig.Teams == null)
            return;

        int teamCount = Mathf.Max(1, mapConfig.TeamCount);
        foreach (var teamConfig in mapConfig.Teams)
        {
            if (teamConfig == null)
                continue;

            Color teamColor = GetTeamGizmoColor(teamConfig.TeamId, teamCount);
            DrawSpawnGizmo(teamConfig.LordSpawn, lordSpawnGizmoRadius, teamColor);

#if UNITY_EDITOR
            Handles.color = teamColor;
            Handles.Label(teamConfig.LordSpawn.Position + Vector3.up * (lordSpawnGizmoRadius + 0.2f), $"Team {teamConfig.TeamId} Lord");
#endif

            if (teamConfig.PlayerSpawnPoints == null)
                continue;

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

    public enum MatchPhase : byte
    {
        Setup = 0,
        Warmup = 1,
        Countdown = 2,
        InGame = 3,
        MatchEnded = 4
    }
}
