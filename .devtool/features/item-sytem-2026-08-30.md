---
id: "item-sytem-2026-08-30"
status: "backlog"
priority: "medium"
assignee: null
epic: null
dueDate: null
created: "2026-08-30T15:46:48.099Z"
modified: "2026-08-30T21:55:00.000Z"
completedAt: null
labels: []
order: "Zt"
---
# Item System

Permanent gear on a character. The thing you look forward to after a match, and later the reason to walk into the world. Not a second game on top of Shadow Infection.

## Feel

Finding a piece and immediately wanting it on. Your character getting a bit more *yours* over rounds. The weapon *is* the class: staff and scythe wear cloth, skirmish weapons wear leather, sword and shield wear plate. A plate drop is a reason to try sword, not a chest the dagger hero wears anyway. Gems are the optional extra: socket stats, or commit one keyword across everything you are wearing and get a unique payoff.

Not: filling every slot, repairing, farming a checklist, or rolling 12 affixes.

Empty slots are fine. A new character starts with a starter dagger in the bag and empty armor. Getting another copy of the same piece is allowed.

## This ticket

Catalog + bag + save. Items exist as clean data, characters keep them across sessions. That is the whole ship.

**In**

- `ItemDefinition` ScriptableObject, enums, `ItemDatabase` on `IGameDatabases`. Professional, small, no inheritance tree.
- Inventory on the character (unlimited). Player roster save so a hero still has their bag next session (`CharacterSaveData` + existing `CharacterManager` PlayerPrefs).
- Gear copies are **bag rows** (`instanceId` + `itemId`) so two Iron Helms are two entries. No rolled stats — the instance id is only "this copy in the bag."
- **Stackables** share one list `{ itemId, count }`: gems now, ore / wood / stone later. Same grant/destroy path.
- `ItemKind`: `Equipment` | `Gem` | `Material`. Equipment has a paper-doll `slot`. Gem / Material have none.
- A unit can *hold* that inventory (read the character bag / a simple bag list). It does not wear it.
- Inventory UI: list, filter by item type, search by name, **destroy/remove** on a row. Opens in the **lobby and in-match** (keybind on the existing input map). Filter includes Material so crafting junk has a home later.
- Lobby debug vendor: every item, 0 gold, always present, `TryGrantItem`. Buying the same gear again adds another copy; stackables increment count.
- New character: grant one starter dagger item into the bag. Loadout weapon is unchanged this ticket.
- Gems exist as stackable items. Socketing and keyword bonuses are data fields on the asset if they fit; they do not run in combat yet.
- No crafting recipes, stations, or material assets required this ticket — only the kind + stack path so adding "Iron Ore" later is a new SO, not a refactor.

**Out — later tickets, do not touch here**

- Making the unit **wear** armor (stats on `StatSystem`, visuals, meshes, attachment points).
- Overhaul of **weapon equipping** (`PlayerLoadout`, `EquipWeapon`, skill gating from a weapon item). Loadout weapons stay as they are.
- Paper-doll, equip/unequip onto slots, armor-weight enforcement, gem sockets in play, keyword set bonus, legendary buffs.
- Match-end loot tables.
- Crafting gameplay (recipes, stations, combining). The bag can already hold materials.
- Anything that changes how a unit fights.

The sections below are the **target design** so the data model is aimed at the right shape. This ticket only implements the **In** list. Empty fields on an item (weight, sockets, `weaponData` ref) are fine; unused combat hooks are not.

## What already exists (use it)

Do not invent a parallel combat or save stack.

| Already there | Role for items |
| --- | --- |
| `CharacterSaveData.InventoryItemIds` | Persistence stub. Replace with gear rows `{ instanceId, itemId }` plus **item stacks** (gems and later materials). Do not replace loadout or add combat hooks. |
| `WeaponData` + `WeaponType` | Combat stays here. Item assets may *reference* a `WeaponData`; this ticket does not call `EquipWeapon`. |
| `CharacterManager` roster PlayerPrefs | Where the bag is saved. Same path as name / loadout ids. |
| Loadout window | Skills + weapons stay. Inventory is a sibling window, not a rewrite of loadout. |
| `InteractionZone` + vendor window | Reuse for the lobby debug stall. |

## Core rules

1. **Catalog definitions, bag copies.** Every *kind* of item is a ScriptableObject with a stable `itemId`. No rolled stats, no durability. The bag may hold **several copies** of the same gear; each copy is a row with its own `instanceId` so later sockets can sit on *that* helm, not all helms of that name.
2. **Per character.** Gear lives on the roster character, not the account. Makes *this* hero matter. Dying in a match does not empty the bag.
3. **Copies (equipment). Stack (gems and materials).** Armor and weapons: `TryGrantItem` always adds a new row. If `ItemKind` is Gem or Material: increment `{ itemId, count }` — interchangeable.
4. **No bag cap.** A character can hold as many items as they own. No slot count, no weight limit, no tetris grid.
5. **Destroy is allowed.** Inventory row can be removed (gear instance gone, or stack count −1). No sell value this ticket.
6. **Equip is a slot swap (later ticket).** One item per slot. Unequip returns that instance to the bag. Gems stay in that piece's sockets.
7. **One armor type per weapon family.** Cloth, leather, and plate are identities, not a ladder. See [Armor weight](#armor-weight).
8. **Gear and socketed gems add stats (later ticket).** They do not grant skills. Skills stay loadout.
9. **Weapon items wrap `WeaponData` (later ticket).** Equipping a weapon item sets the loadout weapon *and* applies the item's stats. This ticket does not call `EquipWeapon`.

That last rule is the whole "weapon as item" design. `WeaponData` remains the combat definition. Several items can share the same `WeaponData` (Rusty Daggers vs Night Daggers both use `Daggers`).

## Slots

Paper-doll. Visual identity now, 3D meshes later.

**Armor**

| Slot | Why it earns a slot |
| --- | --- |
| Head | Obvious, easy icon, usually HP / resist. |
| Shoulder | Silhouette. |
| Cape | Personality slot. Fine to be lighter on raw stats and heavier on a unique feel later. |
| Chest | Biggest armor numbers. |
| Pants | Completes the set look without needing set bonuses. |
| Feet | Move speed lives here more than elsewhere. |
| Gloves | Attack speed / crit live here more than elsewhere. |

**Weapons**

| Slot | v1 |
| --- | --- |
| Main hand | Required. Holds the weapon item. Drives `WeaponData` / skill gating. |
| Off hand | Data exists, **leave empty in v1**. Shield / extra dagger later. Today `SwordAndShield` is one main-hand `WeaponType`, same as now. |

Do not split dual-wield or sword+shield into two items until there are animations and a reason. The off-hand slot is a reserved hole, not a system.

Starter: new character gets **one starter dagger item** in the bag. Armor empty, sockets empty. Loadout's beginner `WeaponId = Daggers` stays as it is this ticket (two systems until the wear ticket).

## Inventory

The bag belongs to the **character** (`CharacterSaveData`). That is what persists across sessions.

A **unit** can expose the same bag at runtime (player unit = active character inventory) so UI and grant code have one place to look. It is a list of owned items, not worn gear. Mobs and NPCs do not get inventories in this ticket.

- Gear rows `{ instanceId, itemId }` (replace the `InventoryItemIds` string list) + `ItemStack` `{ itemId, count }` for every stackable kind.
- No max size.
- `TryGrantItem` always succeeds for a known id (new gear row, or stack count +1).
- `TryDestroyItem` removes one gear row or decrements a stack.
- Leave `ArmorSlotIds` alone this ticket (unused stub). Equipped-slot fields wait for the wear ticket. When sockets exist, they hang off `instanceId`, not `itemId`.

## Gems

Gems are items too — same catalog, `ItemKind = Gem`. They do not go on the paper-doll. They go in sockets. They stack like materials.

**Sockets:** every armor piece and the main-hand weapon has **2** sockets. Always 2, not gated by rarity. Off-hand has none until that slot exists. Empty sockets are fine — filling 14 holes is not a task.

Any gem fits any socket. No colors, no matching shapes.

**While socketed:** the gem's `statModifiers` apply, on top of the piece. Pull the gem → those stats go away, gem returns to the stack.

**Keyword (the unique impact):** a gem may have a `keyword` (optional — most gems are just stats). If **every armor piece you are currently wearing** has that keyword in at least one of its two sockets, the keyword's bonus applies **once**. Not per piece, not per socket.

- Wear 3 pieces, all with the same keyword in one socket → bonus is on. You do not need 7 pieces.
- A worn piece with both sockets empty (or only other keywords) → that keyword is off.
- Two sockets per piece means two keywords can both complete at once (Flame in socket A, Frost in socket B on every worn piece).
- Weapon sockets add stats only. They do **not** count toward the armor keyword.

That is the whole set system. Armor itself has no 2/4/6 set bonuses.

**v1 gems do not grant skills or actives.** A later hook can let a gem add an active (Zhonya-style hourglass, etc.). Leave `activeSkill` null and unused. Do not build item actives now.

**Stacks:** bag holds `{ gemId, count }`. Socketing spends 1. Unsocketing refunds 1. Sockets live on the *piece* (`itemId` → two gem ids), so a gemmed chest stays gemmed in the bag.

## Armor weight

The weapon family assigns **exactly one** weight. No mixing types. You still pick *which* plate helm vs which plate chest.

| Family | Weapons | Armor | Typical stats |
| --- | --- | --- | --- |
| Caster | Staff, Scythe | Cloth | Ability power, maybe crit |
| Skirmish | Dagger, Bow, Pistols, Rifle | Leather | Attack speed, crit, a bit of move speed |
| Heavy | One-hand sword, Shield, two-hand sword (later) | Plate | Armor, Health |

One function: `ArmorWeightFor(WeaponType)`. A piece is legal when `piece.weight == that`. Wrong type stays in the bag.

**Weapon swap inside a family** (bow → daggers, staff → scythe): armor **stays on**.

**Weapon swap across families** (daggers → sword): all armor unequips to the bag, not deleted. Same beat as skills that fall off when the weapon type changes.

**Equip refuse:** paper-doll greys out other weights. Server strips them on loadout apply. No toast essay — the slot just won't take it.

Don't invent a fourth weight. Cape is still cloth/leather/plate like every other slot (a plate cape only on heavy weapons). No move-speed penalty tables — the type *is* the identity.

## Weapons (player-facing → existing `WeaponType`)

Do not add new `WeaponType` values until the model has an animation set.

| Item weapon | Maps to | Armor | v1? |
| --- | --- | --- | --- |
| Staff | `Staff` | Cloth | yes (already in data) |
| Scythe | new type | Cloth | later |
| Dagger | `Daggers` | Leather | yes |
| Bow | `Bow` | Leather | yes |
| Pistols | `Pistols` | Leather | yes |
| Rifle | `Gun` | Leather | yes |
| One-hand sword | `Sword` | Plate | yes |
| Shield | `SwordAndShield` (main hand) | Plate | yes, as a weapon type, not an off-hand item |
| Two-hand sword | new type | Plate | later |

A weapon item that does not match a real `WeaponData` is invalid. Server rejects it the same way it rejects an unknown weapon name today.

## An item (the asset)

One ScriptableObject. ~15 fields, not an inheritance tree.

- `itemId` (stable string, save key)
- `displayName`, `description`, icon
- `kind` — `Equipment` | `Gem` | `Material`
- `slot` (equipment only: paper-doll slots)
- `rarity` (Common / Uncommon / Rare / Epic / Legendary)
- `armorWeight` (only if armor)
- `weaponData` (only if main-hand weapon — the `WeaponData` it equips)
- `statModifiers` (`List<StatModifier>`, already in the game)
- `legendaryBuff` (optional, **Legendary gear only**, can be null)
- `keyword` (optional, **gems only** — identity for the all-worn-armor bonus)
- `keywordBonus` (optional modifiers or a buff, applied once when the keyword completes)
- `activeSkill` (reserved, always null in v1)
- icon / optional later: mesh or attachment point

`kind` decides bag behavior: Equipment → new `instanceId` row. Gem and Material → stack count +1. No extra subclasses.

`ItemDatabase` sits next to `WeaponDatabase` / `SkillDatabase` on `IGameDatabases`.

## Rarity

Five. Color the icon border. That's enough excitement.

| Rarity | Border | What it means |
| --- | --- | --- |
| Common | Grey | A few flat stats. Starter / filler. |
| Uncommon | Green | A clear step up from starter. Easy to spot in the bag. |
| Rare | Blue | Noticeable. The usual drop you are happy to slam on. |
| Epic | Purple | Strong or a slightly spicy stat mix. |
| Legendary | Gold | Named. One unique trick via existing buffs (on-hit feel, a defensive proc, a tiny identity). Not a new skill on the bar. |

No item level. No "required wave 12". If a legendary is too early, don't put it in that loot table.

## Save data

On `CharacterSaveData` **this ticket**:

- `List<InventoryEntry>` — `{ instanceId, itemId }` for each equipment copy (armor / weapons). Duplicates allowed.
- `List<ItemStack>` — `{ itemId, count }` for gems and materials.

New character: create as today, then `TryGrantItem` the starter dagger definition once.

Same PlayerPrefs roster as now (`CharacterRoster_v1`). Switching characters loads that character's bag.

**Not this ticket:** named equipped fields, `ItemSockets` (will key by `instanceId` later), sending items through `CmdRequestSetLoadout`, server applying modifiers.

## Combat (later ticket — not this one)

Do not apply items to the unit in this ticket. Loadout, `EquipWeapon`, and `StatSystem` stay as they are.

When a later ticket does wear:

1. Resolve equipped ids → item assets.
2. If main-hand has `weaponData`, `EquipWeapon` that (existing path). If empty, keep today's beginner weapon.
3. Drop any equipped armor whose weight is not `ArmorWeightFor` that weapon (unequip to bag; do not apply it). Same-family swaps keep armor. Gems stay in the piece.
4. Apply remaining gear `statModifiers` plus every socketed gem's `statModifiers` (one `BuffStat` with id `equipment` and `UniqueMode.Global`). Persist through death like upgrades.
5. If a legendary piece has a buff, apply that too (unique by `buffId`).
6. For each keyword: if every *currently worn armor* piece has that keyword in at least one socket, apply that keyword bonus once.

In-match shop upgrades stay a separate layer on top.

## How you get items

**This ticket:** lobby debug vendor → `TryGrantItem`. Destroy from the inventory UI. That is enough to fill, dump, and test the bag.

**Later:** after a match, a small chance to grant **one** item. One `LootTable` (`{ item, weight }`). Do **not** skip items the player already owns — copies are allowed. Gems increment the stack. Chests, bosses, world pickups call the same `TryGrantItem`.

**Not this ticket:** open world, dungeon bosses, real economy vendors, trading, pickups in the level, match-end loot.

## Debug vendor (lobby)

A test stall in `Assets/Scenes/Lobby.unity` so you can exercise gear without finishing matches.

- Reuse `InteractionZone` (`OpenVendor`) + the existing vendor window. Do not build a second shop UI.
- Catalog = **every** `ItemDefinition` in `ItemDatabase`, gold **0**, unlimited stock. Buy calls `TryGrantItem` on the active character (gear: new row; stackable: count +1). Same item twice = two copies / a bigger stack.
- Buy tab only. Hide sell / in-match upgrades.
- Always placed in the lobby (not editor-only). Label it as debug (name / subtitle). Not the real item economy. Easy to strip or gate later.

## UI

**This ticket — inventory only.** Simple list of everything the character owns. No bag grid, no paper-doll.

- Opens in the **lobby and in-match** (keybind on the existing input map).
- Sort / filter by item type: All, each armor slot, Weapon, Gem, Material.
- Search box: item name (case-insensitive contains). Typing in search does not fire gameplay or UI hotkeys (inventory, loadout, skills, movement).
- Detail on select: name, rarity, type, description, stats text. No equip button that changes the unit.
- Stackables show count. Duplicate gear shows as separate rows.
- Destroy / remove on the selected row (confirm if cheap; no sell gold).

Loadout (skills / weapons) stays the current window, unchanged.

**Later ticket:** paper-doll, 2 gem sockets on a piece, equip/unequip, weight grey-out, keyword complete indicator, armor meshes.

## What we are not building

Cut these even if they are "real" in other games:

- Crafting recipes, stations, combining, repair, durability (bag already holds `Material` stacks; recipes are a later ticket)
- Socket colors / shapes, gem combining, rarity-gated socket counts
- Random affixes / Diablo item instances (bag `instanceId` is "this copy", not rolled stats)
- Armor set bonuses (2/4/6). Gem keywords are the set.
- Trading, auction, mail
- Consumables / potions (in-match upgrades already cover "power this round")
- Quest items, junk
- Bag size / weight limits / tetris grids
- Class / level / wave gates on items (weapon family → one armor type is the only gate)
- Mixing armor types on one character (no plate chest + cloth cape)
- A new skill or active granted by gear or gems (Zhonya-style gem actives are later)
- Inventories on NPCs / mobs
- Off-hand dual wield
- New `WeaponType`s before their animation sets exist
- A real paid item shop (lobby stall is debug, 0 gold)

**Out of this ticket (target design, next tickets):**

- Unit wearing items (combat stats, visuals, meshes)
- Overhaul of unit weapon equipping / `PlayerLoadout`
- Paper-doll, socketing UI, keyword bonus in play
- Match-end loot / `LootTable`

If a future dungeon needs a key item, that's a different, tiny flag — not this system.

## This ticket — build order

1. `ItemDefinition` SO + `ItemSlot` / `ItemRarity` / `ArmorWeight` enums + `ItemDatabase` on `IGameDatabases`. Fields for later wear (`weaponData`, sockets, keyword) may exist on the asset; nothing reads them in combat.
2. Inventory on `CharacterSaveData`: equipment `{ instanceId, itemId }` rows + `ItemStack` for gems/materials. `TryGrantItem` / `TryDestroyItem` on `CharacterManager`. Persist with the existing roster save. No bag cap. New character gets one starter dagger row.
3. Inventory UI: list + type filter + name search + destroy. Open in lobby and in-match (keybind).
4. Lobby debug vendor: `InteractionZone` in Lobby (always), full catalog, 0 gold, `TryGrantItem`.
5. A handful of real item assets so the list is not empty (starter dagger + a few others, including at least one gem). Not a full 7×3×4 matrix.

## Later tickets (not this one)

- Wear: equipped slots, armor-weight gate, apply `StatModifier`s, gem sockets, keyword bonus.
- Weapon items actually calling `EquipWeapon` / replacing loadout weapon picks.
- Paper-doll UI.
- Match-end `LootTable`.
- World pickups, chests, bosses, off-hand, two-hand / scythe types, armor meshes, gem actives, real vendor, crafting recipes.

The system is done when a drop makes you grin and you put it on. This ticket stops at: the item is real, it's in the bag, and it's still there next session.
