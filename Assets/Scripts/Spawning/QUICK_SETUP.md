# Quick Setup Guide - Spawn System

## 5-Minute Setup

### Step 1: Create Your First Spawn Configuration

1. In Unity Project window, right-click
2. Select `Create > Game > Spawning > Mob Spawn Configuration`
3. Name it `ZombieSpawnConfig`
4. In Inspector, configure:
   - **Unit Data**: Drag your existing Zombie UnitData
   - **Spawn Count**: 3
   - **Max Active Units**: 10
   - **Spawn Interval**: 5
   - **Enable Respawn**: ✓
   - **Respawn Delay**: 15
   - **Area Type**: Area
   - **Spawn Radius**: 5

### Step 2: Add a Mob Spawner to Your Scene

1. In Hierarchy, create empty GameObject (name it "ZombieSpawner")
2. Add Component → `MobSpawner`
3. In Inspector:
   - **Spawner ID**: zombie_spawner_01
   - **Spawn Configuration**: Drag your ZombieSpawnConfig
   - **Is Active**: ✓
   - **Auto Start**: ✓
   - **Show Gizmos**: ✓
   - **Gizmo Color**: Yellow
4. Position the spawner where you want zombies to spawn

### Step 3: Add Spawn Manager (Optional but Recommended)

1. In Hierarchy, create empty GameObject (name it "SpawnManager")
2. Add Component → `SpawnManager`
3. In Inspector:
   - **Auto Discover Spawners**: ✓
   - **Debug Mode**: ☐

### Step 4: Test It!

1. Play the scene in Host mode (server + client)
2. Watch the Scene view - you'll see:
   - Yellow Gizmo at spawner location
   - Yellow circle showing spawn area
3. After 5 seconds, zombies should start spawning
4. Kill a zombie - it will respawn after 15 seconds

## Boss Encounter Setup (10 Minutes)

### Step 1: Create Boss Spawn Configuration

1. Right-click in Project → `Create > Game > Spawning > Boss Spawn Configuration`
2. Name it `DragonBossConfig`
3. Configure:
   - **Unit Data**: Drag your Boss UnitData
   - **One Time Spawn**: ✓
   - **Requires Activation**: ✓
   - **Activation Delay**: 2
   - **Area Type**: Point

### Step 2: Create Escort Configuration (If Needed)

1. Create another `Mob Spawn Configuration` named `DragonMinionsConfig`
2. Configure:
   - **Unit Data**: Minion UnitData
   - **Spawn Count**: 1
   - **Enable Respawn**: ☐
   - **Area Type**: Area
   - **Spawn Radius**: 8

### Step 3: Link Escort to Boss

1. Select your `DragonBossConfig`
2. In Inspector:
   - **Spawn Escorts**: ✓
   - **Escort Configuration**: Drag DragonMinionsConfig
   - **Escort Count**: 4

### Step 4: Add Boss Spawner to Scene

1. Create empty GameObject (name it "DragonBossSpawner")
2. Add Component → `BossSpawner`
3. Configure:
   - **Spawner ID**: dragon_boss_01
   - **Spawn Configuration**: Drag DragonBossConfig
   - **Show Gizmos**: ✓
   - **Gizmo Color**: Red
4. Position at boss arena location

### Step 5: Add Boss Trigger

1. Create empty GameObject (name it "BossTrigger")
2. Add Component → Box Collider
3. Set collider as Trigger
4. Size the box to cover entrance area
5. Add Component → `BossEncounterTrigger`
6. Configure:
   - **Boss Spawner**: Drag DragonBossSpawner from Hierarchy
   - **Trigger Team**: 1 (player team)
   - **One Time Use**: ✓
   - **Destroy After Use**: ✓

### Step 6: Test Boss Encounter

1. Play in Host mode
2. Walk into the trigger area
3. Boss should spawn after 2 seconds
4. 4 minions should spawn around the boss
5. The trigger will be destroyed after use

## Common Configurations

### Continuous Spawner (Dungeon Mobs)
```
Spawn Count: 2-4
Max Active: 8-12
Interval: 5-10s
Respawn: Enabled (10-20s)
Waves: Disabled
Area: 5-10m radius
```

### Wave Spawner (Arena/Survival)
```
Spawn Count: 1
Max Active: 15-20
Use Waves: Enabled
Units Per Wave: 5-8
Wave Cooldown: 30-60s
Area: 8-12m radius
```

### Rare Spawn (Elite Mobs)
```
Spawn Count: 1
Max Active: 1
Interval: 60-120s
Respawn: Enabled (120-300s)
Area: Point or small radius
```

### Boss (One-Time)
```
One Time: Enabled
Requires Activation: Enabled
Activation Delay: 2-5s
Escorts: Optional
Area: Point
```

### Boss (Repeatable)
```
One Time: Disabled
Respawn Delay: 300-1800s (5-30 min)
Requires Activation: Disabled
Area: Point or small radius
```

## Troubleshooting Quick Fixes

**Nothing spawns:**
- Check spawner `isActive` is true
- Verify spawn configuration is assigned
- Make sure you're hosting/running server
- Check UnitSpawner exists in scene

**Units spawn in wrong place:**
- Adjust `spawnHeightOffset`
- Enable/disable `alignToGround`
- Check ground layer mask
- Verify spawner position in scene

**Too many units:**
- Set `maxActiveUnits` lower
- Increase `spawnInterval`
- Decrease `spawnCount`
- Check multiple spawners aren't overlapping

**Boss doesn't trigger:**
- Ensure trigger collider is set to IsTrigger
- Check trigger team matches player team
- Verify boss spawner reference is connected
- Check if already triggered (one-time use)

## Next Steps

1. Read full [README.md](README.md) for detailed documentation
2. Check [SpawnConfigurationPresets.cs](SpawnConfigurationPresets.cs) for preset examples
3. Experiment with different spawn configurations
4. Set up SpawnManager for global control
5. Create multiple spawners and test interactions

## Tips

- Use Scene view Gizmos to visualize spawn areas
- Yellow = Mob Spawner, Red = Boss Spawner, Magenta = Trigger
- Start with simple configs and iterate
- Test with one spawner at a time first
- Use SpawnManager.LogStatistics() for debugging
