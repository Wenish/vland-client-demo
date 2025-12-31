# NPC Behaviour System - Complete File Index

## System Components

### Core Foundation (5 files)
1. **BehaviourState.cs** - Base class for all behaviour states
2. **BehaviourProfile.cs** - Container for states and global transitions
3. **BehaviourCondition.cs** - Base class for transition conditions
4. **BehaviourTransition.cs** - State transition definition
5. **BehaviourContext.cs** - Runtime data container

### Executive Components (4 files)
6. **BehaviourExecutor.cs** - Server-side state machine executor
7. **HealthPhaseProfile.cs** - Boss phase definitions
8. **HealthPhaseManager.cs** - Boss phase manager
9. **BehaviourDebugDisplay.cs** - Runtime debugging visualization

### Concrete States (5 files in States/)
10. **IdleState.cs** - Standing idle with optional animation
11. **ChaseState.cs** - Pursuing targets
12. **AttackState.cs** - Combat with skill selection
13. **PatrolState.cs** - Waypoint or random patrol
14. **FleeState.cs** - Retreat behavior

### Conditions (7 files in Conditions/)
15. **DistanceCondition.cs** - Distance-based transitions
16. **HealthCondition.cs** - Health percentage conditions
17. **TimeInStateCondition.cs** - Time-based conditions
18. **HasTargetCondition.cs** - Target existence checks
19. **RandomChanceCondition.cs** - Probability-based conditions
20. **EnemyCountCondition.cs** - Nearby threat counting
21. **CompositeCondition.cs** - AND/OR composition

### Skill Selectors (6 files in SkillSelectors/)
22. **SkillSelector.cs** - Base class for selection strategies
23. **FirstAvailableSkillSelector.cs** - Simplest selection
24. **DistanceBasedSkillSelector.cs** - Range-based selection
25. **HealthBasedSkillSelector.cs** - Health threshold selection
26. **RandomWeightedSkillSelector.cs** - Weighted random selection
27. **PrioritySkillSelector.cs** - Priority-ordered selection

---

## Documentation Files

### Getting Started
- **README.md** - Complete feature guide and tutorial
- **README_SUMMARY.md** - Executive summary of implementation

### Configuration & Examples
- **EXAMPLES.md** - 7 complete configuration examples
  - Basic Zombie
  - Patrol Guard
  - Ranged Sniper
  - Coward NPC
  - Tank Boss (3 Phases)
  - Mage Boss
  - Swarm Enemy

### Technical Reference
- **API_REFERENCE.md** - Complete API documentation
  - All public methods and properties
  - Usage examples
  - Performance notes

### Integration & Architecture
- **INTEGRATION.md** - System architecture and integration
  - How components work together
  - Data flow diagrams
  - Server-client architecture
  - Existing system compatibility

- **MIGRATION.md** - Upgrading from old AI systems
  - Migration checklist
  - Code comparison examples
  - Testing strategies
  - Benefits of migration

---

## File Organization

```
Assets/Scripts/NPCBehaviour/
│
├── Core Classes
│   ├── BehaviourState.cs
│   ├── BehaviourProfile.cs
│   ├── BehaviourCondition.cs
│   ├── BehaviourTransition.cs
│   └── BehaviourContext.cs
│
├── Components
│   ├── BehaviourExecutor.cs
│   ├── BehaviourDebugDisplay.cs
│   ├── HealthPhaseProfile.cs
│   └── HealthPhaseManager.cs
│
├── States/
│   ├── IdleState.cs
│   ├── ChaseState.cs
│   ├── AttackState.cs
│   ├── PatrolState.cs
│   └── FleeState.cs
│
├── Conditions/
│   ├── DistanceCondition.cs
│   ├── HealthCondition.cs
│   ├── TimeInStateCondition.cs
│   ├── HasTargetCondition.cs
│   ├── RandomChanceCondition.cs
│   ├── EnemyCountCondition.cs
│   └── CompositeCondition.cs
│
├── SkillSelectors/
│   ├── SkillSelector.cs
│   ├── FirstAvailableSkillSelector.cs
│   ├── DistanceBasedSkillSelector.cs
│   ├── HealthBasedSkillSelector.cs
│   ├── RandomWeightedSkillSelector.cs
│   └── PrioritySkillSelector.cs
│
└── Documentation/
    ├── README.md
    ├── README_SUMMARY.md
    ├── EXAMPLES.md
    ├── API_REFERENCE.md
    ├── INTEGRATION.md
    ├── MIGRATION.md
    └── INDEX.md (this file)
```

---

## Quick Reference by Feature

### Want to create a new NPC type?
1. Read **EXAMPLES.md** for templates
2. Follow the 5-step setup in **README.md**
3. Reference **API_REFERENCE.md** for available classes

### Want to understand the architecture?
1. Start with **README_SUMMARY.md** for overview
2. Read **INTEGRATION.md** for architecture details
3. Check **EXAMPLES.md** for data flow examples

### Want to integrate with existing NPCs?
1. Read **MIGRATION.md** for step-by-step process
2. Check **INTEGRATION.md** for compatibility info
3. Review **EXAMPLES.md** for concrete examples

### Want to extend the system?
1. Review **API_REFERENCE.md** for public APIs
2. Check **README.md** "Extending the System" section
3. Use existing concrete classes as templates

### Having trouble?
1. Enable debug mode on BehaviourExecutor
2. Check **README.md** troubleshooting section
3. Review **INTEGRATION.md** debugging guide
4. Verify configuration using **API_REFERENCE.md**

---

## Class Hierarchy

```
ScriptableObject
├─ BehaviourState (abstract)
│  ├─ IdleState
│  ├─ ChaseState
│  ├─ AttackState
│  ├─ PatrolState
│  └─ FleeState
│
├─ BehaviourCondition (abstract)
│  ├─ DistanceCondition
│  ├─ HealthCondition
│  ├─ TimeInStateCondition
│  ├─ HasTargetCondition
│  ├─ RandomChanceCondition
│  ├─ EnemyCountCondition
│  └─ CompositeCondition
│
├─ BehaviourProfile
├─ BehaviourTransition
├─ HealthPhaseProfile
│
└─ SkillSelector (abstract)
   ├─ FirstAvailableSkillSelector
   ├─ DistanceBasedSkillSelector
   ├─ HealthBasedSkillSelector
   ├─ RandomWeightedSkillSelector
   └─ PrioritySkillSelector

NetworkBehaviour
├─ BehaviourExecutor
└─ HealthPhaseManager

MonoBehaviour
└─ BehaviourDebugDisplay

Standalone Class
└─ BehaviourContext (no base class, standalone)
```

---

## Feature Matrix

| Feature | Class | Status |
|---------|-------|--------|
| State machines | BehaviourState, BehaviourProfile | ✓ Complete |
| Transitions | BehaviourTransition | ✓ Complete |
| Conditions | BehaviourCondition + 7 implementations | ✓ Complete |
| Execution | BehaviourExecutor | ✓ Complete |
| Boss phases | HealthPhaseManager, HealthPhaseProfile | ✓ Complete |
| Skill selection | SkillSelector + 5 implementations | ✓ Complete |
| Path finding | ChaseState, PatrolState, FleeState | ✓ Complete |
| Target detection | ChaseState, FleeState | ✓ Complete |
| Debugging | BehaviourDebugDisplay | ✓ Complete |
| Documentation | 6 markdown files | ✓ Complete |

---

## Component Dependencies

```
BehaviourExecutor (requires)
├─ UnitController
├─ UnitMediator
└─ BehaviourProfile
   ├─ BehaviourState (multiple)
   │  └─ BehaviourTransition (multiple)
   │     └─ BehaviourCondition (multiple)
   └─ GlobalTransitions (multiple)
      └─ BehaviourCondition (multiple)

AttackState (uses)
└─ SkillSelector

HealthPhaseManager (requires)
├─ BehaviourExecutor
├─ UnitController
└─ HealthPhaseProfile
   └─ HealthPhase (multiple)
      ├─ BehaviourProfile
      ├─ Skills (names as strings)
      └─ VFX (optional)

BehaviourDebugDisplay (uses)
└─ BehaviourExecutor
   └─ BehaviourContext
```

---

## Setup Checklist

For every new NPC:

- [ ] Read relevant section of EXAMPLES.md
- [ ] Create needed BehaviourState assets
- [ ] Create needed BehaviourCondition assets
- [ ] Create needed BehaviourTransition assets
- [ ] Create BehaviourProfile asset
- [ ] Create SkillSelector asset(s)
- [ ] Add BehaviourExecutor component
- [ ] Assign BehaviourProfile to executor
- [ ] Add skills via SkillSystem
- [ ] Test with debug mode enabled
- [ ] Verify in Scene using Gizmos
- [ ] (Optional) Add HealthPhaseManager for bosses

---

## Performance Optimization Tips

**For many NPCs (50+):**
1. Increase `transitionCheckInterval` in BehaviourExecutor
2. Reduce `targetUpdateInterval` in ChaseState for less frequent updates
3. Increase distance checks in conditions (use larger ranges)
4. Use simpler conditions (avoid CompositeCondition)
5. Disable NPCs far from players

**For high-detail NPCs:**
1. Use FirstAvailableSkillSelector for simplicity
2. Keep state count low (3-4 states)
3. Reduce NavMesh calculation frequency
4. Cache target finding results

---

## Creating Custom Extensions

### Custom State Template
```csharp
[CreateAssetMenu(fileName = "MyState", menuName = "...States/My State")]
public class MyCustomState : BehaviourState
{
    public override void OnEnter(BehaviourContext context) { }
    public override bool OnUpdate(BehaviourContext context, float dt) { return true; }
    public override void OnExit(BehaviourContext context) { }
    public override BehaviourTransition EvaluateTransitions(BehaviourContext ctx) { return null; }
}
```

### Custom Condition Template
```csharp
[CreateAssetMenu(fileName = "MyCondition", menuName = "...Conditions/My Condition")]
public class MyCustomCondition : BehaviourCondition
{
    public override bool Evaluate(BehaviourContext context)
    {
        return true; // Your logic here
    }
}
```

### Custom Selector Template
```csharp
[CreateAssetMenu(fileName = "MySelector", menuName = "...Skill Selectors/My Selector")]
public class MyCustomSkillSelector : SkillSelector
{
    public override NetworkedSkillInstance SelectSkill(
        BehaviourContext context, 
        List<NetworkedSkillInstance> availableSkills)
    {
        return availableSkills.Count > 0 ? availableSkills[0] : null;
    }
}
```

---

## Version Information

- **System Version:** 1.0 (Complete Implementation)
- **Unity Version:** 2022 LTS+ (uses C# 7.3+ features)
- **Mirror Version:** Compatible with current versions
- **Status:** Production Ready

---

## Support Resources

1. **README.md** - Start here for understanding
2. **EXAMPLES.md** - Copy working configurations
3. **API_REFERENCE.md** - Look up specific classes
4. **INTEGRATION.md** - Understand architecture
5. **MIGRATION.md** - Upgrade from old systems
6. **Code Comments** - Detailed inline documentation

---

## Summary

This index covers all 27 C# files and 6 documentation files that make up the complete NPC Behaviour System. Everything is organized, well-documented, and ready for use.

**Total Implementation: 33 files including 27 C# classes**

Choose your documentation file based on your needs, and use this index as a quick reference.

Happy NPC creation!
