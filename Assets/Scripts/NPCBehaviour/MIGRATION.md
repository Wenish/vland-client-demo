# Migration Guide: From Old AI to Behaviour System

This guide helps you migrate existing NPC AI code to the new behaviour system.

---

## Overview of Changes

### Before (Old System)
- AI logic hardcoded in MonoBehaviour components
- State stored in component fields
- Difficult to reuse or modify without code changes
- Limited to single behaviour pattern per NPC type

### After (New System)
- AI logic defined in ScriptableObject assets
- State stored in separate BehaviourContext
- Easy to create variations by configuring assets
- Can swap behaviour profiles at runtime

---

## Migrating AiZombieController

Your existing `AiZombieController` demonstrates the old approach. Here's how to migrate it:

### Old Code Structure

```csharp
public class AiZombieController : MonoBehaviour
{
    public UnitController _unitController;
    public Vector3 Destination;
    private NavMeshPath _path;
    private UnitController _targetPlayer;

    void Update()
    {
        GetAllPlayerControllers();
        CalcNearestPlayer();
        CalcPathToDestination();
        SetMoveTarget();
        CalculateAngle();
        CalculateMoveInput();
        CalcShouldAttack();
    }
}
```

### New Approach

#### Step 1: Remove AiZombieController Component

You can safely remove or disable the `AiZombieController` component from zombie prefabs.

#### Step 2: Add BehaviourExecutor

1. Select zombie prefab
2. Add Component → Behaviour Executor
3. Ensure UnitController and UnitMediator are present

#### Step 3: Create Zombie Behaviour Assets

**Create States:**

1. **Chase State** (replaces CalcNearestPlayer + movement logic)
   - Detection Range: 30
   - Stopping Distance: 1.1 (matches your old distance check)
   - Prioritize Closest: true
   - Target Update Interval: 0.5s

2. **Attack State** (replaces CalcShouldAttack)
   - Skill Selector: FirstAvailableSkillSelector
   - Skill Cooldown: 1.0s

**Create Conditions:**

1. **In Attack Range** (DistanceCondition)
   - Comparison: LessThan
   - Distance: Get value from `_unitController.currentWeapon.attackRange`
   - Use Current Target: true

2. **Out of Attack Range** (DistanceCondition)
   - Comparison: GreaterThan
   - Distance: Same as above
   - Use Current Target: true

**Create Transitions:**

1. Chase → Attack: When "In Attack Range"
2. Attack → Chase: When "Out of Attack Range"

**Create Profile:**

- Name: "ZombieBehaviour"
- Initial State: Chase (zombies immediately chase)
- Available States: [Chase, Attack]

#### Step 4: Assign Profile to Prefab

Assign the "ZombieBehaviour" profile to the BehaviourExecutor component.

#### Step 5: Test

The zombie should now behave identically to the old system, but configured via data!

---

## Migration Checklist

For each old AI script:

### 1. Identify Distinct Behaviors

**Old code example:**
```csharp
if (state == "patrol") { DoPatrol(); }
else if (state == "chase") { DoChase(); }
else if (state == "attack") { DoAttack(); }
```

**New approach:** Create one BehaviourState asset per behavior.

### 2. Extract Transition Logic

**Old code example:**
```csharp
if (distanceToPlayer < attackRange) {
    state = "attack";
}
```

**New approach:** Create DistanceCondition asset + Transition asset.

### 3. Move State Data to Context

**Old code example:**
```csharp
public float timeSinceLastAttack;
public Vector3 lastKnownPlayerPosition;
```

**New approach:** Use BehaviourContext.SetStateData() / GetStateData()

### 4. Convert Parameters to ScriptableObject Fields

**Old code example:**
```csharp
[SerializeField] float chaseSpeed = 5f;
[SerializeField] float detectionRange = 20f;
```

**New approach:** These become fields in the state ScriptableObject assets.

### 5. Skill Execution

**Old code example:**
```csharp
if (Time.time > lastAttackTime + attackCooldown) {
    Attack();
}
```

**New approach:** Handled automatically by AttackState + SkillSelector.

---

## Common Migration Patterns

### Pattern 1: Target Selection

**Old:**
```csharp
void FindTarget() {
    var players = FindObjectsOfType<Player>();
    target = GetClosest(players);
}
```

**New:**
- ChaseState handles this automatically
- Configure detection range and priority in the state asset

### Pattern 2: Movement

**Old:**
```csharp
void MoveToTarget() {
    NavMesh.CalculatePath(pos, targetPos, path);
    // Calculate input from path...
}
```

**New:**
- ChaseState and PatrolState handle this automatically
- NavMesh integration is built-in

### Pattern 3: Attack Logic

**Old:**
```csharp
void Attack() {
    if (CanAttack()) {
        currentWeapon.Use();
    }
}
```

**New:**
- AttackState + SkillSelector handle this
- Uses existing SkillSystem infrastructure

### Pattern 4: State Persistence

**Old:**
```csharp
public float stateTimer;
public string previousState;
```

**New:**
```csharp
// In state's OnUpdate:
context.SetStateData("timer", timer);
context.TimeInState // built-in
```

### Pattern 5: Health-Based Behavior

**Old:**
```csharp
void Update() {
    if (health < maxHealth * 0.3f) {
        FleeMode();
    }
}
```

**New:**
- Create HealthCondition (< 30%)
- Add global transition to Flee state
- Or use HealthPhaseManager for bosses

---

## Side-by-Side Comparison

### Example: Patrol Guard AI

#### Old System (100+ lines of code)

```csharp
public class PatrolGuardAI : MonoBehaviour
{
    [SerializeField] Transform[] waypoints;
    [SerializeField] float detectionRange = 15f;
    [SerializeField] float attackRange = 3f;
    [SerializeField] float chaseSpeed = 4f;
    [SerializeField] float patrolSpeed = 2f;
    
    private int waypointIndex;
    private string state = "patrol";
    private Transform target;
    private NavMeshAgent agent;
    
    void Start() {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = patrolSpeed;
    }
    
    void Update() {
        switch (state) {
            case "patrol":
                Patrol();
                CheckForEnemies();
                break;
            case "chase":
                Chase();
                CheckAttackRange();
                CheckIfLostTarget();
                break;
            case "attack":
                Attack();
                CheckIfTargetEscaped();
                break;
        }
    }
    
    void Patrol() {
        if (Vector3.Distance(transform.position, waypoints[waypointIndex].position) < 1f) {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
        }
        agent.SetDestination(waypoints[waypointIndex].position);
    }
    
    void CheckForEnemies() {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        foreach (var hit in hits) {
            if (hit.CompareTag("Enemy")) {
                target = hit.transform;
                state = "chase";
                agent.speed = chaseSpeed;
                break;
            }
        }
    }
    
    // ... 70+ more lines of state logic ...
}
```

#### New System (0 lines of code + configuration)

**Configuration in Unity Inspector:**

- **Behaviour Profile:** "GuardBehaviour"
  - States: Patrol, Chase, Attack
  - Initial: Patrol

- **Patrol State:**
  - Waypoints: [Waypoint1, Waypoint2, Waypoint3]
  - Loop: true

- **Transitions:**
  - Patrol → Chase: DistanceCondition (< 15)
  - Chase → Attack: DistanceCondition (< 3)
  - Attack → Patrol: DistanceCondition (> 20) OR no target

**That's it!** All the logic is reusable across different NPCs.

---

## Benefits of Migration

### Before Migration
- ✗ Need to write code for every NPC type
- ✗ Hard to tweak parameters (recompile required)
- ✗ Difficult to reuse logic across NPCs
- ✗ State bugs due to manual state management
- ✗ No way to change behavior at runtime easily

### After Migration
- ✓ Configure NPCs via Unity Inspector
- ✓ Instant parameter changes (no recompile)
- ✓ Reuse states and conditions across all NPCs
- ✓ Robust state transitions (built-in)
- ✓ Runtime profile swapping for boss phases

---

## Migration Timeline Estimate

For a typical project:

- **Simple NPCs** (like zombies): 30 minutes per type
- **Medium NPCs** (patrol guards): 1 hour per type
- **Complex NPCs** (bosses): 2-3 hours per type
- **First-time setup** (learning curve): +2 hours

**Total for 5 NPC types:** ~1 day of work

---

## Maintaining Old and New Systems

You can run both systems in parallel during migration:

1. Keep old AI scripts on existing NPCs
2. Create new behaviour profiles for new NPCs
3. Gradually migrate old NPCs one at a time
4. Test each migration thoroughly
5. Once all NPCs are migrated, remove old scripts

**Important:** Don't attach both old AI script AND BehaviourExecutor to the same NPC!

---

## Testing Migration

### Before Removing Old Code

1. Place old NPC and new NPC in test scene
2. Compare behavior side-by-side
3. Verify:
   - Movement patterns match
   - Attack timing matches
   - State transitions match
   - Performance is acceptable

### Automated Testing

You can write unit tests for conditions:

```csharp
[Test]
public void TestDistanceCondition() {
    var condition = ScriptableObject.CreateInstance<DistanceCondition>();
    condition.distance = 10f;
    condition.comparison = ComparisonType.LessThan;
    
    var context = CreateMockContext(distanceToTarget: 5f);
    Assert.IsTrue(condition.Evaluate(context));
}
```

---

## Getting Help

If you encounter issues during migration:

1. Enable Debug Mode on BehaviourExecutor
2. Check Console for state transition logs
3. Use BehaviourDebugDisplay component
4. Review the README.md examples
5. Check that all conditions are properly configured

---

## Next Steps After Migration

Once migration is complete, you can:

1. **Create Behavior Variations**: Duplicate profiles and tweak for variety
2. **Add Boss Phases**: Use HealthPhaseManager for multi-phase fights
3. **Create Skill Combos**: Use sophisticated skill selectors
4. **Build Behavior Library**: Reusable states and conditions for rapid development
5. **Runtime Events**: Hook into OnEnter/OnExit for custom game events

The new system is significantly more flexible and maintainable than hardcoded AI!
