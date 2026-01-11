# Interrupt System Documentation

## Overview
The interrupt system allows units to cancel ongoing actions (attacks, skill casts, etc.) at any time during the action. This enables better gameplay mechanics like crowd control, dodging, and ability cancellation.

## How It Works

### Components Modified
1. **UnitController.cs** - Added `InterruptAction()` method and `OnActionInterrupted` event
2. **WeaponController.cs** - Added cancellation token support for attack tasks

### Key Features
- **Server-authoritative**: Only the server can interrupt actions
- **Networked**: Interrupts are automatically synced to all clients via RPC
- **Event-driven**: Subscriptable `OnActionInterrupted` event for custom handling
- **Clean state management**: Automatically clears action state and removes modifiers

## Usage

### Basic Interrupt
```csharp
// On the server, interrupt a unit's current action
UnitController targetUnit = /* ... */;
targetUnit.InterruptAction();
```

### Listen to Interrupts
```csharp
// Subscribe to interrupt events
unitController.OnActionInterrupted += HandleUnitInterrupted;

private void HandleUnitInterrupted(UnitController unit)
{
    Debug.Log($"Unit {unit.name} was interrupted!");
    // Trigger custom effects, animations, etc.
}
```

### Example: Crowd Control Ability
```csharp
[Server]
public void StunUnit(UnitController targetUnit, float stunDuration)
{
    // Interrupt whatever they're doing
    targetUnit.InterruptAction();
    
    // Apply stun effect
    targetUnit.ApplyStun(stunDuration);
}
```

## What Gets Interrupted

### Attacks
- In-progress weapon attacks are cancelled
- Movement speed modifiers applied during the attack are removed
- The attack swing never executes

### Skills (Ready for Integration)
- The `OnActionInterrupted` event can be used by skill systems
- Subscribe to this event in your skill/ability controller to cancel casts/channels

### Action State
- Current action state is reset to `Idle`
- Works with any action type: `Attacking`, `Casting`, `Channeling`

## Implementation Details

### UnitController Changes
- **New Event**: `OnActionInterrupted` - Fired when an action is interrupted
- **New Method**: `InterruptAction()` - Server-only method to interrupt current action
- **RPC Method**: `RpcOnActionInterrupted()` - Syncs interrupt to all clients

### WeaponController Changes
- **CancellationToken**: Each attack now uses a `CancellationTokenSource`
- **New Method**: `CancelAttack()` - Cancels the current attack task
- **Exception Handling**: `TaskCanceledException` is caught and handled gracefully

## Example Scenarios

### 1. Knockback Effect
```csharp
[Server]
public void KnockbackUnit(UnitController unit, Vector3 direction, float force)
{
    unit.InterruptAction(); // Stop attacking
    unit.ApplyKnockback(direction, force);
}
```

### 2. Silence Effect (Blocks Skills)
```csharp
[Server]
public void SilenceUnit(UnitController unit, float duration)
{
    unit.InterruptAction(); // Cancel current skill if casting
    // Apply silence status effect preventing new casts
}
```

### 3. Movement Interrupt (from Dodge/Dash)
```csharp
[Server]
public void HandleDodge(UnitController unit)
{
    unit.InterruptAction(); // Stop attacking to perform dodge
    unit.StartDash(dodgeDirection, dodgeSpeed, dodgeDistance);
}
```

## Future Enhancements
- Add animation callbacks for interrupt animations
- Implement partial cast system (interrupt at different stages)
- Add interrupt resistance/tenacity stats
- Create interrupt combo system (multiple interrupts with cooldown management)
