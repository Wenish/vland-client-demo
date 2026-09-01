---
id: "cape-universal-armor-weight-2026-09-01"
status: "done"
priority: "medium"
assignee: null
epic: null
dueDate: null
created: "2026-09-01T16:30:00.000Z"
modified: "2026-09-01T16:35:00.000Z"
completedAt: "2026-09-01T16:35:00.000Z"
labels: []
order: "Zw"
---
# Universal cape slot (no armor-weight gate)

Capes are a **personality slot**, not part of the cloth / leather / plate identity tied to weapon family. Any equipped weapon should be able to wear **any** cape. No leather capes, plate capes, or weight-gated cape variants.

This is a small design correction on top of the [item system](done/item-sytem-2026-08-30.md) and should land **before or with** [equipment & character window](equipment-character-window-2026-09-01.md) so equip validation never ships with cape weight checks.

## Feel

You found a cool cloak — you put it on. Dagger rogue, plate knight, staff caster: same cape slot, no “wrong armor type” grey-out. Weapon family still picks head / chest / etc.; the cape is flair on top.

## This ticket

**In**

- **Equip rule:** `ItemSlot.Cape` is **exempt** from armor-weight validation. All other armor slots keep `piece.armorWeight == ItemRules.TryGetArmorWeightFor(mainHandWeapon)`.
  - Add a single helper, e.g. `ItemRules.EnforcesArmorWeight(ItemSlot slot)` → `false` for `Cape`, `true` for Head–Gloves.
  - Character window bag filter + `TryEquip` use that helper (when that ticket lands). If equip service does not exist yet, add the helper + unit test now so the window ticket only calls it.
- **Runtime strip:** when main-hand changes across families and other armor unequips to the bag, **cape stays equipped** (cape is not in the strip list).
- **Catalog content** — retune the three existing cape assets:
  - `cape_cloth`, `cape_hunter`, `cape_war` remain separate **items** (different icons / stats / names) but are **not** weight-tiered.
  - Set every cape asset `armorWeight = Cloth` (field ignored for equip; keeps editor consistent).
  - Rename display names so they do not imply armor family: e.g. **Cloth Cape**, **Hunter Cape**, **War Cape** (or similar). Update descriptions — no “for casters / hunters / heavy” weight language.
  - Keep distinct icons (`item_cape_cloth`, `item_cape_hunter`, `item_cape_war` art is fine as visual variety).
- **Presentation:** inventory / detail type line for capes shows **“Cape”** only, not `Cloth Cape` / `Leather Cape` from `armorWeight` + slot (use slot label when `slot == Cape`).
- **Docs:** add a one-line note in the item system design’s armor-weight section that **cape is the exception** (or link this ticket). Do not rewrite the whole design doc.

**Out**

- New cape items beyond retuning the existing three.
- Cape meshes / attachment on the unit model.
- Stat rebalance pass on capes (keep current modifiers unless a rename forces a description touch-up).
- Removing `ArmorWeight` from `ItemDefinition` globally.

## What already exists

| Already there | Change |
| --- | --- |
| `ClothCape`, `HunterCape`, `WarCape` assets | All `armorWeight = Cloth`; rename display strings |
| `ItemRules.TryGetArmorWeightFor` | Unchanged for non-cape slots |
| `equipment-character-window` ticket | Uses `EnforcesArmorWeight` for filter + `TryEquip`; cape always eligible when `slot == Cape` |
| `item-starter-catalog-icons` cape art | Reuse as-is — art theme ≠ equip gate |

## Validation sketch

```csharp
public static bool EnforcesArmorWeight(ItemSlot slot)
    => IsArmorSlot(slot) && slot != ItemSlot.Cape;

public static bool CanEquipWithWeapon(ItemDefinition piece, WeaponType? mainHandWeapon)
{
    if (!EnforcesArmorWeight(piece.slot)) return true;
    if (!mainHandWeapon.HasValue) return true;
    if (!TryGetArmorWeightFor(mainHandWeapon.Value, out var required)) return true;
    return piece.armorWeight == required;
}
```

Weapon family change: unequip Head, Shoulder, Chest, Pants, Feet, Gloves when weight mismatches; **never** unequip Cape.

## Build order

1. `ItemRules.EnforcesArmorWeight` (+ `CanEquipWithWeapon` or equivalent used by equip service).
2. Tests: cape equips with Staff, Daggers, and Sword main-hand; helm still refused when weight wrong; family swap strips chest but not cape.
3. Retune `ClothCape`, `HunterCape`, `WarCape` assets (weight + names + descriptions).
4. `ItemPresentation` cape type line → `"Cape"` only.
5. Wire into character equipment when that ticket merges (or same PR if done together).

## Done when

- Any cape can be equipped regardless of main-hand weapon / armor family.
- Other armor slots still enforce cloth / leather / plate.
- Cape slot survives cross-family weapon swap.
- No UI or tooltip presents capes as leather / plate gear.
- Three cape items remain in the debug vendor as distinct personality pieces, not three armor weights.
