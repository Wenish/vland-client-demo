# Threat System Guide

## Overview

The Threat System (also called "Aggro System") allows NPCs to prioritize targets based on accumulated threat values. This creates more dynamic combat where NPCs respond to player actions like damage, healing, and taunts.

## Core Concepts

### What is Threat?

Threat is a numeric value representing how much an NPC wants to attack a specific target. The NPC will typically focus on the target with the highest threat value.

### Key Components

1. **ThreatTable** - Core data structure tracking threat per target
2. **ThreatManager** - MonoBehaviour managing threat for an NPC
3. **Threat-based Conditions** - ScriptableObject conditions for state transitions
4. **Threat-aware States** - Updated Chase/Attack states that use threat

## Setup Guide

### Step 1: Add ThreatManager to NPCs

Add the `ThreatManager` component to any NPC that should use threat-based targeting:

```
1. Select your NPC prefab/GameObject
2. Add Component > NPCBehaviour > ThreatManager
3. Configure threat settings in Inspector
```

**Configuration Options:**

- **Enable Threat**: Toggle threat system on/off
- **Threat Decay Rate**: How much threat decays per second (e.g., 1.0 = 1 threat/sec)
- **Max Threat**: Maximum threat value per target (e.g., 1000)
- **Threat Range**: Maximum distance to maintain threat (e.g., 50 units)
- **Damage Threat Multiplier**: Threat generated per damage point (e.g., 1.0 = 1:1 ratio)
- **Healing Generates Threat**: Should healing create threat?
- **Healing Threat Multiplier**: Threat per healing point (e.g., 0.5)

### Step 2: Configure States for Threat

#### Chase State

Enable threat targeting in your Chase state assets:

1. Open your Chase state ScriptableObject
2. Check "Use Threat Targeting"
3. The state will now prioritize the highest-threat target

#### Attack State

Configure threat target updates:

1. Open your Attack state ScriptableObject
2. Check "Update Threat Target"
3. Set "Threat Update Interval" (e.g., 1.0 seconds)

### Step 3: Create Threat-Based Transitions

Use threat conditions for dynamic state transitions:

**Example: Attack Highest Threat Target**

1. Create: `Assets > Create > Game > NPC Behaviour > Conditions > Highest Threat`
2. Configure:
   - Check "Check Current Target" = false
   - Check "Update Current Target" = true
3. Add to transition that leads to Attack state

**Example: Flee When Too Many Threats**

1. Create: `Assets > Create > Game > NPC Behaviour > Conditions > Threat Threshold`
2. Configure:
   - Mode: "Total Target Count"
   - Comparison: "Greater Than"
   - Threshold: 5
3. Add to transition leading to Flee state

## Threat Generation

### Automatic Threat

The ThreatManager automatically generates threat from:

- **Damage Taken**: When NPC receives damage (multiplied by `damageThreatMultiplier`)
- **Healing Received**: When NPC is healed nearby enemies (if enabled)

### Manual Threat

Add threat through code when needed:

```csharp
// From skill/ability scripts
ThreatManager threatManager = npcUnit.GetComponent<ThreatManager>();
if (threatManager != null)
{
    // Add threat when dealing damage
    threatManager.OnDamageDealt(targetUnit, damageAmount, threatMultiplier: 2.0f);
    
    // Add threat when healing
    threatManager.OnHealingDealt(healTarget, healAmount);
}

// Direct threat manipulation
threatManager.AddThreat(targetUnit, 50f);
threatManager.RemoveThreat(targetUnit, 25f);
threatManager.ClearThreat(targetUnit);
```

### Taunt Mechanics

Force an NPC to focus on a specific target:

```csharp
ThreatManager enemyThreat = enemy.GetComponent<ThreatManager>();
if (enemyThreat != null)
{
    // Tank taunts enemy
    enemyThreat.Taunt(tankUnit, duration: 5f);
}
```

### Threat Transfer

Transfer threat between targets (tank swaps):

```csharp
// Transfer 50% of threat from tank1 to tank2
threatManager.TransferThreat(tank1, tank2, percentage: 0.5f);
```

### AoE Threat Reduction

Scale all threat by a multiplier:

```csharp
// Reduce all threat by 30%
threatManager.ScaleAllThreat(0.7f);
```

## Threat Conditions

### HighestThreatCondition

Checks if a target has the highest threat value.

**Use Cases:**
- Transition to attack when highest threat target is in range
- Switch targets to whoever has highest threat

**Configuration:**
- **Check Current Target**: If true, validates current target is highest. If false, just checks if any target has threat.
- **Update Current Target**: Automatically sets CurrentTarget to highest threat target

### ThreatThresholdCondition

Checks if threat values meet certain criteria.

**Modes:**

1. **Target Above Threshold**: Specific target's threat > threshold
   ```
   Example: Current target has > 100 threat
   ```

2. **Any Above Threshold**: Any target in table has threat > threshold
   ```
   Example: Any enemy has > 200 threat (someone did something aggressive)
   ```

3. **Total Target Count**: Number of targets with threat
   ```
   Example: More than 3 enemies attacking (trigger AoE or flee)
   ```

4. **Highest Threat Value**: The highest threat value in table
   ```
   Example: Highest threat > 500 (someone is really aggressive)
   ```

**Comparisons:**
- Less Than
- Less Than Or Equal
- Equal
- Greater Than Or Equal
- Greater Than

## Example Configurations

### Example 1: Basic Threat Tank

**NPC Configuration:**
```
ThreatManager:
  - Enable Threat: ✓
  - Threat Decay Rate: 2.0
  - Max Threat: 1000
  - Damage Threat Multiplier: 1.0
```

**Chase State:**
```
- Use Threat Targeting: ✓
```

**Attack State:**
```
- Update Threat Target: ✓
- Threat Update Interval: 0.5
```

### Example 2: Boss with Threat Reset

**Behaviour Profile:**
```
Global Transitions:
  1. Threat Threshold Condition
     - Mode: Total Target Count
     - Comparison: Greater Than
     - Threshold: 10
     → Transition to AoE Attack State
     
  2. On AoE Attack State Exit:
     - Clear all threat (via code)
     - Reset targeting
```

### Example 3: Healer Aggro

**ThreatManager:**
```
- Healing Generates Threat: ✓
- Healing Threat Multiplier: 0.5
```

When a player heals nearby:
- All nearby NPCs gain threat towards that healer
- NPCs may switch to attack the healer

### Example 4: Multi-Phase Boss

```csharp
// In health phase transition code
public void OnPhaseChange(int newPhase)
{
    var threatManager = GetComponent<ThreatManager>();
    
    if (newPhase == 2)
    {
        // Reset threat for dramatic phase change
        threatManager.ClearAllThreat();
    }
    else if (newPhase == 3)
    {
        // Scale down threat but don't reset
        threatManager.ScaleAllThreat(0.5f);
    }
}
```

## Advanced Techniques

### Threat Modifiers by Role

```csharp
// Tank ability: Generate extra threat
void TankAbilityHit(UnitController target, float damage)
{
    var targetThreat = target.GetComponent<ThreatManager>();
    if (targetThreat != null)
    {
        // Tanks generate 3x threat
        targetThreat.AddThreat(this.unit, damage * 3f);
    }
}

// DPS ability: Generate normal threat
void DPSAbilityHit(UnitController target, float damage)
{
    var targetThreat = target.GetComponent<ThreatManager>();
    if (targetThreat != null)
    {
        // DPS generate 1x threat
        targetThreat.AddThreat(this.unit, damage);
    }
}

// Stealth ability: Reduce threat
void StealthActivate()
{
    // Find all enemies targeting me
    var enemies = FindObjectsByType<ThreatManager>();
    foreach (var enemy in enemies)
    {
        // Reduce my threat by 80%
        float currentThreat = enemy.GetThreat(this.unit);
        enemy.RemoveThreat(this.unit, currentThreat * 0.8f);
    }
}
```

### Smart Target Switching

```csharp
// Boss mechanic: Attack lowest threat target
void AttackWeakest()
{
    var threats = threatManager.ThreatTable.GetThreatList();
    if (threats.Count > 0)
    {
        // Get target with LOWEST threat (supports/healers)
        var weakestTarget = threats[threats.Count - 1].unit;
        context.CurrentTarget = weakestTarget;
    }
}
```

### Threat-based Special Attacks

```csharp
// Use special attack on high-threat target
void CheckForSpecialAttack(BehaviourContext context)
{
    var highestThreat = context.GetHighestThreatTarget();
    if (highestThreat != null)
    {
        float threat = context.GetThreat(highestThreat);
        
        if (threat > 500f)
        {
            // Execute special "frustrated" attack
            ExecuteEnragedAttack(highestThreat);
        }
    }
}
```

## Debugging Threat

### Enable Debug Mode

```
ThreatManager Component:
  - Debug Mode: ✓
```

This logs threat changes to the console:
```
[ThreatManager] Player gained 50 threat (Total: 150)
[ThreatManager] Healer lost 25 threat (Total: 75)
[ThreatManager] Cleared all threat
```

### Query Threat Values

```csharp
// Check specific target's threat
float threat = threatManager.GetThreat(targetUnit);
Debug.Log($"{targetUnit.name} has {threat} threat");

// List all threats
var allThreats = threatManager.ThreatTable.GetThreatList();
foreach (var (unit, threatValue) in allThreats)
{
    Debug.Log($"{unit.name}: {threatValue} threat");
}

// Get threat count
int count = threatManager.ThreatTargetCount;
Debug.Log($"Currently tracking {count} targets");
```

### Visual Debug Display

You can extend `BehaviourDebugDisplay` to show threat values:

```csharp
// In BehaviourDebugDisplay.cs
if (context.HasThreatSystem)
{
    var threats = context.ThreatManager.ThreatTable.GetThreatList();
    debugText += $"\nThreat Targets: {threats.Count}";
    
    foreach (var (unit, threat) in threats.Take(3))
    {
        debugText += $"\n  {unit.name}: {threat:F0}";
    }
}
```

## Integration with Existing Code

The threat system integrates seamlessly with the existing NPC Behaviour System:

1. **Optional**: NPCs work with or without ThreatManager
2. **Backward Compatible**: Existing states/conditions still work
3. **Context-Aware**: BehaviourContext provides threat helper methods
4. **State-Friendly**: States can optionally use threat or ignore it

### Gradual Migration

You can enable threat gradually:

1. Add ThreatManager to select NPCs only
2. Keep existing targeting logic as fallback
3. Test threat-based targeting on new NPCs
4. Migrate existing NPCs once tested

## Performance Considerations

### Threat Decay

- Set `threatDecayRate = 0` for bosses that should never forget
- Higher decay for roaming NPCs that reset quickly

### Update Intervals

- Use longer intervals for less critical NPCs
- Shorter intervals for bosses requiring responsive targeting

### Threat Range

- Set appropriate range to automatically cleanup distant targets
- Prevents memory/performance issues with large threat tables

### Target Count

- Monitor `ThreatTargetCount` for performance
- Use conditions to react when too many targets

## Common Patterns

### Tank-Healer-DPS Trinity

```
Tank: Generates high threat (3x multiplier on abilities)
Healer: Generates moderate threat from healing
DPS: Generates normal threat from damage
```

### Boss Enrage

```
When total threat > threshold:
  - Increase attack speed
  - Use special abilities
  - Become more aggressive
```

### Stealth/Vanish

```
Player uses stealth ability:
  - Remove 80-100% of threat
  - NPC switches to next highest threat target
```

### Taunt/Challenge

```
Tank uses taunt:
  - Set threat to maximum
  - Lock targeting for duration
  - Optionally increase damage taken
```

## Troubleshooting

**NPC not using threat targeting**
- Ensure ThreatManager component is attached
- Check "Enable Threat" is checked
- Verify state has threat options enabled

**Threat not decaying**
- Check Threat Decay Rate > 0
- Ensure NPC is alive and active

**Targets not being tracked**
- Verify targets are within Threat Range
- Check that damage is being applied
- Enable Debug Mode to see threat changes

**NPC switches targets too often**
- Increase Threat Update Interval in states
- Increase threat generation multipliers
- Reduce threat decay rate

## API Reference

See [API_REFERENCE.md](API_REFERENCE.md) for complete class documentation.

## Examples

See [EXAMPLES.md](EXAMPLES.md) for complete example configurations.
