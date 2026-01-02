using Mirror;
using UnityEngine;

/// <summary>
/// Helper component for spawners to track their spawn origin point.
/// Useful for NPCs that need to return to their spawn location.
/// Integrates with TooFarFromSpawnCondition for leashing behavior.
/// </summary>
public class SpawnPointTracker : NetworkBehaviour
{
    [Header("Spawn Point")]
    [Tooltip("The original spawn position (set automatically by spawner)")]
    public Vector3 spawnPosition;

    [Tooltip("The spawner that created this unit")]
    public UnitSpawnerBase sourceSpawner;

    /// <summary>
    /// Initialize the spawn point tracker.
    /// Should be called by the spawner after unit creation.
    /// </summary>
    [Server]
    public void Initialize(Vector3 position, UnitSpawnerBase spawner)
    {
        spawnPosition = position;
        sourceSpawner = spawner;
    }

    /// <summary>
    /// Get the distance from the current position to the spawn point.
    /// </summary>
    public float GetDistanceFromSpawn()
    {
        return Vector3.Distance(transform.position, spawnPosition);
    }

    /// <summary>
    /// Check if the unit is within a certain distance of its spawn point.
    /// </summary>
    public bool IsNearSpawnPoint(float maxDistance)
    {
        return GetDistanceFromSpawn() <= maxDistance;
    }

    /// <summary>
    /// Get the direction vector from current position to spawn point.
    /// </summary>
    public Vector3 GetDirectionToSpawn()
    {
        return (spawnPosition - transform.position).normalized;
    }

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPosition, 1f);
            Gizmos.DrawLine(transform.position, spawnPosition);

#if UNITY_EDITOR
            Vector3 midPoint = (transform.position + spawnPosition) / 2f;
            UnityEditor.Handles.Label(midPoint, $"Distance: {GetDistanceFromSpawn():F1}m");
#endif
        }
    }

    #endregion
}
