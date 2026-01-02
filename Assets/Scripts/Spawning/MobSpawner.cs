using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// Spawner for normal world mobs (respawnable enemies, patrols, ambient creatures).
/// Supports continuous spawning, wave-based spawning, respawn logic, and max active unit limits.
/// Runs only on the server.
/// </summary>
public class MobSpawner : UnitSpawnerBase
{
    [Header("Mob Spawner Configuration")]
    [Tooltip("The spawn configuration to use")]
    public SpawnConfigurationMob spawnConfiguration;

    [Header("Spawner State")]
    [Tooltip("Is this spawner currently active?")]
    public bool isActive = true;

    [Tooltip("Should spawning start automatically on server start?")]
    public bool autoStart = true;

    // Internal state
    private Coroutine spawnCoroutine;
    private float nextSpawnTime;
    private int currentWaveCount;
    private int totalUnitsSpawned;

    #region Initialization

    protected override void Initialize()
    {
        base.Initialize();

        if (spawnConfiguration == null)
        {
            Debug.LogError($"[MobSpawner] Spawner '{spawnerId}' has no spawn configuration!");
            return;
        }

        if (!spawnConfiguration.Validate())
        {
            Debug.LogError($"[MobSpawner] Spawner '{spawnerId}' has invalid configuration!");
            return;
        }

        if (autoStart && isActive)
        {
            StartSpawning();
        }
    }

    #endregion

    #region Spawning Control

    /// <summary>
    /// Start the spawning process.
    /// </summary>
    [Server]
    public void StartSpawning()
    {
        if (!isServer) return;

        if (isSpawning)
        {
            Debug.LogWarning($"[MobSpawner] Spawner '{spawnerId}' is already spawning!");
            return;
        }

        if (spawnConfiguration == null || !spawnConfiguration.Validate())
        {
            Debug.LogError($"[MobSpawner] Cannot start spawning - invalid configuration!");
            return;
        }

        isSpawning = true;
        nextSpawnTime = Time.time + spawnConfiguration.initialSpawnDelay;
        currentWaveCount = 0;
        totalUnitsSpawned = 0;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// Stop the spawning process.
    /// </summary>
    [Server]
    public void StopSpawning()
    {
        if (!isServer) return;

        isSpawning = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    /// <summary>
    /// Toggle spawner active state.
    /// </summary>
    [Server]
    public void SetActive(bool active)
    {
        if (!isServer) return;

        isActive = active;

        if (isActive && !isSpawning)
        {
            StartSpawning();
        }
        else if (!isActive && isSpawning)
        {
            StopSpawning();
        }
    }

    #endregion

    #region Spawn Logic

    private IEnumerator SpawnRoutine()
    {
        // Wait for initial delay
        if (spawnConfiguration.initialSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(spawnConfiguration.initialSpawnDelay);
        }

        while (isSpawning && isActive)
        {
            if (spawnConfiguration.useWaves)
            {
                // Wave-based spawning
                yield return SpawnWave();
                yield return new WaitForSeconds(spawnConfiguration.waveCooldown);
            }
            else
            {
                // Continuous spawning - wait interval BEFORE spawning (except first time)
                if (totalUnitsSpawned > 0)
                {
                    yield return new WaitForSeconds(spawnConfiguration.spawnInterval);
                }
                
                yield return SpawnBatch();
            }
        }
    }

    private IEnumerator SpawnWave()
    {
        int unitsToSpawn = spawnConfiguration.unitsPerWave;
        currentWaveCount++;

        for (int i = 0; i < unitsToSpawn; i++)
        {
            // Check max active units limit
            if (spawnConfiguration.maxActiveUnits > 0 && GetActiveUnitCount() >= spawnConfiguration.maxActiveUnits)
            {
                yield break; // Don't spawn more units
            }

            SpawnSingleUnit();

            // Small delay between units in a wave
            if (i < unitsToSpawn - 1)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    private IEnumerator SpawnBatch()
    {
        int unitsToSpawn = spawnConfiguration.spawnCount;

        for (int i = 0; i < unitsToSpawn; i++)
        {
            // Check max active units limit
            if (spawnConfiguration.maxActiveUnits > 0 && GetActiveUnitCount() >= spawnConfiguration.maxActiveUnits)
            {
                yield break; // Don't spawn more units
            }

            SpawnSingleUnit();

            // Small delay between units
            if (i < unitsToSpawn - 1)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void SpawnSingleUnit()
    {
        GameObject spawnedUnit = SpawnUnit(spawnConfiguration);
        
        if (spawnedUnit != null)
        {
            totalUnitsSpawned++;
        }
    }

    #endregion

    #region Respawn Logic

    protected override void OnSpawnedUnitDied(GameObject unit, UnitController unitController)
    {
        base.OnSpawnedUnitDied(unit, unitController);

        // Remove from tracking
        UntrackUnit(unit);

        // Schedule corpse cleanup
        ScheduleCorpseCleanup(unit, spawnConfiguration);

        // Handle respawn - only if respawn is enabled and we're not using waves
        // Wave spawners handle their own spawn timing
        if (spawnConfiguration.enableRespawn && isActive && !spawnConfiguration.useWaves)
        {
            StartCoroutine(RespawnUnit());
        }
    }

    private IEnumerator RespawnUnit()
    {
        // Wait for respawn delay
        yield return new WaitForSeconds(spawnConfiguration.respawnDelay);

        // Check if still active and not at max units
        if (isActive && isSpawning)
        {
            // Only respawn if we're below the max active units limit
            if (spawnConfiguration.maxActiveUnits <= 0 || GetActiveUnitCount() < spawnConfiguration.maxActiveUnits)
            {
                SpawnSingleUnit();
            }
        }
    }

    #endregion

    #region Gizmos

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (!showGizmos || spawnConfiguration == null) return;

        Gizmos.color = gizmoColor;

        // Draw spawn area if using area spawning
        if (spawnConfiguration.areaType == SpawnAreaType.Area)
        {
            Gizmos.color = gizmoColor * 0.3f;
            DrawCircle(transform.position, spawnConfiguration.spawnRadius, 32);
        }

        // Draw spawn count indicator
        Vector3 labelPos = transform.position + Vector3.up * 2.5f;
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos, $"Mob Spawner\n{spawnConfiguration.unitData?.unitName ?? "No Unit"}");
#endif
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!showGizmos || spawnConfiguration == null) return;

        Gizmos.color = gizmoColor;

        // Draw spawn area with more detail
        if (spawnConfiguration.areaType == SpawnAreaType.Area)
        {
            Gizmos.color = gizmoColor * 0.5f;
            DrawCircle(transform.position, spawnConfiguration.spawnRadius, 64);
            
            // Draw a few sample spawn points
            for (int i = 0; i < 8; i++)
            {
                Vector3 samplePos = spawnConfiguration.GetSpawnPosition(transform.position);
                Gizmos.DrawWireSphere(samplePos, 0.3f);
            }
        }

        // Draw config info
#if UNITY_EDITOR
        Vector3 infoPos = transform.position + Vector3.up * 3.5f;
        string info = $"Active: {isActive}\n";
        info += $"Count: {spawnConfiguration.spawnCount}\n";
        info += $"Interval: {spawnConfiguration.spawnInterval}s\n";
        if (spawnConfiguration.maxActiveUnits > 0)
            info += $"Max Active: {spawnConfiguration.maxActiveUnits}\n";
        if (spawnConfiguration.enableRespawn)
            info += $"Respawn: {spawnConfiguration.respawnDelay}s\n";
        if (spawnConfiguration.useWaves)
            info += $"Wave: {spawnConfiguration.unitsPerWave} units\n";
        
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

    #region Editor Support

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }

    #endregion
}
