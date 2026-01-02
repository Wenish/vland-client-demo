using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Base class for all unit spawners in the game.
/// Provides common spawning functionality, tracking, and server-authoritative spawning.
/// All spawning logic runs only on the server.
/// </summary>
public abstract class UnitSpawnerBase : NetworkBehaviour
{
    [Header("Spawner Identity")]
    [Tooltip("Unique identifier for this spawner (used for save/load and external references)")]
    public string spawnerId = "";

    [Header("Debug Settings")]
    [Tooltip("Show debug gizmos in the Scene view")]
    public bool showGizmos = true;

    [Tooltip("Color for gizmos in the Scene view")]
    public Color gizmoColor = Color.yellow;

    // Tracking
    protected List<GameObject> spawnedUnits = new List<GameObject>();
    protected bool isInitialized = false;
    protected bool isSpawning = false;

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // Generate unique spawner ID if not set
        if (string.IsNullOrEmpty(spawnerId))
        {
            spawnerId = $"{gameObject.name}_{GetInstanceID()}";
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Initialize();
    }

    protected virtual void OnDestroy()
    {
        if (isServer)
        {
            CleanupSpawnedUnits();
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize the spawner. Called automatically on server start.
    /// Override to add custom initialization logic.
    /// </summary>
    protected virtual void Initialize()
    {
        if (isInitialized)
        {
            Debug.LogWarning($"[UnitSpawnerBase] Spawner '{spawnerId}' already initialized!");
            return;
        }

        isInitialized = true;
    }

    #endregion

    #region Spawning Core

    /// <summary>
    /// Spawn a unit using the provided configuration.
    /// </summary>
    [Server]
    protected GameObject SpawnUnit(SpawnConfigurationBase config, Vector3? overridePosition = null)
    {
        if (config == null || !config.Validate())
        {
            Debug.LogError($"[UnitSpawnerBase] Invalid spawn configuration for spawner '{spawnerId}'!");
            return null;
        }

        // Determine spawn position
        Vector3 spawnPosition = overridePosition ?? config.GetSpawnPosition(transform.position);
        Quaternion spawnRotation = Quaternion.identity;

        // Use UnitSpawner singleton to spawn the unit
        if (UnitSpawner.Instance == null)
        {
            Debug.LogError($"[UnitSpawnerBase] UnitSpawner.Instance is null! Cannot spawn unit.");
            return null;
        }

        GameObject spawnedUnit = UnitSpawner.Instance.Spawn(
            config.unitData,
            spawnPosition,
            spawnRotation,
            isNpc: true
        );

        if (spawnedUnit != null)
        {
            // Track spawned unit
            spawnedUnits.Add(spawnedUnit);

            // Subscribe to death event for cleanup
            UnitController unitController = spawnedUnit.GetComponent<UnitController>();
            if (unitController != null)
            {
                unitController.OnDied += () => OnSpawnedUnitDied(spawnedUnit, unitController);
            }

            // Add spawn point tracker if not present
            SpawnPointTracker tracker = spawnedUnit.GetComponent<SpawnPointTracker>();
            if (tracker == null)
            {
                tracker = spawnedUnit.AddComponent<SpawnPointTracker>();
            }
            tracker.Initialize(spawnPosition, this);

            OnUnitSpawned(spawnedUnit, config);
        }

        return spawnedUnit;
    }

    /// <summary>
    /// Called when a unit is successfully spawned.
    /// Override to add custom post-spawn logic.
    /// </summary>
    protected virtual void OnUnitSpawned(GameObject unit, SpawnConfigurationBase config)
    {
        // Override in derived classes
    }

    /// <summary>
    /// Called when a spawned unit dies.
    /// Override to handle respawn logic or cleanup.
    /// </summary>
    protected virtual void OnSpawnedUnitDied(GameObject unit, UnitController unitController)
    {
        // Override in derived classes
    }

    /// <summary>
    /// Clean up a dead unit after a delay.
    /// </summary>
    [Server]
    protected void ScheduleCorpseCleanup(GameObject unit, SpawnConfigurationBase config)
    {
        if (unit == null || config == null) return;

        if (config.autoCleanupCorpse)
        {
            StartCoroutine(CleanupCorpseAfterDelay(unit, config.corpseCleanupDelay));
        }
    }

    /// <summary>
    /// Coroutine to destroy a unit after a delay.
    /// </summary>
    private System.Collections.IEnumerator CleanupCorpseAfterDelay(GameObject unit, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (unit != null)
        {
            NetworkServer.Destroy(unit);
        }
    }

    #endregion

    #region Tracking & Cleanup

    /// <summary>
    /// Get the number of currently alive units spawned by this spawner.
    /// </summary>
    protected int GetActiveUnitCount()
    {
        // Remove null references (destroyed units)
        spawnedUnits.RemoveAll(unit => unit == null);
        return spawnedUnits.Count;
    }

    /// <summary>
    /// Check if a unit was spawned by this spawner.
    /// </summary>
    protected bool IsUnitFromThisSpawner(GameObject unit)
    {
        return spawnedUnits.Contains(unit);
    }

    /// <summary>
    /// Remove a unit from tracking without destroying it.
    /// </summary>
    protected void UntrackUnit(GameObject unit)
    {
        spawnedUnits.Remove(unit);
    }

    /// <summary>
    /// Destroy all spawned units and clear tracking.
    /// </summary>
    [Server]
    protected void CleanupSpawnedUnits()
    {
        foreach (var unit in spawnedUnits)
        {
            if (unit != null)
            {
                NetworkServer.Destroy(unit);
            }
        }
        spawnedUnits.Clear();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Check if the spawner is currently active and spawning.
    /// </summary>
    public bool IsSpawning()
    {
        return isSpawning;
    }

    /// <summary>
    /// Get the spawner's unique identifier.
    /// </summary>
    public string GetSpawnerId()
    {
        return spawnerId;
    }

    /// <summary>
    /// Get all units currently spawned by this spawner.
    /// </summary>
    public List<GameObject> GetSpawnedUnits()
    {
        spawnedUnits.RemoveAll(unit => unit == null);
        return new List<GameObject>(spawnedUnits);
    }

    #endregion

    #region Gizmos

    protected virtual void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        
        // Draw spawner position marker
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor * 0.7f;
        
        // Draw spawner forward direction
        Gizmos.DrawRay(transform.position, transform.forward * 3f);
    }

    #endregion
}
