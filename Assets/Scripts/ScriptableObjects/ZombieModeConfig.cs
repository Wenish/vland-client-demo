using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ZombieModeConfig", menuName = "Game/Zombie/Mode Config")]
public class ZombieModeConfig : ScriptableObject
{
    public enum CurveExtrapolationMode
    {
        ClampToLastKey = 0,
        Linear = 1,
        Exponential = 2
    }

    [Serializable]
    public class SpawnEntry
    {
        [Tooltip("Unit name from your UnitDatabase.")]
        public string unitName = "Infected";

        [Tooltip("Always spawn at least this many of this unit in the wave (if total count allows it).")]
        [Min(0)] public int guaranteedCount = 0;

        [Tooltip("Relative spawn chance for the remaining slots after guaranteed counts are assigned.")]
        [Min(0.01f)] public float weight = 1f;

        [Tooltip("Maximum allowed of this unit in this wave. -1 means unlimited.")]
        [Min(-1)] public int maxCount = -1;
    }

    [Serializable]
    public class WaveDefinition
    {
        [Tooltip("Display/debug name for this wave definition.")]
        public string waveName = "Wave";

        [Tooltip("Base total units before wave and player scaling is applied.")]
        [Min(1)] public int baseTotalSpawnCount = 1;

        [Tooltip("Unit composition for this wave.")]
        public List<SpawnEntry> entries = new List<SpawnEntry>();
    }

    [Serializable]
    public class WaveOverride
    {
        [Tooltip("Exact wave number this override applies to.")]
        [Min(1)] public int waveNumber = 1;

        [Tooltip("When enabled, this override replaces the regular wave definition.")]
        public bool replaceRegularWave = true;

        [Tooltip("Wave setup used for this exact wave number.")]
        public WaveDefinition wave = new WaveDefinition();
    }

    [Serializable]
    public class RecurringSpecialWaveRule
    {
        [Tooltip("Unique id used to track this recurring rule state.")]
        public string ruleId = "FastWave";

        [Tooltip("Minimum number of waves until this special wave happens again.")]
        [Min(1)] public int minInterval = 4;

        [Tooltip("Maximum number of waves until this special wave happens again.")]
        [Min(1)] public int maxInterval = 6;

        [Tooltip("When enabled, this special wave replaces the regular wave when triggered.")]
        public bool replaceRegularWave = true;

        [Tooltip("Wave setup used when this recurring rule triggers.")]
        public WaveDefinition wave = new WaveDefinition();
    }

    [Serializable]
    public class SpawnSettings
    {
        [Tooltip("Delay between wave starts in seconds.")]
        [Min(0f)] public float timeBetweenWavesSeconds = 10f;

        [Tooltip("Delay between two spawns in seconds.")]
        [Min(0f)] public float timeBetweenSpawnsSeconds = 0.5f;

        [Tooltip("Maximum concurrent alive zombies. Controls pressure but not total wave size.")]
        [Min(1)] public int maxZombiesAlive = 5;

        [Tooltip("Delay after a zombie dies until it is despawned from network in seconds.")]
        [Min(0f)] public float despawnDelaySeconds = 5f;
    }

    [Serializable]
    public class ScalingSettings
    {
        [Header("Player Count Scaling (Linear)")]
        [Tooltip("Extra spawn multiplier per extra player. Example: 0.5 means 2 players = x1.5 total count.")]
        [Min(0f)] public float unitCountPerExtraPlayer = 1f;

        [Tooltip("Extra health multiplier per extra player. Example: 0.2 means 2 players = x1.2 health.")]
        [Min(0f)] public float healthPerExtraPlayer = 0.2f;

        [Tooltip("Extra damage multiplier per extra player. Example: 0.15 means 2 players = x1.15 damage.")]
        [Min(0f)] public float damagePerExtraPlayer = 0.15f;

        [Header("Wave Scaling Curves")]
        [Tooltip("Wave -> count multiplier. Final units = baseTotalSpawnCount * this multiplier * player multiplier.")]
        public AnimationCurve unitCountByWave = AnimationCurve.Linear(1f, 1f, 20f, 4f);

        [Tooltip("How unitCountByWave behaves after the last curve key. Use Exponential for infinite growth.")]
        public CurveExtrapolationMode unitCountExtrapolation = CurveExtrapolationMode.Exponential;

        [Tooltip("Wave -> health multiplier for spawned zombies.")]
        public AnimationCurve healthByWave = AnimationCurve.Linear(1f, 1f, 20f, 3f);

        [Tooltip("How healthByWave behaves after the last curve key.")]
        public CurveExtrapolationMode healthExtrapolation = CurveExtrapolationMode.ClampToLastKey;

        [Tooltip("Wave -> damage multiplier for spawned zombies.")]
        public AnimationCurve damageByWave = AnimationCurve.Linear(1f, 1f, 20f, 2f);

        [Tooltip("How damageByWave behaves after the last curve key.")]
        public CurveExtrapolationMode damageExtrapolation = CurveExtrapolationMode.ClampToLastKey;
    }

    [Header("Spawning")]
    public SpawnSettings spawnSettings = new SpawnSettings();

    [Header("Regular Wave")]
    public WaveDefinition regularWave = new WaveDefinition
    {
        waveName = "Regular Wave",
        baseTotalSpawnCount = 5,
        entries = new List<SpawnEntry>
        {
            new SpawnEntry { unitName = "Infected", guaranteedCount = 4, weight = 4f, maxCount = -1 },
            new SpawnEntry { unitName = "Crawler", guaranteedCount = 1, weight = 1f, maxCount = -1 }
        }
    };

    [Header("Exact Round Overrides")]
    public List<WaveOverride> waveOverrides = new List<WaveOverride>();

    [Header("Recurring Special Waves")]
    public List<RecurringSpecialWaveRule> recurringSpecialWaves = new List<RecurringSpecialWaveRule>();

    [Header("Scaling")]
    public ScalingSettings scaling = new ScalingSettings();

    public float GetUnitCountWaveMultiplier(int waveNumber)
    {
        return EvaluateWaveScalingCurve(scaling.unitCountByWave, waveNumber, scaling.unitCountExtrapolation, 0f);
    }

    public float GetHealthWaveMultiplier(int waveNumber)
    {
        return EvaluateWaveScalingCurve(scaling.healthByWave, waveNumber, scaling.healthExtrapolation, 0.01f);
    }

    public float GetDamageWaveMultiplier(int waveNumber)
    {
        return EvaluateWaveScalingCurve(scaling.damageByWave, waveNumber, scaling.damageExtrapolation, 0.01f);
    }

    public bool TryGetOverride(int waveNumber, out WaveOverride waveOverride)
    {
        for (int i = 0; i < waveOverrides.Count; i++)
        {
            var candidate = waveOverrides[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.waveNumber == waveNumber)
            {
                waveOverride = candidate;
                return true;
            }
        }

        waveOverride = null;
        return false;
    }

    private float EvaluateWaveScalingCurve(
        AnimationCurve curve,
        float x,
        CurveExtrapolationMode extrapolationMode,
        float minValue)
    {
        if (curve == null || curve.length == 0)
        {
            return Mathf.Max(minValue, 1f);
        }

        float evaluated = curve.Evaluate(x);
        var keys = curve.keys;
        var last = keys[keys.Length - 1];

        if (x <= last.time)
        {
            return Mathf.Max(minValue, evaluated);
        }

        if (extrapolationMode == CurveExtrapolationMode.ClampToLastKey || keys.Length == 1)
        {
            return Mathf.Max(minValue, last.value);
        }

        var prev = keys[keys.Length - 2];
        float deltaTime = Mathf.Max(0.0001f, last.time - prev.time);
        float beyondLast = x - last.time;

        if (extrapolationMode == CurveExtrapolationMode.Linear)
        {
            float slopePerWave = (last.value - prev.value) / deltaTime;
            return Mathf.Max(minValue, last.value + slopePerWave * beyondLast);
        }

        if (Mathf.Approximately(prev.value, 0f) || prev.value < 0f || last.value <= 0f)
        {
            return Mathf.Max(minValue, last.value);
        }

        float factorPerWave = Mathf.Pow(last.value / prev.value, 1f / deltaTime);
        return Mathf.Max(minValue, last.value * Mathf.Pow(factorPerWave, beyondLast));
    }
}
