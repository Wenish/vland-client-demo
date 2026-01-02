using UnityEngine;

/// <summary>
/// Configuration for boss spawning (unique encounters, one-time spawns, phase-driven fights).
/// </summary>
[CreateAssetMenu(fileName = "NewBossSpawnConfig", menuName = "Game/Spawning/Boss Spawn Configuration")]
public class SpawnConfigurationBoss : SpawnConfigurationBase
{
    [Header("Boss Spawn Settings")]
    [Tooltip("Only spawn the boss once (will not respawn after being killed)")]
    public bool oneTimeSpawn = true;

    [Tooltip("Respawn delay if one-time spawn is disabled (in seconds)")]
    [Min(0f)]
    public float respawnDelay = 300f;

    [Header("Encounter Settings")]
    [Tooltip("Require manual activation (e.g., through trigger or event)")]
    public bool requiresActivation = false;

    [Tooltip("Delay after activation before spawning (in seconds)")]
    [Min(0f)]
    public float activationDelay = 2f;

    [Header("Escort Units")]
    [Tooltip("Should the boss spawn with escort units?")]
    public bool spawnEscorts = false;

    [Tooltip("Configuration for escort mobs (will spawn alongside the boss)")]
    public SpawnConfigurationMob escortConfiguration;

    [Tooltip("Number of escort units to spawn")]
    [Min(0)]
    public int escortCount = 0;

    public override bool Validate()
    {
        if (!base.Validate()) return false;

        if (spawnEscorts && escortCount > 0 && escortConfiguration == null)
        {
            Debug.LogError($"BossSpawnConfiguration '{name}' has escorts enabled but no escort configuration!", this);
            return false;
        }

        if (spawnEscorts && escortCount > 0 && !escortConfiguration.Validate())
        {
            Debug.LogError($"BossSpawnConfiguration '{name}' has invalid escort configuration!", this);
            return false;
        }

        return true;
    }
}