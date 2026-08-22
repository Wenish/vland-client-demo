using System;
using System.Collections.Generic;
using Mirror;
using MyGame.Events;
using R3;
using ShadowInfection.DI;
using ShadowInfection.Zombie;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZombieGameManager : NetworkBehaviour, IZombieWaveRuntime, IZombieRunEndHost
{
    private const int DefaultGoldDrop = 10;
    private const float LeaderboardReconcileIntervalSeconds = 1f;

    [Header("Config")]
    [SerializeField] private ZombieModeConfig modeConfig;
    [SerializeField] private ZombieSpawnController[] zombieSpawns;
    [SerializeField] private bool autoStartOnServer = true;

    [Header("Runtime State")]
    [SyncVar]
    [SerializeField] private int currentWave = 0;

    [SyncVar]
    [SerializeField] private bool isGamePaused = false;

    [SerializeField] private bool isWaveRunning = false;
    [SerializeField] private int zombiesAlive = 0;
    [SerializeField] private int queuedSpawnCount = 0;

    [SyncVar(hook = nameof(HookOnWaveTotalCountChanged))]
    [SerializeField] private int currentWaveTotalCount = 0;

    [SyncVar(hook = nameof(HookOnWaveKilledCountChanged))]
    [SerializeField] private int currentWaveKilledCount = 0;

    [SyncVar(hook = nameof(HookOnIsGameOverChanged))]
    [SerializeField] private bool isGameOver = false;

    [SyncVar(hook = nameof(HookOnAutoReturnToLobbyEnabledChanged))]
    [SerializeField] private bool autoReturnToLobbyEnabled = false;

    [SyncVar(hook = nameof(HookOnReturnToLobbyCountdownChanged))]
    [SerializeField] private float returnToLobbyCountdownSeconds = 0f;

    public struct ZombieLeaderboardEntry
    {
        public int ConnectionId;
        public string PlayerName;
        public int Points;
        public int Kills;
        public int Deaths;
        public int GoldGathered;
        public bool IsConnected;
    }

    public readonly SyncList<ZombieLeaderboardEntry> zombieLeaderboardEntries = new SyncList<ZombieLeaderboardEntry>();

    private DisposableBag serverSubscriptions;
    private float nextLeaderboardReconcileAt;
    private float runStartedAtServerTime;
    private bool returnToLobbyRequested;
    private ZombieWaveDirector waveDirector;
    private ZombieLeaderboardService leaderboardService;
    private ZombieRunEndService runEndService;

    public int CurrentWave => currentWave;
    public bool IsGamePaused => isGamePaused;
    public bool IsWaveRunning => isWaveRunning;
    public bool IsGameOver => isGameOver;
    public bool IsAutoReturnToLobbyEnabled => autoReturnToLobbyEnabled;
    public float ReturnToLobbyCountdownSeconds => returnToLobbyCountdownSeconds;
    public float RunStartedAtServerTime => runStartedAtServerTime;
    public int CurrentWaveTotalCount => currentWaveTotalCount;
    public int CurrentWaveKilledCount => currentWaveKilledCount;
    public float CurrentWaveKilledPercent => currentWaveTotalCount <= 0
        ? 0f
        : (currentWaveKilledCount / (float)currentWaveTotalCount) * 100f;

    public IReadOnlyList<ZombieLeaderboardEntry> LeaderboardEntries => zombieLeaderboardEntries;
    public ZombieWaveDirector WaveDirector => waveDirector;
    public ZombieLeaderboardService LeaderboardService => leaderboardService;
    public ZombieRunEndService RunEndService => runEndService;

    bool IZombieWaveRuntime.IsServer => isServer;
    bool IZombieWaveRuntime.IsGameOver => isGameOver;
    bool IZombieWaveRuntime.IsPaused => isGamePaused;
    ZombieModeConfig IZombieWaveRuntime.ModeConfig => modeConfig;
    int IZombieWaveRuntime.CurrentWave => currentWave;
    int IZombieWaveRuntime.ZombiesAlive => zombiesAlive;

    bool IZombieRunEndHost.IsServer => isServer;
    bool IZombieRunEndHost.IsServerOnly => isServerOnly;
    bool IZombieRunEndHost.IsGameOver => isGameOver;
    bool IZombieRunEndHost.ReturnToLobbyRequested
    {
        get => returnToLobbyRequested;
        set => returnToLobbyRequested = value;
    }

    internal void EnsureServices()
    {
        waveDirector ??= new ZombieWaveDirector(this);
        leaderboardService ??= new ZombieLeaderboardService(zombieLeaderboardEntries);
        runEndService ??= new ZombieRunEndService(this);
    }

    private void Awake()
    {
        RefreshZombieSpawns();
        EnsureServices();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        EnsureServices();

        GameMessages.Subscribe<UnitDiedEvent>(ref serverSubscriptions, OnUnitDied);
        GameMessages.Subscribe<UnitDamagedEvent>(ref serverSubscriptions, OnUnitDamaged);
        GameMessages.Subscribe<PlayerReceivesGoldEvent>(ref serverSubscriptions, OnPlayerReceivesGold);
        GameMessages.Subscribe<PlayerUnitSpawnedEvent>(ref serverSubscriptions, OnPlayerUnitSpawned);
        waveDirector.ResetRecurringState();
        leaderboardService.Reset();

        if (autoStartOnServer)
            StartZombieMode();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        zombieLeaderboardEntries.OnAdd += HandleLeaderboardListChanged;
        zombieLeaderboardEntries.OnSet += HandleLeaderboardListChanged;
        zombieLeaderboardEntries.OnRemove += HandleLeaderboardListChanged;
        zombieLeaderboardEntries.OnClear += HandleLeaderboardListCleared;
        RaiseLeaderboardChanged();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        StopZombieMode();
        runEndService?.Dispose();
        DisposeServerSubscriptions();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        zombieLeaderboardEntries.OnAdd -= HandleLeaderboardListChanged;
        zombieLeaderboardEntries.OnSet -= HandleLeaderboardListChanged;
        zombieLeaderboardEntries.OnRemove -= HandleLeaderboardListChanged;
        zombieLeaderboardEntries.OnClear -= HandleLeaderboardListCleared;
    }

    private void OnDestroy()
    {
        DisposeServerSubscriptions();
        waveDirector?.Dispose();
        runEndService?.Dispose();
    }

    private void DisposeServerSubscriptions()
    {
        serverSubscriptions.Dispose();
        serverSubscriptions = new DisposableBag();
    }

    private void Update()
    {
        if (!isServer)
            return;

        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            isGamePaused = !isGamePaused;

        if (Time.time >= nextLeaderboardReconcileAt)
        {
            nextLeaderboardReconcileAt = Time.time + LeaderboardReconcileIntervalSeconds;
            leaderboardService?.ReconcileConnectivity();
        }
    }

    [Server]
    public void StartZombieMode()
    {
        if (modeConfig == null)
        {
            Debug.LogError("ZombieModeConfig is missing on ZombieGameManager.", this);
            return;
        }

        EnsureServices();
        if (waveDirector.IsRunning)
            return;

        leaderboardService.Reset();
        runEndService.ResetRun();
        zombiesAlive = 0;
        queuedSpawnCount = 0;
        isWaveRunning = false;
        runStartedAtServerTime = Time.time;

        GameMessages.Publish(new ZombieGameOverEvent(false));
        GameMessages.Publish(new ZombieReturnToLobbyCountdownEvent(false, 0f));

        waveDirector.Start();
    }

    [Server]
    public void StopZombieMode()
    {
        waveDirector?.Stop();
        isWaveRunning = false;
        queuedSpawnCount = 0;
    }

    [Server]
    public void ServerReturnToLobby()
    {
        EnsureServices();
        runEndService.RequestReturnToLobby();
    }

    [Server]
    public void SetPaused(bool paused)
    {
        isGamePaused = paused;
    }

    private void HookOnWaveTotalCountChanged(int oldValue, int newValue)
    {
        RaiseWaveProgressChangedEvent();
    }

    private void HookOnWaveKilledCountChanged(int oldValue, int newValue)
    {
        RaiseWaveProgressChangedEvent();
    }

    private void HookOnIsGameOverChanged(bool oldValue, bool newValue)
    {
        GameMessages.Publish(new ZombieGameOverEvent(newValue));
    }

    private void HookOnAutoReturnToLobbyEnabledChanged(bool oldValue, bool newValue)
    {
        GameMessages.Publish(new ZombieReturnToLobbyCountdownEvent(newValue, returnToLobbyCountdownSeconds));
    }

    private void HookOnReturnToLobbyCountdownChanged(float oldValue, float newValue)
    {
        GameMessages.Publish(new ZombieReturnToLobbyCountdownEvent(autoReturnToLobbyEnabled, newValue));
    }

    private void RefreshZombieSpawns()
    {
        zombieSpawns = FindObjectsByType<ZombieSpawnController>();
    }

    private Vector3 GetZombieSpawnPosition()
    {
        if (zombieSpawns == null || zombieSpawns.Length == 0)
            RefreshZombieSpawns();

        var activeSpawns = Array.FindAll(zombieSpawns, spawn => spawn != null && spawn.isActive);
        if (activeSpawns.Length == 0)
        {
            Debug.LogError("No active zombie spawns available.", this);
            return Vector3.zero;
        }

        int spawnIndex = UnityEngine.Random.Range(0, activeSpawns.Length);
        return activeSpawns[spawnIndex].transform.position;
    }

    [Server]
    private void OnUnitDied(UnitDiedEvent unitDiedEvent)
    {
        if (unitDiedEvent?.Unit == null)
            return;

        if (unitDiedEvent.Unit.unitType != UnitType.Zombie)
        {
            if (unitDiedEvent.Unit.unitType == UnitType.Player)
            {
                leaderboardService.CreditDeath(unitDiedEvent.Unit);
                runEndService.TryTriggerGameOverFromAllHumanPlayersDead();
            }
            return;
        }

        leaderboardService.CreditKill(unitDiedEvent.Killer);
        ZombieDropGold(unitDiedEvent.Unit, unitDiedEvent.Killer, DefaultGoldDrop);

        var identity = unitDiedEvent.Unit.netIdentity;
        if (identity == null)
            return;

        if (!waveDirector.TryHandleZombieDeath(identity.netId))
            return;

        currentWaveKilledCount = Mathf.Min(currentWaveTotalCount, currentWaveKilledCount + 1);
        RaiseWaveProgressChangedEvent();
        waveDirector.DespawnAfterDelay(identity.gameObject);
    }

    [Server]
    private void OnUnitDamaged(UnitDamagedEvent unitDamagedEvent)
    {
        if (unitDamagedEvent == null)
            return;

        leaderboardService.CreditDamage(unitDamagedEvent.Attacker, unitDamagedEvent.AppliedDamageAmount);
    }

    [Server]
    private void OnPlayerReceivesGold(PlayerReceivesGoldEvent playerReceivesGoldEvent)
    {
        if (playerReceivesGoldEvent == null)
            return;

        leaderboardService.CreditGold(playerReceivesGoldEvent.Player, playerReceivesGoldEvent.GoldAmount);
    }

    [Server]
    private void OnPlayerUnitSpawned(PlayerUnitSpawnedEvent playerUnitSpawnedEvent)
    {
        if (playerUnitSpawnedEvent?.Unit == null)
            return;

        leaderboardService.OnPlayerSpawned(
            playerUnitSpawnedEvent.ConnectionId,
            playerUnitSpawnedEvent.Unit.GetComponent<UnitController>());
    }

    private void HandleLeaderboardListChanged(int _)
    {
        RaiseLeaderboardChanged();
    }

    private void HandleLeaderboardListChanged(int _, ZombieLeaderboardEntry __)
    {
        RaiseLeaderboardChanged();
    }

    private void HandleLeaderboardListCleared()
    {
        RaiseLeaderboardChanged();
    }

    private void RaiseLeaderboardChanged()
    {
        GameMessages.Publish(new ZombieLeaderboardChangedEvent(CopyLeaderboardRows()));
    }

    private ZombieLeaderboardRow[] CopyLeaderboardRows()
    {
        var count = zombieLeaderboardEntries.Count;
        if (count == 0)
            return Array.Empty<ZombieLeaderboardRow>();

        var rows = new ZombieLeaderboardRow[count];
        for (var i = 0; i < count; i++)
        {
            var entry = zombieLeaderboardEntries[i];
            rows[i] = new ZombieLeaderboardRow(
                entry.ConnectionId,
                entry.PlayerName,
                entry.Points,
                entry.Kills,
                entry.Deaths,
                entry.GoldGathered,
                entry.IsConnected);
        }

        return rows;
    }

    private void RaiseWaveProgressChangedEvent()
    {
        GameMessages.Publish(new WaveProgressChangedEvent(
            currentWave,
            currentWaveKilledCount,
            currentWaveTotalCount,
            CurrentWaveKilledPercent));
    }

    [Server]
    private void ZombieDropGold(UnitController zombie, UnitController killer, int amount)
    {
        if (killer == null || killer.unitType != UnitType.Player)
            return;

        GameMessages.Publish(new UnitDroppedGoldEvent(zombie, amount, killer));
        RpcZombieDroppedGold(amount, zombie, killer);
        GameMessages.Publish(new PlayerReceivesGoldEvent(killer, amount));
        RpcPlayerReceivedGold(amount, killer);
    }

    [ClientRpc]
    private void RpcWaveStarted(int waveNumber, int totalZombies)
    {
        if (isServer)
            return;

        GameMessages.Publish(new WaveStartedEvent(waveNumber, totalZombies));
    }

    [ClientRpc]
    private void RpcZombieDroppedGold(int amount, UnitController zombie, UnitController killer)
    {
        if (isServer)
            return;

        GameMessages.Publish(new UnitDroppedGoldEvent(zombie, amount, killer));
    }

    [ClientRpc]
    private void RpcPlayerReceivedGold(int amount, UnitController player)
    {
        if (isServer)
            return;

        GameMessages.Publish(new PlayerReceivesGoldEvent(player, amount));
    }

    void IZombieWaveRuntime.SetCurrentWave(int wave) => currentWave = wave;
    void IZombieWaveRuntime.SetWaveRunning(bool running) => isWaveRunning = running;
    void IZombieWaveRuntime.SetQueuedSpawnCount(int count) => queuedSpawnCount = count;
    void IZombieWaveRuntime.SetZombiesAlive(int count) => zombiesAlive = count;

    void IZombieWaveRuntime.BeginWaveProgress(int total)
    {
        currentWaveTotalCount = Mathf.Max(0, total);
        currentWaveKilledCount = 0;
        RaiseWaveProgressChangedEvent();
    }

    void IZombieWaveRuntime.NotifySpawnFailure()
    {
        currentWaveTotalCount = Mathf.Max(0, currentWaveTotalCount - 1);
        RaiseWaveProgressChangedEvent();
    }

    void IZombieWaveRuntime.NotifyWaveStarted(int wave, int total)
    {
        GameMessages.Publish(new WaveStartedEvent(wave, total));
        RpcWaveStarted(wave, total);
    }

    bool IZombieWaveRuntime.TrySpawnZombie(string unitName, float healthMultiplier, float damageMultiplier, out uint netId)
    {
        netId = 0;
        Vector3 spawnPosition = GetZombieSpawnPosition();
        var units = GameServices.Units;
        if (units == null)
        {
            Debug.LogError("Unit spawner is missing. Cannot spawn zombie.", this);
            return false;
        }

        var zombie = units.SpawnUnit(unitName, spawnPosition, Quaternion.identity, true);
        if (zombie == null)
        {
            Debug.LogError($"Failed to spawn zombie unit '{unitName}'.", this);
            return false;
        }

        var identity = zombie.GetComponent<NetworkIdentity>();
        if (identity == null)
        {
            Debug.LogError("Spawned zombie has no NetworkIdentity.", zombie);
            NetworkServer.Destroy(zombie);
            return false;
        }

        netId = identity.netId;
        zombiesAlive++;

        var unitController = zombie.GetComponent<UnitController>();
        if (unitController == null)
            return true;

        int scaledMaxHealth = Mathf.Max(1, Mathf.RoundToInt(unitController.maxHealth * healthMultiplier));
        unitController.unitMediator.Stats.SetBaseStat(StatType.Health, scaledMaxHealth);
        unitController.maxHealth = scaledMaxHealth;
        unitController.health = scaledMaxHealth;

        float scaledAttackPower = Mathf.Max(1f, unitController.unitMediator.Stats.GetStat(StatType.AttackPower) * damageMultiplier);
        unitController.unitMediator.Stats.SetBaseStat(StatType.AttackPower, scaledAttackPower);
        return true;
    }

    void IZombieWaveRuntime.DestroyZombie(GameObject zombie)
    {
        if (zombie != null)
            NetworkServer.Destroy(zombie);
    }

    int IZombieWaveRuntime.GetActivePlayerCount()
    {
        if (GameServices.PlayerUnits == null)
            return 1;

        int count = 0;
        for (int i = 0; i < GameServices.PlayerUnits.playerUnits.Count; i++)
        {
            if (GameServices.PlayerUnits.playerUnits[i].Unit != null)
                count++;
        }

        return Mathf.Max(1, count);
    }

    void IZombieRunEndHost.SetGameOver(bool value) => isGameOver = value;
    void IZombieRunEndHost.StopWaves() => StopZombieMode();

    void IZombieRunEndHost.SetAutoReturnEnabled(bool value)
    {
        if (autoReturnToLobbyEnabled == value)
            return;

        autoReturnToLobbyEnabled = value;
        if (isServerOnly)
            GameMessages.Publish(new ZombieReturnToLobbyCountdownEvent(value, returnToLobbyCountdownSeconds));
    }

    void IZombieRunEndHost.SetReturnCountdown(float seconds)
    {
        if (Mathf.Approximately(returnToLobbyCountdownSeconds, seconds))
            return;

        returnToLobbyCountdownSeconds = seconds;
        if (isServerOnly)
            GameMessages.Publish(new ZombieReturnToLobbyCountdownEvent(autoReturnToLobbyEnabled, seconds));
    }

    bool IZombieRunEndHost.ChangeToRoomScene()
    {
        if (NetworkManager.singleton is NetworkRoomManager roomManager)
        {
            roomManager.ServerChangeScene(roomManager.RoomScene);
            return true;
        }

        Debug.LogWarning($"[{nameof(ZombieGameManager)}] NetworkManager is not a NetworkRoomManager. Unable to return to room scene.", this);
        return false;
    }

    void IZombieRunEndHost.PublishGameOver(bool value)
    {
        GameMessages.Publish(new ZombieGameOverEvent(value));
    }

    void IZombieRunEndHost.PublishRunEnded(ZombieRunEndReason reason)
    {
        GameMessages.Publish(new ZombieRunEndedEvent(reason));
    }
}
