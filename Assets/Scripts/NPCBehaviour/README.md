# NPC Behaviour System - Complete Implementation Guide

## Overview

This is a fully server-authoritative, data-driven NPC behaviour system for Unity games using Mirror networking. The system is built around ScriptableObjects, making it completely configurable without writing code.

## Architecture

### Core Components

1. **BehaviourState** - Base class for defining NPC states (Idle, Chase, Attack, etc.)
2. **BehaviourProfile** - Collections of states and transitions that define an NPC's behaviour
3. **BehaviourCondition** - Reusable conditions for state transitions
4. **BehaviourTransition** - Defines when and how to switch between states
5. **BehaviourContext** - Runtime data container (never stored in ScriptableObjects)
6. **BehaviourExecutor** - Server-side component that runs the behaviour logic
7. **SkillSelector** - Strategies for choosing which skills to use
8. **HealthPhaseManager** - Boss fight phase system based on health thresholds
9. **ThreatManager** - Optional threat/aggro system for dynamic target prioritization

### Design Principles

- **Server-Authoritative**: All NPC logic runs only on the server
- **Data-Driven**: Behaviour is configured via ScriptableObjects, no hardcoding
- **Stateless ScriptableObjects**: Runtime data lives in BehaviourContext, not in assets
- **Modular**: Easy to add new states, conditions, and selectors
- **Reusable**: Components can be mixed and matched across different NPCs
- **Threat-Aware**: Optional threat system for MMO-style aggro mechanics

## Getting Started

### Step 1: Create Behaviour States

Create ScriptableObject assets for the states your NPC will use:

**Example: Zombie Behaviour**

1. Create Idle State: `Assets > Create > Game > NPC Behaviour > States > Idle`
2. Create Chase State: `Assets > Create > Game > NPC Behaviour > States > Chase`
3. Create Attack State: `Assets > Create > Game > NPC Behaviour > States > Attack`

Configure each state's parameters in the Inspector.

### Step 2: Create Conditions

Create conditions that determine when to transition between states:

**Example: Distance-Based Transitions**

1. Create "Enemy In Range" condition: `Assets > Create > Game > NPC Behaviour > Conditions > Distance`
   - Set comparison to "LessThan"
   - Set distance to 15
   - Enable "useCurrentTarget" to false (detects ANY enemy)

2. Create "Attack Range" condition: `Assets > Create > Game > NPC Behaviour > Conditions > Distance`
   - Set comparison to "LessThan"
   - Set distance to 3
   - Enable "useCurrentTarget" to true

### Step 3: Create Transitions

Create transition assets that link states together:

**Example: Idle to Chase Transition**

1. Create transition: `Assets > Create > Game > NPC Behaviour > Transition`
2. Set target state to your Chase state
3. Add the "Enemy In Range" condition to the conditions list

**Example: Chase to Attack Transition**

1. Create transition: `Assets > Create > Game > NPC Behaviour > Transition`
2. Set target state to your Attack state
3. Add the "Attack Range" condition

### Step 4: Configure States with Transitions

Open each state asset and add transitions:

- **Idle State**: Add "Idle to Chase" transition
- **Chase State**: Add "Chase to Attack" transition
- **Attack State**: Add "Attack to Chase" transition (with distance > 3 condition)

### Step 5: Create Skill Selectors

Create a skill selector for the Attack state:

**Example: First Available Selector**

1. Create selector: `Assets > Create > Game > NPC Behaviour > Skill Selectors > First Available`
2. Assign this selector to your Attack state's `skillSelector` field

**Example: Distance-Based Selector** (for more advanced NPCs)

1. Create selector: `Assets > Create > Game > NPC Behaviour > Skill Selectors > Distance Based`
2. Add distance mappings:
   - 0-5 range: "MeleeSlash"
   - 5-15 range: "RangedShot"
   - 15-30 range: "LongRangeBolt"

### Step 6: Create Behaviour Profile

Tie everything together in a behaviour profile:

1. Create profile: `Assets > Create > Game > NPC Behaviour > Behaviour Profile`
2. Name it "ZombieBehaviour"
3. Set initial state to your Idle state
4. Add all states to the available states list
5. Optionally add global transitions (transitions that work from any state)

### Step 7: Attach to NPC

1. Select your NPC prefab
2. Add the `BehaviourExecutor` component
3. Assign your behaviour profile to the component
4. Ensure the NPC has `UnitController` and `UnitMediator` components
5. Add skills to the NPC using the existing `SkillSystem`

### Step 8: Test

1. Start the game as server/host
2. The NPC should automatically begin executing its behaviour
3. Enable "Debug Mode" on BehaviourExecutor to see state transitions in the console

## Advanced Features

### Boss Phases

Create multi-phase bosses that change behaviour as they lose health:

1. Create multiple behaviour profiles (one per phase)
2. Create a Health Phase Profile: `Assets > Create > Game > NPC Behaviour > Health Phase Profile`
3. Add phases:
   - Phase 1 (100%-70% health): Use aggressive behaviour
   - Phase 2 (70%-40% health): Use defensive behaviour with healing
   - Phase 3 (40%-0% health): Use berserk behaviour with ultimate skills
4. For each phase, specify:
   - Health threshold
   - Behaviour profile
   - Skills to add/remove
   - Optional transition effects
5. Add the `HealthPhaseManager` component to your boss
6. Assign the Health Phase Profile

### Complex Conditions

Use `CompositeCondition` to combine multiple conditions:

**Example: "Low Health AND Enemy Nearby"**

1. Create a Composite Condition with AND logic
2. Add a Health condition (< 30%)
3. Add a Distance condition (< 10 units)
4. Use this for a "Flee" transition

### Weighted Random Skill Selection

Make bosses unpredictable:

1. Create a Random Weighted Selector
2. Add skills with weights:
   - "FireBlast": 3.0 (most common)
   - "IceStorm": 2.0
   - "LightningStrike": 1.0 (rare)

### Priority-Based Skills

Create optimal skill rotations:

1. Create a Priority Selector
2. Order skills by priority:
   - Priority 1: "ExecuteFinisher" (use when available)
   - Priority 2: "PowerfulAttack"
   - Priority 3: "BasicAttack" (fallback)

## Example Configurations

### Basic Zombie

**States**: Idle, Chase, Attack
**Logic**: 
- Idle until enemy in range
- Chase when enemy detected
- Attack when close enough
- Use first available skill

### Patrol Guard

**States**: Patrol, Chase, Attack
**Logic**:
- Patrol between waypoints
- Chase when enemy detected
- Return to patrol after enemy defeated

### Boss Fight (3 Phases)

**Phase 1 (100-70%)**: Aggressive, uses basic attacks
**Phase 2 (70-40%)**: Defensive, summons minions, heals
**Phase 3 (40-0%)**: Berserk, uses ultimate abilities

## Performance Considerations

- **Transition Check Interval**: Default is 0.2s. Lower = more responsive but more CPU usage
- **Target Update Frequency**: Chase state updates target every 0.5s by default
- **Path Recalculation**: Flee state recalculates every 1s by default

Adjust these values in the state/component Inspector for your performance needs.

## Integration with Existing Systems

The behaviour system integrates seamlessly with:

- **UnitController**: Provides health, team, position, movement input
- **UnitMediator**: Provides access to stats, buffs, and skills
- **SkillSystem**: Skills are selected and triggered through existing infrastructure
- **NetworkedSkillInstance**: Skill execution uses existing server-side skill system
- **NavMesh**: All movement uses Unity's NavMesh system

## Extending the System

### Creating Custom States

```csharp
using NPCBehaviour;
using UnityEngine;

[CreateAssetMenu(fileName = "MyCustomState", menuName = "Game/NPC Behaviour/States/My Custom State")]
public class MyCustomState : BehaviourState
{
    public override void OnEnter(BehaviourContext context)
    {
        // Initialize state
    }

    public override bool OnUpdate(BehaviourContext context, float deltaTime)
    {
        // Update logic
        return true; // Return false to force exit
    }

    public override void OnExit(BehaviourContext context)
    {
        // Cleanup
    }

    public override BehaviourTransition EvaluateTransitions(BehaviourContext context)
    {
        // Check your transitions
        return null;
    }
}
```

### Creating Custom Conditions

```csharp
using NPCBehaviour;
using UnityEngine;

[CreateAssetMenu(fileName = "MyCustomCondition", menuName = "Game/NPC Behaviour/Conditions/My Custom")]
public class MyCustomCondition : BehaviourCondition
{
    public float threshold = 0.5f;

    public override bool Evaluate(BehaviourContext context)
    {
        // Your condition logic
        return context.HealthPercent < threshold;
    }
}
```

### Creating Custom Skill Selectors

```csharp
using System.Collections.Generic;
using NPCBehaviour;
using UnityEngine;

[CreateAssetMenu(fileName = "MyCustomSelector", menuName = "Game/NPC Behaviour/Skill Selectors/My Custom")]
public class MyCustomSkillSelector : SkillSelector
{
    public override NetworkedSkillInstance SelectSkill(BehaviourContext context, List<NetworkedSkillInstance> availableSkills)
    {
        // Your selection logic
        return availableSkills.Count > 0 ? availableSkills[0] : null;
    }
}
```

## Troubleshooting

### NPC Not Moving
- Check that NavMesh is baked in your scene
- Verify UnitController movement input is being set
- Enable debug mode and check Gizmos in Scene view

### States Not Transitioning
- Verify conditions are properly configured
- Check transition priorities if multiple transitions are valid
- Enable debug mode to see state changes in Console

### Skills Not Executing
- Ensure NPC has skills added via SkillSystem
- Check that SkillSelector is assigned to Attack state
- Verify skills are not all on cooldown

### Boss Phases Not Triggering
- Ensure HealthPhaseManager is attached
- Check that health thresholds are set correctly
- Verify behaviour profiles are valid

## File Structure

```
Assets/Scripts/NPCBehaviour/
├── BehaviourState.cs
├── BehaviourProfile.cs
├── BehaviourCondition.cs
├── BehaviourTransition.cs
├── BehaviourContext.cs
├── BehaviourExecutor.cs
├── HealthPhaseProfile.cs
├── HealthPhaseManager.cs
├── States/
│   ├── IdleState.cs
│   ├── ChaseState.cs
│   ├── AttackState.cs
│   ├── PatrolState.cs
│   └── FleeState.cs
├── Conditions/
│   ├── DistanceCondition.cs
│   ├── HealthCondition.cs
│   ├── TimeInStateCondition.cs
│   ├── HasTargetCondition.cs
│   ├── RandomChanceCondition.cs
│   ├── EnemyCountCondition.cs
│   ├── CompositeCondition.cs
│   ├── HighestThreatCondition.cs
│   └── ThreatThresholdCondition.cs
├── SkillSelectors/
│   ├── SkillSelector.cs
│   ├── FirstAvailableSkillSelector.cs
│   ├── DistanceBasedSkillSelector.cs
│   ├── HealthBasedSkillSelector.cs
│   ├── RandomWeightedSkillSelector.cs
│   └── PrioritySkillSelector.cs
├── ThreatTable.cs
└── ThreatManager.cs
```

## Threat System (Optional)

The system includes an optional threat/aggro system for MMO-style combat. NPCs can track which targets are most threatening and prioritize them accordingly.

**Key Features:**
- Automatic threat generation from damage/healing
- Threat decay over time and distance
- Taunt mechanics
- Threat-based targeting in Chase/Attack states
- Threat-based conditions for transitions

See [THREAT_SYSTEM.md](THREAT_SYSTEM.md) for complete documentation.

## Best Practices

1. **Keep States Simple**: Each state should have one clear purpose
2. **Use Transitions Wisely**: Don't create too many transitions from one state
3. **Test Incrementally**: Build and test one behaviour at a time
4. **Use Debug Mode**: Enable it during development to see what's happening
5. **Reuse Components**: Create a library of conditions and selectors to reuse
6. **Document Profiles**: Use the description fields to explain behaviour intent
7. **Profile Performance**: Monitor transition check frequency in production
8. **Threat System**: Add ThreatManager for dynamic target prioritization

## Credits

This system was designed to integrate cleanly with the existing Unity Mirror-based game architecture, respecting the existing UnitController, SkillSystem, and networking infrastructure.
