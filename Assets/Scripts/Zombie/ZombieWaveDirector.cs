using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ShadowInfection.Zombie
{
    public sealed class ZombieWaveDirector : IDisposable
    {
        private readonly IZombieWaveRuntime runtime;
        private readonly HashSet<uint> trackedZombieNetIds = new HashSet<uint>();
        private readonly Dictionary<string, int> recurringRuleNextWave = new Dictionary<string, int>();
        private CancellationTokenSource loopCts;
        private CancellationTokenSource despawnCts;

        public ZombieWaveDirector(IZombieWaveRuntime runtime)
        {
            this.runtime = runtime;
            despawnCts = new CancellationTokenSource();
        }

        public void ResetRecurringState()
        {
            recurringRuleNextWave.Clear();

            var modeConfig = runtime.ModeConfig;
            if (modeConfig == null || modeConfig.recurringSpecialWaves == null)
                return;

            for (int i = 0; i < modeConfig.recurringSpecialWaves.Count; i++)
            {
                var rule = modeConfig.recurringSpecialWaves[i];
                if (rule == null)
                    continue;

                if (string.IsNullOrWhiteSpace(rule.ruleId))
                    rule.ruleId = $"Rule_{i}";

                recurringRuleNextWave[rule.ruleId] = GetNextWaveFromInterval(rule, 0);
            }
        }

        public bool IsRunning => loopCts != null;

        public void Start()
        {
            if (loopCts != null)
                return;

            trackedZombieNetIds.Clear();
            runtime.SetZombiesAlive(0);
            runtime.SetQueuedSpawnCount(0);
            loopCts = new CancellationTokenSource();
            RunWaveLoopAsync(loopCts.Token).Forget();
        }

        public void Stop()
        {
            if (loopCts != null)
            {
                loopCts.Cancel();
                loopCts.Dispose();
                loopCts = null;
            }

            runtime.SetWaveRunning(false);
            runtime.SetQueuedSpawnCount(0);
        }

        public bool TryHandleZombieDeath(uint netId)
        {
            if (!trackedZombieNetIds.Remove(netId))
                return false;

            runtime.SetZombiesAlive(Mathf.Max(0, runtime.ZombiesAlive - 1));
            return true;
        }

        public void DespawnAfterDelay(GameObject zombie)
        {
            if (zombie == null || despawnCts == null)
                return;

            DespawnAfterDelayAsync(zombie, despawnCts.Token).Forget();
        }

        public void Dispose()
        {
            Stop();
            if (despawnCts != null)
            {
                despawnCts.Cancel();
                despawnCts.Dispose();
                despawnCts = null;
            }
        }

        private async UniTaskVoid RunWaveLoopAsync(CancellationToken ct)
        {
            var modeConfig = runtime.ModeConfig;
            if (modeConfig == null)
                return;

            try
            {
                while (runtime.IsServer && !runtime.IsGameOver && !ct.IsCancellationRequested)
                {
                    await WaitWhilePaused(ct);
                    if (runtime.IsGameOver)
                        return;

                    await WaitForSecondsServer(modeConfig.spawnSettings.timeBetweenWavesSeconds, ct);
                    if (runtime.IsGameOver)
                        return;

                    runtime.SetCurrentWave(runtime.CurrentWave + 1);

                    var wavePlan = BuildWavePlan(runtime.CurrentWave);
                    runtime.SetQueuedSpawnCount(wavePlan.SpawnQueue.Count);
                    runtime.BeginWaveProgress(wavePlan.SpawnQueue.Count);
                    runtime.SetWaveRunning(true);
                    runtime.NotifyWaveStarted(runtime.CurrentWave, wavePlan.SpawnQueue.Count);

                    while (wavePlan.SpawnQueue.Count > 0 && runtime.IsServer)
                    {
                        await WaitWhilePaused(ct);
                        if (runtime.IsGameOver)
                            return;

                        while (runtime.IsServer && runtime.ZombiesAlive >= modeConfig.spawnSettings.maxZombiesAlive)
                        {
                            if (runtime.IsGameOver)
                                return;

                            await UniTask.Yield(PlayerLoopTiming.Update, ct);
                        }

                        if (!runtime.IsServer || runtime.IsGameOver)
                            return;

                        string unitName = wavePlan.SpawnQueue.Dequeue();
                        if (runtime.TrySpawnZombie(unitName, wavePlan.HealthMultiplier, wavePlan.DamageMultiplier, out uint netId))
                            trackedZombieNetIds.Add(netId);
                        else
                            runtime.NotifySpawnFailure();

                        runtime.SetQueuedSpawnCount(wavePlan.SpawnQueue.Count);
                        await WaitForSecondsServer(modeConfig.spawnSettings.timeBetweenSpawnsSeconds, ct);
                    }

                    while (runtime.IsServer && runtime.ZombiesAlive > 0)
                    {
                        if (runtime.IsGameOver)
                            return;

                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    }

                    if (!runtime.IsServer || runtime.IsGameOver)
                        return;

                    runtime.SetWaveRunning(false);
                    runtime.NotifyWaveCompleted(runtime.CurrentWave, wavePlan.IsRecurringSpecial);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTaskVoid DespawnAfterDelayAsync(GameObject zombie, CancellationToken ct)
        {
            try
            {
                var delay = runtime.ModeConfig != null
                    ? runtime.ModeConfig.spawnSettings.despawnDelaySeconds
                    : 0f;
                await WaitForSecondsServer(delay, ct);
                if (runtime.IsServer && zombie != null)
                    runtime.DestroyZombie(zombie);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask WaitForSecondsServer(float seconds, CancellationToken ct)
        {
            if (seconds <= 0f)
                return;

            float endTime = Time.time + seconds;
            while (runtime.IsServer && Time.time < endTime)
            {
                if (runtime.IsPaused)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    continue;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        private async UniTask WaitWhilePaused(CancellationToken ct)
        {
            while (runtime.IsServer && runtime.IsPaused)
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        private ZombieWavePlan BuildWavePlan(int waveNumber)
        {
            var modeConfig = runtime.ModeConfig;
            ZombieModeConfig.WaveDefinition selectedWave = modeConfig.regularWave;
            bool isRecurringSpecial = false;

            if (modeConfig.TryGetOverride(waveNumber, out var fixedOverride) && fixedOverride.wave != null)
            {
                if (fixedOverride.replaceRegularWave)
                    selectedWave = fixedOverride.wave;
            }
            else
            {
                var recurringOverride = TryGetRecurringSpecialWave(waveNumber);
                if (recurringOverride != null && recurringOverride.wave != null && recurringOverride.replaceRegularWave)
                {
                    selectedWave = recurringOverride.wave;
                    isRecurringSpecial = true;
                }
            }

            int playerCount = runtime.GetActivePlayerCount();
            float unitWaveMultiplier = modeConfig.GetUnitCountWaveMultiplier(waveNumber);
            float healthWaveMultiplier = modeConfig.GetHealthWaveMultiplier(waveNumber);
            float damageWaveMultiplier = modeConfig.GetDamageWaveMultiplier(waveNumber);

            float playerCountMultiplier = 1f + Mathf.Max(0, playerCount - 1) * modeConfig.scaling.unitCountPerExtraPlayer;
            float playerHealthMultiplier = 1f + Mathf.Max(0, playerCount - 1) * modeConfig.scaling.healthPerExtraPlayer;
            float playerDamageMultiplier = 1f + Mathf.Max(0, playerCount - 1) * modeConfig.scaling.damagePerExtraPlayer;

            int totalToSpawn = Mathf.Max(1, Mathf.RoundToInt(selectedWave.baseTotalSpawnCount * unitWaveMultiplier * playerCountMultiplier));
            return new ZombieWavePlan
            {
                SpawnQueue = BuildSpawnQueue(selectedWave, totalToSpawn),
                HealthMultiplier = Mathf.Max(0.01f, healthWaveMultiplier * playerHealthMultiplier),
                DamageMultiplier = Mathf.Max(0.01f, damageWaveMultiplier * playerDamageMultiplier),
                IsRecurringSpecial = isRecurringSpecial
            };
        }

        private static Queue<string> BuildSpawnQueue(ZombieModeConfig.WaveDefinition waveDefinition, int totalSpawnCount)
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
                    break;
            }

            while (assigned < totalSpawnCount)
            {
                int chosen = ChooseWeightedEntryIndex(waveDefinition.entries, counts);
                if (chosen < 0)
                    break;

                counts[chosen]++;
                assigned++;
            }

            for (int i = 0; i < waveDefinition.entries.Count; i++)
            {
                string unitName = string.IsNullOrWhiteSpace(waveDefinition.entries[i].unitName)
                    ? "Infected"
                    : waveDefinition.entries[i].unitName;

                for (int j = 0; j < counts[i]; j++)
                    result.Add(unitName);
            }

            Shuffle(result);
            return new Queue<string>(result);
        }

        private static int ChooseWeightedEntryIndex(IReadOnlyList<ZombieModeConfig.SpawnEntry> entries, IReadOnlyList<int> counts)
        {
            float totalWeight = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.maxCount >= 0 && counts[i] >= entry.maxCount)
                    continue;

                totalWeight += Mathf.Max(0f, entry.weight);
            }

            if (totalWeight <= 0f)
                return -1;

            float pick = UnityEngine.Random.value * totalWeight;
            float accumulated = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.maxCount >= 0 && counts[i] >= entry.maxCount)
                    continue;

                accumulated += Mathf.Max(0f, entry.weight);
                if (pick <= accumulated)
                    return i;
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

        private ZombieModeConfig.RecurringSpecialWaveRule TryGetRecurringSpecialWave(int waveNumber)
        {
            var modeConfig = runtime.ModeConfig;
            if (modeConfig == null || modeConfig.recurringSpecialWaves == null)
                return null;

            for (int i = 0; i < modeConfig.recurringSpecialWaves.Count; i++)
            {
                var rule = modeConfig.recurringSpecialWaves[i];
                if (rule == null || !recurringRuleNextWave.TryGetValue(rule.ruleId, out int nextWave))
                    continue;

                if (waveNumber != nextWave)
                    continue;

                recurringRuleNextWave[rule.ruleId] = GetNextWaveFromInterval(rule, waveNumber);
                return rule;
            }

            return null;
        }

        private static int GetNextWaveFromInterval(ZombieModeConfig.RecurringSpecialWaveRule rule, int baseWave)
        {
            int minInterval = Mathf.Max(1, rule.minInterval);
            int maxInterval = Mathf.Max(minInterval, rule.maxInterval);
            int nextOffset = UnityEngine.Random.Range(minInterval, maxInterval + 1);
            return baseWave + nextOffset;
        }
    }
}
