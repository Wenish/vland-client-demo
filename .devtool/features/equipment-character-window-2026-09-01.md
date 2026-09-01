---
id: "equipment-character-window-2026-09-01"
status: "backlog"
priority: "medium"
assignee: null
epic: null
dueDate: null
created: "2026-09-01T16:30:00.000Z"
modified: "2026-09-01T16:45:00.000Z"
completedAt: null
labels: []
order: "Zv"
---
# Equipment & character window

Units can **wear** armor and weapons from the item bag. Player manages loadout on a WoW-style **character panel**: paper-doll slots around a center **3D model placeholder**. Equip / unequip / swap only — no combat hooks yet.

This is a **new system** sitting on top of the finished item catalog + bag ticket. Do **not** migrate or remove the old weapon path (`PlayerLoadout`, `UnitController.EquipWeapon`, loadout window weapons) in this ticket.

## Feel

Open character **and** bag together — two floating panels side by side, like WoW `C` + `B`. Drag either window by the title bar; they stay out of each other's way. Select a paper-doll slot on the character panel → click an item in the inventory panel → it equips and leaves the bag. Click an occupied slot → unequip back to the bag. No full-screen dim blocking the game behind you. Stats and 3D meshes on the unit are still out of scope.

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
  - Destroying a bag row that is currently equipped is impossible (or auto-unequips first — pick one and document).
- **MessagePipe** `EquipmentChangedEvent` so UI refreshes when roster / active character / equip changes.
- **Character window UI** (new, sibling to inventory + loadout):
  - WoW-style layout: **slots around the edges**, **center = model placeholder** (empty panel, label like "Character preview" — no RenderTexture or unit spawn this ticket).
  - Slots: Head, Shoulder, Cape, Chest, Pants, Feet, Gloves, Main hand, Off hand (off-hand visible but may stay empty in content).
  - Each slot shows empty chrome or the item icon + rarity border (reuse inventory icon resolve path).
  - **No bag list on the character panel** — the inventory window is the bag. Select a slot on character → click a valid row in inventory (or an **Equip** action on inventory detail when a slot is selected) → equip. Click occupied slot → unequip. Inventory row highlight / disable when selected slot cannot accept that item.
  - Opens in **lobby and in-match** (new keybind on the input map). **Can be open at the same time as inventory** — they are independent toggles, not mutual-exclusive.
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
| Toggle inventory key | toggle | unchanged | closes inv + char | closes inv |
| Toggle character key | unchanged | toggle | closes inv + char | closes char |
| Open loadout | closes | closes | toggle | — |
| Open vendor | closes | closes | — | toggle |

- New input action + `SetCharacterWindowOpenEvent`.
- Presenters subscribe to the other's open event only to run **default side-by-side layout** when both become visible (not to close).
- Panel positions saved per window in PlayerPrefs (`InventoryPanelPos`, `CharacterPanelPos`). On resize, clamp both so they stay on screen.

**Equip flow (v1)**

1. Open character (inventory may already be open).
2. Click paper-doll slot → `selectedEquipSlot` set; inventory refreshes row enabled state + detail **Equip** button.
3. Click inventory row or **Equip** → `TryEquip(instanceId, slot)`.
4. Click occupied paper-doll slot → `TryUnequip(slot)`.
5. Item drag between panels is **out** this ticket (click only).

## Build order

1. `UiDraggablePanel` helper (extract from `VendorView`); unit-test clamp math if cheap.
2. **Inventory window refactor**: floating panel host (no dim overlay), draggable title bar, PlayerPrefs position, remove mutual-close with character (not built yet), keep loadout/vendor close rules.
3. `EquippedSlotEntry` + save field on `CharacterSaveData`; migration: drop `ArmorSlotIds`, init empty `EquippedSlots`.
4. `CharacterEquipment` + `ICharacterEquipment` + `EquipmentChangedEvent`; wire in `GameLifetimeScope`.
5. Character window UXML/USS + draggable panel + lifetime scope + bootstrap prefab.
6. Cross-window equip UX: `selectedEquipSlot` shared state (small service or MessagePipe `CharacterSlotSelectedEvent`); inventory presenter calls `TryEquip` / shows Equip on detail.
7. Side-by-side default layout when both panels open without saved positions.
8. Input bindings + open/close matrix (table above).
9. Manual pass: open both → drag apart → restart → positions restored → vendor grants item → equip each slot → unequip → swap → destroy blocked on equipped item.

## Follow-up ticket (not this one)

**Equipped weapons in combat** — resolve main-hand item → `weaponData` → replace loadout `WeaponId` / `EquipWeapon` path; auto-attack reads equipped weapon; remove old parallel weapon system from loadout + `UnitController.weaponName` sync. Then armor stats + visuals ticket.

## What we are not building

- Combat or visual side effects of wearing gear
- Gem socketing UI
- Dragging **items** between panels (click-to-equip is enough v1; window drag is in scope)
- Replacing or merging the loadout window
- Server RPC equip (local roster meta is enough for now; note in code if match sync is TODO)
- Full item matrix content — a few armor + weapon assets from the catalog is enough to test slots

## Done when

- Player opens character and inventory **together** in lobby or match; panels default side by side, draggable, positions persist.
- Equipping from inventory while a slot is selected works; unequip / swap works; state survives app restart.
- Inventory no longer uses full-screen dim or click-outside-to-close.
- Old loadout weapon and `EquipWeapon` behavior unchanged.
- No unit mesh / stat / combat changes.
