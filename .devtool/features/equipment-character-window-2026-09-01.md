---
id: "equipment-character-window-2026-09-01"
status: "backlog"
priority: "medium"
assignee: null
epic: null
dueDate: null
created: "2026-09-01T16:30:00.000Z"
modified: "2026-09-01T18:40:00.000Z"
completedAt: null
labels: []
order: "Zv"
---
# Equipment & character window

Units can **wear** armor and weapons from the item bag. Player manages loadout on a WoW-style **character panel**: paper-doll slots around a center **3D model placeholder**. Equip / unequip / swap only — no combat hooks yet.

This is a **new system** sitting on top of the finished item catalog + bag ticket. Do **not** migrate or remove the old weapon path (`PlayerLoadout`, `UnitController.EquipWeapon`, loadout window weapons) in this ticket.

## Feel

Open character **and** bag together — two floating panels side by side, like WoW Classic character + all bags. **`U`** toggles character (paper doll); **`B`** toggles inventory (`C` is Skill 1 in this project). Drag either window by the title bar. **Right-click** or **double-click** a bag item to quick-equip; **right-click** an equipped slot to unequip — same muscle memory as Classic. Left-click a slot to select it and see details. No full-screen dim. Stats and 3D meshes on the unit are still out of scope.

## This ticket

Equipped slots + persistence + character UI. The unit **remembers** what is worn; the player can **change** it in the UI. Nothing on the unit model or in combat reads equipped gear yet.

**In**

- **Equipped state** on `CharacterSaveData`: one `instanceId` per paper-doll slot (or empty). Replace the unused `ArmorSlotIds` stub with a typed list, e.g. `List<EquippedSlotEntry> { ItemSlot slot, string instanceId }`. Persist with the existing roster save (`CharacterManager` PlayerPrefs).
- **Equipment service** (`ICharacterEquipment` or similar): read equipped slots for the active character; `TryEquip(instanceId, slot)` and `TryUnequip(slot)`. Registered on `GameLifetimeScope` like `IItemInventory`.
- **Equip rules** (validation only — no stat application):
  - Item must exist in the character bag (`InventoryEquipment`).
  - Item `kind` must be `Equipment` and `slot` must match the target paper-doll slot.
  - **Armor weight**: use `ItemRules.CanEquipWithWeapon(piece, mainHandWeaponType)` — cape slot is exempt (`ItemRules.EnforcesArmorWeight`). Wrong weight stays in the bag; slot shows disabled / tooltip, no server toast.
  - **Swap**: equipping into an occupied slot unequips the incumbent back to the bag first, then moves the new instance from bag → slot. Unequip moves `instanceId` back to `InventoryEquipment`.
  - Destroying a bag row that is currently equipped is **blocked**; detail shows “Equipped — unequip first” (WoW Classic behavior).
- **MessagePipe** `EquipmentChangedEvent` so UI refreshes when roster / active character / equip changes.
- **Character window UI** (new, sibling to inventory + loadout):
  - WoW-style layout: **slots around the edges**, **center = model placeholder** (empty panel, label like "Character preview" — no RenderTexture or unit spawn this ticket).
  - Slots: Head, Shoulder, Cape, Chest, Pants, Feet, Gloves, Main hand, Off hand (off-hand visible but may stay empty in content).
  - Each slot shows empty chrome or the item icon + rarity border (reuse inventory icon resolve path).
  - **No bag list on the character panel** — the inventory window is the bag. Equip via right-click / double-click on bag rows, or left-click slot then left-click row / **Equip** on detail (see [UX decisions](#ux-decisions-wow-classic)). Right-click occupied slot to unequip.
  - Opens in **lobby and in-match**. Default bind: **`U`** (`PlayerActionId.Character`, settings label **Character**). **`C` is not used** — it is Skill 1. **Can be open at the same time as inventory** — independent toggles (`U` / `B`), like WoW Classic.
  - Search / destroy stay on the **inventory** window only.
- **Inventory window update** (part of this ticket):
  - Refactor from centered full-screen modal (`inventory-overlay` dim + click-outside-to-close) to a **floating draggable panel**, same interaction model as `VendorView` (title-bar drag, clamp to viewport, position persisted in PlayerPrefs).
  - Default position when alone: similar to today (comfortable center-left). When **both** character + inventory are open and neither has a saved position yet, **tile side by side** (character left, inventory right, small gap, no overlap).
  - Overlay click no longer closes the panel — **Close** button only (matches vendor).
  - Remove mutual-close with character window. Opening inventory must **not** close character; opening character must **not** close inventory.
  - **Loadout** and **vendor** still close inventory / character when they open (skills shop and gear panels are different contexts — keep today's behavior for those).
- **Shared draggable panel helper** — extract title-bar drag + clamp + position read/write from `VendorView` into a small reusable utility (e.g. `UiDraggablePanel`) used by vendor, inventory, and character window. Vendor can migrate to it in the same PR or immediately after; inventory + character must use it.
- **DI**: dedicated `CharacterWindowLifetimeScope` + bootstrap prefab, same pattern as `InventoryWindowLifetimeScope` / `LoadoutWindowLifetimeScope`.
- **Starter state**: new characters keep today's bag (starter dagger in bag, armor empty). Equipped slots all empty unless we add one debug piece — not required.

**Out — later tickets, do not touch here**

- Applying equipped gear to the **unit** (meshes, `UnitModelWeaponEquipper`, armor attachments).
- **`StatSystem` / buffs** from gear or socketed gems.
- **`EquipWeapon` / loadout weapon** driven by equipped main-hand item (dedicated follow-up: auto-attack + skill gating from new equipment, then delete old loadout weapon path).
- Gem **socket** UI on pieces (2 sockets per piece — data fields may exist on assets; no UI).
- Keyword set bonus, legendary procs, match-end loot.
- Armor-weight **auto-strip on family change** at runtime (only manual equip refuse in UI for now).
- Real 3D character preview in the center pane.
- NPC / mob inventories or equipped gear.

The [item system design](done/item-sytem-2026-08-30.md) sections on slots, armor weight, and save shape are the **target**; this ticket implements equip persistence + paper-doll UI only.

## What already exists (use it)

| Already there | Role for this ticket |
| --- | --- |
| `ItemDefinition`, `ItemSlot`, `ItemDatabase`, `IItemCatalog` | Slot types, icons, `weaponData` ref for weight check |
| `CharacterSaveData.InventoryEquipment` | Bag rows `{ instanceId, itemId }` — source/target for equip |
| `IItemInventory` / `ActiveCharacterInventory` | Bag reads; equip service moves rows between bag list and equipped list |
| `ItemRules.IsArmorSlot` / `IsWeaponSlot` / `TryGetArmorWeightFor` | Validation |
| Inventory + loadout UI Toolkit windows | Copy presenter / view / lifetime scope patterns |
| `VendorView` drag + `PlayerPrefs` position persist | Reuse via shared `UiDraggablePanel` helper |
| `SetInventoryWindowOpenEvent`, `SetLoadoutWindowOpenEvent` | Add `SetCharacterWindowOpenEvent`; inventory ↔ character stay independent |

Do not invent a second item catalog or save file.

## Slots (paper-doll)

Same as item design — one item per slot, unequip returns that **instance** to the bag.

**Armor:** Head, Shoulder, Cape, Chest, Pants, Feet, Gloves.

**Weapons:** Main hand (required for weight check when present), Off hand (slot exists; v1 content may leave it empty).

## Save data

On `CharacterSaveData` **this ticket**:

```csharp
// Replace ArmorSlotIds stub
public List<EquippedSlotEntry> EquippedSlots = new();
// EquippedSlotEntry: ItemSlot slot, string instanceId (null/empty = bare slot)
```

- Equipped items are **not** duplicated in `InventoryEquipment` while worn.
- Switching active character loads that character's `EquippedSlots`.
- Sockets (future) stay on `instanceId` when unequipped.

**Not this ticket:** syncing equipped state to the networked unit, `CmdRequestEquip`, server-authoritative strip on death.

## Service API (sketch)

```csharp
public interface ICharacterEquipment
{
    bool TryGetEquipped(ItemSlot slot, out string instanceId);
    bool TryEquip(string instanceId, ItemSlot slot);
    bool TryUnequip(ItemSlot slot);
    IReadOnlyList<EquippedSlotEntry> Equipped { get; }
}
```

Implementation lives next to `CharacterInventory` static helpers or a `CharacterEquipment` class called from `CharacterManager` (roster save + `SaveRoster()` on mutation). Publish `EquipmentChangedEvent` after success.

## Character + inventory UI

**Side-by-side (default when both open)**

```
┌─ Character ─────────┐   ┌─ Inventory ─────────┐
│      [ Head ]       │   │ search + filters    │
│ [Shldr] [MODEL][Cape]│   │ item list           │
│ [Glv]  [PLACE][Chest]│   │ detail + destroy    │
│        [ Pants ]    │   │                     │
│      [ Feet ]       │   │                     │
│ [Main]    [Off]     │   │                     │
└─────────────────────┘   └─────────────────────┘
     (draggable)              (draggable)
```

- Center placeholder: styled empty `VisualElement` — future RenderTexture / mannequin. No gameplay dependency.
- Character panel: paper-doll slots only (+ slot highlight for "choosing: Head" subheading, like loadout).
- Inventory panel: unchanged feature set (list, filter, search, destroy); gains equip affordance when a character slot is selected.
- Invalid equip (wrong slot, wrong armor weight): inventory row disabled or detail shows why; equip action hidden.
- Rarity border on slot icons — reuse inventory styling.

**Open / close / coexistence**

| Action | Inventory | Character | Loadout | Vendor |
| --- | --- | --- | --- | --- |
| Toggle inventory (`B`) | toggle | unchanged | closes inv + char | closes inv |
| Toggle character (`U`) | unchanged | toggle | closes inv + char | closes char |
| Open loadout | closes | closes | toggle | — |
| Open vendor | closes | closes | — | toggle |

- `PlayerActionId.Character` + catalog entry (default **`U`**, Interface group, settings label **Character**) + `SetCharacterWindowOpenEvent`.
- Presenters subscribe to the other's open event only to run **default side-by-side layout** when both become visible (not to close).
- Panel positions saved per window in PlayerPrefs (`InventoryPanelPos`, `CharacterPanelPos`). On resize, clamp both so they stay on screen.

## UX decisions (WoW Classic)

Usability should match what Classic players expect. Implement all of the following.

### Keybinds

| Action | Default key | `PlayerActionId` | Notes |
| --- | --- | --- | --- |
| Character (paper doll) | **`U`** | `Character` (new, id 26) | Not `C` — Skill 1 uses `C`. `U` is free and rebindable in settings. |
| Inventory (bags) | **`B`** | `Inventory` (existing) | Already the bag key; keep it. |

Both keys are independent toggles. Pressing `U` does not auto-close `B`, and vice versa (Classic default).

### Equip / unequip (three paths — all supported)

1. **Quick equip (primary):** **Right-click** or **double-click** a bag row → `TryEquip` to that item’s slot (no slot pre-selection). If slot occupied → swap. If invalid (wrong weight / wrong slot) → no-op + brief detail reason.
2. **Slot-first equip:** Left-click paper-doll slot → select (`selectedEquipSlot`). Left-click a valid bag row or **Equip** on inventory detail → equip.
3. **Unequip:** **Right-click** an occupied paper-doll slot → `TryUnequip`. Left-click only selects; it does **not** unequip (avoids mis-clicks).

### Other Classic behaviors

- **Destroy equipped item:** blocked; show “Equipped — unequip first” on inventory detail. Destroy button disabled.
- **Modal input:** while character and/or inventory is open, push `UiModalInputBlock` (movement/skills blocked — same as inventory today). No dark fullscreen overlay.
- **Tooltips:** hover bag row or equipped slot → name, slot, stats text (reuse inventory detail strings). Wrong-weight items in bag show why they won’t equip when main-hand is set.
- **Shift-click:** out of scope (no stack split on gear copies).
- **Item drag** between panels: out of scope; window title-bar drag only.

### Cross-window state

`IEquipSlotSelection` (or `CharacterSlotSelectedEvent`) holds `selectedEquipSlot?`. Character presenter sets it on left-click slot; inventory presenter reads it for highlight, Equip button, and row enable state. Cleared when character window closes.

## Build order

1. `UiDraggablePanel` helper (extract from `VendorView`); unit-test clamp math if cheap.
2. **Inventory window refactor**: floating panel host (no dim overlay), draggable title bar, PlayerPrefs position, remove mutual-close with character (not built yet), keep loadout/vendor close rules.
3. `EquippedSlotEntry` + save field on `CharacterSaveData`; migration: drop `ArmorSlotIds`, init empty `EquippedSlots`.
4. `CharacterEquipment` + `ICharacterEquipment` + `EquipmentChangedEvent`; wire in `GameLifetimeScope`.
5. Character window UXML/USS + draggable panel + lifetime scope + bootstrap prefab.
6. `IEquipSlotSelection` + right-click / double-click handlers on inventory rows; right-click unequip on character slots.
7. Side-by-side default layout when both panels open without saved positions.
8. `PlayerActionId.Character` in catalog (default `U`) + open/close matrix (table above).
9. Manual pass: `U`+`B` open both → drag apart → restart → positions restored → right-click equip → double-click equip → slot-select equip → right-click unequip → swap → destroy blocked on equipped item.

## Follow-up ticket (not this one)

**Equipped weapons in combat** — resolve main-hand item → `weaponData` → replace loadout `WeaponId` / `EquipWeapon` path; auto-attack reads equipped weapon; remove old parallel weapon system from loadout + `UnitController.weaponName` sync. Then armor stats + visuals ticket.

## What we are not building

- Combat or visual side effects of wearing gear
- Gem socketing UI
- Dragging **items** between panels (right-click / double-click / slot-select equip cover Classic UX; window drag is in scope)
- Replacing or merging the loadout window
- Server RPC equip (local roster meta is enough for now; note in code if match sync is TODO)
- Full item matrix content — a few armor + weapon assets from the catalog is enough to test slots

## Done when

- Player opens character and inventory **together** in lobby or match; panels default side by side, draggable, positions persist.
- Right-click / double-click quick-equip, slot-select equip, and right-click unequip all work; swap + persist across restart.
- Inventory no longer uses full-screen dim or click-outside-to-close.
- Old loadout weapon and `EquipWeapon` behavior unchanged.
- No unit mesh / stat / combat changes.
