# NPC Behaviour System - API Reference

Complete API documentation for all classes and components.

---

## Core Classes

### BehaviourState

Base class for NPC behaviour states. Create subclasses as ScriptableObjects.

```csharp
public abstract class BehaviourState : ScriptableObject
{
    public string stateId;
    public string description;
    
    public virtual void OnEnter(BehaviourContext context);
    public virtual bool OnUpdate(BehaviourContext context, float deltaTime);
    public virtual void OnExit(BehaviourContext context);
    public virtual BehaviourTransition EvaluateTransitions(BehaviourContext context);
}
```

**Methods:**
- `OnEnter()` - Called when state becomes active
- `OnUpdate()` - Called every frame, return false to exit state
- `OnExit()` - Called when state is being exited
- `EvaluateTransitions()` - Check state-specific transitions

**Concrete Implementations:**
- `IdleState` - NPC stands idle
- `ChaseState` - Pursues target
- `AttackState` - Uses skills on target
- `PatrolState` - Moves between waypoints
- `FleeState` - Runs away from threats

---

### BehaviourProfile

Container for states and transitions. Defines complete NPC behaviour.

```csharp
[CreateAssetMenu(fileName = "NewBehaviourProfile", menuName = "Game/NPC Behaviour/Behaviour Profile")]
public class BehaviourProfile : ScriptableObject
{
    public string profileName;
    public string profileDescription;
    public BehaviourState initialState;
    public List<BehaviourState> availableStates = new();
    public List<BehaviourTransition> globalTransitions = new();
    
    public BehaviourState GetInitialState();
    public bool Validate();
}
```

**Fields:**
- `initialState` - First state to activate
- `availableStates` - All states in this profile
- `globalTransitions` - Transitions valid from any state

**Methods:**
- `GetInitialState()` - Returns the initial state
- `Validate()` - Check configuration is valid

---

### BehaviourCondition

Base class for transition conditions.

```csharp
public abstract class BehaviourCondition : ScriptableObject
{
    public string description;
    public abstract bool Evaluate(BehaviourContext context);
}
```

**Concrete Implementations:**
- `DistanceCondition` - Distance to target
- `HealthCondition` - Health percentage
- `TimeInStateCondition` - Time in current state
- `HasTargetCondition` - Has valid target
- `RandomChanceCondition` - Random probability
- `EnemyCountCondition` - Number of nearby enemies
- `CompositeCondition` - AND/OR combination of conditions

---

### BehaviourTransition

Defines transition from one state to another.

```csharp
[CreateAssetMenu(fileName = "NewTransition", menuName = "Game/NPC Behaviour/Transition")]
public class BehaviourTransition : ScriptableObject
{
    public BehaviourState targetState;
    public List<BehaviourCondition> conditions = new();
    public int priority = 0;
    
    public bool CanTransition(BehaviourContext context);
}
```

**Fields:**
- `targetState` - State to transition to
- `conditions` - All must be true to trigger
- `priority` - Higher = more important when multiple valid

**Methods:**
- `CanTransition()` - Check if all conditions are met

---

### BehaviourContext

Runtime data container for behaviour execution.

```csharp
public class BehaviourContext
{
    // Core references
    public UnitController Unit { get; }
    public UnitMediator Mediator { get; }
    public Transform Transform { get; }
    
    // Current behaviour
    public BehaviourState CurrentState { get; set; }
    public BehaviourProfile CurrentProfile { get; set; }
    public float TimeInState { get; set; }
    
    // Target tracking
    public UnitController CurrentTarget { get; set; }
    public Vector3 TargetPosition { get; set; }
    public float LastTargetUpdateTime { get; set; }
    
    // Movement
    public NavMeshPath CurrentPath { get; set; }
    public Vector3 CurrentDestination { get; set; }
    public bool IsMoving { get; set; }
    
    // Combat
    public float LastAttackTime { get; set; }
    public float LastSkillUseTime { get; set; }
    public List<NetworkedSkillInstance> AvailableSkills { get; set; }
    
    public BehaviourContext(UnitController unit);
    
    // State data storage
    public void SetStateData<T>(string key, T value);
    public T GetStateData<T>(string key, T defaultValue = default);
    public bool HasStateData(string key);
    public void ClearStateData();
    
    // Convenience properties
    public Vector3 Position { get; }
    public float Health { get; }
    public float MaxHealth { get; }
    public float HealthPercent { get; }
    public bool IsDead { get; }
    public int Team { get; }
    
    // Helper methods
    public float DistanceToTarget();
    public float DistanceToPosition(Vector3 position);
    public void RefreshAvailableSkills();
    public List<NetworkedSkillInstance> GetOffCooldownSkills();
}
```

---

## Components

### BehaviourExecutor

Server-side component that executes NPC behaviour.

```csharp
[RequireComponent(typeof(UnitController))]
[RequireComponent(typeof(UnitMediator))]
public class BehaviourExecutor : NetworkBehaviour
{
    [SerializeField] private BehaviourProfile behaviourProfile;
    [SerializeField] private bool debugMode = false;
    [SerializeField] private float transitionCheckInterval = 0.2f;
    
    // Properties
    public BehaviourProfile CurrentProfile { get; }
    public BehaviourState CurrentState { get; }
    public BehaviourContext Context { get; }
    
    // Initialization
    public void Initialize();
    
    // Profile management
    [Server] public void SetBehaviourProfile(BehaviourProfile newProfile);
    public BehaviourProfile GetBehaviourProfile();
    
    // State transitions
    [Server] public void TransitionToState(BehaviourState newState);
    [Server] public void ForceTransitionCheck();
    
    // Query
    public bool IsInitialized();
}
```

**Usage:**
```csharp
var executor = GetComponent<BehaviourExecutor>();
executor.SetBehaviourProfile(newProfile);
executor.TransitionToState(attackState);
```

---

### HealthPhaseManager

Manages health-based boss phases.

```csharp
[RequireComponent(typeof(BehaviourExecutor))]
[RequireComponent(typeof(UnitController))]
public class HealthPhaseManager : NetworkBehaviour
{
    [SerializeField] private HealthPhaseProfile phaseProfile;
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool spawnTransitionEffects = true;
    
    // Query
    public int GetCurrentPhaseIndex();
    public HealthPhaseProfile.HealthPhase GetCurrentPhase();
    public bool IsPhaseCompleted(int phaseIndex);
    
    // Control
    [Server] public void ForcePhase(int phaseIndex);
}
```

**Usage:**
```csharp
var phaseManager = GetComponent<HealthPhaseManager>();
int currentPhase = phaseManager.GetCurrentPhaseIndex();
phaseManager.ForcePhase(2); // Force phase 2
```

---

### HealthPhaseProfile

Defines health-based phases for bosses.

```csharp
[CreateAssetMenu(fileName = "NewHealthPhaseProfile", menuName = "Game/NPC Behaviour/Health Phase Profile")]
public class HealthPhaseProfile : ScriptableObject
{
    [System.Serializable]
    public class HealthPhase
    {
        public string phaseName;
        public float healthThreshold;  // 0-1
        public BehaviourProfile behaviourProfile;
        public List<string> skillsToAdd = new();
        public List<string> skillsToRemove = new();
        public GameObject phaseTransitionEffect;
        public string notes;
    }
    
    public List<HealthPhase> phases = new();
    public bool allowPhaseRepeat = false;
    
    public HealthPhase GetPhaseForHealth(float healthPercent, HashSet<int> completedPhases = null);
    public int GetPhaseIndex(HealthPhase phase);
    public bool Validate();
}
```

---

### BehaviourDebugDisplay

Debug helper for visualizing behaviour in Inspector.

```csharp
[RequireComponent(typeof(BehaviourExecutor))]
public class BehaviourDebugDisplay : MonoBehaviour
{
    // Inspector display (runtime)
    [SerializeField] private string currentState;
    [SerializeField] private float timeInState;
    [SerializeField] private string currentTarget;
    [SerializeField] private float distanceToTarget;
    [SerializeField] private float healthPercent;
    [SerializeField] private bool isMoving;
    [SerializeField] private int availableSkillCount;
    
    // Settings
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showLabels = true;
}
```

---

## Skill Selectors

Base class for skill selection strategies.

```csharp
public abstract class SkillSelector : ScriptableObject
{
    public string selectorDescription;
    public abstract NetworkedSkillInstance SelectSkill(
        BehaviourContext context, 
        List<NetworkedSkillInstance> availableSkills);
}
```

**Concrete Implementations:**

### FirstAvailableSkillSelector
Selects the first skill that's off cooldown.
```csharp
[CreateAssetMenu(..., menuName = "...Skill Selectors/First Available")]
public class FirstAvailableSkillSelector : SkillSelector { }
```

### DistanceBasedSkillSelector
Selects skills based on distance to target.
```csharp
[System.Serializable]
public class DistanceSkillMapping
{
    public float minDistance;
    public float maxDistance;
    public List<string> skillNames = new();
}

[CreateAssetMenu(..., menuName = "...Skill Selectors/Distance Based")]
public class DistanceBasedSkillSelector : SkillSelector
{
    public List<DistanceSkillMapping> distanceMappings = new();
    public bool allowFallback = true;
}
```

### HealthBasedSkillSelector
Selects skills based on NPC health.
```csharp
[System.Serializable]
public class HealthSkillMapping
{
    public float minHealthPercent;
    public float maxHealthPercent = 1f;
    public List<string> skillNames = new();
    public int priority = 0;
}

[CreateAssetMenu(..., menuName = "...Skill Selectors/Health Based")]
public class HealthBasedSkillSelector : SkillSelector
{
    public List<HealthSkillMapping> healthMappings = new();
}
```

### RandomWeightedSkillSelector
Randomly selects from skills with weighting.
```csharp
[System.Serializable]
public class WeightedSkill
{
    public string skillName;
    public float weight = 1f;
}

[CreateAssetMenu(..., menuName = "...Skill Selectors/Random Weighted")]
public class RandomWeightedSkillSelector : SkillSelector
{
    public List<WeightedSkill> weightedSkills = new();
    public bool respectCooldowns = true;
}
```

### PrioritySkillSelector
Uses skills in priority order.
```csharp
[System.Serializable]
public class PrioritySkill
{
    public string skillName;
    public int priority = 1;  // Lower = higher priority
}

[CreateAssetMenu(..., menuName = "...Skill Selectors/Priority Based")]
public class PrioritySkillSelector : SkillSelector
{
    public List<PrioritySkill> prioritySkills = new();
}
```

---

## Concrete Conditions

### DistanceCondition

```csharp
public enum ComparisonType { LessThan, GreaterThan, Between }

[CreateAssetMenu(..., menuName = "...Conditions/Distance")]
public class DistanceCondition : BehaviourCondition
{
    public ComparisonType comparison = ComparisonType.LessThan;
    public float distance = 5f;
    public float maxDistance = 10f;
    public bool useCurrentTarget = true;
}
```

### HealthCondition

```csharp
[CreateAssetMenu(..., menuName = "...Conditions/Health")]
public class HealthCondition : BehaviourCondition
{
    public ComparisonType comparison = ComparisonType.LessThan;
    [Range(0f, 1f)] public float healthPercent = 0.5f;
    [Range(0f, 1f)] public float maxHealthPercent = 1f;
}
```

### TimeInStateCondition

```csharp
[CreateAssetMenu(..., menuName = "...Conditions/Time In State")]
public class TimeInStateCondition : BehaviourCondition
{
    public float minTimeInState = 5f;
}
```

### HasTargetCondition

```csharp
[CreateAssetMenu(..., menuName = "...Conditions/Has Target")]
public class HasTargetCondition : BehaviourCondition
{
    public bool shouldHaveTarget = true;
    public bool requireAlive = true;
}
```

### RandomChanceCondition

```csharp
[CreateAssetMenu(..., menuName = "...Conditions/Random Chance")]
public class RandomChanceCondition : BehaviourCondition
{
    [Range(0f, 1f)] public float chance = 0.5f;
    public float cooldown = 1f;
}
```

### EnemyCountCondition

```csharp
[CreateAssetMenu(..., menuName = "...Conditions/Enemy Count")]
public class EnemyCountCondition : BehaviourCondition
{
    public ComparisonType comparison = ComparisonType.GreaterThan;
    public int enemyCount = 3;
    public float detectionRange = 15f;
}
```

### CompositeCondition

```csharp
public enum LogicType { And, Or }

[CreateAssetMenu(..., menuName = "...Conditions/Composite")]
public class CompositeCondition : BehaviourCondition
{
    public LogicType logicType = LogicType.And;
    public BehaviourCondition[] conditions;
}
```

---

## Concrete States

### IdleState

```csharp
[CreateAssetMenu(..., menuName = "...States/Idle")]
public class IdleState : BehaviourState
{
    public List<BehaviourTransition> transitions = new();
    public bool lookAround = false;
    public float rotationSpeed = 30f;
}
```

### ChaseState

```csharp
[CreateAssetMenu(..., menuName = "...States/Chase")]
public class ChaseState : BehaviourState
{
    public List<BehaviourTransition> transitions = new();
    public float targetUpdateInterval = 0.5f;
    public float detectionRange = 30f;
    public bool prioritizeClosest = true;
    public float stoppingDistance = 2f;
}
```

### AttackState

```csharp
[CreateAssetMenu(..., menuName = "...States/Attack")]
public class AttackState : BehaviourState
{
    public List<BehaviourTransition> transitions = new();
    public SkillSelector skillSelector;
    public float skillCooldown = 1f;
    public bool faceTarget = true;
}
```

### PatrolState

```csharp
public enum PatrolType { Random, FixedWaypoints }

[CreateAssetMenu(..., menuName = "...States/Patrol")]
public class PatrolState : BehaviourState
{
    public List<BehaviourTransition> transitions = new();
    public PatrolType patrolType = PatrolType.Random;
    public float randomWaypointRange = 10f;
    public float waypointWaitTime = 2f;
    public List<Vector3> waypoints = new();
    public bool loopWaypoints = true;
}
```

### FleeState

```csharp
[CreateAssetMenu(..., menuName = "...States/Flee")]
public class FleeState : BehaviourState
{
    public List<BehaviourTransition> transitions = new();
    public float fleeDistance = 15f;
    public float recalculateInterval = 1f;
    public float threatDetectionRange = 20f;
}
```

---

## Namespace

All classes use the `NPCBehaviour` namespace:

```csharp
using NPCBehaviour;
```

---

## Event Hooks

### State Lifecycle

```csharp
// Override in custom states
public override void OnEnter(BehaviourContext context)
{
    // Called when state becomes active
}

public override void OnExit(BehaviourContext context)
{
    // Called when state is exiting
}

public override bool OnUpdate(BehaviourContext context, float deltaTime)
{
    // Called every frame
    // Return false to trigger exit
    return true;
}
```

### Phase Events

```csharp
// Hook into HealthPhaseManager for custom phase logic
var phaseManager = GetComponent<HealthPhaseManager>();
phaseManager.GetCurrentPhase(); // Get current phase
```

---

## Common Patterns

### Detect if NPC is fighting

```csharp
var executor = GetComponent<BehaviourExecutor>();
bool isFighting = executor.CurrentState is AttackState || executor.CurrentState is ChaseState;
```

### Get current health percent

```csharp
var context = executor.Context;
float healthPercent = context.HealthPercent; // 0-1
```

### Check available skills

```csharp
context.RefreshAvailableSkills();
int skillCount = context.AvailableSkills.Count;
```

### Switch profile at runtime

```csharp
executor.SetBehaviourProfile(newProfile);
```

### Force state transition

```csharp
executor.TransitionToState(fleeState);
```

---

## Threat System API

### ThreatTable

Core data structure for tracking threat values.

```csharp
public class ThreatTable
{
    public ThreatTable(float decayRate, float maxThreat, float threatRange);
    
    // Threat manipulation
    public void AddThreat(UnitController unit, float amount);
    public void SetThreat(UnitController unit, float amount);
    public void RemoveThreat(UnitController unit, float amount);
    public void ClearThreat(UnitController unit);
    public void ClearAll();
    
    // Queries
    public float GetThreat(UnitController unit);
    public UnitController GetHighestThreatTarget(bool requiresLOS, Vector3 origin, LayerMask losLayerMask);
    public List<UnitController> GetUnitsAboveThreat(float threshold);
    public List<UnitController> GetAllTargetsByThreat();
    public List<(UnitController unit, float threat)> GetThreatList();
    public bool HasThreat(UnitController unit);
    public int GetThreatCount();
    
    // Advanced
    public void Update(float deltaTime, Vector3 npcPosition);
    public void ScaleAllThreat(float multiplier);
    public void TransferThreat(UnitController from, UnitController to, float percentage);
    
    // Configuration
    public float DecayRate { get; set; }
    public float MaxThreat { get; set; }
    public float ThreatRange { get; set; }
}
```

**Key Methods:**

- `AddThreat()` - Increase threat for a target
- `GetHighestThreatTarget()` - Get target with most threat
- `ScaleAllThreat()` - Multiply all threat by factor (for AoE reduction)
- `TransferThreat()` - Move threat between targets (tank swaps)
- `Update()` - Apply decay and cleanup (call every frame)

---

### ThreatManager

MonoBehaviour component for managing NPC threat.

```csharp
[RequireComponent(typeof(UnitController))]
public class ThreatManager : NetworkBehaviour
{
    // Configuration
    public bool enableThreat;
    public float threatDecayRate;
    public float maxThreat;
    public float threatRange;
    public float damageThreatMultiplier;
    public bool healingGeneratesThreat;
    public float healingThreatMultiplier;
    
    // Properties
    public ThreatTable ThreatTable { get; }
    public bool IsEnabled { get; }
    public int ThreatTargetCount { get; }
    
    // Threat manipulation
    public void AddThreat(UnitController target, float amount);
    public void RemoveThreat(UnitController target, float amount);
    public void ClearThreat(UnitController target);
    public void ClearAllThreat();
    public float GetThreat(UnitController target);
    public UnitController GetHighestThreatTarget();
    
    // Special mechanics
    public void Taunt(UnitController taunter, float duration);
    public void TransferThreat(UnitController from, UnitController to, float percentage);
    public void ScaleAllThreat(float multiplier);
    
    // Skill integration
    public void OnDamageDealt(UnitController target, float damage, float threatMultiplier);
    public void OnHealingDealt(UnitController healTarget, float healAmount);
    
    // Configuration
    public void SetThreatEnabled(bool enabled);
    public void ConfigureThreat(float decayRate, float maxThreat, float range);
}
```

**Usage Example:**

```csharp
// Add ThreatManager to NPC
ThreatManager threatMgr = npc.AddComponent<ThreatManager>();
threatMgr.enableThreat = true;
threatMgr.threatDecayRate = 2f;
threatMgr.maxThreat = 1000f;

// When dealing damage
threatMgr.OnDamageDealt(target, damageAmount, threatMultiplier: 2.0f);

// Tank taunt
threatMgr.Taunt(tankUnit);

// Check highest threat
UnitController highestThreat = threatMgr.GetHighestThreatTarget();
```

---

### BehaviourContext - Threat Helpers

Added threat convenience methods:

```csharp
public class BehaviourContext
{
    public ThreatManager ThreatManager { get; }
    
    // Threat helpers
    public bool HasThreatSystem { get; }
    public UnitController GetHighestThreatTarget();
    public float GetThreat(UnitController target);
    public int GetThreatTargetCount();
}
```

**Usage in States:**

```csharp
public override bool OnUpdate(BehaviourContext context, float deltaTime)
{
    if (context.HasThreatSystem)
    {
        var target = context.GetHighestThreatTarget();
        if (target != null)
        {
            context.CurrentTarget = target;
        }
    }
    return true;
}
```

---

### Threat Conditions

#### HighestThreatCondition

Checks if highest threat target exists or matches current target.

```csharp
[CreateAssetMenu(menuName = "Game/NPC Behaviour/Conditions/Highest Threat")]
public class HighestThreatCondition : BehaviourCondition
{
    public bool checkCurrentTarget;
    public bool updateCurrentTarget;
    
    public override bool Evaluate(BehaviourContext context);
}
```

**Configuration:**
- `checkCurrentTarget = true`: Validates current target is highest threat
- `checkCurrentTarget = false`: Just checks if any target has threat
- `updateCurrentTarget = true`: Sets context.CurrentTarget to highest threat

---

#### ThreatThresholdCondition

Checks threat values against thresholds.

```csharp
public enum ThresholdMode
{
    TargetAboveThreshold,    // Specific target's threat
    AnyAboveThreshold,       // Any target's threat
    TotalTargetCount,        // Number of targets
    HighestThreatValue       // Highest threat value
}

[CreateAssetMenu(menuName = "Game/NPC Behaviour/Conditions/Threat Threshold")]
public class ThreatThresholdCondition : BehaviourCondition
{
    public ThresholdMode mode;
    public ComparisonType comparison;
    public float threshold;
    public bool useCurrentTarget;
    
    public override bool Evaluate(BehaviourContext context);
}
```

**Example Usage:**

```csharp
// Check if more than 5 enemies have threat
mode = ThresholdMode.TotalTargetCount;
comparison = ComparisonType.GreaterThan;
threshold = 5f;

// Check if current target has > 100 threat
mode = ThresholdMode.TargetAboveThreshold;
comparison = ComparisonType.GreaterThan;
threshold = 100f;
useCurrentTarget = true;
```

---

### Updated States with Threat Support

#### ChaseState

```csharp
public class ChaseState : BehaviourState
{
    public bool useThreatTargeting = true;
    // ... other properties
}
```

When `useThreatTargeting = true`, automatically targets highest threat enemy.

#### AttackState

```csharp
public class AttackState : BehaviourState
{
    public bool updateThreatTarget = false;
    public float threatUpdateInterval = 1f;
    // ... other properties
}
```

When `updateThreatTarget = true`, periodically switches to highest threat target.

---

## Performance Notes

- **Transition Check:** Expensive operation, default interval is 0.2s
- **Path Calculation:** Done each frame in chase/flee states
- **Target Finding:** Done periodically in chase state (default 0.5s)
- **NavMesh Sampling:** Used in patrol/flee states
- **Threat Updates:** ThreatTable.Update() called every frame per NPC
- **Threat Decay:** Linear decay per frame, negligible cost
- **Threat Queries:** Dictionary lookups, O(1) access

For 50+ NPCs, consider:
- Increasing transition check intervals
- Reducing detection/threat ranges
- Disabling threat decay for static threats
- Using longer threat update intervals

---

This API reference covers all public methods and properties. For implementation details, consult the source code documentation within each class.

For complete threat system documentation, see [THREAT_SYSTEM.md](THREAT_SYSTEM.md).
