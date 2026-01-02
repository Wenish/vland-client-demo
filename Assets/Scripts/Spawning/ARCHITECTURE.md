# Spawn System Architecture Diagram

## Component Hierarchy

```
┌─────────────────────────────────────────────────────────────────┐
│                        Spawn System                              │
│                     (Server-Authoritative)                       │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┴───────────────┐
                │                               │
        ┌───────▼────────┐            ┌────────▼─────────┐
        │ Configuration  │            │    Spawners      │
        │ (ScriptableObj)│            │ (NetworkBehaviour)│
        └───────┬────────┘            └────────┬─────────┘
                │                               │
    ┌───────────┼───────────┐       ┌──────────┼──────────┐
    │           │           │       │          │          │
┌───▼──┐   ┌───▼──┐   ┌───▼──┐  ┌─▼──┐   ┌───▼───┐  ┌──▼──┐
│ Base │   │ Mob  │   │ Boss │  │Base│   │  Mob  │  │Boss │
│Config│   │Config│   │Config│  │    │   │Spawner│  │Spawn│
└──────┘   └──────┘   └──────┘  └─┬──┘   └───────┘  └─────┘
                                   │
                          ┌────────┴────────┐
                          │                 │
                    ┌─────▼──────┐   ┌─────▼──────┐
                    │   Spawn    │   │   Spawn    │
                    │  Manager   │   │Point Track.│
                    └────────────┘   └────────────┘
```

## Data Flow: Mob Spawning

```
1. Server Start
   │
   └──> MobSpawner.Initialize()
        │
        └──> Validate Configuration
             │
             └──> Start Spawn Routine
                  │
                  ├──> Wait Initial Delay
                  │
                  └──> Spawn Loop
                       │
                       ├──> Check Max Active Units
                       │
                       ├──> Spawn Unit (via UnitSpawner.Instance)
                       │    │
                       │    ├──> NetworkServer.Spawn()
                       │    │
                       │    ├──> Attach SpawnPointTracker
                       │    │
                       │    └──> Subscribe to OnDied event
                       │
                       ├──> Wait Spawn Interval
                       │
                       └──> Loop Back
                            │
                            └──> On Unit Death
                                 │
                                 ├──> Wait Respawn Delay
                                 │
                                 └──> Respawn (if enabled)
```

## Data Flow: Boss Encounter

```
1. Scene Load
   │
   └──> BossSpawner.Initialize()
        │
        ├──> If requiresActivation
        │    └──> Wait for Activation
        │         │
        │         └──> Player enters BossEncounterTrigger
        │              │
        │              └──> Trigger.ActivateBossEncounter()
        │                   │
        │                   └──> BossSpawner.ActivateEncounter()
        │
        └──> If !requiresActivation
             └──> Auto-Start Encounter
                  │
                  ├──> Wait Activation Delay
                  │
                  ├──> Wait Initial Delay
                  │
                  ├──> Spawn Boss
                  │    │
                  │    ├──> NetworkServer.Spawn()
                  │    │
                  │    ├──> Attach SpawnPointTracker
                  │    │
                  │    └──> Subscribe to OnDied event
                  │
                  ├──> Spawn Escorts (if configured)
                  │    │
                  │    └──> For each escort
                  │         └──> Spawn Unit
                  │
                  └──> On Boss Death
                       │
                       ├──> End Encounter
                       │
                       ├──> Cleanup Escorts
                       │
                       └──> If !oneTimeSpawn
                            │
                            ├──> Wait Respawn Delay
                            │
                            └──> Restart Encounter
```

## Integration Points

```
┌────────────────────────────────────────────────────────────┐
│                     Spawn System                           │
└──────────┬─────────────────────────────────────┬──────────┘
           │                                     │
    ┌──────▼────────┐                    ┌──────▼────────┐
    │  UnitSpawner  │                    │ SpawnManager  │
    │  (Singleton)  │                    │  (Singleton)  │
    └──────┬────────┘                    └──────┬────────┘
           │                                     │
           │                                     │
    ┌──────▼────────────────────────────────────▼──────┐
    │              UnitController                      │
    │          (NetworkBehaviour)                      │
    └──────┬─────────────────────────────────┬────────┘
           │                                  │
    ┌──────▼────────┐                 ┌──────▼────────┐
    │ UnitMediator  │                 │BehaviourExec. │
    │  (Stats,      │                 │   (AI)        │
    │   Skills)     │                 │               │
    └───────────────┘                 └───────────────┘
```

## Scene Setup Example

```
Scene Hierarchy:
├── GameManagement
│   ├── NetworkManager
│   ├── UnitSpawner (Singleton)
│   └── SpawnManager
│
├── Spawners
│   ├── GraveyardSpawner (MobSpawner)
│   │   └── SpawnConfig: ZombieSpawnConfig
│   │
│   ├── ForestSpawner (MobSpawner)
│   │   └── SpawnConfig: GoblinSpawnConfig
│   │
│   └── DragonBossSpawner (BossSpawner)
│       └── SpawnConfig: DragonBossConfig
│
└── Triggers
    └── DragonArenaTrigger (BossEncounterTrigger)
        └── Links to: DragonBossSpawner

Project Assets:
├── ScriptableObjects
│   ├── SpawnConfigs
│   │   ├── ZombieSpawnConfig (MobSpawnConfiguration)
│   │   ├── GoblinSpawnConfig (MobSpawnConfiguration)
│   │   ├── DragonBossConfig (BossSpawnConfiguration)
│   │   └── DragonMinionsConfig (MobSpawnConfiguration)
│   │
│   └── UnitData
│       ├── ZombieData
│       ├── GoblinData
│       ├── DragonBossData
│       └── DragonMinionData
```

## API Call Flow Example

```csharp
// Programmatic Control Example

// Get the spawn manager
SpawnManager manager = SpawnManager.Instance;

// Control mob spawners
manager.StartAllMobSpawners();  // Start all mob spawning
manager.StopAllMobSpawners();   // Stop all mob spawning

// Control specific spawners
MobSpawner goblinSpawner = manager.GetMobSpawner("forest_goblins");
goblinSpawner.SetActive(true);  // Enable spawner
goblinSpawner.StartSpawning();  // Start spawning

// Activate boss encounter
manager.ActivateBossEncounter("dragon_boss_01");

// Query system state
SpawnStatistics stats = manager.GetStatistics();
Debug.Log($"Active spawners: {stats.activeMobSpawners}");
Debug.Log($"Total units: {stats.totalSpawnedUnits}");

// Get spawned units
List<GameObject> allUnits = manager.GetAllSpawnedUnits();
foreach (var unit in allUnits)
{
    UnitController controller = unit.GetComponent<UnitController>();
    Debug.Log($"Unit: {controller.unitName}, Team: {controller.team}");
}
```

## Gizmo Visualization Guide

```
Scene View Visualization:

MOB SPAWNER (Yellow):
    ╔═══╗
    ║   ║  ← Small sphere at spawner position
    ║ │ ║  ← Vertical line indicator
    ║ ▼ ║
    ╚═══╝
      ○ ○   ← Area circle (if area spawning)
    ○     ○
   ○       ○
    ○     ○
      ○ ○

BOSS SPAWNER (Red):
    ╔═════╗
    ║     ║  ← Large sphere at spawner position
    ║  │  ║  ← Tall vertical line
    ║  │  ║
    ║  ▼  ║
    ╚═════╝
       ●     ← Boss spawn point
      ○ ○    ← Escort spawn area (yellow)
    ○     ○
   ○       ○

BOSS TRIGGER (Magenta):
    ┌───────────┐
    │           │  ← Trigger collider wireframe
    │    ▣      │
    │           │
    └───────────┘
         │
         └──────→ [Line to Boss Spawner]

SPAWN POINT TRACKER (Cyan):
    Unit Position ●───────● Spawn Origin
                   ↑
              Distance line
```

## Network Architecture

```
Server:
┌────────────────────────────────────────┐
│  All spawning logic runs here          │
│                                        │
│  ┌──────────────────────────────────┐ │
│  │ Spawner Components               │ │
│  │ - MobSpawner                     │ │
│  │ - BossSpawner                    │ │
│  │ - SpawnManager                   │ │
│  └──────────────────────────────────┘ │
│           │                            │
│           ▼                            │
│  ┌──────────────────────────────────┐ │
│  │ UnitSpawner.Spawn()              │ │
│  │ - Instantiate unit               │ │
│  │ - Configure stats                │ │
│  │ - NetworkServer.Spawn()          │ │
│  └──────────────────────────────────┘ │
│           │                            │
│           ▼                            │
│  ┌──────────────────────────────────┐ │
│  │ Network Replication              │ │
│  └──────────────────────────────────┘ │
└────────────────┬───────────────────────┘
                 │
                 │ SyncVars, RPCs
                 │
        ┌────────▼─────────┐
        │                  │
    ┌───▼───┐         ┌───▼───┐
    │Client1│         │Client2│
    └───────┘         └───────┘
       │                  │
       ▼                  ▼
    Observe           Observe
    Spawned           Spawned
    Units             Units
```

## Configuration Inheritance

```
SpawnConfigurationBase (Abstract)
├── unitData
├── spawnHeightOffset
├── areaType
├── spawnRadius
├── alignToGround
└── groundLayer
    │
    ├──> MobSpawnConfiguration
    │    ├── spawnCount
    │    ├── maxActiveUnits
    │    ├── spawnInterval
    │    ├── enableRespawn
    │    ├── respawnDelay
    │    ├── useWaves
    │    ├── unitsPerWave
    │    └── waveCooldown
    │
    └──> BossSpawnConfiguration
         ├── oneTimeSpawn
         ├── respawnDelay
         ├── requiresActivation
         ├── activationDelay
         ├── spawnEscorts
         ├── escortConfiguration → MobSpawnConfiguration
         └── escortCount
```

## Event Flow

```
Spawn Events:
    Initialize → OnStartServer → Initialize()
                      │
                      ▼
    Spawn Unit → SpawnUnit() → OnUnitSpawned()
                      │
                      ▼
    Unit Death → UnitController.OnDied → OnSpawnedUnitDied()
                      │
                      ├──> Respawn Logic
                      └──> Cleanup
```
