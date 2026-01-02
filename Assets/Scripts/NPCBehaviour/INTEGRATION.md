# Integration Guide - How the Behaviour System Works with Existing Architecture

This document explains how the NPC Behaviour System integrates with the existing Unity/Mirror game architecture.

---

## Architecture Overview

```
┌─────────────────────────────────────────┐
│         BehaviourExecutor (Server)      │
│    Manages state machine & transitions  │
└─────────────────┬───────────────────────┘
                  │
        ┌─────────┴─────────┐
        │                   │
        ▼                   ▼
┌────────────────┐  ┌──────────────────┐
│ UnitController │  │  UnitMediator    │
│ - Movement     │  │  - Skills        │
│ - Health       │  │  - Buffs         │
│ - Team         │  │  - Stats         │
└────────────────┘  └──────────────────┘
        │                   │
        └─────────┬─────────┘
                  │
        ┌─────────┴─────────┐
        │                   │
        ▼                   ▼
┌──────────────────┐  ┌──────────────────┐
│  SkillSystem     │  │  NavMesh (Unity) │
│  - Skills        │  │  - Pathfinding   │
│  - Execution     │  │  - Movement      │
└──────────────────┘  └──────────────────┘
        │
        ▼
┌─────────────────────────────┐
│  NetworkedSkillInstance     │
│  - Server-side execution    │
│  - Cooldown tracking        │
│  - Effect chains            │
└─────────────────────────────┘
```

---

## Component Integration

### UnitController Integration

**What the behaviour system uses from UnitController:**

```csharp
// From BehaviourContext
public float Health => Unit.health;
public float MaxHealth => Unit.maxHealth;
public bool IsDead => Unit.IsDead;
public int Team => Unit.team;
public Vector3 Position => Unit.transform.position;

// Movement control
unit.horizontalInput = 1.0f;  // Set by ChaseState, PatrolState
unit.verticalInput = 0.5f;   // Set by movement states
unit.angle = 45.0f;          // Set by all states
```

**How the behaviour system works without modifying UnitController:**

- Behaviour system reads UnitController properties (read-only from behaviour perspective)
- Behaviour system controls movement by setting input values that UnitController already reads
- No changes to UnitController code are needed
- UnitController continues to handle networking, animations, etc.

---

### UnitMediator Integration

**What the behaviour system uses from UnitMediator:**

```csharp
// Access to systems
var mediator = context.Mediator;
mediator.Skills;           // SkillSystem - for skill management
mediator.Buffs;           // BuffSystem - for buff information
mediator.Stats;           // StatSystem - for stat queries
mediator.UnitController;  // Direct UnitController reference
```

**Skill Management:**

```csharp
// In AttackState.TryUseSkill()
context.RefreshAvailableSkills(); // Reads from mediator.Skills

// Then use skill selector
var skill = skillSelector.SelectSkill(context, availableSkills);

// Finally execute skill
skill.TriggerCast(aimPoint, aimRotation);
```

The skill execution goes through existing NetworkedSkillInstance infrastructure - no changes needed!

---

## Server-Client Architecture

### Server Side (Where Behaviour Runs)

```
Server:
  BehaviourExecutor.Update()
    ├─ CurrentState.OnUpdate()  (e.g., ChaseState)
    │   ├─ Find target
    │   ├─ Calculate path
    │   └─ Set UnitController.horizontalInput/verticalInput
    │
    ├─ Check Transitions
    │   └─ Evaluate conditions
    │
    └─ AttackState
        └─ Select skill
            └─ skill.TriggerCast()  (triggers NetworkedSkillInstance)
                └─ [RPC] Executes on server, syncs to clients
```

**Key Point:** ALL behaviour logic runs on server only!

### Client Side (What Clients See)

Clients automatically receive updates through Mirror:

```csharp
// UnitController SyncVars (automatically synced to clients)
[SyncVar] public float horizontalInput;
[SyncVar] public float verticalInput;
[SyncVar] public float angle;
[SyncVar] public int health;

// SkillSystem (automatically synced to clients)
public readonly SyncList<NetworkedSkillInstance> normalSkills;
public readonly SyncList<NetworkedSkillInstance> ultimateSkills;
```

Clients just see the results:
- NPC moving in certain direction
- NPC attacking with animations
- NPC health changing

**Clients never run behaviour logic** - they just see the effects!

---

## Skill System Integration

### Flow: How Behaviour Uses Skills

```
1. AttackState.OnUpdate()
   └─> TryUseSkill()
       ├─ skillSelector.SelectSkill(context, availableSkills)
       │  └─ Returns best NetworkedSkillInstance
       │
       └─ skill.TriggerCast(aimPoint, aimRotation)
           ├─ [Server] Validates skill can be cast
           ├─ [Server] Executes skill logic (SkillData.castTrigger)
           ├─ [Server] Applies effects through SkillEffectChainData
           ├─ [Server] Sets cooldown via CastContext.MarkCastCounted()
           └─ [RPC] Syncs to clients (animations, visual effects)
```

### Skill Selectors in Detail

**BehaviourContext provides all needed context:**

```csharp
// In SkillSelector.SelectSkill()
public override NetworkedSkillInstance SelectSkill(
    BehaviourContext context,      // Has health, target, position, etc.
    List<NetworkedSkillInstance> availableSkills)
{
    // Example: DistanceBasedSkillSelector
    float distance = context.DistanceToTarget();
    
    if (distance < 5)
        return availableSkills.FirstOrDefault(s => s.skillName == "Melee");
    else if (distance < 15)
        return availableSkills.FirstOrDefault(s => s.skillName == "Ranged");
    else
        return availableSkills.FirstOrDefault(s => s.skillName == "LongRange");
}
```

**Key Integration Points:**

1. SkillSelector receives BehaviourContext with all runtime data
2. Selector uses context to intelligently choose skills
3. Returns a NetworkedSkillInstance from the available list
4. Behaviour executor calls `skill.TriggerCast()` with aim data
5. Existing skill system handles execution and networking

---

## NavMesh Integration

### How Movement Works

```csharp
// In ChaseState.MoveTowardsTarget()
NavMesh.CalculatePath(
    startPos: context.Position,
    endPos: targetPos,
    areaMask: NavMesh.AllAreas,
    path: context.CurrentPath  // NavMeshPath
);

// Get next waypoint from calculated path
Vector3 nextWaypoint = context.CurrentPath.corners[1];

// Calculate direction and set input
Vector3 direction = (nextWaypoint - context.Position).normalized;
context.Unit.horizontalInput = direction.x;
context.Unit.verticalInput = direction.z;
```

**Why NavMesh?**

- Respects walkable areas in your level
- Avoids obstacles automatically
- Smooth, pathfinding-based movement
- Integrates with existing level design

**No NavMeshAgent required** - the behaviour system uses NavMesh directly for path calculation, leaving movement implementation to UnitController.

---

## Data Flow Example: Complete Chase → Attack Sequence

### Server Update Cycle

```
Frame 1:
├─ BehaviourExecutor.Update()
│  ├─ ChaseState.OnUpdate()
│  │  ├─ RefreshTarget() finds player at 8 meters
│  │  ├─ NavMesh.CalculatePath() to player
│  │  ├─ Set horizontalInput = 0.7, verticalInput = 0.3
│  │  └─ Set angle toward target
│  │
│  └─ CheckTransitions()
│     └─ "In Attack Range" condition (distance < 3) = FALSE
│        └─ Stay in Chase state
│
└─ UnitController.FixedUpdate()
   └─ Apply movement based on inputs

[Mirror Syncs: inputs, angle, position to clients]

Frame 2:
├─ BehaviourExecutor.Update()
│  ├─ ChaseState.OnUpdate()
│  │  ├─ RefreshTarget() finds player at 2.5 meters
│  │  ├─ NavMesh path shows arrive at target
│  │  └─ Set inputs (minimal movement to track target)
│  │
│  └─ CheckTransitions()
│     └─ "In Attack Range" condition (distance < 3) = TRUE
│        └─ Transition to AttackState
│
├─ AttackState.OnEnter()
│  └─ RefreshAvailableSkills()
│
└─ [State changed, animations update on clients]

Frame 3:
├─ BehaviourExecutor.Update()
│  └─ AttackState.OnUpdate()
│     ├─ FaceTarget()
│     ├─ TryUseSkill()
│     │  ├─ skillSelector.SelectSkill() -> "Melee Attack"
│     │  └─ skill.TriggerCast(targetPos, null)
│     │     └─ [Server] Executes skill
│     │        ├─ SkillData.castTrigger.ExecuteCoroutine()
│     │        ├─ Effects apply (damage, knockback, etc.)
│     │        └─ [RPC] Syncs to clients (animation + sound)
│     │
│     └─ Set cooldown
│
└─ Client sees attack animation playing

Frame 4+:
└─ AttackState continues until cooldown expires or condition triggers transition
```

---

## Boss Phase Integration

### HealthPhaseManager Interaction

```csharp
// Server Update
if (unit.health < maxHealth * 0.7) // Entering Phase 2
{
    // 1. Get new behaviour profile from HealthPhaseProfile
    var newProfile = phaseProfile.GetPhaseForHealth(0.65f);
    
    // 2. Switch behaviour via BehaviourExecutor
    behaviourExecutor.SetBehaviourProfile(newProfile.behaviourProfile);
    
    // 3. Modify skills
    foreach (var skillName in newProfile.skillsToAdd)
        skillSystem.AddSkill(SkillSlotType.Normal, skillName);
    
    // 4. Spawn transition effect
    [RPC] SpawnPhaseEffect(vfxPrefab, position);
}

// Result:
// - BehaviourExecutor enters new profile's initial state
// - NPC has new skills available (selected by new profile's skill selectors)
// - Clients see VFX and animation changes
// - Behaviour changes based on new profile
```

---

## Integration Checklist

When adding behaviour system to an NPC:

- [ ] NPC has UnitController component
- [ ] NPC has UnitMediator component
- [ ] NPC has SkillSystem component
- [ ] NPC has skills added via SkillSystem.AddSkill()
- [ ] BehaviourExecutor component added
- [ ] Behaviour profile assigned to BehaviourExecutor
- [ ] Behaviour profile has valid initial state
- [ ] All states in profile are in availableStates list
- [ ] Skills referenced in selectors exist on NPC
- [ ] Conditions reference correct distance/health values
- [ ] NavMesh is baked in scene (for movement states)

---

## Performance Considerations

### Server CPU Cost

**Per NPC:**
- BehaviourExecutor.Update(): ~0.1ms
- State.OnUpdate(): ~0.05-0.2ms (depends on state)
- Transition evaluation: ~0.1-0.5ms (depends on conditions)
- NavMesh path calculation: ~0.2-1ms (done periodically)
- Skill selection: ~0.1-0.5ms

**Total per NPC:** ~0.6-2.5ms per frame (varies by complexity)

**Optimization strategies:**
1. Increase transitionCheckInterval (default 0.2s)
2. Reduce condition count per transition
3. Use simpler conditions (Distance vs Composite)
4. Reduce NavMesh path calculation frequency
5. Disable NPCs far from players

### Network Cost

- Only input/angle changes synced (already done by UnitController)
- No extra bandwidth for behaviour system
- Skill execution already networked via SkillSystem
- HealthPhaseManager uses minimal RPCs

---

## Existing System Compatibility

### Changes Required: NONE

The behaviour system works with existing code:

✓ **UnitController** - No changes needed (reads exist)  
✓ **UnitMediator** - No changes needed (reads exist)  
✓ **SkillSystem** - No changes needed (uses existing methods)  
✓ **NetworkedSkillInstance** - No changes needed (calls existing TriggerCast)  
✓ **BuffSystem** - No changes needed (read-only access)  
✓ **StatSystem** - No changes needed (read-only access)  

### Adding Behaviour System

Simply add these components to NPC prefabs:
1. BehaviourExecutor
2. Create/assign BehaviourProfile

That's it!

---

## Example: Adding to Existing Zombie Prefab

### Before Integration
```
ZombiePrefab
├─ Transform
├─ UnitController
├─ UnitMediator
├─ SkillSystem
├─ AiZombieController  ← Old hardcoded AI
└─ Renderer
```

### After Integration
```
ZombiePrefab
├─ Transform
├─ UnitController
├─ UnitMediator
├─ SkillSystem
├─ AiZombieController  ← Can delete now
├─ BehaviourExecutor   ← New component
└─ Renderer
```

**Behaviour Profile (external asset):**
```
ZombieBehaviour
├─ Idle State
├─ Chase State
├─ Attack State
├─ Transitions
└─ Skill Selector
```

The behaviour system reads the same data, navigates the same NavMesh, uses the same skills - but configured via data instead of code!

---

## Debugging Integration Issues

### NPC not moving
- Check NavMesh is baked
- Check UnitController movement code
- Verify ChaseState inputs are being set
- Check `context.Position` in debugger

### Skills not triggering
- Verify skills exist on NPC (SkillSystem)
- Check skill names match selector configuration
- Verify skill isn't on cooldown
- Check `context.AvailableSkills` in debugger

### Wrong behaviour profile
- Check BehaviourExecutor.CurrentProfile
- Verify profile's initial state exists
- Check `behaviourProfile.Validate()` returns true

### State not transitioning
- Enable debug mode on BehaviourExecutor
- Check Console for state transition logs
- Verify condition is properly configured
- Verify transition priority if multiple valid

---

## Summary

The behaviour system is designed as a **non-invasive overlay** on existing architecture:

1. **Reads** from UnitController and UnitMediator
2. **Writes** to UnitController movement inputs only
3. **Delegates** skill execution to existing SkillSystem
4. **Respects** existing networking infrastructure
5. **Requires** no modifications to existing code

This clean separation makes it easy to add to any existing NPC without refactoring!
