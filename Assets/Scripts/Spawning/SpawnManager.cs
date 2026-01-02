using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>
/// Central manager for all spawners in the scene.
/// Provides global control, tracking, and coordination of spawn systems.
/// Runs only on the server.
/// </summary>
public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawner Registry")]
    [Tooltip("Automatically find all spawners in the scene on start")]
    public bool autoDiscoverSpawners = true;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool debugMode = false;

    // Spawner registries
    private Dictionary<string, MobSpawner> mobSpawners = new Dictionary<string, MobSpawner>();
    private Dictionary<string, BossSpawner> bossSpawners = new Dictionary<string, BossSpawner>();
    private List<UnitSpawnerBase> allSpawners = new List<UnitSpawnerBase>();

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton pattern
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

        if (autoDiscoverSpawners)
        {
            DiscoverSpawners();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region Spawner Discovery & Registration

    /// <summary>
    /// Find and register all spawners in the scene.
    /// </summary>
    [Server]
    public void DiscoverSpawners()
    {
        if (!isServer) return;

        // Clear existing registries
        mobSpawners.Clear();
        bossSpawners.Clear();
        allSpawners.Clear();

        // Find all mob spawners
        MobSpawner[] foundMobSpawners = FindObjectsByType<MobSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in foundMobSpawners)
        {
            RegisterMobSpawner(spawner);
        }

        // Find all boss spawners
        BossSpawner[] foundBossSpawners = FindObjectsByType<BossSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in foundBossSpawners)
        {
            RegisterBossSpawner(spawner);
        }

        if (debugMode)
        {
            Debug.Log($"[SpawnManager] Discovered {mobSpawners.Count} mob spawners and {bossSpawners.Count} boss spawners");
        }
    }

    /// <summary>
    /// Register a mob spawner with the manager.
    /// </summary>
    [Server]
    public void RegisterMobSpawner(MobSpawner spawner)
    {
        if (spawner == null) return;

        string spawnerId = spawner.GetSpawnerId();
        
        if (mobSpawners.ContainsKey(spawnerId))
        {
            Debug.LogWarning($"[SpawnManager] Mob spawner '{spawnerId}' is already registered!");
            return;
        }

        mobSpawners[spawnerId] = spawner;
        allSpawners.Add(spawner);

        if (debugMode)
        {
            Debug.Log($"[SpawnManager] Registered mob spawner: {spawnerId}");
        }
    }

    /// <summary>
    /// Register a boss spawner with the manager.
    /// </summary>
    [Server]
    public void RegisterBossSpawner(BossSpawner spawner)
    {
        if (spawner == null) return;

        string spawnerId = spawner.GetSpawnerId();
        
        if (bossSpawners.ContainsKey(spawnerId))
        {
            Debug.LogWarning($"[SpawnManager] Boss spawner '{spawnerId}' is already registered!");
            return;
        }

        bossSpawners[spawnerId] = spawner;
        allSpawners.Add(spawner);

        if (debugMode)
        {
            Debug.Log($"[SpawnManager] Registered boss spawner: {spawnerId}");
        }
    }

    /// <summary>
    /// Unregister a spawner from the manager.
    /// </summary>
    [Server]
    public void UnregisterSpawner(string spawnerId)
    {
        if (mobSpawners.ContainsKey(spawnerId))
        {
            allSpawners.Remove(mobSpawners[spawnerId]);
            mobSpawners.Remove(spawnerId);
        }
        else if (bossSpawners.ContainsKey(spawnerId))
        {
            allSpawners.Remove(bossSpawners[spawnerId]);
            bossSpawners.Remove(spawnerId);
        }
    }

    #endregion

    #region Spawner Control

    /// <summary>
    /// Start all mob spawners.
    /// </summary>
    [Server]
    public void StartAllMobSpawners()
    {
        if (!isServer) return;

        foreach (var spawner in mobSpawners.Values)
        {
            if (spawner != null)
            {
                spawner.StartSpawning();
            }
        }

        if (debugMode)
        {
            Debug.Log($"[SpawnManager] Started all mob spawners");
        }
    }

    /// <summary>
    /// Stop all mob spawners.
    /// </summary>
    [Server]
    public void StopAllMobSpawners()
    {
        if (!isServer) return;

        foreach (var spawner in mobSpawners.Values)
        {
            if (spawner != null)
            {
                spawner.StopSpawning();
            }
        }

        if (debugMode)
        {
            Debug.Log($"[SpawnManager] Stopped all mob spawners");
        }
    }

    /// <summary>
    /// Activate a specific boss encounter.
    /// </summary>
    [Server]
    public void ActivateBossEncounter(string spawnerId)
    {
        if (!isServer) return;

        if (bossSpawners.TryGetValue(spawnerId, out BossSpawner spawner))
        {
            if (spawner != null)
            {
                spawner.ActivateEncounter();
                
                if (debugMode)
                {
                    Debug.Log($"[SpawnManager] Activated boss encounter: {spawnerId}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[SpawnManager] Boss spawner '{spawnerId}' not found!");
        }
    }

    /// <summary>
    /// Set active state for a specific mob spawner.
    /// </summary>
    [Server]
    public void SetMobSpawnerActive(string spawnerId, bool active)
    {
        if (!isServer) return;

        if (mobSpawners.TryGetValue(spawnerId, out MobSpawner spawner))
        {
            if (spawner != null)
            {
                spawner.SetActive(active);
            }
        }
        else
        {
            Debug.LogWarning($"[SpawnManager] Mob spawner '{spawnerId}' not found!");
        }
    }

    /// <summary>
    /// Set active state for multiple mob spawners by group ID.
    /// Useful for wave-based progression or area unlocking.
    /// </summary>
    [Server]
    public void SetMobSpawnerGroupActive(int groupId, bool active)
    {
        if (!isServer) return;

        // This assumes spawners can be tagged with group IDs
        // You can extend MobSpawner to include a groupId field for this functionality
        // For now, this is a placeholder that can be expanded

        if (debugMode)
        {
            Debug.Log($"[SpawnManager] Set spawner group {groupId} active: {active}");
        }
    }

    #endregion

    #region Querying

    /// <summary>
    /// Get a mob spawner by its ID.
    /// </summary>
    public MobSpawner GetMobSpawner(string spawnerId)
    {
        mobSpawners.TryGetValue(spawnerId, out MobSpawner spawner);
        return spawner;
    }

    /// <summary>
    /// Get a boss spawner by its ID.
    /// </summary>
    public BossSpawner GetBossSpawner(string spawnerId)
    {
        bossSpawners.TryGetValue(spawnerId, out BossSpawner spawner);
        return spawner;
    }

    /// <summary>
    /// Get all registered mob spawners.
    /// </summary>
    public List<MobSpawner> GetAllMobSpawners()
    {
        return mobSpawners.Values.Where(s => s != null).ToList();
    }

    /// <summary>
    /// Get all registered boss spawners.
    /// </summary>
    public List<BossSpawner> GetAllBossSpawners()
    {
        return bossSpawners.Values.Where(s => s != null).ToList();
    }

    /// <summary>
    /// Get all active spawners (currently spawning).
    /// </summary>
    public List<UnitSpawnerBase> GetActiveSpawners()
    {
        return allSpawners.Where(s => s != null && s.IsSpawning()).ToList();
    }

    /// <summary>
    /// Get total count of all units spawned by all spawners.
    /// </summary>
    public int GetTotalSpawnedUnitCount()
    {
        int total = 0;
        foreach (var spawner in allSpawners)
        {
            if (spawner != null)
            {
                total += spawner.GetSpawnedUnits().Count;
            }
        }
        return total;
    }

    /// <summary>
    /// Get all units spawned by all spawners.
    /// </summary>
    public List<GameObject> GetAllSpawnedUnits()
    {
        List<GameObject> allUnits = new List<GameObject>();
        foreach (var spawner in allSpawners)
        {
            if (spawner != null)
            {
                allUnits.AddRange(spawner.GetSpawnedUnits());
            }
        }
        return allUnits;
    }

    #endregion

    #region Statistics & Debugging

    /// <summary>
    /// Get statistics about the spawn system.
    /// </summary>
    public SpawnStatistics GetStatistics()
    {
        return new SpawnStatistics
        {
            totalMobSpawners = mobSpawners.Count,
            totalBossSpawners = bossSpawners.Count,
            activeMobSpawners = mobSpawners.Values.Count(s => s != null && s.IsSpawning()),
            activeBossEncounters = bossSpawners.Values.Count(s => s != null && s.isEncounterActive),
            totalSpawnedUnits = GetTotalSpawnedUnitCount()
        };
    }

    /// <summary>
    /// Log spawn system statistics to console.
    /// </summary>
    [Server]
    public void LogStatistics()
    {
        if (!isServer) return;

        SpawnStatistics stats = GetStatistics();
        Debug.Log($"[SpawnManager] Statistics:");
        Debug.Log($"  Mob Spawners: {stats.totalMobSpawners} ({stats.activeMobSpawners} active)");
        Debug.Log($"  Boss Spawners: {stats.totalBossSpawners} ({stats.activeBossEncounters} active)");
        Debug.Log($"  Total Spawned Units: {stats.totalSpawnedUnits}");
    }

    #endregion
}

/// <summary>
/// Statistics about the spawn system.
/// </summary>
public struct SpawnStatistics
{
    public int totalMobSpawners;
    public int totalBossSpawners;
    public int activeMobSpawners;
    public int activeBossEncounters;
    public int totalSpawnedUnits;
}
