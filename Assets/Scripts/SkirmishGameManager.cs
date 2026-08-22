using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using ShadowInfection.Skirmish;

public class SkirmishGameManager : MatchGameManagerBase, ISkirmishHost
{
    [Header("Team Spawns")]
    [Tooltip("Each index represents a team. Spawn 0 = Team 0, Spawn 1 = Team 1, etc.")]
    [SerializeField] private List<Transform> teamSpawns = new List<Transform>();

    [Header("Round Timing")]
    [SerializeField, Min(0f)] private float preRoundCountdownSeconds = 5f;
    [SerializeField, Min(0f)] private float postRoundDelaySeconds = 3f;

    [Header("Match Rules")]
    [SerializeField, Min(1)] private int targetRoundWins = 10;

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

    [SyncVar]
    private int LastRoundWinnerTeam = -1;

    [SyncVar]
    private bool LastRoundWasDraw = false;

    [SyncVar(hook = nameof(HookOnRoundResolutionSequenceChanged))]
    private int RoundResolutionSequence = 0;

    public override int TeamCount => teamSpawns?.Count ?? 0;
    public int TargetRoundWins => targetRoundWins;

    public event Action<int> OnRoundChanged = delegate { };
    public event Action<RoundState> OnRoundStateChanged = delegate { };
    public event Action<float> OnCountdownChanged = delegate { };
    public event Action<(int winnerTeam, bool isDraw)> OnRoundEnded = delegate { };
    public event Action<int> OnMatchEnded = delegate { };

    private readonly SyncDictionary<int, int> _teamRoundWins = new SyncDictionary<int, int>();
    private bool _hasRaisedMatchEndedEvent;
    private int _lastRaisedMatchWinnerTeam = -1;
    private int _lastRaisedRoundResolutionSequence;
    private SkirmishPlayerService playerService;
    private SkirmishScoreService scoreService;
    private SkirmishRoundDirector roundDirector;

    public SkirmishPlayerService PlayerService => playerService;
    public SkirmishScoreService ScoreService => scoreService;
    public SkirmishRoundDirector RoundDirector => roundDirector;

    bool ISkirmishHost.IsServer => isServer;
    bool ISkirmishHost.IsServerOnly => isServerOnly;
    bool ISkirmishHost.MatchEnded
    {
        get => MatchEnded;
        set => MatchEnded = value;
    }
    int ISkirmishHost.MatchWinnerTeam
    {
        get => MatchWinnerTeam;
        set => MatchWinnerTeam = value;
    }
    int ISkirmishHost.CurrentRound => CurrentRound;
    RoundState ISkirmishHost.CurrentRoundState => CurrentRoundState;
    int ISkirmishHost.TeamCount => TeamCount;
    int ISkirmishHost.TargetRoundWins => targetRoundWins;
    float ISkirmishHost.PreRoundCountdownSeconds => preRoundCountdownSeconds;
    float ISkirmishHost.PostRoundDelaySeconds => postRoundDelaySeconds;
    int ISkirmishHost.RoundResolutionSequence => RoundResolutionSequence;
    IEnumerable<int> ISkirmishHost.AssignedConnectionIds => ConnectionTeamAssignments.Keys;
    IEnumerable<int> ISkirmishHost.AssignedTeamIds => ConnectionTeamAssignments.Values;

    internal void EnsureServices()
    {
        playerService ??= new SkirmishPlayerService(this);
        scoreService ??= new SkirmishScoreService(this);
        roundDirector ??= new SkirmishRoundDirector(this, playerService, scoreService);
    }

    private void ResetRoundEndedEventState()
    {
        _lastRaisedRoundResolutionSequence = 0;
    }

    private void RaiseRoundEndedIfNeeded(int resolutionSequence, int winnerTeam, bool isDraw)
    {
        if (resolutionSequence <= _lastRaisedRoundResolutionSequence)
            return;

        _lastRaisedRoundResolutionSequence = resolutionSequence;
        OnRoundEnded((winnerTeam, isDraw));
    }

    private void ResetMatchEndedEventState()
    {
        _hasRaisedMatchEndedEvent = false;
        _lastRaisedMatchWinnerTeam = -1;
    }

    private void RaiseMatchEndedIfNeeded(int winnerTeam)
    {
        if (winnerTeam < 0)
            return;

        if (_hasRaisedMatchEndedEvent && _lastRaisedMatchWinnerTeam == winnerTeam)
            return;

        _hasRaisedMatchEndedEvent = true;
        _lastRaisedMatchWinnerTeam = winnerTeam;
        OnMatchEnded(winnerTeam);
    }

    [Server]
    private void SetCurrentRound(int value)
    {
        if (CurrentRound == value) return;
        CurrentRound = value;

        if (isServerOnly)
            OnRoundChanged(value);
    }

    [Server]
    private void SetRoundState(RoundState value)
    {
        if (CurrentRoundState == value) return;
        CurrentRoundState = value;

        if (isServerOnly)
            OnRoundStateChanged(value);
    }

    [Server]
    private void SetCountdownRemaining(float value)
    {
        if (Mathf.Approximately(CountdownRemaining, value)) return;
        CountdownRemaining = value;

        if (isServerOnly)
            OnCountdownChanged(value);
    }

    protected override void Awake()
    {
        base.Awake();
        ResetMatchEndedEventState();
        ResetRoundEndedEventState();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ResetMatchEndedEventState();
        ResetRoundEndedEventState();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        EnsureServices();
        EnsureBotFillManager();

        ServerEnterPreMatch();
        ClearTeamAssignments();
        _teamRoundWins.Clear();
        ResetMatchEndedEventState();
        ResetRoundEndedEventState();
        LastRoundWinnerTeam = -1;
        LastRoundWasDraw = false;
        RoundResolutionSequence = 0;

        if (teamSpawns == null || teamSpawns.Count == 0)
        {
            Debug.LogError("[SkirmishGameManager] No team spawns assigned. Skirmish cannot start.", this);
            return;
        }

        playerService.AssignTeamsToNewPlayers();
        roundDirector.Stop();
        roundDirector.Start();
    }

    [Server]
    private void EnsureBotFillManager()
    {
        var botFill = GetComponent<PvpBotFillManager>();
        if (botFill == null)
            botFill = gameObject.AddComponent<PvpBotFillManager>();

        int teamCount = Mathf.Max(1, TeamCount);
        int targetPlayers = Mathf.Max(4, teamCount * 2);
        int maxBots = Mathf.Max(0, targetPlayers - 1);
        botFill.ServerConfigure(targetPlayers, maxBots, "Player");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        roundDirector?.Stop();
        ClearTeamAssignments();
        _teamRoundWins.Clear();
        ResetMatchEndedEventState();
        ResetRoundEndedEventState();
    }

    protected override void OnDestroy()
    {
        roundDirector?.Dispose();
        base.OnDestroy();
    }

    [Server]
    protected override void OnServerPlayerTeamAssigned(int connectionId, int teamId)
    {
        base.OnServerPlayerTeamAssigned(connectionId, teamId);
        EnsureServices();
        playerService.TeleportAssignedPlayer(connectionId, teamId);
    }

    public int GetTeamRoundWins(int teamId)
    {
        return _teamRoundWins.TryGetValue(teamId, out int wins) ? wins : 0;
    }

    [Server]
    private void LogTeamScores()
    {
        if (teamSpawns == null || teamSpawns.Count == 0)
            return;

        var scoreEntries = new List<string>(teamSpawns.Count);
        for (int teamId = 0; teamId < teamSpawns.Count; teamId++)
            scoreEntries.Add($"Team {teamId}: {GetTeamRoundWins(teamId)}");

        Debug.Log($"[SkirmishGameManager] Score Update -> {string.Join(" | ", scoreEntries)}", this);
    }

    Transform ISkirmishHost.GetTeamSpawn(int teamId)
    {
        if (teamSpawns == null || teamId < 0 || teamId >= teamSpawns.Count)
            return null;
        return teamSpawns[teamId];
    }

    bool ISkirmishHost.TryGetAssignedTeam(int connectionId, out int teamId)
    {
        return ConnectionTeamAssignments.TryGetValue(connectionId, out teamId);
    }

    void ISkirmishHost.SetAssignedTeam(int connectionId, int teamId)
    {
        ConnectionTeamAssignments[connectionId] = teamId;
    }

    void ISkirmishHost.RemoveAssignedTeam(int connectionId)
    {
        ConnectionTeamAssignments.Remove(connectionId);
    }

    void ISkirmishHost.SetCurrentRound(int value) => SetCurrentRound(value);
    void ISkirmishHost.SetRoundState(RoundState value) => SetRoundState(value);
    void ISkirmishHost.SetCountdownRemaining(float value) => SetCountdownRemaining(value);

    void ISkirmishHost.SetLastRoundResult(int winnerTeam, bool isDraw)
    {
        LastRoundWinnerTeam = winnerTeam;
        LastRoundWasDraw = isDraw;
    }

    void ISkirmishHost.IncrementRoundResolutionSequence()
    {
        RoundResolutionSequence++;
    }

    void ISkirmishHost.RaiseRoundEnded(int winnerTeam, bool isDraw)
    {
        RaiseRoundEndedIfNeeded(RoundResolutionSequence, winnerTeam, isDraw);
    }

    void ISkirmishHost.RaiseMatchEnded(int winnerTeam)
    {
        RaiseMatchEndedIfNeeded(winnerTeam);
    }

    void ISkirmishHost.ServerStartMatchLifecycle() => ServerStartMatchLifecycle();
    void ISkirmishHost.ServerEndMatchLifecycle(int winnerTeamId) => ServerEndMatchLifecycle(winnerTeamId);
    int ISkirmishHost.GetTeamRoundWins(int teamId) => GetTeamRoundWins(teamId);

    void ISkirmishHost.AddTeamRoundWin(int teamId)
    {
        if (!_teamRoundWins.ContainsKey(teamId))
            _teamRoundWins[teamId] = 0;
        _teamRoundWins[teamId]++;
    }

    void ISkirmishHost.LogTeamScores() => LogTeamScores();

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
            RaiseMatchEndedIfNeeded(MatchWinnerTeam);
    }

    private void HookOnMatchWinnerTeamChanged(int oldValue, int newValue)
    {
        if (newValue < 0) return;
        if (!MatchEnded) return;
        RaiseMatchEndedIfNeeded(newValue);
    }

    private void HookOnRoundResolutionSequenceChanged(int oldValue, int newValue)
    {
        if (newValue <= 0) return;
        RaiseRoundEndedIfNeeded(newValue, LastRoundWinnerTeam, LastRoundWasDraw);
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
