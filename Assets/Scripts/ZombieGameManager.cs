using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using MyGame.Events;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZombieGameManager : NetworkBehaviour
{
    private const int DefaultGoldDrop = 10;

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

    private readonly HashSet<uint> trackedZombieNetIds = new HashSet<uint>();
    private readonly Dictionary<string, int> recurringRuleNextWave = new Dictionary<string, int>();
    private Coroutine waveLoopCoroutine;

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

    private void Awake()
    {
        Singleton = this;
        RefreshZombieSpawns();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        EventManager.Instance.Subscribe<UnitDiedEvent>(OnUnitDied);
        ResetRecurringSpecialWaveState();

        if (autoStartOnServer)
        {
            StartZombieMode();
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        StopZombieMode();
        EventManager.Instance.Unsubscribe<UnitDiedEvent>(OnUnitDied);
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
            return;
        }

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
