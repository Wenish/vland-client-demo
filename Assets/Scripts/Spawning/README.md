# Server-Authoritative Unit Spawn System

## Overview

This spawn system provides a complete, data-driven solution for spawning units in a Unity Mirror multiplayer game. It supports both regular world mobs and boss encounters with a unified, extensible architecture.

## Key Features

- **Server-Authoritative**: All spawning logic runs only on the server
- **Data-Driven**: Configuration through ScriptableObjects
- **Modular Architecture**: Shared base classes with specialized implementations
- **Network-Safe**: Fully integrated with Mirror networking
- **Designer-Friendly**: Inspector-configurable with visual Gizmos
- **Extensible**: Easy to add new spawn patterns and behaviors

## Components

### ScriptableObject Configurations

#### SpawnConfigurationBase
Base class for all spawn configurations with common properties:
- Unit data reference
- Spawn area (Point or Area with radius)
- Height offset and ground alignment
- Initial spawn delay

#### MobSpawnConfiguration
Configuration for normal world mobs:
- Spawn count and intervals
- Max active units limit
- Respawn settings
- Wave-based spawning
- Continuous spawning mode

**Menu**: `Game/Spawning/Mob Spawn Configuration`

#### BossSpawnConfiguration
Configuration for boss encounters:
- One-time spawn option
- Manual activation requirement
- Activation delay
- Escort unit spawning
- Respawn delay for non-one-time bosses

**Menu**: `Game/Spawning/Boss Spawn Configuration`

### Spawner Components

#### UnitSpawnerBase
Abstract base class providing:
- Server-authoritative spawning
- Unit tracking and cleanup
- Gizmo visualization
- Integration with UnitSpawner singleton

#### MobSpawner
Spawner for regular world mobs:
- **Features**:
  - Continuous or wave-based spawning
  - Automatic respawning
  - Max active unit limits
  - Start/Stop control
  - Active state toggling
  
- **Inspector Properties**:
  - `spawnConfiguration`: The MobSpawnConfiguration to use
  - `isActive`: Current active state
  - `autoStart`: Start spawning on server initialization
  - `showGizmos`: Display visual indicators in Scene view
  - `gizmoColor`: Color for visualization

#### BossSpawner
Spawner for boss encounters:
- **Features**:
  - One-time or repeatable spawns
  - Manual activation support
  - Escort unit spawning
  - Encounter state tracking
  - Reset functionality
  
- **Inspector Properties**:
  - `spawnConfiguration`: The BossSpawnConfiguration to use
  - `hasBeenDefeated`: Tracks if boss was killed (runtime)
  - `isEncounterActive`: Current encounter state (runtime)
  - `showGizmos`: Display visual indicators in Scene view

### Utility Components

#### SpawnManager
Central manager for all spawners:
- **Features**:
  - Auto-discovery of spawners
  - Global control (start/stop all)
  - Spawner registration and lookup
  - Statistics and querying
  
- **API Methods**:
  - `StartAllMobSpawners()`: Start all mob spawners
  - `StopAllMobSpawners()`: Stop all mob spawners
  - `ActivateBossEncounter(string id)`: Activate specific boss
  - `GetMobSpawner(string id)`: Get spawner by ID
  - `GetStatistics()`: Get spawn system stats

#### BossEncounterTrigger
Trigger component for activating boss encounters:
- **Features**:
  - Collider-based activation
  - Team filtering
  - One-time use option
  - Unity Events integration
  
- **Inspector Properties**:
  - `bossSpawner`: The boss spawner to activate
  - `triggerTeam`: Only trigger for this team (0 = any)
  - `oneTimeUse`: Trigger only once
  - `destroyAfterUse`: Destroy trigger after activation

#### SpawnPointTracker
Tracks spawn origin for leashing behavior:
- **Features**:
  - Automatically added by spawners
  - Distance tracking
  - Integration with TooFarFromSpawnCondition
  
- **API Methods**:
  - `GetDistanceFromSpawn()`: Get distance to spawn point
  - `IsNearSpawnPoint(float distance)`: Check if near spawn
  - `GetDirectionToSpawn()`: Get direction vector to spawn

## Setup Guide

### Creating Spawn Configurations

#### Mob Spawn Configuration

1. Right-click in Project window
2. Select `Create > Game > Spawning > Mob Spawn Configuration`
3. Configure properties:
   ```
   Unit Data: [Your UnitData asset]
   Spawn Count: 3
   Max Active Units: 10
   Spawn Interval: 5.0
   Enable Respawn: ✓
   Respawn Delay: 15.0
   Use Waves: ☐
   ```

#### Boss Spawn Configuration

1. Right-click in Project window
2. Select `Create > Game > Spawning > Boss Spawn Configuration`
3. Configure properties:
   ```
   Unit Data: [Your Boss UnitData asset]
   One Time Spawn: ✓
   Requires Activation: ✓
   Activation Delay: 2.0
   Spawn Escorts: ✓
   Escort Configuration: [Mob config for escorts]
   Escort Count: 4
   ```

### Placing Spawners in Scene

#### Mob Spawner

1. Create empty GameObject in scene
2. Add `MobSpawner` component
3. Assign spawn configuration
4. Position spawner in world
5. Configure:
   ```
   Spawner ID: [Unique identifier]
   Spawn Configuration: [Your mob config]
   Is Active: ✓
   Auto Start: ✓
   Show Gizmos: ✓
   Gizmo Color: Yellow
   ```

#### Boss Spawner

1. Create empty GameObject in scene
2. Add `BossSpawner` component
3. Assign spawn configuration
4. Position spawner in world
5. Configure:
   ```
   Spawner ID: boss_arena_01
   Spawn Configuration: [Your boss config]
   Show Gizmos: ✓
   Gizmo Color: Red
   ```

### Setting Up Boss Trigger

1. Create empty GameObject with Collider
2. Add `BossEncounterTrigger` component
3. Configure:
   ```
   Boss Spawner: [Drag boss spawner from scene]
   Trigger Team: 1
   One Time Use: ✓
   Destroy After Use: ✓
   ```

### Adding Spawn Manager

1. Create empty GameObject in scene
2. Add `SpawnManager` component
3. Configure:
   ```
   Auto Discover Spawners: ✓
   Debug Mode: ☐
   ```

The manager will automatically find and register all spawners on server start.

## Usage Examples

### Example 1: Basic Mob Spawner
```
Scenario: Spawn zombies continuously in a graveyard
Configuration:
- Unit: Zombie
- Spawn Count: 2
- Max Active: 8
- Interval: 10s
- Respawn: Enabled (15s delay)
- Area Type: Area (radius: 8m)
```

### Example 2: Wave-Based Spawner
```
Scenario: Spawn enemies in waves for a survival mode
Configuration:
- Unit: Enemy Soldier
- Use Waves: Enabled
- Units Per Wave: 5
- Wave Cooldown: 30s
- Max Active: 20
- Area Type: Area (radius: 12m)
```

### Example 3: Boss with Escorts
```
Scenario: Boss encounter with minions
Boss Configuration:
- Unit: Dragon Boss
- One Time Spawn: Enabled
- Requires Activation: Enabled
- Spawn Escorts: Enabled
- Escort Config: [Goblin spawn config]
- Escort Count: 6
- Area Type: Point
```

### Example 4: Trigger-Activated Boss
```
Setup:
1. Place BossSpawner with requiresActivation = true
2. Place BossEncounterTrigger near entrance
3. Connect trigger to spawner
4. Set trigger team to player team (1)
Result: Boss spawns when player enters area
```

## Gizmo Visualization

### Mob Spawner (Yellow)
- Small sphere at spawn point
- Upward line indicator
- Circle for area spawning (when selected)
- Sample spawn points (when selected)
- Configuration info (when selected)

### Boss Spawner (Red)
- Large sphere at spawn point
- Tall upward line indicator
- Red circle for boss spawn area
- Yellow circle for escort spawn area
- Configuration and runtime info (when selected)

### Boss Trigger (Magenta)
- Wireframe of trigger collider
- Line connecting to boss spawner (when selected)
- Trigger info label

## Integration with Existing Systems

### UnitController Integration
Spawned units are fully integrated with:
- Stats system (health, shield, movement speed)
- Weapon system (weapon assignment)
- Model system (model instantiation)
- Skill system (passive, normal, ultimate skills)
- Team system (team assignment)

### Behaviour System Integration
For NPCs with BehaviourExecutor:
- Behaviour profile is automatically assigned
- SpawnPointTracker enables leashing (TooFarFromSpawnCondition)
- Health phase system works seamlessly

### Network Integration
- All spawning is server-authoritative
- NetworkServer.Spawn called automatically
- Proper cleanup on destroy
- Network-safe event handling

## Advanced Usage

### Custom Spawn Conditions
Extend SpawnConfigurationBase to add custom conditions:
```csharp
public class ConditionalSpawnConfig : MobSpawnConfiguration
{
    public TimeOfDay spawnTime;
    public WeatherType spawnWeather;
    
    public override bool Validate()
    {
        return base.Validate() && CheckConditions();
    }
    
    private bool CheckConditions()
    {
        // Custom validation logic
        return true;
    }
}
```

### Programmatic Spawner Control
```csharp
// Get spawner from manager
MobSpawner spawner = SpawnManager.Instance.GetMobSpawner("graveyard_01");

// Control spawning
spawner.StartSpawning();
spawner.StopSpawning();
spawner.SetActive(false);

// Query state
bool isSpawning = spawner.IsSpawning();
int unitCount = spawner.GetSpawnedUnits().Count;
```

### Boss Encounter Scripting
```csharp
// Activate boss manually
BossSpawner boss = SpawnManager.Instance.GetBossSpawner("dragon_boss");
boss.ActivateEncounter();

// Check boss state
bool isAlive = boss.IsBossAlive();
GameObject bossUnit = boss.GetBossInstance();

// Reset for testing
boss.ResetEncounter();
```

### Event-Driven Spawning
```csharp
// Listen to gate open event
EventManager.Instance.Subscribe<OpenGateEvent>(OnGateOpen);

void OnGateOpen(OpenGateEvent evt)
{
    // Activate spawners for this gate
    SpawnManager.Instance.SetMobSpawnerActive($"gate_{evt.GateId}_spawner", true);
}
```

## Best Practices

1. **Spawner IDs**: Use descriptive, unique IDs (e.g., `forest_goblin_01`, `boss_dragon_arena`)

2. **Spawn Limits**: Always set maxActiveUnits for mob spawners to prevent performance issues

3. **Ground Alignment**: Enable alignToGround for outdoor spawners, disable for indoor spawners with flat floors

4. **Boss Activation**: Use requiresActivation for important bosses to prevent accidental early spawning

5. **Gizmo Colors**: Use consistent colors (yellow for mobs, red for bosses) for easy scene visualization

6. **Configuration Reuse**: Create reusable configurations for common enemy types

7. **Testing**: Use SpawnManager.LogStatistics() to debug spawner behavior

8. **Cleanup**: Let the system handle unit cleanup automatically through NetworkServer.Destroy

## Performance Considerations

- **Max Active Units**: Limit concurrent spawns to prevent performance degradation
- **Spawn Intervals**: Use reasonable intervals (5-30s) for continuous spawning
- **Area Spawning**: Larger spawn areas reduce unit clustering
- **Respawn Delays**: Use delays (10-30s) to avoid spawn bursts

## Troubleshooting

### Units not spawning
- Check that UnitSpawner.Instance exists in scene
- Verify spawn configuration is assigned and valid
- Ensure spawner is active (isActive = true)
- Check that server is running (spawners only work on server)

### Boss trigger not working
- Verify collider is set to IsTrigger
- Check trigger team matches unit team
- Ensure boss spawner reference is assigned
- Confirm trigger hasn't already been used (if oneTimeUse = true)

### Units spawning at wrong location
- Check spawn height offset
- Verify ground alignment settings
- Ensure proper ground layer mask
- Check area type and radius settings

### Too many units spawning
- Set maxActiveUnits limit
- Adjust spawn count and interval
- Verify respawn settings
- Check for multiple spawners overlapping

## Future Extensions

The system is designed for easy extension:

1. **Time-Based Spawning**: Add day/night spawn conditions
2. **Player Proximity**: Add spawn activation based on player distance
3. **Spawn Pools**: Implement object pooling for performance
4. **Spawn Waves**: Add complex wave patterns with multiple enemy types
5. **Spawn Cinematics**: Add camera control and dramatic spawn sequences
6. **Save/Load**: Persist spawner states across sessions
7. **Difficulty Scaling**: Scale spawn parameters based on player level/count

## API Reference

See inline XML documentation in source files for complete API reference.
