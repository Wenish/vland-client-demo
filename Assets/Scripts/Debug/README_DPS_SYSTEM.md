# DPS Debug System for Shadow Infection

## Overview

A lightweight, modern DPS (Damage Per Second) monitoring system designed for Shadow Infection. This debug tool helps developers observe combat performance during gameplay without modifying or interfering with existing game systems.

## Features

- **Non-Invasive Design**: Uses event-based observation pattern - no modifications to existing combat, damage, or unit systems
- **Player Team Focus**: Only tracks damage from units on the player's team
- **Rolling Time Window**: Displays DPS calculated over a 60-second rolling window
- **Smart Filtering**: Automatically hides units that haven't dealt damage in the last 60 seconds
- **Modern UI**: Clean, readable UI Toolkit interface with color-coded DPS values
- **Performance Optimized**: Minimal allocations, cached calculations, automatic cleanup
- **Easy to Remove**: Can be completely deleted without affecting any game systems

## Architecture

### Component Structure

```
Assets/Scripts/Debug/
├── DPSTracker.cs          # Core tracking service (singleton)
├── DPSDebugWindow.cs      # UI display component
├── DPSDebugWindow.uxml    # UI layout (optional)
└── DPSDebugWindow.uss     # UI stylesheet (optional)
```

### How It Works

1. **Event Listening**: `DPSTracker` subscribes to `UnitDamagedEvent` from the existing event system
2. **Damage Recording**: Records damage amount and timestamp for each attacker
3. **Team Filtering**: Captures player team via `MyPlayerUnitSpawnedEvent`, filters damage accordingly
4. **DPS Calculation**: Calculates damage/time over rolling 60-second window
5. **UI Display**: `DPSDebugWindow` reads aggregated data and displays in clean UI

### Design Principles

- **Separation of Concerns**: Tracking logic (DPSTracker) is separate from UI logic (DPSDebugWindow)
- **Event-Driven**: Uses existing EventManager - no direct coupling to game systems
- **Passive Observer**: Only listens to events, never modifies game state
- **Cache & Cleanup**: Caches calculated DPS values, cleans up old data automatically
- **Zero Dependencies**: Other systems don't know this exists

## Setup Instructions

### Quick Setup (Recommended)

1. **Create Tracker GameObject**:
   - In your main scene, create an empty GameObject named `DPSTracker`
   - Add the `DPSTracker` component
   - It will persist across scenes (DontDestroyOnLoad)

2. **Create UI GameObject**:
   - Create a new GameObject named `DPSDebugUI`
   - Add `UIDocument` component
   - Add `DPSDebugWindow` component
   - The UI will build itself programmatically (no UXML/USS needed)

3. **Done!** The system will automatically:
   - Wait for player spawn
   - Start tracking damage from player's team
   - Display active units in the top-right corner

### Alternative Setup (Using UXML/USS)

If you prefer using the provided UXML/USS files:

1. Follow steps 1-2 above
2. In the `UIDocument` component:
   - Set `Source Asset` to `DPSDebugWindow.uxml`
   - The system will use the UXML layout
3. If you want to customize styles, edit `DPSDebugWindow.uss`

## Usage

### In-Game Controls

- **F3**: Toggle window visibility (configurable in Inspector)
- Window appears in top-right corner
- Updates 2 times per second (configurable)

### Inspector Configuration

**DPSTracker Settings:**
- `Time Window`: Duration for DPS calculation (default: 60 seconds)

**DPSDebugWindow Settings:**
- `Show Window`: Enable/disable at start
- `Update Frequency`: How often to refresh display (default: 2/sec)
- `Toggle Key`: Keyboard shortcut (default: F3)
- `Max Units To Display`: Limit displayed units (default: 10)
- `Minimum DPS To Show`: Filter low values (default: 0.1)

### Display Information

The window shows:
- **Header**: "DPS Monitor"
- **Status**: Active unit count and time window
- **List**: Ranked units with:
  - Rank number (#1, #2, etc.)
  - Unit name
  - DPS value (color-coded)

**Color Coding:**
- 🟢 Green: < 20 DPS (Low)
- 🟡 Yellow: 20-50 DPS (Medium)
- 🟠 Orange: 50-100 DPS (High)
- 🔴 Red: > 100 DPS (Very High)

## Technical Details

### Data Structure

```csharp
// Damage records stored per unit
Dictionary<UnitController, List<DamageRecord>>
  where DamageRecord = (float damage, float timestamp)

// Cached DPS values (updated 10x/sec)
Dictionary<UnitController, float> dpsCache
```

### Performance Characteristics

- **Memory**: ~100 bytes per active unit (minimal overhead)
- **Update Cost**: O(n) where n = active units (typically < 20)
- **Cleanup**: Runs every frame, removes expired records
- **Cache Updates**: 10 times/second (independent of UI refresh)
- **UI Updates**: 2 times/second (configurable)
- **Allocations**: Minimal - uses cached structures

### Event Dependencies

The system relies on these existing events:
- `UnitDamagedEvent`: Fires when any unit takes damage
- `MyPlayerUnitSpawnedEvent`: Fires when player's unit spawns

Both events are part of the existing game architecture.

## Testing

### Manual Testing

1. Start a game with your player unit
2. Deal damage to enemies
3. Verify:
   - Your unit appears in the DPS list
   - DPS updates in real-time
   - Old damage drops off after 60 seconds
   - F3 toggles visibility
   - Colors match DPS ranges

### Edge Cases Handled

- Player unit not spawned yet → Shows "Waiting for player spawn..."
- No damage in 60 seconds → Unit removed from list
- Unit dies → Filtered out of display
- Unit is null → Automatically cleaned up
- DPSTracker not found → Displays error message

## Customization

### Changing Time Window

In `DPSTracker` Inspector, adjust `Time Window` (e.g., 30 or 120 seconds).

### Changing UI Position

Edit `DPSDebugWindow.cs`, modify `ApplyStyles()` method:
```csharp
windowContainer.style.top = 20;    // Distance from top
windowContainer.style.right = 20;  // Distance from right
// Or use: left, bottom for other corners
```

### Changing Colors

Edit `GetDPSColor()` method in `DPSDebugWindow.cs`:
```csharp
private Color GetDPSColor(float dps)
{
    if (dps >= 100f)
        return new Color(1f, 0.4f, 0.4f, 1f); // Customize here
    // ...
}
```

### Adding More Metrics

To extend with additional stats:

1. Add fields to `DPSTracker` to track new data
2. Add calculation methods similar to `GetDPS()`
3. Update `DPSDebugWindow.CreateUnitRow()` to display new data

Example: Total damage dealt
```csharp
// In DPSTracker:
public float GetTotalDamage(UnitController unit)
{
    if (!damageHistory.ContainsKey(unit)) return 0f;
    return damageHistory[unit].Sum(r => r.damage);
}

// In DPSDebugWindow.CreateUnitRow():
var totalLabel = new Label($"{tracker.GetTotalDamage(unit):F0}");
row.Add(totalLabel);
```

## Removal

To completely remove the system:

1. Delete the `Assets/Scripts/Debug/` folder
2. Remove DPSTracker and DPSDebugUI GameObjects from your scene
3. Done - no other systems are affected

No code cleanup needed elsewhere since the system uses events and is fully decoupled.

## Troubleshooting

### Window doesn't appear
- Check `DPSDebugWindow` component has `Show Window` enabled
- Verify `UIDocument` component is on the same GameObject
- Check console for error messages

### No units showing
- Ensure you've dealt damage recently (last 60 seconds)
- Check `Minimum DPS To Show` isn't too high
- Verify `DPSTracker` GameObject exists in scene
- Confirm you're on the same team as the player

### DPS values seem wrong
- Check `Time Window` setting in DPSTracker
- Verify damage events are firing (check FloatingDamageText system)
- Test with known damage amounts

### Performance issues
- Reduce `Update Frequency` in DPSDebugWindow
- Lower `Max Units To Display` value
- Increase `Minimum DPS To Show` to filter more units

## Future Enhancements

Possible extensions (not implemented):
- Total damage dealt
- Damage breakdown by skill/ability
- Damage taken tracking
- Healing per second (HPS)
- Threat per second (TPS)
- Historical graphs
- Export to CSV
- Multi-player team comparison

## License & Credits

Created for Shadow Infection by the development team.
Uses Unity UI Toolkit for modern, performant UI rendering.

## Support

For issues or questions about the DPS system:
1. Check this README
2. Review code comments in `DPSTracker.cs` and `DPSDebugWindow.cs`
3. Test with simple scenarios (one unit, known damage values)
4. Verify EventManager and existing events are working

---

**Last Updated**: January 2, 2026  
**Unity Version**: Compatible with Unity 2021.3+  
**Dependencies**: UI Toolkit, EventManager, UnitController
