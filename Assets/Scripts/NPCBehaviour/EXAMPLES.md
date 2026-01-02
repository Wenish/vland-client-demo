# Example Behaviour Configurations

This document provides ready-to-use configuration examples for common NPC archetypes.

---

## 1. Basic Zombie (Melee Aggressor)

### Overview
Simple aggressive NPC that chases and attacks the nearest player.

### States Required
1. **Idle State**
   - Look Around: false
   
2. **Chase State**
   - Target Update Interval: 0.5s
   - Detection Range: 30
   - Stopping Distance: 2
   - Prioritize Closest: true

3. **Attack State**
   - Skill Selector: FirstAvailableSkillSelector
   - Skill Cooldown: 1.0s
   - Face Target: true

### Conditions Required
1. **"Enemy Detected" (DistanceCondition)**
   - Comparison: LessThan
   - Distance: 30
   - Use Current Target: false

2. **"In Attack Range" (DistanceCondition)**
   - Comparison: LessThan
   - Distance: 3
   - Use Current Target: true

3. **"Out Of Attack Range" (DistanceCondition)**
   - Comparison: GreaterThan
   - Distance: 4
   - Use Current Target: true

### Transitions Required
1. **Idle → Chase**
   - Conditions: ["Enemy Detected"]
   - Priority: 0

2. **Chase → Attack**
   - Conditions: ["In Attack Range"]
   - Priority: 0

3. **Attack → Chase**
   - Conditions: ["Out Of Attack Range"]
   - Priority: 0

### Behaviour Profile
- Name: "ZombieBehaviour"
- Initial State: Idle
- Available States: [Idle, Chase, Attack]
- Global Transitions: None

### Skills Required
- Add a basic melee attack skill to the NPC's SkillSystem

---

## 2. Patrol Guard (Defensive NPC)

### Overview
NPC that patrols waypoints and only engages enemies that come close.

### States Required
1. **Patrol State**
   - Patrol Type: FixedWaypoints
   - Waypoints: [Define 3-5 points in your level]
   - Loop Waypoints: true
   - Waypoint Wait Time: 3s

2. **Chase State**
   - Target Update Interval: 0.5s
   - Detection Range: 15 (shorter than zombie)
   - Stopping Distance: 5
   - Prioritize Closest: true

3. **Attack State**
   - Skill Selector: PrioritySkillSelector
   - Skill Cooldown: 1.5s
   - Face Target: true

### Conditions Required
1. **"Enemy Close" (DistanceCondition)**
   - Comparison: LessThan
   - Distance: 15
   - Use Current Target: false

2. **"In Attack Range" (DistanceCondition)**
   - Comparison: LessThan
   - Distance: 7
   - Use Current Target: true

3. **"Enemy Escaped" (CompositeCondition)**
   - Logic: OR
   - Sub-conditions:
     - HasTargetCondition (should have target: false)
     - DistanceCondition (> 25 units)

### Transitions Required
1. **Patrol → Chase**
   - Conditions: ["Enemy Close"]
   - Priority: 5

2. **Chase → Attack**
   - Conditions: ["In Attack Range"]
   - Priority: 0

3. **Chase → Patrol** (Global Transition)
   - Conditions: ["Enemy Escaped"]
   - Priority: 10

4. **Attack → Patrol** (Global Transition)
   - Conditions: ["Enemy Escaped"]
   - Priority: 10

### Behaviour Profile
- Name: "GuardBehaviour"
- Initial State: Patrol
- Available States: [Patrol, Chase, Attack]
- Global Transitions: [Chase→Patrol, Attack→Patrol]

---

## 3. Ranged Sniper (Kiting NPC)

### Overview
Keeps distance from targets and uses ranged attacks.

### States Required
1. **Idle State**
   - Look Around: true
   - Rotation Speed: 30

2. **Chase State**
   - Detection Range: 40
   - Stopping Distance: 15 (maintain distance)
   - Prioritize Closest: true

3. **Attack State**
   - Skill Selector: DistanceBasedSkillSelector
   - Skill Cooldown: 2.0s
   - Face Target: true

4. **Flee State**
   - Flee Distance: 20
   - Recalculate Interval: 0.8s
   - Threat Detection Range: 15

### Conditions Required
1. **"Enemy Visible" (DistanceCondition)**
   - Comparison: LessThan
   - Distance: 40
   - Use Current Target: false

2. **"Optimal Range" (DistanceCondition)**
   - Comparison: Between
   - Min Distance: 15
   - Max Distance: 30
   - Use Current Target: true

3. **"Too Close" (DistanceCondition)**
   - Comparison: LessThan
   - Distance: 10
   - Use Current Target: true

4. **"Safe Distance" (DistanceCondition)**
   - Comparison: GreaterThan
   - Distance: 15
   - Use Current Target: true

### Transitions Required
1. **Idle → Chase**
   - Conditions: ["Enemy Visible"]
   - Priority: 0

2. **Chase → Attack**
   - Conditions: ["Optimal Range"]
   - Priority: 0

3. **Attack → Flee** (Global)
   - Conditions: ["Too Close"]
   - Priority: 10

4. **Flee → Chase**
   - Conditions: ["Safe Distance"]
   - Priority: 0

### Skill Selector Configuration
**DistanceBasedSkillSelector**:
- 15-25 range: ["LongRangeBolt", "SniperShot"]
- 25-40 range: ["UltraLongRangeBeam"]
- Allow Fallback: true

---

## 4. Coward NPC (Flee on Low Health)

### Overview
Fights normally but flees when health is low.

### States Required
1. **Idle State**
2. **Chase State**
3. **Attack State**
4. **Flee State**

### Conditions Required
1. **"Low Health" (HealthCondition)**
   - Comparison: LessThan
   - Health Percent: 0.3 (30%)

2. **"Healthy" (HealthCondition)**
   - Comparison: GreaterThan
   - Health Percent: 0.5 (50%)

3. Standard distance conditions...

### Key Global Transition
**Any State → Flee**
- Conditions: ["Low Health"]
- Priority: 100 (highest priority!)

**Flee → Chase**
- Conditions: ["Healthy", "Enemy Visible"]
- Priority: 0

---

## 5. Tank Boss (3-Phase Fight)

### Overview
Boss that gets more aggressive and gains abilities as health decreases.

### Phase 1: Defensive Phase (100%-70%)

#### States
1. **Idle State** (brief between attacks)
2. **Chase State** (slow)
3. **Attack State**
   - Skill Selector: PrioritySkillSelector
   - Skills: ["ShieldBash", "GroundSlam"]

### Phase 2: Aggressive Phase (70%-40%)

#### States
1. **Chase State** (faster)
2. **Attack State**
   - Skill Selector: RandomWeightedSkillSelector
   - Skills: ["ShieldBash": 2.0, "GroundSlam": 3.0, "ChargeAttack": 1.0]

### Phase 3: Berserk Phase (40%-0%)

#### States
1. **Chase State** (very fast)
2. **Attack State**
   - Skill Selector: HealthBasedSkillSelector
   - Skills: All ultimate abilities

### Health Phase Profile Configuration

**Phase 1** (Defensive):
- Health Threshold: 0.7 (70%)
- Behaviour Profile: "BossPhase1_Defensive"
- Skills to Add: ["ShieldBash", "GroundSlam"]
- Skills to Remove: []
- Notes: "Slow, tanky, basic attacks"

**Phase 2** (Aggressive):
- Health Threshold: 0.4 (40%)
- Behaviour Profile: "BossPhase2_Aggressive"
- Skills to Add: ["ChargeAttack", "WhirlwindSpin"]
- Skills to Remove: []
- Phase Transition Effect: [Rage Aura VFX]
- Notes: "Faster movement, adds charge ability"

**Phase 3** (Berserk):
- Health Threshold: 0.0 (0%)
- Behaviour Profile: "BossPhase3_Berserk"
- Skills to Add: ["DestructiveUltimate", "ExecuteSlam"]
- Skills to Remove: ["ShieldBash"]
- Phase Transition Effect: [Fire Aura VFX]
- Notes: "Maximum aggression, all ultimate abilities"

---

## 6. Mage Boss (Skill-Focused)

### Overview
Boss that uses different skills based on distance and health.

### Single Profile with Smart Selectors

#### States
1. **Idle State** (teleport cooldown)
2. **Attack State**
   - Skill Selector: CompositeSelector (custom, or use multiple states)

#### Distance-Based Skill Selector
**DistanceBasedSkillSelector**:
- 0-5 range: ["TeleportAway", "FrostNova"]
- 5-15 range: ["Fireball", "ChainLightning"]
- 15-30 range: ["MeteorStrike", "ArcaneBarrage"]

#### Health-Based Skill Selector (for ultimate phases)
**HealthBasedSkillSelector**:
- 100%-50%: ["Fireball", "IceShard"]
- 50%-25%: ["Fireball", "ChainLightning", "TeleportStrike"]
- 25%-0%: ["MeteorStorm", "TimeFreeze", "DeathRay"]

### Transitions
- Mostly stays in Attack state
- Uses **Random Chance** conditions to occasionally Idle (for variety)

---

## 7. Swarm Enemy (Group Behavior)

### Overview
Weak individually, but becomes aggressive in groups.

### States Required
1. **Flee State** (when alone)
2. **Idle State** (waiting for allies)
3. **Chase State** (when in group)
4. **Attack State**

### Conditions Required
1. **"Allies Nearby" (EnemyCountCondition)** (Note: customize to count allies, not enemies)
   - Comparison: GreaterThan
   - Count: 2
   - Detection Range: 10

2. **"Alone" (EnemyCountCondition)**
   - Comparison: LessThan
   - Count: 2

### Transitions
**Flee/Idle → Chase**
- Conditions: ["Allies Nearby", "Enemy Detected"]
- Priority: 5

**Chase/Attack → Flee**
- Conditions: ["Alone"]
- Priority: 10

---

## Quick Start Checklist

For any new NPC:

1. ☐ Create 2-4 behaviour states
2. ☐ Create 3-5 conditions
3. ☐ Create 2-4 transitions
4. ☐ Create 1 skill selector
5. ☐ Create 1 behaviour profile
6. ☐ Attach BehaviourExecutor component
7. ☐ Add skills to NPC's SkillSystem
8. ☐ Test with debug mode enabled
9. ☐ Adjust parameters based on gameplay
10. ☐ (Optional) Add HealthPhaseManager for bosses

---

## Tips for Designing Good NPC Behavior

1. **Start Simple**: Begin with Idle→Chase→Attack, then add complexity
2. **Use Priority**: Set high priority for critical transitions (flee, enrage)
3. **Add Variety**: Use RandomChance conditions for unpredictable behavior
4. **Balance Responsiveness**: Lower transition check intervals for bosses, higher for minions
5. **Test Edge Cases**: What happens when target dies? When out of range?
6. **Use Global Transitions**: For "emergency" behaviors like flee or enrage
7. **Layer Selectors**: Combine distance, health, and random selectors for depth

---

This configuration guide provides complete templates for common NPC archetypes. Mix and match elements to create unique behaviors!
