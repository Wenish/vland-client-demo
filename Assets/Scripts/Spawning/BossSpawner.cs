using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Spawner for boss encounters (unique enemies, one-time spawns, phase-driven fights).
/// Supports manual activation, one-time spawning, escort units, and encounter mechanics.
/// Integrates with existing BehaviourExecutor and HealthPhaseManager for boss mechanics.
/// Runs only on the server.
/// </summary>
public class BossSpawner : UnitSpawnerBase
{
    [Header("Boss Spawner Configuration")]
    [Tooltip("The boss spawn configuration to use")]
    public SpawnConfigurationBoss spawnConfiguration;

    [Header("Spawner State")]
    [Tooltip("Has this boss been spawned and defeated (for one-time spawns)?")]
    public bool hasBeenDefeated = false;

    [Tooltip("Is this boss encounter currently active?")]
    public bool isEncounterActive = false;

    // Boss tracking
    private GameObject bossInstance;
    private List<GameObject> escortUnits = new List<GameObject>();
    private bool isWaitingForActivation = false;

    #region Initialization

    protected override void Initialize()
    {
        base.Initialize();

        if (spawnConfiguration == null)
        {
            Debug.LogError($"[BossSpawner] Spawner '{spawnerId}' has no spawn configuration!");
            return;
        }

        if (!spawnConfiguration.Validate())
        {
            Debug.LogError($"[BossSpawner] Spawner '{spawnerId}' has invalid configuration!");
            return;
        }

        // If requires activation, wait for manual trigger
        if (spawnConfiguration.requiresActivation)
        {
            isWaitingForActivation = true;
        }
        else
        {
            // Auto-spawn if not requiring activation
            StartEncounter();
        }
    }

    #endregion

    #region Encounter Control

    /// <summary>
    /// Manually activate the boss encounter.
    /// Can be called from triggers, events, or other systems.
    /// </summary>
    [Server]
    public void ActivateEncounter()
    {
        if (!isServer) return;

        if (!isWaitingForActivation)
        {
            Debug.LogWarning($"[BossSpawner] Boss '{spawnerId}' is not waiting for activation!");
            return;
        }

        if (hasBeenDefeated && spawnConfiguration.oneTimeSpawn)
        {
            Debug.LogWarning($"[BossSpawner] Boss '{spawnerId}' has already been defeated (one-time spawn)!");
            return;
        }

        isWaitingForActivation = false;
        StartEncounter();
    }

    /// <summary>
    /// Start the boss encounter.
    /// </summary>
    [Server]
    private void StartEncounter()
    {
        if (!isServer) return;

        if (isEncounterActive)
        {
            Debug.LogWarning($"[BossSpawner] Boss encounter '{spawnerId}' is already active!");
            return;
        }

        if (hasBeenDefeated && spawnConfiguration.oneTimeSpawn)
        {
            Debug.LogWarning($"[BossSpawner] Boss '{spawnerId}' has already been defeated!");
            return;
        }

        StartCoroutine(EncounterSequence());
    }

    /// <summary>
    /// End the boss encounter (called when boss is defeated).
    /// </summary>
    [Server]
    private void EndEncounter()
    {
        if (!isServer) return;

        isEncounterActive = false;
        hasBeenDefeated = true;

        // Cleanup escort units
        CleanupEscorts();

        // Handle respawn for non-one-time spawns
        if (!spawnConfiguration.oneTimeSpawn)
        {
            StartCoroutine(RespawnBoss());
        }
    }

    #endregion

    #region Spawn Logic

    private IEnumerator EncounterSequence()
    {
        isEncounterActive = true;
        isSpawning = true;

        // Activation delay
        if (spawnConfiguration.activationDelay > 0f)
        {
            yield return new WaitForSeconds(spawnConfiguration.activationDelay);
        }

        // Initial spawn delay
        if (spawnConfiguration.initialSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(spawnConfiguration.initialSpawnDelay);
        }

        // Spawn the boss
        SpawnBoss();

        // Spawn escort units if configured
        if (spawnConfiguration.spawnEscorts && spawnConfiguration.escortCount > 0)
        {
            SpawnEscorts();
        }

        isSpawning = false;
    }

    private void SpawnBoss()
    {
        bossInstance = SpawnUnit(spawnConfiguration);

        if (bossInstance != null)
        {
            // Additional boss-specific setup can go here
            // The UnitSpawner already handles BehaviourExecutor setup
        }
    }

    private void SpawnEscorts()
    {
        if (spawnConfiguration.escortConfiguration == null)
        {
            Debug.LogWarning($"[BossSpawner] No escort configuration set for boss '{spawnerId}'!");
            return;
        }

        StartCoroutine(SpawnEscortsCoroutine());
    }

    private IEnumerator SpawnEscortsCoroutine()
    {
        for (int i = 0; i < spawnConfiguration.escortCount; i++)
        {
            // Spawn escort using the escort configuration
            GameObject escort = SpawnUnit(spawnConfiguration.escortConfiguration);
            
            if (escort != null)
            {
                escortUnits.Add(escort);
                
                // Subscribe to escort death
                UnitController escortController = escort.GetComponent<UnitController>();
                if (escortController != null)
                {
                    escortController.OnDied += () => OnEscortDied(escort);
                }
            }

            // Small delay between escort spawns
            if (i < spawnConfiguration.escortCount - 1)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    protected override void OnSpawnedUnitDied(GameObject unit, UnitController unitController)
    {
        base.OnSpawnedUnitDied(unit, unitController);

        // Schedule corpse cleanup
        ScheduleCorpseCleanup(unit, spawnConfiguration);

        // Check if this is the boss
        if (unit == bossInstance)
        {
            OnBossDied();
        }
    }

    private void OnBossDied()
    {
        bossInstance = null;
        EndEncounter();
    }

    private void OnEscortDied(GameObject escort)
    {
        escortUnits.Remove(escort);
        
        // Schedule cleanup for escorts too
        if (spawnConfiguration != null && spawnConfiguration.escortConfiguration != null)
        {
            ScheduleCorpseCleanup(escort, spawnConfiguration.escortConfiguration);
        }
    }

    #endregion

    #region Respawn Logic

    private IEnumerator RespawnBoss()
    {
        yield return new WaitForSeconds(spawnConfiguration.respawnDelay);

        // Reset encounter state
        hasBeenDefeated = false;
        
        // Restart encounter if not requiring manual activation
        if (!spawnConfiguration.requiresActivation)
        {
            StartEncounter();
        }
        else
        {
            isWaitingForActivation = true;
        }
    }

    #endregion

    #region Cleanup

    private void CleanupEscorts()
    {
        foreach (var escort in escortUnits)
        {
            if (escort != null)
            {
                NetworkServer.Destroy(escort);
            }
        }
        escortUnits.Clear();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (isServer)
        {
            CleanupEscorts();
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Check if the boss is currently alive.
    /// </summary>
    public bool IsBossAlive()
    {
        return bossInstance != null;
    }

    /// <summary>
    /// Get the boss instance if it exists.
    /// </summary>
    public GameObject GetBossInstance()
    {
        return bossInstance;
    }

    /// <summary>
    /// Get all currently alive escort units.
    /// </summary>
    public List<GameObject> GetEscortUnits()
    {
        escortUnits.RemoveAll(unit => unit == null);
        return new List<GameObject>(escortUnits);
    }

    /// <summary>
    /// Check if the encounter is waiting for activation.
    /// </summary>
    public bool IsWaitingForActivation()
    {
        return isWaitingForActivation;
    }

    /// <summary>
    /// Reset the boss spawner (clear defeated state, allow re-spawn).
    /// Useful for testing or resetting encounters.
    /// </summary>
    [Server]
    public void ResetEncounter()
    {
        if (!isServer) return;

        hasBeenDefeated = false;
        isEncounterActive = false;
        isWaitingForActivation = spawnConfiguration.requiresActivation;

        // Cleanup existing boss and escorts
        if (bossInstance != null)
        {
            NetworkServer.Destroy(bossInstance);
            bossInstance = null;
        }

        CleanupEscorts();
    }

    #endregion

    #region Gizmos

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (!showGizmos || spawnConfiguration == null) return;

        // Use distinct color for boss spawners
        Gizmos.color = Color.red;

        // Draw larger marker for boss spawner
        Gizmos.DrawWireSphere(transform.position, 1f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 3f);

        // Draw spawn area if using area spawning
        if (spawnConfiguration.areaType == SpawnAreaType.Area)
        {
            Gizmos.color = Color.red * 0.3f;
            DrawCircle(transform.position, spawnConfiguration.spawnRadius, 32);
        }

        // Draw escort spawn area if configured
        if (spawnConfiguration.spawnEscorts && spawnConfiguration.escortConfiguration != null)
        {
            Gizmos.color = Color.yellow * 0.3f;
            if (spawnConfiguration.escortConfiguration.areaType == SpawnAreaType.Area)
            {
                DrawCircle(transform.position, spawnConfiguration.escortConfiguration.spawnRadius, 24);
            }
        }

#if UNITY_EDITOR
        Vector3 labelPos = transform.position + Vector3.up * 3.5f;
        string label = $"BOSS SPAWNER\n{spawnConfiguration.unitData?.unitName ?? "No Unit"}";
        if (spawnConfiguration.oneTimeSpawn)
            label += "\n[One-Time]";
        if (spawnConfiguration.requiresActivation)
            label += "\n[Requires Activation]";
        
        UnityEditor.Handles.Label(labelPos, label);
#endif
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!showGizmos || spawnConfiguration == null) return;

        Gizmos.color = Color.red;

        // Draw spawn area with more detail
        if (spawnConfiguration.areaType == SpawnAreaType.Area)
        {
            Gizmos.color = Color.red * 0.5f;
            DrawCircle(transform.position, spawnConfiguration.spawnRadius, 64);
        }

        // Draw escort spawn area
        if (spawnConfiguration.spawnEscorts && spawnConfiguration.escortConfiguration != null)
        {
            Gizmos.color = Color.yellow * 0.5f;
            if (spawnConfiguration.escortConfiguration.areaType == SpawnAreaType.Area)
            {
                DrawCircle(transform.position, spawnConfiguration.escortConfiguration.spawnRadius, 48);
            }

            // Draw sample escort positions
            for (int i = 0; i < spawnConfiguration.escortCount; i++)
            {
                Vector3 escortPos = spawnConfiguration.escortConfiguration.GetSpawnPosition(transform.position);
                Gizmos.DrawWireCube(escortPos, Vector3.one * 0.5f);
            }
        }

#if UNITY_EDITOR
        Vector3 infoPos = transform.position + Vector3.up * 5f;
        string info = $"Boss: {spawnConfiguration.unitData?.unitName ?? "None"}\n";
        info += $"One-Time: {spawnConfiguration.oneTimeSpawn}\n";
        info += $"Requires Activation: {spawnConfiguration.requiresActivation}\n";
        if (!spawnConfiguration.oneTimeSpawn)
            info += $"Respawn Delay: {spawnConfiguration.respawnDelay}s\n";
        if (spawnConfiguration.spawnEscorts)
            info += $"Escorts: {spawnConfiguration.escortCount}\n";
        
        if (Application.isPlaying)
        {
            info += $"\n--- Runtime ---\n";
            info += $"Active: {isEncounterActive}\n";
            info += $"Defeated: {hasBeenDefeated}\n";
            info += $"Waiting: {isWaitingForActivation}\n";
            info += $"Boss Alive: {IsBossAlive()}\n";
        }
        
        UnityEditor.Handles.Label(infoPos, info);
#endif
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 previousPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previousPoint, newPoint);
            previousPoint = newPoint;
        }
    }

    #endregion
}
