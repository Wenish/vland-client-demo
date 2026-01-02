using UnityEngine;

/// <summary>
/// Base configuration for unit spawning.
/// Defines common spawn parameters shared by all spawn types.
/// </summary>
public abstract class SpawnConfigurationBase : ScriptableObject
{
    [Header("Unit Configuration")]
    [Tooltip("The unit data to spawn")]
    public UnitData unitData;

    [Tooltip("Override the spawn position Y value (useful for terrain alignment). Leave at 0 to use spawner height.")]
    public float spawnHeightOffset = 0f;

    [Header("Spawn Timing")]
    [Tooltip("Delay before the first spawn occurs (in seconds)")]
    [Min(0f)]
    public float initialSpawnDelay = 0f;

    [Header("Spawn Area")]
    [Tooltip("Spawn type: Point spawns at exact position, Area spawns within a radius")]
    public SpawnAreaType areaType = SpawnAreaType.Point;

    [Tooltip("Radius for area-based spawning (only used if Area Type is Area)")]
    [Min(0f)]
    public float spawnRadius = 5f;

    [Tooltip("If true, spawned units will be placed on the ground using raycasting")]
    public bool alignToGround = true;

    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer = -1;

    [Header("Corpse Cleanup")]
    [Tooltip("Automatically destroy the unit instance after death (prevents memory buildup)")]
    public bool autoCleanupCorpse = true;

    [Tooltip("Delay before destroying the corpse (allows time for death animations, loot, etc.)")]
    [Min(0f)]
    public float corpseCleanupDelay = 5f;

    /// <summary>
    /// Validates the configuration. Override in derived classes for specific validation.
    /// </summary>
    public virtual bool Validate()
    {
        if (unitData == null)
        {
            Debug.LogError($"SpawnConfiguration '{name}' has no unit data assigned!", this);
            return false;
        }

        if (areaType == SpawnAreaType.Area && spawnRadius <= 0f)
        {
            Debug.LogError($"SpawnConfiguration '{name}' has Area type but radius is <= 0!", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get a random spawn position based on the configuration.
    /// </summary>
    public Vector3 GetSpawnPosition(Vector3 spawnerPosition)
    {
        Vector3 spawnPos = spawnerPosition;

        // Apply area-based offset
        if (areaType == SpawnAreaType.Area)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            spawnPos += new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

        // Apply height offset
        spawnPos.y += spawnHeightOffset;

        // Align to ground if enabled
        if (alignToGround)
        {
            if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 50f, groundLayer))
            {
                spawnPos.y = hit.point.y + spawnHeightOffset;
            }
        }

        return spawnPos;
    }
}


/// <summary>
/// Defines the spawn area type.
/// </summary>
public enum SpawnAreaType
{
    Point,  // Spawn at exact position
    Area    // Spawn within a radius
}
