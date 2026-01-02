# Threat System - Quick Start Example

## Setup in 5 Minutes

### 1. Add ThreatManager to Your NPC

```
1. Select your NPC GameObject in the scene
2. Add Component > Scripts > NPCBehaviour > ThreatManager
3. Configure in Inspector:
   - Enable Threat: ✓
   - Threat Decay Rate: 2.0
   - Max Threat: 1000
   - Threat Range: 50
   - Damage Threat Multiplier: 1.0
```

### 2. Enable Threat in Chase State

```
1. Find your Chase state ScriptableObject
2. In Inspector:
   - Use Threat Targeting: ✓
```

That's it! Your NPC now uses threat-based targeting.

---

## Example: Boss with Threat Mechanics

### Boss Prefab Setup

```
BossNPC GameObject:
  - UnitController
  - UnitMediator
  - BehaviourExecutor
  - ThreatManager ← ADD THIS
    ├─ Enable Threat: ✓
    ├─ Threat Decay Rate: 1.0 (slower decay)
    ├─ Max Threat: 2000
    ├─ Threat Range: 60
    ├─ Damage Threat Multiplier: 1.0
    └─ Debug Mode: ✓ (for testing)
```

### Create Threat Conditions

**1. HighestThreatCondition** (for targeting)

```
Assets > Create > Game > NPC Behaviour > Conditions > Highest Threat

Name: "HighestThreatTarget"
Settings:
  - Check Current Target: false
  - Update Current Target: ✓
```

**2. ThreatThresholdCondition** (for enrage)

```
Assets > Create > Game > NPC Behaviour > Conditions > Threat Threshold

Name: "TooManyEnemies"
Settings:
  - Mode: Total Target Count
  - Comparison: Greater Than
  - Threshold: 5
```

### Update Your Behaviour Profile

```
Boss Behaviour Profile:
  
  States:
    - IdleState
    - ChaseState (Use Threat Targeting: ✓)
    - AttackState (Update Threat Target: ✓)
    - EnrageState (new!)
  
  Global Transitions:
    1. When "TooManyEnemies" → EnrageState
    2. When "HighestThreatTarget" AND distance < 30 → ChaseState
```

---

## Example: Tank-Healer-DPS Combat

### Tank Ability with High Threat

```csharp
// In your tank's skill script
public class TauntAbility : NetworkedSkillInstance
{
    public override void OnCast(Vector3? aimPoint)
    {
        // Find nearby enemies
        var enemies = Physics.OverlapSphere(transform.position, 15f);
        
        foreach (var enemy in enemies)
        {
            var threatManager = enemy.GetComponent<ThreatManager>();
            if (threatManager != null && threatManager.IsEnabled)
            {
                // Force enemies to attack tank
                threatManager.Taunt(ownerUnit, duration: 5f);
                
                Debug.Log($"Taunted {enemy.name}!");
            }
        }
    }
}
```

### DPS Ability with Normal Threat

```csharp
// In your DPS skill script
public class DamageAbility : NetworkedSkillInstance
{
    public override void OnHit(UnitController target, float damage)
    {
        // Apply damage
        target.TakeDamage(damage);
        
        // Generate threat
        var threatManager = target.GetComponent<ThreatManager>();
        if (threatManager != null && threatManager.IsEnabled)
        {
            // Normal threat (1:1 ratio)
            threatManager.AddThreat(ownerUnit, damage);
        }
    }
}
```

### Healer Ability that Generates Threat

```csharp
// In your healer skill script
public class HealAbility : NetworkedSkillInstance
{
    public override void OnCast(Vector3? aimPoint)
    {
        // Heal ally
        targetAlly.Heal(healAmount);
        
        // Generate threat on nearby enemies
        var enemies = FindObjectsByType<ThreatManager>();
        foreach (var enemyThreat in enemies)
        {
            if (enemyThreat.IsEnabled)
            {
                // Healers generate 50% threat
                enemyThreat.AddThreat(ownerUnit, healAmount * 0.5f);
            }
        }
    }
}
```

---

## Example: Debugging Threat

### Enable Debug Display

Add to your `BehaviourDebugDisplay.cs`:

```csharp
private void UpdateDebugDisplay()
{
    if (context == null) return;
    
    string debugText = $"State: {context.CurrentState?.name ?? "None"}\n";
    debugText += $"Target: {context.CurrentTarget?.name ?? "None"}\n";
    
    // Add threat information
    if (context.HasThreatSystem)
    {
        debugText += "\n=== THREAT TABLE ===\n";
        debugText += $"Targets: {context.GetThreatTargetCount()}\n";
        
        var threats = context.ThreatManager.ThreatTable.GetThreatList();
        foreach (var (unit, threat) in threats.Take(5))
        {
            bool isHighest = unit == context.GetHighestThreatTarget();
            string marker = isHighest ? "►" : " ";
            debugText += $"{marker} {unit.name}: {threat:F0}\n";
        }
    }
    
    debugLabel.text = debugText;
}
```

### Console Logging

```csharp
// In your custom state or condition
if (context.HasThreatSystem)
{
    Debug.Log($"=== Threat Table for {context.Unit.name} ===");
    var threats = context.ThreatManager.ThreatTable.GetThreatList();
    foreach (var (unit, threat) in threats)
    {
        Debug.Log($"  {unit.name}: {threat:F1} threat");
    }
}
```

---

## Example: Multi-Phase Boss with Threat

### Phase Manager Script

```csharp
public class BossPhaseManager : MonoBehaviour
{
    private ThreatManager threatManager;
    private BehaviourExecutor executor;
    private UnitController unit;
    
    [SerializeField] private BehaviourProfile phase1Profile;
    [SerializeField] private BehaviourProfile phase2Profile;
    [SerializeField] private BehaviourProfile phase3Profile;
    
    void Start()
    {
        threatManager = GetComponent<ThreatManager>();
        executor = GetComponent<BehaviourExecutor>();
        unit = GetComponent<UnitController>();
    }
    
    void Update()
    {
        float healthPercent = unit.health / unit.maxHealth;
        
        // Phase transitions
        if (healthPercent < 0.3f && executor.CurrentProfile != phase3Profile)
        {
            StartPhase3();
        }
        else if (healthPercent < 0.6f && executor.CurrentProfile != phase2Profile)
        {
            StartPhase2();
        }
    }
    
    void StartPhase2()
    {
        Debug.Log("Boss entering Phase 2!");
        
        // Switch behaviour
        executor.SetBehaviourProfile(phase2Profile);
        
        // Scale down threat but don't reset
        threatManager.ScaleAllThreat(0.7f);
    }
    
    void StartPhase3()
    {
        Debug.Log("Boss entering Phase 3 - ENRAGE!");
        
        // Switch behaviour
        executor.SetBehaviourProfile(phase3Profile);
        
        // Reset threat completely for dramatic effect
        threatManager.ClearAllThreat();
        
        // Disable threat decay in final phase
        threatManager.ConfigureThreat(
            decayRate: 0f,
            maxThreat: 2000f,
            range: 100f
        );
    }
}
```

---

## Testing Checklist

- [ ] ThreatManager component added to NPC
- [ ] "Enable Threat" is checked
- [ ] Chase state has "Use Threat Targeting" enabled
- [ ] Threat conditions created and assigned to transitions
- [ ] Skills/abilities call AddThreat() or OnDamageDealt()
- [ ] Debug mode enabled to see threat changes
- [ ] Test with multiple players/enemies
- [ ] Verify threat decay works as expected
- [ ] Test threat range (distant targets removed)
- [ ] Verify taunt mechanics if implemented

---

## Common Issues

**NPC not switching to threat target**
- Ensure ThreatManager is enabled
- Check Chase state has "Use Threat Targeting" = true
- Verify threat is being generated (enable debug mode)

**Threat not decaying**
- Check Threat Decay Rate > 0
- Ensure NPC is alive and Update() is running

**NPC targets wrong enemy**
- Check threat values in debug mode
- Verify threat multipliers are appropriate
- Ensure threat isn't being reset unexpectedly

**Performance issues**
- Increase threat update intervals in states
- Reduce threat range to limit tracked targets
- Disable threat for less important NPCs

---

For complete documentation, see:
- [THREAT_SYSTEM.md](THREAT_SYSTEM.md) - Full threat system guide
- [API_REFERENCE.md](API_REFERENCE.md) - Complete API documentation
- [README.md](README.md) - Main system documentation
