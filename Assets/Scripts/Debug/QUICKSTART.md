# DPS Debug System - Quick Start Guide

## 5-Minute Setup

### Step 1: Create the Tracker (30 seconds)

1. In Unity Hierarchy, right-click → Create Empty
2. Name it: `DPSTracker`
3. In Inspector, click "Add Component"
4. Search for and add: `DPS Tracker`
5. Done! (It will automatically persist across scenes)

### Step 2: Create the UI (30 seconds)

1. In Unity Hierarchy, right-click → UI Toolkit → UI Document
2. Name it: `DPSDebugUI`
3. In Inspector, click "Add Component"
4. Search for and add: `DPS Debug Window`
5. Done! (The UI builds itself automatically)

### Step 3: Test It! (1 minute)

1. Press Play
2. Start your game and spawn your player unit
3. Deal damage to enemies
4. Watch the DPS window appear in the top-right corner
5. Press **F3** to toggle visibility

## That's It!

The system is now fully functional and will:
- ✅ Track all damage from your team
- ✅ Calculate DPS over 60 seconds
- ✅ Show active units automatically
- ✅ Hide units that haven't dealt damage recently
- ✅ Color-code by DPS level
- ✅ Update in real-time

## Optional Customization

Want to tweak settings? Select the `DPSDebugUI` GameObject and adjust:

- **Update Frequency**: How often display refreshes (default: 2/sec)
- **Toggle Key**: Change from F3 to another key
- **Max Units To Display**: Limit how many units show (default: 10)
- **Minimum DPS To Show**: Filter out low values (default: 0.1)

## Keyboard Shortcuts

- **F3**: Toggle window on/off

## What You'll See

```
┌─────────────────────────────────┐
│ DPS Monitor                     │
│ 3 active units (60s window)     │
├─────────────────────────────────┤
│ #1  PlayerUnit1    [245.3 DPS] │ ← Red (High)
│ #2  PlayerUnit2    [89.7 DPS]  │ ← Orange (Medium)
│ #3  PlayerUnit3    [23.1 DPS]  │ ← Yellow (Low)
└─────────────────────────────────┘
```

## Troubleshooting

**Window doesn't appear?**
- Make sure you spawned your player unit first
- Check that "Show Window" is enabled on `DPSDebugWindow` component

**No units showing?**
- Deal some damage to enemies
- Units appear only after dealing damage in the last 60 seconds

**Want to remove it?**
- Just delete the two GameObjects: `DPSTracker` and `DPSDebugUI`
- Or delete the entire `Assets/Scripts/Debug/` folder
- No other cleanup needed!

## Next Steps

For more details, customization options, and technical information, see:
**README_DPS_SYSTEM.md** in the same folder.

Enjoy tracking your DPS! 🎯
