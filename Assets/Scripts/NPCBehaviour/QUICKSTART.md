# Quick Start Guide - NPC Behaviour System

Get your first NPC running in **5 minutes**!

---

## Pre-requisites

- Your NPC prefab has:
  - ✓ UnitController component
  - ✓ UnitMediator component
  - ✓ SkillSystem component
  - ✓ At least one skill added to SkillSystem
- NavMesh is baked in your scene

---

## Step 1: Create Chase State (1 minute)

1. Right-click in your Assets folder
2. Create > Game > NPC Behaviour > States > Chase
3. Name it: `ZombieChaseState`
4. Settings (keep defaults, they work!)

---

## Step 2: Create Attack State (1 minute)

1. Create > Game > NPC Behaviour > States > Attack
2. Name it: `ZombieAttackState`
3. Open the asset
4. In Inspector:
   - Drag any **FirstAvailableSkillSelector** into the `Skill Selector` field
   - If you don't have one, create it:
     - Create > Game > NPC Behaviour > Skill Selectors > First Available
     - Name: `FirstAvailableSkillSelector`

---

## Step 3: Create Conditions (1 minute)

1. Create distance condition for "attack range":
   - Create > Game > NPC Behaviour > Conditions > Distance
   - Name: `InAttackRange`
   - Set: Distance = 3, Comparison = LessThan

2. Create distance condition for "out of range":
   - Create > Game > NPC Behaviour > Conditions > Distance
   - Name: `OutOfAttackRange`
   - Set: Distance = 4, Comparison = GreaterThan

3. Create condition for "no valid target":
   - Create > Game > NPC Behaviour > Conditions > Has Target
   - Name: `NoValidTarget`
   - Set: Should Have Target = false (unchecked), Require Alive = true

---

## Step 4: Create Transitions (2 minutes)

1. Create transition: Chase → Attack
   - Create > Game > NPC Behaviour > Transition
   - Name: `ChaseToAttack`
   - Target State: `ZombieAttackState`
   - Add Condition: `InAttackRange`

2. Create transition: Attack → Chase
   - Create > Game > NPC Behaviour > Transition
   - Name: `AttackToChase`
   - Target State: `ZombieChaseState`
   - Add Condition: `OutOfAttackRange`

3. Create **global** transition: Any → Chase (target lost/died)
   - Create > Game > NPC Behaviour > Transition
   - Name: `BackToChase_Global`
   - Target State: `ZombieChaseState`
   - Add Condition: `NoValidTarget`
   - **Note**: This will be added as a global transition in Step 6

---

## Step 5: Configure States with Transitions (1 minute)

1. Open `ZombieChaseState`
   - Drag `ChaseToAttack` transition into the Transitions list

2. Open `ZombieAttackState`
   - Drag `AttackToChase` transition into the Transitions list

---

## Step 6: Create Behaviour Profile (1 minute)

1. Create > Game > NPC Behaviour > Behaviour Profile
2. Name: `ZombieBehaviour`
3. Configure:
   - Initial State: `ZombieChaseState`
   - Available States: [ZombieChaseState, ZombieAttackState]
   - **Global Transitions**: [BackToChase_Global]
   
   💡 **Global transitions** work from ANY state, so you don't need to add them to each individual state!

---

## Step 7: Add to Your NPC (1 minute)

1. Select your NPC prefab
2. Add Component > Behaviour Executor
3. Drag `ZombieBehaviour` into the `Behaviour Profile` field
4. Done!

---

## Test It!

1. Press Play
2. The zombie should:
   - Chase any enemies in range
   - Switch to attacking when close
   - Switch back to chasing when far
   - **Return to chasing if target dies or is lost**
   - Use its skill when attacking

---

## That's It!

Your NPC is now running the behaviour system! 🎉

---

## Next Steps

### Customize Your NPC

Open `ZombieChaseState` and tweak:
- `Detection Range` - How far to detect enemies
- `Stopping Distance` - How close to get before attacking

Open `ZombieAttackState` and tweak:
- `Skill Cooldown` - Wait time between attacks
- `Face Target` - Uncheck to stop face tracking

### Make It More Complex

Add more states! Copy EXAMPLES.md templates:
- Add `IdleState` - NPC waits before detecting enemies
- Add `PatrolState` - NPC walks around until finding enemies
- Add `FleeState` - NPC runs away at low health

### Add Boss Phases

Create a HealthPhaseProfile for health-based behavior changes (see EXAMPLES.md).

---

## Troubleshooting

### NPC not moving
- [ ] Is there a NavMesh in your scene?
- [ ] Is the NPC on the NavMesh?
- [ ] Check Console - enable debug mode on BehaviourExecutor

### Skills not executing
- [ ] Does the NPC have a skill added? (Check SkillSystem)
- [ ] Is the skill name correct in your selector?
- [ ] Check Console for warnings

### Wrong behavior
- [ ] Verify BehaviourProfile is assigned to BehaviourExecutor
- [ ] Check all transitions have correct target states
- [ ] Enable debug mode to see state changes

---

## Enable Debug Mode

To see what's happening:

1. Select your NPC in the scene
2. Find BehaviourExecutor component
3. Check "Debug Mode"
4. Open Console (Ctrl+Shift+C or View > Console)
5. Watch state transitions logged in real-time

---

## Common Configurations

### Aggressive Zombie
```
Chase State:
  - Detection Range: 50 (very aggressive)
  - Stopping Distance: 0.5 (chase all the way)
  - Prioritize Closest: true

Attack State:
  - Skill Cooldown: 0.5 (attack fast!)
```

### Cautious Guard
```
Chase State:
  - Detection Range: 15 (normal)
  - Stopping Distance: 3 (keep distance)
  - Prioritize Closest: true

Attack State:
  - Skill Cooldown: 2 (slow attacks)
  - Face Target: true
```

### Ranged Sniper
```
Add Flee State when too close!

Flee State:
  - Flee Distance: 20
  - Threat Detection Range: 10
```

---

## 5-Minute Recap

1. ✓ Create 2 states (Chase, Attack)
2. ✓ Create 3 conditions (InRange, OutOfRange, NoValidTarget)
3. ✓ Create 3 transitions (C→A, A→C, Any→C)
4. ✓ Create 1 skill selector
5. ✓ Create 1 behaviour profile
6. ✓ Assign to NPC (add component + assign profile)

**Total: 9 assets created, NPC fully functional!**

---

## Key Insight

The behaviour system replaces hardcoded AI with **data-driven configuration**. You didn't write a single line of code, yet your NPC is intelligent and responsive!

Everything is:
- **Configurable** - Change in Inspector
- **Reusable** - Apply same states to different NPCs
- **Extendable** - Add new states without touching code

---

## Next: Read the Full Docs

Once this works, check out:
- **README.md** - Complete feature guide
- **EXAMPLES.md** - 7 configuration templates
- **API_REFERENCE.md** - Full API documentation

---

**Welcome to data-driven NPC development!** 🚀
