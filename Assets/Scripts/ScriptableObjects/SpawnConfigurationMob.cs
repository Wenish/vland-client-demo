using UnityEngine;

/// <summary>
/// Configuration for normal mob spawning (respawnable enemies, patrols, ambient creatures).
/// </summary>
[CreateAssetMenu(fileName = "NewMobSpawnConfig", menuName = "Game/Spawning/Mob Spawn Configuration")]
public class SpawnConfigurationMob : SpawnConfigurationBase
{
    [Header("Mob Spawn Settings")]
    [Tooltip("Number of units to spawn per wave")]
    [Min(1)]
    public int spawnCount = 1;

    [Tooltip("Maximum number of units that can be alive at once from this spawner (0 = unlimited)")]
    [Min(0)]
    public int maxActiveUnits = 0;

    [Tooltip("Time between spawns when continuously spawning (in seconds)")]
    [Min(0f)]
    public float spawnInterval = 5f;

    [Header("Respawn Configuration")]
    [Tooltip("Should units respawn after being killed?")]
    public bool enableRespawn = true;

    [Tooltip("Delay before respawning after a unit dies (in seconds)")]
    [Min(0f)]
    public float respawnDelay = 10f;

    [Header("Wave Spawning")]
    [Tooltip("Enable wave-based spawning (spawn in groups at intervals)")]
    public bool useWaves = false;

    [Tooltip("Number of units per wave (only used if Use Waves is enabled)")]
    [Min(1)]
    public int unitsPerWave = 3;

    [Tooltip("Delay between waves (in seconds, only used if Use Waves is enabled)")]
    [Min(0f)]
    public float waveCooldown = 30f;

    public override bool Validate()
    {
        if (!base.Validate()) return false;

        if (spawnCount <= 0)
        {
            Debug.LogError($"MobSpawnConfiguration '{name}' has spawn count <= 0!", this);
            return false;
        }

        if (useWaves && unitsPerWave <= 0)
        {
            Debug.LogError($"MobSpawnConfiguration '{name}' has waves enabled but units per wave <= 0!", this);
            return false;
        }

        return true;
    }
}