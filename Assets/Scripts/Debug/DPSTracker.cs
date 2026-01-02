using System.Collections.Generic;
using System.Linq;
using MyGame.Events;
using UnityEngine;

namespace ShadowInfection.Debug
{
    /// <summary>
    /// Lightweight DPS tracking service that observes damage events and calculates
    /// damage-per-second for units over a rolling 60-second window.
    /// Non-invasive: uses event system to passively listen to damage without modifying game code.
    /// </summary>
    public class DPSTracker : MonoBehaviour
    {
        public static DPSTracker Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Time window for DPS calculation (in seconds)")]
        [SerializeField]
        private float timeWindow = 60f;

        [Tooltip("Only track player's team (set at runtime via MyPlayerUnitSpawnedEvent)")]
        private int playerTeam = -1;

        // Track damage events: unit -> list of (damage, timestamp) pairs
        private Dictionary<UnitController, List<DamageRecord>> damageHistory = new Dictionary<UnitController, List<DamageRecord>>();

        // Cache for calculated DPS values to avoid recalculation every frame
        private Dictionary<UnitController, float> dpsCache = new Dictionary<UnitController, float>();
        private float lastCacheUpdateTime;
        private const float CACHE_UPDATE_INTERVAL = 0.1f; // Update cache 10 times per second

        private struct DamageRecord
        {
            public float damage;
            public float timestamp;

            public DamageRecord(float damage, float timestamp)
            {
                this.damage = damage;
                this.timestamp = timestamp;
            }
        }

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
                EventManager.Instance.Subscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
            }
        }

        private void OnDisable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
                EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(OnMyPlayerUnitSpawned);
            }
        }

        private void Update()
        {
            // Clean up old damage records periodically
            CleanupOldRecords();

            // Update DPS cache at intervals
            if (Time.time - lastCacheUpdateTime >= CACHE_UPDATE_INTERVAL)
            {
                UpdateDPSCache();
                lastCacheUpdateTime = Time.time;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Listen to damage events and track damage dealt by units on player's team.
        /// </summary>
        private void OnUnitDamaged(UnitDamagedEvent evt)
        {
            if (evt == null || evt.Attacker == null) return;

            // Only track damage from player's team
            if (playerTeam >= 0 && evt.Attacker.team != playerTeam) return;

            // Get or create damage history for this attacker
            if (!damageHistory.ContainsKey(evt.Attacker))
            {
                damageHistory[evt.Attacker] = new List<DamageRecord>();
            }

            // Record the damage with timestamp
            damageHistory[evt.Attacker].Add(new DamageRecord(evt.DamageAmount, Time.time));
        }

        /// <summary>
        /// Capture the player's team when their unit spawns.
        /// </summary>
        private void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent evt)
        {
            if (evt == null || evt.PlayerCharacter == null) return;
            playerTeam = evt.PlayerCharacter.team;
        }

        #endregion

        #region DPS Calculation

        /// <summary>
        /// Get DPS for a specific unit (from cache).
        /// </summary>
        public float GetDPS(UnitController unit)
        {
            if (unit == null) return 0f;
            return dpsCache.ContainsKey(unit) ? dpsCache[unit] : 0f;
        }

        /// <summary>
        /// Get all units that have dealt damage in the time window, sorted by DPS descending.
        /// Returns only units that dealt damage within the last 60 seconds.
        /// </summary>
        public List<(UnitController unit, float dps)> GetActiveDPSUnits()
        {
            float currentTime = Time.time;
            var activeUnits = new List<(UnitController, float)>();

            foreach (var kvp in damageHistory)
            {
                var unit = kvp.Key;
                var records = kvp.Value;

                // Skip if unit is null or dead
                if (unit == null || unit.IsDead) continue;

                // Skip if unit has no recent damage
                if (records.Count == 0) continue;
                if (currentTime - records[records.Count - 1].timestamp > timeWindow) continue;

                // Get cached DPS
                float dps = dpsCache.ContainsKey(unit) ? dpsCache[unit] : 0f;
                
                // Only include units with DPS > 0
                if (dps > 0f)
                {
                    activeUnits.Add((unit, dps));
                }
            }

            // Sort by DPS descending
            activeUnits.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            return activeUnits;
        }

        /// <summary>
        /// Update the DPS cache for all tracked units.
        /// </summary>
        private void UpdateDPSCache()
        {
            float currentTime = Time.time;
            dpsCache.Clear();

            foreach (var kvp in damageHistory)
            {
                var unit = kvp.Key;
                var records = kvp.Value;

                if (unit == null || records.Count == 0) continue;

                // Calculate total damage in time window
                float totalDamage = 0f;
                float oldestTimestamp = float.MaxValue;

                foreach (var record in records)
                {
                    if (currentTime - record.timestamp <= timeWindow)
                    {
                        totalDamage += record.damage;
                        if (record.timestamp < oldestTimestamp)
                            oldestTimestamp = record.timestamp;
                        // newest timestamp is not needed for current DPS calculation
                    }
                }

                // Calculate DPS: total damage / time span within rolling window
                // Use actual time since first damage, clamped to the configured window
                // This prevents artificially low DPS for units that just started fighting
                if (totalDamage > 0f && oldestTimestamp != float.MaxValue)
                {
                    float timeSpan = currentTime - oldestTimestamp;
                    // Clamp to window and enforce a small floor to avoid division spikes
                    float divisor = Mathf.Max(Mathf.Min(timeSpan, timeWindow), 0.5f);
                    dpsCache[unit] = totalDamage / divisor;
                }
            }
        }

        /// <summary>
        /// Remove damage records older than the time window to prevent memory bloat.
        /// </summary>
        private void CleanupOldRecords()
        {
            float currentTime = Time.time;
            var unitsToRemove = new List<UnitController>();

            foreach (var kvp in damageHistory)
            {
                var unit = kvp.Key;
                var records = kvp.Value;

                // Remove old records
                records.RemoveAll(r => currentTime - r.timestamp > timeWindow);

                // Mark units with no records for removal
                if (records.Count == 0 || unit == null)
                {
                    unitsToRemove.Add(unit);
                }
            }

            // Clean up empty entries
            foreach (var unit in unitsToRemove)
            {
                damageHistory.Remove(unit);
                dpsCache.Remove(unit);
            }
        }

        /// <summary>
        /// Clear all tracking data (useful for testing or reset).
        /// </summary>
        public void ClearAllData()
        {
            damageHistory.Clear();
            dpsCache.Clear();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the time window used for DPS calculation.
        /// </summary>
        public float GetTimeWindow() => timeWindow;

        /// <summary>
        /// Check if tracker is initialized with player team.
        /// </summary>
        public bool IsInitialized() => playerTeam >= 0;

        /// <summary>
        /// Get the player team being tracked.
        /// </summary>
        public int GetPlayerTeam() => playerTeam;

        #endregion
    }
}
