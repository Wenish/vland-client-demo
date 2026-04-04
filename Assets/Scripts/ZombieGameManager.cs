using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using MyGame.Events;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZombieGameManager : NetworkBehaviour
{
    private const int DefaultGoldDrop = 10;
    private const int KillPointsReward = 25;
    private const float LeaderboardReconcileIntervalSeconds = 1f;

    public static ZombieGameManager Singleton { get; private set; }

    [Header("Config")]
    [SerializeField] private ZombieModeConfig modeConfig;
    [SerializeField] private ZombieSpawnController[] zombieSpawns;
    [SerializeField] private bool autoStartOnServer = true;

    [Header("Runtime State")]
    [SyncVar(hook = nameof(HookOnCurrentWaveChanged))]
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

    private readonly HashSet<uint> trackedZombieNetIds = new HashSet<uint>();
    private readonly Dictionary<string, int> recurringRuleNextWave = new Dictionary<string, int>();
    private Coroutine waveLoopCoroutine;
    private float nextLeaderboardReconcileAt = 0f;

    public int CurrentWave => currentWave;
    public bool IsGamePaused => isGamePaused;
    public bool IsWaveRunning => isWaveRunning;
    public int CurrentWaveTotalCount => currentWaveTotalCount;
    public int CurrentWaveKilledCount => currentWaveKilledCount;
    public float CurrentWaveKilledPercent => currentWaveTotalCount <= 0
        ? 0f
        : (currentWaveKilledCount / (float)currentWaveTotalCount) * 100f;

    public event Action<int> OnNewWaveStarted = delegate { };
    public event Action<float, int, int> OnWaveProgressChanged = delegate { };
    public event Action OnLeaderboardChanged = delegate { };

    public IReadOnlyList<ZombieLeaderboardEntry> LeaderboardEntries => zombieLeaderboardEntries;

    private void Awake()
    {
        Singleton = this;
        RefreshZombieSpawns();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        EventManager.Instance.Subscribe<UnitDiedEvent>(OnUnitDied);
        EventManager.Instance.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
        EventManager.Instance.Subscribe<PlayerReceivesGoldEvent>(OnPlayerReceivesGold);
        EventManager.Instance.Subscribe<PlayerUnitSpawnedEvent>(OnPlayerUnitSpawned);
        ResetRecurringSpecialWaveState();
        ResetLeaderboardState();

        if (autoStartOnServer)
        {
            StartZombieMode();
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        zombieLeaderboardEntries.OnAdd += HandleLeaderboardListChanged;
        zombieLeaderboardEntries.OnSet += HandleLeaderboardListChanged;
        zombieLeaderboardEntries.OnRemove += HandleLeaderboardListChanged;
        zombieLeaderboardEntries.OnClear += HandleLeaderboardListCleared;
        OnLeaderboardChanged();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        StopZombieMode();
        EventManager.Instance.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        EventManager.Instance.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
        EventManager.Instance.Unsubscribe<PlayerReceivesGoldEvent>(OnPlayerReceivesGold);
        EventManager.Instance.Unsubscribe<PlayerUnitSpawnedEvent>(OnPlayerUnitSpawned);
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
        if (Singleton == this)
        {
            Singleton = null;
        }

        if (isServer)
        {
            EventManager.Instance.Unsubscribe<UnitDiedEvent>(OnUnitDied);
            EventManager.Instance.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventManager.Instance.Unsubscribe<PlayerReceivesGoldEvent>(OnPlayerReceivesGold);
            EventManager.Instance.Unsubscribe<PlayerUnitSpawnedEvent>(OnPlayerUnitSpawned);
        }

        StopAllCoroutines();
    }

    private void Update()
    {
        if (!isServer)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            isGamePaused = !isGamePaused;
        }

        if (Time.time >= nextLeaderboardReconcileAt)
        {
            nextLeaderboardReconcileAt = Time.time + LeaderboardReconcileIntervalSeconds;
            ReconcileLeaderboardConnectivity();
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

        if (waveLoopCoroutine != null)
        {
            return;
        }

        ResetLeaderboardState();

        waveLoopCoroutine = StartCoroutine(ServerWaveLoop());
    }

    [Server]
    public void StopZombieMode()
    {
        if (waveLoopCoroutine != null)
        {
            StopCoroutine(waveLoopCoroutine);
            waveLoopCoroutine = null;
        }

        isWaveRunning = false;
        queuedSpawnCount = 0;
    }

    [Server]
    public void SetPaused(bool paused)
    {
        isGamePaused = paused;
    }

    private IEnumerator ServerWaveLoop()
    {
        while (isServer)
        {
            yield return WaitWhilePaused();
            yield return WaitForSecondsServer(modeConfig.spawnSettings.timeBetweenWavesSeconds);

            currentWave++;

            var wavePlan = BuildWavePlan(currentWave);
            queuedSpawnCount = wavePlan.SpawnQueue.Count;
            BeginWaveProgressTracking(queuedSpawnCount);

            isWaveRunning = true;
            RaiseOnNewWaveStartedEvent(currentWave, queuedSpawnCount);

            while (wavePlan.SpawnQueue.Count > 0 && isServer)
            {
                yield return WaitWhilePaused();

                while (isServer && zombiesAlive >= modeConfig.spawnSettings.maxZombiesAlive)
                {
                    yield return null;
                }

                if (!isServer)
                {
                    yield break;
                }

                string unitName = wavePlan.SpawnQueue.Dequeue();
                bool hasSpawned = SpawnZombie(unitName, wavePlan.HealthMultiplier, wavePlan.DamageMultiplier);
                if (!hasSpawned)
                {
                    HandleSpawnFailureForProgress();
                }

                queuedSpawnCount = wavePlan.SpawnQueue.Count;

                yield return WaitForSecondsServer(modeConfig.spawnSettings.timeBetweenSpawnsSeconds);
            }

            while (isServer && zombiesAlive > 0)
            {
                yield return null;
            }

            isWaveRunning = false;
        }
    }

    private IEnumerator WaitForSecondsServer(float seconds)
    {
        if (seconds <= 0f)
        {
            yield break;
        }

        float endTime = Time.time + seconds;
        while (isServer && Time.time < endTime)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            yield return null;
        }
    }

    private IEnumerator WaitWhilePaused()
    {
        while (isServer && isGamePaused)
        {
            yield return null;
        }
    }

    private void HookOnCurrentWaveChanged(int oldValue, int newValue)
    {
        OnNewWaveStarted(newValue);
    }

    private void HookOnWaveTotalCountChanged(int oldValue, int newValue)
    {
        RaiseWaveProgressChangedEvent();
    }

    private void HookOnWaveKilledCountChanged(int oldValue, int newValue)
    {
        RaiseWaveProgressChangedEvent();
    }

    private void RefreshZombieSpawns()
    {
        zombieSpawns = FindObjectsByType<ZombieSpawnController>();
    }

    private Vector3 GetZombieSpawnPosition()
    {
        if (zombieSpawns == null || zombieSpawns.Length == 0)
        {
            RefreshZombieSpawns();
        }

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
    private bool SpawnZombie(string unitName, float healthMultiplier, float damageMultiplier)
    {
        Vector3 spawnPosition = GetZombieSpawnPosition();
        Quaternion spawnRotation = Quaternion.identity;

        var zombie = UnitSpawner.Instance.SpawnUnit(unitName, spawnPosition, spawnRotation, true);
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

        trackedZombieNetIds.Add(identity.netId);
        zombiesAlive++;

        var unitController = zombie.GetComponent<UnitController>();
        if (unitController == null)
        {
            return true;
        }

        int scaledMaxHealth = Mathf.Max(1, Mathf.RoundToInt(unitController.maxHealth * healthMultiplier));
        unitController.unitMediator.Stats.SetBaseStat(StatType.Health, scaledMaxHealth);
        unitController.maxHealth = scaledMaxHealth;
        unitController.health = scaledMaxHealth;

        float scaledAttackPower = Mathf.Max(1f, unitController.unitMediator.Stats.GetStat(StatType.AttackPower) * damageMultiplier);
        unitController.unitMediator.Stats.SetBaseStat(StatType.AttackPower, scaledAttackPower);
        return true;
    }

    private WavePlan BuildWavePlan(int waveNumber)
    {
        ZombieModeConfig.WaveDefinition selectedWave = modeConfig.regularWave;

        if (modeConfig.TryGetOverride(waveNumber, out var fixedOverride) && fixedOverride.wave != null)
        {
            if (fixedOverride.replaceRegularWave)
            {
                selectedWave = fixedOverride.wave;
            }
        }
        else
        {
            var recurringOverride = TryGetRecurringSpecialWave(waveNumber);
            if (recurringOverride != null && recurringOverride.wave != null && recurringOverride.replaceRegularWave)
            {
                selectedWave = recurringOverride.wave;
            }
        }

        int playerCount = GetActivePlayerCount();
        float unitWaveMultiplier = modeConfig.GetUnitCountWaveMultiplier(waveNumber);
        float healthWaveMultiplier = modeConfig.GetHealthWaveMultiplier(waveNumber);
        float damageWaveMultiplier = modeConfig.GetDamageWaveMultiplier(waveNumber);

        float playerCountMultiplier = 1f + Mathf.Max(0, playerCount - 1) * modeConfig.scaling.unitCountPerExtraPlayer;
        float playerHealthMultiplier = 1f + Mathf.Max(0, playerCount - 1) * modeConfig.scaling.healthPerExtraPlayer;
        float playerDamageMultiplier = 1f + Mathf.Max(0, playerCount - 1) * modeConfig.scaling.damagePerExtraPlayer;

        int totalToSpawn = Mathf.Max(1, Mathf.RoundToInt(selectedWave.baseTotalSpawnCount * unitWaveMultiplier * playerCountMultiplier));
        var spawnQueue = BuildSpawnQueue(selectedWave, totalToSpawn);

        return new WavePlan
        {
            SpawnQueue = spawnQueue,
            HealthMultiplier = Mathf.Max(0.01f, healthWaveMultiplier * playerHealthMultiplier),
            DamageMultiplier = Mathf.Max(0.01f, damageWaveMultiplier * playerDamageMultiplier)
        };
    }

    private Queue<string> BuildSpawnQueue(ZombieModeConfig.WaveDefinition waveDefinition, int totalSpawnCount)
    {
        var result = new List<string>(Mathf.Max(1, totalSpawnCount));
        if (waveDefinition == null || waveDefinition.entries == null || waveDefinition.entries.Count == 0)
        {
            result.Add("Infected");
            return new Queue<string>(result);
        }

        var counts = new int[waveDefinition.entries.Count];
        int assigned = 0;

        for (int i = 0; i < waveDefinition.entries.Count; i++)
        {
            int guaranteed = Mathf.Max(0, waveDefinition.entries[i].guaranteedCount);
            int clamped = Mathf.Min(guaranteed, totalSpawnCount - assigned);
            counts[i] = clamped;
            assigned += clamped;
            if (assigned >= totalSpawnCount)
            {
                break;
            }
        }

        while (assigned < totalSpawnCount)
        {
            int chosen = ChooseWeightedEntryIndex(waveDefinition.entries, counts);
            if (chosen < 0)
            {
                break;
            }

            counts[chosen]++;
            assigned++;
        }

        for (int i = 0; i < waveDefinition.entries.Count; i++)
        {
            string unitName = string.IsNullOrWhiteSpace(waveDefinition.entries[i].unitName)
                ? "Infected"
                : waveDefinition.entries[i].unitName;

            for (int j = 0; j < counts[i]; j++)
            {
                result.Add(unitName);
            }
        }

        Shuffle(result);
        return new Queue<string>(result);
    }

    private int ChooseWeightedEntryIndex(IReadOnlyList<ZombieModeConfig.SpawnEntry> entries, IReadOnlyList<int> counts)
    {
        float totalWeight = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.maxCount >= 0 && counts[i] >= entry.maxCount)
            {
                continue;
            }

            totalWeight += Mathf.Max(0f, entry.weight);
        }

        if (totalWeight <= 0f)
        {
            return -1;
        }

        float pick = UnityEngine.Random.value * totalWeight;
        float accumulated = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.maxCount >= 0 && counts[i] >= entry.maxCount)
            {
                continue;
            }

            accumulated += Mathf.Max(0f, entry.weight);
            if (pick <= accumulated)
            {
                return i;
            }
        }

        return entries.Count - 1;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    [Server]
    private void ResetRecurringSpecialWaveState()
    {
        recurringRuleNextWave.Clear();

        if (modeConfig == null || modeConfig.recurringSpecialWaves == null)
        {
            return;
        }

        for (int i = 0; i < modeConfig.recurringSpecialWaves.Count; i++)
        {
            var rule = modeConfig.recurringSpecialWaves[i];
            if (rule == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.ruleId))
            {
                rule.ruleId = $"Rule_{i}";
            }

            recurringRuleNextWave[rule.ruleId] = GetNextWaveFromInterval(rule, 0);
        }
    }

    private ZombieModeConfig.RecurringSpecialWaveRule TryGetRecurringSpecialWave(int waveNumber)
    {
        if (modeConfig == null || modeConfig.recurringSpecialWaves == null)
        {
            return null;
        }

        for (int i = 0; i < modeConfig.recurringSpecialWaves.Count; i++)
        {
            var rule = modeConfig.recurringSpecialWaves[i];
            if (rule == null || !recurringRuleNextWave.TryGetValue(rule.ruleId, out int nextWave))
            {
                continue;
            }

            if (waveNumber != nextWave)
            {
                continue;
            }

            recurringRuleNextWave[rule.ruleId] = GetNextWaveFromInterval(rule, waveNumber);
            return rule;
        }

        return null;
    }

    private int GetNextWaveFromInterval(ZombieModeConfig.RecurringSpecialWaveRule rule, int baseWave)
    {
        int minInterval = Mathf.Max(1, rule.minInterval);
        int maxInterval = Mathf.Max(minInterval, rule.maxInterval);
        int nextOffset = UnityEngine.Random.Range(minInterval, maxInterval + 1);
        return baseWave + nextOffset;
    }

    [Server]
    private int GetActivePlayerCount()
    {
        if (PlayerUnitsManager.Instance == null)
        {
            return 1;
        }

        int count = 0;
        for (int i = 0; i < PlayerUnitsManager.Instance.playerUnits.Count; i++)
        {
            if (PlayerUnitsManager.Instance.playerUnits[i].Unit != null)
            {
                count++;
            }
        }

        return Mathf.Max(1, count);
    }

    private void RaiseOnNewWaveStartedEvent(int waveNumber, int totalZombies)
    {
        OnNewWaveStarted(waveNumber);
        EventManager.Instance.Publish(new WaveStartedEvent(waveNumber, totalZombies));
        RpcWaveStarted(waveNumber, totalZombies);
    }

    [ClientRpc]
    private void RpcWaveStarted(int waveNumber, int totalZombies)
    {
        if (isServer)
        {
            return;
        }

        OnNewWaveStarted(waveNumber);
        EventManager.Instance.Publish(new WaveStartedEvent(waveNumber, totalZombies));
    }

    [Server]
    private void OnUnitDied(UnitDiedEvent unitDiedEvent)
    {
        if (unitDiedEvent?.Unit == null)
        {
            return;
        }

        if (unitDiedEvent.Unit.unitType != UnitType.Zombie)
        {
            if (unitDiedEvent.Unit.unitType == UnitType.Player)
            {
                TryIncrementPlayerDeath(unitDiedEvent.Unit);
            }
            return;
        }

        TryCreditZombieKill(unitDiedEvent.Killer);

        ZombieDropGold(unitDiedEvent.Unit, unitDiedEvent.Killer, DefaultGoldDrop);

        var identity = unitDiedEvent.Unit.netIdentity;
        if (identity == null)
        {
            return;
        }

        if (!trackedZombieNetIds.Remove(identity.netId))
        {
            return;
        }

        zombiesAlive = Mathf.Max(0, zombiesAlive - 1);
        currentWaveKilledCount = Mathf.Min(currentWaveTotalCount, currentWaveKilledCount + 1);
        RaiseWaveProgressChangedEvent();
        StartCoroutine(DespawnZombieAfterDelay(identity.gameObject));
    }

    [Server]
    private void OnUnitDamaged(UnitDamagedEvent unitDamagedEvent)
    {
        if (unitDamagedEvent == null || unitDamagedEvent.AppliedDamageAmount <= 0)
        {
            return;
        }

        if (unitDamagedEvent.Attacker == null || unitDamagedEvent.Attacker.unitType != UnitType.Player)
        {
            return;
        }

        if (!TryGetConnectionIdForPlayerUnit(unitDamagedEvent.Attacker, out int connectionId))
        {
            return;
        }

        if (!TryGetLeaderboardIndex(connectionId, out int rowIndex))
        {
            return;
        }

        var row = zombieLeaderboardEntries[rowIndex];
        row.Points += unitDamagedEvent.AppliedDamageAmount;
        row.PlayerName = ResolveDisplayName(connectionId, unitDamagedEvent.Attacker, row.PlayerName);
        row.IsConnected = true;
        zombieLeaderboardEntries[rowIndex] = row;
        SortLeaderboardRows();
    }

    [Server]
    private void OnPlayerReceivesGold(PlayerReceivesGoldEvent playerReceivesGoldEvent)
    {
        if (playerReceivesGoldEvent == null || playerReceivesGoldEvent.GoldAmount <= 0)
        {
            return;
        }

        var creditedPlayer = playerReceivesGoldEvent.Player;
        if (creditedPlayer == null || creditedPlayer.unitType != UnitType.Player)
        {
            return;
        }

        if (!TryGetConnectionIdForPlayerUnit(creditedPlayer, out int connectionId))
        {
            return;
        }

        if (!TryGetLeaderboardIndex(connectionId, out int rowIndex))
        {
            return;
        }

        var row = zombieLeaderboardEntries[rowIndex];
        row.GoldGathered += playerReceivesGoldEvent.GoldAmount;
        row.PlayerName = ResolveDisplayName(connectionId, creditedPlayer, row.PlayerName);
        row.IsConnected = true;
        zombieLeaderboardEntries[rowIndex] = row;
        SortLeaderboardRows();
    }

    [Server]
    private void OnPlayerUnitSpawned(PlayerUnitSpawnedEvent playerUnitSpawnedEvent)
    {
        if (playerUnitSpawnedEvent == null || playerUnitSpawnedEvent.Unit == null)
        {
            return;
        }

        EnsureLeaderboardEntry(playerUnitSpawnedEvent.ConnectionId, playerUnitSpawnedEvent.Unit.GetComponent<UnitController>(), true);
    }

    private void HandleLeaderboardListChanged(int _)
    {
        OnLeaderboardChanged();
    }

    private void HandleLeaderboardListChanged(int _, ZombieLeaderboardEntry __)
    {
        OnLeaderboardChanged();
    }

    private void HandleLeaderboardListCleared()
    {
        OnLeaderboardChanged();
    }

    [Server]
    private void ResetLeaderboardState()
    {
        zombieLeaderboardEntries.Clear();
        ReconcileLeaderboardConnectivity();
    }

    [Server]
    private void ReconcileLeaderboardConnectivity()
    {
        if (PlayerUnitsManager.Instance == null)
        {
            return;
        }

        var activeHumanConnectionIds = new HashSet<int>();
        for (int i = 0; i < PlayerUnitsManager.Instance.playerUnits.Count; i++)
        {
            var playerUnit = PlayerUnitsManager.Instance.playerUnits[i];
            if (playerUnit.ConnectionId < 0 || playerUnit.Unit == null)
            {
                continue;
            }

            activeHumanConnectionIds.Add(playerUnit.ConnectionId);
            EnsureLeaderboardEntry(playerUnit.ConnectionId, playerUnit.Unit.GetComponent<UnitController>(), true);
        }

        for (int i = 0; i < zombieLeaderboardEntries.Count; i++)
        {
            var row = zombieLeaderboardEntries[i];
            bool isConnected = activeHumanConnectionIds.Contains(row.ConnectionId);
            if (row.IsConnected == isConnected)
            {
                continue;
            }

            row.IsConnected = isConnected;
            zombieLeaderboardEntries[i] = row;
        }

        SortLeaderboardRows();
    }

    [Server]
    private void TryCreditZombieKill(UnitController killer)
    {
        if (killer == null || killer.unitType != UnitType.Player)
        {
            return;
        }

        if (!TryGetConnectionIdForPlayerUnit(killer, out int connectionId))
        {
            return;
        }

        if (!TryGetLeaderboardIndex(connectionId, out int rowIndex))
        {
            return;
        }

        var row = zombieLeaderboardEntries[rowIndex];
        row.Kills += 1;
        row.Points += KillPointsReward;
        row.PlayerName = ResolveDisplayName(connectionId, killer, row.PlayerName);
        row.IsConnected = true;
        zombieLeaderboardEntries[rowIndex] = row;
        SortLeaderboardRows();
    }

    [Server]
    private void TryIncrementPlayerDeath(UnitController deadUnit)
    {
        if (deadUnit == null || deadUnit.unitType != UnitType.Player)
        {
            return;
        }

        if (!TryGetConnectionIdForPlayerUnit(deadUnit, out int connectionId))
        {
            return;
        }

        if (!TryGetLeaderboardIndex(connectionId, out int rowIndex))
        {
            return;
        }

        var row = zombieLeaderboardEntries[rowIndex];
        row.Deaths += 1;
        row.PlayerName = ResolveDisplayName(connectionId, deadUnit, row.PlayerName);
        row.IsConnected = true;
        zombieLeaderboardEntries[rowIndex] = row;
        SortLeaderboardRows();
    }

    [Server]
    private bool TryGetConnectionIdForPlayerUnit(UnitController playerUnit, out int connectionId)
    {
        connectionId = default;
        if (playerUnit == null || PlayerUnitsManager.Instance == null)
        {
            return false;
        }

        for (int i = 0; i < PlayerUnitsManager.Instance.playerUnits.Count; i++)
        {
            var playerUnitEntry = PlayerUnitsManager.Instance.playerUnits[i];
            if (playerUnitEntry.ConnectionId < 0 || playerUnitEntry.Unit == null)
            {
                continue;
            }

            if (playerUnitEntry.Unit != playerUnit.gameObject)
            {
                continue;
            }

            connectionId = playerUnitEntry.ConnectionId;
            return true;
        }

        return false;
    }

    [Server]
    private bool TryGetLeaderboardIndex(int connectionId, out int index)
    {
        EnsureLeaderboardEntry(connectionId, null, false);
        for (int i = 0; i < zombieLeaderboardEntries.Count; i++)
        {
            if (zombieLeaderboardEntries[i].ConnectionId != connectionId)
            {
                continue;
            }

            index = i;
            return true;
        }

        index = -1;
        return false;
    }

    [Server]
    private void EnsureLeaderboardEntry(int connectionId, UnitController playerUnit, bool isConnected)
    {
        if (connectionId < 0)
        {
            return;
        }

        string resolvedName = playerUnit != null
            ? ResolveDisplayName(connectionId, playerUnit, $"Player {connectionId}")
            : null;

        for (int i = 0; i < zombieLeaderboardEntries.Count; i++)
        {
            var current = zombieLeaderboardEntries[i];
            if (current.ConnectionId != connectionId)
            {
                continue;
            }

            bool changed = false;
            if (!string.IsNullOrWhiteSpace(resolvedName) && !string.Equals(current.PlayerName, resolvedName, StringComparison.Ordinal))
            {
                current.PlayerName = resolvedName;
                changed = true;
            }

            if (current.IsConnected != isConnected)
            {
                current.IsConnected = isConnected;
                changed = true;
            }

            if (changed)
            {
                zombieLeaderboardEntries[i] = current;
            }
            return;
        }

        zombieLeaderboardEntries.Add(new ZombieLeaderboardEntry
        {
            ConnectionId = connectionId,
            PlayerName = ResolveDisplayName(connectionId, playerUnit, $"Player {connectionId}"),
            Points = 0,
            Kills = 0,
            Deaths = 0,
            GoldGathered = 0,
            IsConnected = isConnected
        });
        SortLeaderboardRows();
    }

    [Server]
    private void SortLeaderboardRows()
    {
        if (zombieLeaderboardEntries.Count <= 1)
        {
            return;
        }

        var orderedRows = zombieLeaderboardEntries
            .OrderByDescending(entry => entry.Points)
            .ThenByDescending(entry => entry.Kills)
            .ThenBy(entry => entry.Deaths)
            .ThenBy(entry => entry.ConnectionId)
            .ToList();

        bool changed = false;
        for (int i = 0; i < orderedRows.Count; i++)
        {
            var lhs = orderedRows[i];
            var rhs = zombieLeaderboardEntries[i];
            bool same = lhs.ConnectionId == rhs.ConnectionId
                && lhs.Points == rhs.Points
                && lhs.Kills == rhs.Kills
                && lhs.Deaths == rhs.Deaths
                && lhs.GoldGathered == rhs.GoldGathered
                && lhs.IsConnected == rhs.IsConnected
                && string.Equals(lhs.PlayerName, rhs.PlayerName, StringComparison.Ordinal);
            if (!same)
            {
                changed = true;
                break;
            }
        }

        if (!changed)
        {
            return;
        }

        zombieLeaderboardEntries.Clear();
        for (int i = 0; i < orderedRows.Count; i++)
        {
            zombieLeaderboardEntries.Add(orderedRows[i]);
        }
    }

    private static string ResolveDisplayName(int connectionId, UnitController playerUnit, string fallback)
    {
        if (playerUnit != null && !string.IsNullOrWhiteSpace(playerUnit.unitName))
        {
            return playerUnit.unitName;
        }

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        return $"Player {connectionId}";
    }

    [Server]
    private void BeginWaveProgressTracking(int totalCount)
    {
        currentWaveTotalCount = Mathf.Max(0, totalCount);
        currentWaveKilledCount = 0;
        RaiseWaveProgressChangedEvent();
    }

    [Server]
    private void HandleSpawnFailureForProgress()
    {
        currentWaveTotalCount = Mathf.Max(0, currentWaveTotalCount - 1);
        RaiseWaveProgressChangedEvent();
    }

    private void RaiseWaveProgressChangedEvent()
    {
        float percent = CurrentWaveKilledPercent;
        OnWaveProgressChanged(percent, currentWaveKilledCount, currentWaveTotalCount);
        EventManager.Instance.Publish(new WaveProgressChangedEvent(currentWave, currentWaveKilledCount, currentWaveTotalCount, percent));
    }

    [Server]
    private IEnumerator DespawnZombieAfterDelay(GameObject zombie)
    {
        if (zombie == null)
        {
            yield break;
        }

        yield return WaitForSecondsServer(modeConfig.spawnSettings.despawnDelaySeconds);
        if (!isServer || zombie == null)
        {
            yield break;
        }

        NetworkServer.Destroy(zombie);
    }

    [Server]
    private void ZombieDropGold(UnitController zombie, UnitController killer, int amount)
    {
        if (killer == null)
        {
            return;
        }

        if (killer.unitType != UnitType.Player)
        {
            return;
        }

        EventManager.Instance.Publish(new UnitDroppedGoldEvent(zombie, amount, killer));
        RpcZombieDroppedGold(amount, zombie, killer);

        EventManager.Instance.Publish(new PlayerReceivesGoldEvent(killer, amount));
        RpcPlayerReceivedGold(amount, killer);
    }

    [ClientRpc]
    private void RpcZombieDroppedGold(int amount, UnitController zombie, UnitController killer)
    {
        if (isServer)
        {
            return;
        }

        EventManager.Instance.Publish(new UnitDroppedGoldEvent(zombie, amount, killer));
    }

    [ClientRpc]
    private void RpcPlayerReceivedGold(int amount, UnitController player)
    {
        if (isServer)
        {
            return;
        }

        EventManager.Instance.Publish(new PlayerReceivesGoldEvent(player, amount));
    }

    private class WavePlan
    {
        public Queue<string> SpawnQueue;
        public float HealthMultiplier;
        public float DamageMultiplier;
    }
}
