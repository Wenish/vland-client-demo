using UnityEngine;

/// <summary>
/// Example configuration presets for common spawn scenarios.
/// These can be used as starting points for creating spawn configurations.
/// To use: Create the ScriptableObject assets manually and copy these values.
/// </summary>
public static class SpawnConfigurationPresets
{
    // ===== MOB SPAWN PRESETS =====

    /// <summary>
    /// Standard roaming enemy spawn.
    /// Continuously spawns enemies with moderate respawn rate.
    /// Good for: Open world mobs, patrol groups, ambient enemies
    /// </summary>
    public static void ConfigureStandardMob(SpawnConfigurationMob config)
    {
        config.spawnCount = 2;
        config.maxActiveUnits = 6;
        config.spawnInterval = 8f;
        config.enableRespawn = true;
        config.respawnDelay = 15f;
        config.useWaves = false;
        config.areaType = SpawnAreaType.Area;
        config.spawnRadius = 5f;
        config.alignToGround = true;
        config.initialSpawnDelay = 2f;
    }

    /// <summary>
    /// High-density spawn for crowded areas.
    /// Spawns many enemies with quick respawn.
    /// Good for: Dungeons, infested areas, high-activity zones
    /// </summary>
    public static void ConfigureHighDensityMob(SpawnConfigurationMob config)
    {
        config.spawnCount = 4;
        config.maxActiveUnits = 12;
        config.spawnInterval = 5f;
        config.enableRespawn = true;
        config.respawnDelay = 10f;
        config.useWaves = false;
        config.areaType = SpawnAreaType.Area;
        config.spawnRadius = 10f;
        config.alignToGround = true;
        config.initialSpawnDelay = 1f;
    }

    /// <summary>
    /// Rare spawn for special enemies.
    /// Long intervals, limited count, slow respawn.
    /// Good for: Elite mobs, rare spawns, treasure guardians
    /// </summary>
    public static void ConfigureRareMob(SpawnConfigurationMob config)
    {
        config.spawnCount = 1;
        config.maxActiveUnits = 1;
        config.spawnInterval = 60f;
        config.enableRespawn = true;
        config.respawnDelay = 120f;
        config.useWaves = false;
        config.areaType = SpawnAreaType.Point;
        config.spawnRadius = 0f;
        config.alignToGround = true;
        config.initialSpawnDelay = 30f;
    }

    /// <summary>
    /// Wave-based spawn for arena combat.
    /// Spawns groups of enemies in waves with cooldowns.
    /// Good for: Survival modes, arena battles, defense scenarios
    /// </summary>
    public static void ConfigureWaveSpawn(SpawnConfigurationMob config)
    {
        config.spawnCount = 1;
        config.maxActiveUnits = 20;
        config.spawnInterval = 1f;
        config.enableRespawn = false; // Waves don't use individual respawn
        config.respawnDelay = 0f;
        config.useWaves = true;
        config.unitsPerWave = 5;
        config.waveCooldown = 30f;
        config.areaType = SpawnAreaType.Area;
        config.spawnRadius = 8f;
        config.alignToGround = true;
        config.initialSpawnDelay = 5f;
    }

    /// <summary>
    /// Ambient creature spawn.
    /// Very few units, large area, slow respawn.
    /// Good for: Wildlife, ambient creatures, decorative enemies
    /// </summary>
    public static void ConfigureAmbientMob(SpawnConfigurationMob config)
    {
        config.spawnCount = 1;
        config.maxActiveUnits = 3;
        config.spawnInterval = 20f;
        config.enableRespawn = true;
        config.respawnDelay = 60f;
        config.useWaves = false;
        config.areaType = SpawnAreaType.Area;
        config.spawnRadius = 15f;
        config.alignToGround = true;
        config.initialSpawnDelay = 0f;
    }

    /// <summary>
    /// Guard spawn for fixed positions.
    /// Single unit at exact position, long respawn.
    /// Good for: Guards, sentries, checkpoint enemies
    /// </summary>
    public static void ConfigureGuardSpawn(SpawnConfigurationMob config)
    {
        config.spawnCount = 1;
        config.maxActiveUnits = 1;
        config.spawnInterval = 30f;
        config.enableRespawn = true;
        config.respawnDelay = 45f;
        config.useWaves = false;
        config.areaType = SpawnAreaType.Point;
        config.spawnRadius = 0f;
        config.alignToGround = true;
        config.initialSpawnDelay = 0f;
    }

    // ===== BOSS SPAWN PRESETS =====

    /// <summary>
    /// Standard boss encounter.
    /// One-time spawn, requires manual activation.
    /// Good for: Main bosses, story bosses, dungeon bosses
    /// </summary>
    public static void ConfigureStandardBoss(SpawnConfigurationBoss config)
    {
        config.oneTimeSpawn = true;
        config.respawnDelay = 0f;
        config.requiresActivation = true;
        config.activationDelay = 2f;
        config.spawnEscorts = false;
        config.escortCount = 0;
        config.areaType = SpawnAreaType.Point;
        config.spawnRadius = 0f;
        config.alignToGround = true;
        config.initialSpawnDelay = 0f;
    }

    /// <summary>
    /// Boss with escort units.
    /// Spawns boss with supporting minions.
    /// Good for: Group boss fights, commander encounters, arena bosses
    /// </summary>
    public static void ConfigureBossWithEscorts(SpawnConfigurationBoss config)
    {
        config.oneTimeSpawn = true;
        config.respawnDelay = 0f;
        config.requiresActivation = true;
        config.activationDelay = 3f;
        config.spawnEscorts = true;
        config.escortCount = 4;
        // Note: escortConfiguration must be assigned manually
        config.areaType = SpawnAreaType.Point;
        config.spawnRadius = 0f;
        config.alignToGround = true;
        config.initialSpawnDelay = 1f;
    }

    /// <summary>
    /// Repeatable boss encounter.
    /// Boss respawns after being defeated.
    /// Good for: Farming bosses, world bosses, repeatable content
    /// </summary>
    public static void ConfigureRepeatableBoss(SpawnConfigurationBoss config)
    {
        config.oneTimeSpawn = false;
        config.respawnDelay = 300f; // 5 minutes
        config.requiresActivation = false;
        config.activationDelay = 0f;
        config.spawnEscorts = false;
        config.escortCount = 0;
        config.areaType = SpawnAreaType.Point;
        config.spawnRadius = 0f;
        config.alignToGround = true;
        config.initialSpawnDelay = 60f;
    }

    /// <summary>
    /// World boss spawn.
    /// Large-scale boss that spawns automatically on cooldown.
    /// Good for: Open world bosses, server events, timed spawns
    /// </summary>
    public static void ConfigureWorldBoss(SpawnConfigurationBoss config)
    {
        config.oneTimeSpawn = false;
        config.respawnDelay = 1800f; // 30 minutes
        config.requiresActivation = false;
        config.activationDelay = 10f; // Warning time
        config.spawnEscorts = true;
        config.escortCount = 8;
        // Note: escortConfiguration must be assigned manually
        config.areaType = SpawnAreaType.Area;
        config.spawnRadius = 5f;
        config.alignToGround = true;
        config.initialSpawnDelay = 300f; // First spawn after 5 minutes
    }

    /// <summary>
    /// Arena boss spawn.
    /// Triggered boss for arena-style combat.
    /// Good for: Colosseum fights, challenge arenas, test encounters
    /// </summary>
    public static void ConfigureArenaBoss(SpawnConfigurationBoss config)
    {
        config.oneTimeSpawn = false;
        config.respawnDelay = 60f; // Quick respawn for repeated arena matches
        config.requiresActivation = true;
        config.activationDelay = 5f; // Preparation time
        config.spawnEscorts = false;
        config.escortCount = 0;
        config.areaType = SpawnAreaType.Point;
        config.spawnRadius = 0f;
        config.alignToGround = true;
        config.initialSpawnDelay = 0f;
    }

    // ===== ESCORT CONFIGURATION PRESETS =====

    /// <summary>
    /// Standard escort configuration for boss fights.
    /// Creates moderate number of support units around boss.
    /// </summary>
    public static void ConfigureStandardEscort(SpawnConfigurationMob config)
    {
        config.spawnCount = 1;
        config.maxActiveUnits = 0; // No limit for escorts
        config.spawnInterval = 1f;
        config.enableRespawn = false; // Escorts don't respawn
        config.respawnDelay = 0f;
        config.useWaves = false;
        config.areaType = SpawnAreaType.Area;
        config.spawnRadius = 8f; // Spawn around boss position
        config.alignToGround = true;
        config.initialSpawnDelay = 0.5f;
    }

    /// <summary>
    /// Elite escort configuration.
    /// Fewer but stronger support units.
    /// </summary>
    public static void ConfigureEliteEscort(SpawnConfigurationMob config)
    {
        config.spawnCount = 1;
        config.maxActiveUnits = 0;
        config.spawnInterval = 1f;
        config.enableRespawn = false;
        config.respawnDelay = 0f;
        config.useWaves = false;
        config.areaType = SpawnAreaType.Area;
        config.spawnRadius = 6f;
        config.alignToGround = true;
        config.initialSpawnDelay = 1f;
    }
}
