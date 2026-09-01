---
id: "item-starter-catalog-icons-2026-08-30"
status: "done"
priority: "medium"
assignee: null
epic: null
dueDate: null
created: "2026-08-30T20:36:00.000Z"
modified: "2026-09-01T16:23:23.324Z"
completedAt: "2026-09-01T16:23:23.324Z"
labels: ["Art"]
order: "Zo"
---
# Item starter catalog icons

Art briefs for a first full gear catalog: one armor look per weight, one weapon per v1 type. Related: `item-sytem-2026-08-30`. This ticket is art only — no new `ItemDefinition` assets.

**In:** 28 inventory icons (21 armor + 7 weapons) for `Assets/Art/Items/`.

**Out:** gems, materials, scythe, two-hand sword, off-hand as its own item, rarity variants (UI paints the border), replacing the gold loadout icons in `Assets/Art/Weapons/`. `bag.png` is the inventory window icon, not a catalog item.

## Style

Match the existing icons in `Assets/Art/Items/` (`dagger.png`, `bow.png`, `sword.png`, `shield.png`). Do **not** copy the gold glow / orange painterly loadout icons.

- Square PNG, **512×512**, solid **black** background
- No text, no rarity frame, no drop shadow outside the object
- Faceted / painterly, directional light from **top-left**, thick dark silhouette
- Must read at **56px** (inventory row) and **72px** (detail pane)
- One object fills most of the square; slight 3/4 view, not a flat orthographic stamp

**Cloth** — ash linen, lavender or muted teal trim, fabric folds, no heavy metal. Caster.

**Leather** — walnut / olive hide, brass buckles, straps. Skirmish.

**Plate** — cold faceted iron, rivets, same metal language as `sword.png` / `shield.png`. Heavy.

Filenames: `item_<weight>_<slot>.png` or `item_weapon_<type>.png`.

## Already drawn (keep)

Use as style reference. Re-export only if size/background is off.

| File | Item |
| --- | --- |
| `Assets/Art/Items/dagger.png` | Starter Dagger |
| `Assets/Art/Items/bow.png` | Starter Bow |
| `Assets/Art/Items/sword.png` | Starter Sword |
| `Assets/Art/Items/shield.png` | Shield half of Sword and Shield (reference, not a catalog row by itself) |

## Weapons (7)

### item_weapon_staff.png — Starter Staff — **new**

Long wooden staff on the diagonal. Cloth wrap on the grip (same ash/lavender as cloth armor). Top: a faceted crystal orb in the same ice-blue / amethyst language as `dagger.png`, held by a simple metal ring. No flame, no sparkle field, no gold glow background.

### item_weapon_dagger.png — Starter Dagger — **exists**

Single crystalline dagger, tip up-left. Broad faceted blade (white edges, sapphire / amethyst core). Wing-shaped steel crossguard with a small blue diamond gem. Purple criss-cross wrap on the grip, matching steel pommel.

### item_weapon_bow.png — Starter Bow — **exists**

Recurve bow, no string, diagonal. Dark reddish wood limbs, gold fittings with sharp beak points, four red diamond gems, olive-green wrapped grip.

### item_weapon_pistols.png — Starter Pistols — **new**

Two compact flintlock-style pistols, crossed or slightly offset so it reads as a **pair**, not one gun. Steel barrel and lock, brass fittings, leather-wrapped grips. Same faceted metal as the sword, not the gold loadout pistol.

### item_weapon_rifle.png — Starter Rifle — **new**

Long barrel on the diagonal (same energy as the bow). Dark wood stock, iron bands, a leather sling. Simple marksman rifle — no ornate scope, no legendary gold burst.

### item_weapon_sword.png — Starter Sword — **exists**

Short one-hand sword, diagonal. Faceted silver blade with a dark navy fuller, gold wing-shaped guard with a blue gem, dark wrapped grip, gold pommel with a smaller blue gem.

### item_weapon_swordandshield.png — Starter Sword and Shield — **new** (one icon)

One item: short sword **and** heater shield in the same frame. Shield: dark wood planks, faceted steel rim, central boss and rivets (`shield.png`). Sword: same family as `sword.png`, slightly smaller so both silhouettes stay clear. Do not deliver shield-only as the catalog icon.

## Cloth (7)

Ash linen, lavender or muted teal accent, fabric folds, no plate.

### item_cloth_head.png — Cloth Hood

Soft pointed hood, fabric only, small metal clasp at the throat. No visor, no helmet skull. Hood opening dark so the shape reads at 56px.

### item_cloth_shoulder.png — Cloth Pauldrons

Cloth draped over both shoulders, light padding underneath, a cord tying them. Not metal spaulders.

### item_cape_cloth.png — Cloth Cape

Long falling cape, simple clasp, big readable folds. Personality piece — more drape than stats.

### item_cloth_chest.png — Cloth Robe

Belted robe torso: wrap or V-neck, sash at the waist, cloth folds across the chest. No breastplate.

### item_cloth_pants.png — Cloth Pants

Loose wrapped trousers **or** the lower robe hem with a sash. Must read as legs / lower body, not a second cape.

### item_cloth_feet.png — Cloth Slippers

Soft pointed cloth shoes or footwraps. Light, no boot shaft, no hobnails.

### item_cloth_gloves.png — Cloth Wraps

Fingerless mage wraps around palm and wrist. Cloth strips, maybe a small teal/lavender cord. No gauntlet plates.

## Leather (7)

Walnut / olive, brass buckles, straps.

### item_leather_head.png — Leather Cap

Close-fitting leather cap or light brimmed hat with a chin strap. Not a pot helm, not a cloth hood.

### item_leather_shoulder.png — Leather Shoulders

Layered hide spaulders, brass rivets, a strap across the chest. Chunky enough to silhouette, still clearly leather.

### item_cape_hunter.png — Hunter Cape

Short hunter cloak, rough hem, leather clasp. Shorter and stiffer than the cloth cape.

### item_leather_chest.png — Leather Chest

Layered leather cuirass / vest with crossed chest straps. Matches the existing `LeatherChest` item.

### item_leather_pants.png — Leather Pants

Fitted leather trousers, a buckle on the hip or calf strap. No plate greaves.

### item_leather_feet.png — Leather Boots

Mid-calf boots, one brass buckle, slightly worn. Sole and shaft must read at 56px.

### item_leather_gloves.png — Leather Gloves

Full-finger leather gloves, stitching over the knuckles. Not plate gauntlets, not cloth wraps.

## Plate (7)

Cold faceted iron, rivets, same metal as sword / shield.

### item_plate_head.png — Iron Helm

Nasal or visored iron helm, faceted plates, eye slit, rivets. Closed and heavy. Matches the existing `IronHelm` item.

### item_plate_shoulder.png — Iron Pauldrons

Heavy angular shoulder plates, same facets as the helm. One or both shoulders, overlapping lames.

### item_cape_war.png — War Cape

Heavy war cloak with a metal clasp and a short chain, steel-trimmed hem. Darker wool than cloth; still a cape, not a tabard-only stamp.

### item_plate_chest.png — Iron Chest

Breastplate with a central ridge and rivets. Same iron as the sword and shield. No cloth robe showing through.

### item_plate_pants.png — Iron Greaves

Cuisses / thigh-and-shin plates that read as the pants slot. Articulated lames, not a second chest.

### item_plate_feet.png — Iron Boots

Sabatons: articulated foot plates, rounded toe cap, rivets. Clearly metal boots.

### item_plate_gloves.png — Iron Gauntlets

Plate gloves, knuckle plates, rivets at the wrist. Fingers or mitten — either is fine if the silhouette is a gauntlet.

## Done when

24 new PNGs are in `Assets/Art/Items/` next to the four existing weapon icons, named as above, and readable at inventory size. Wiring them onto `ItemDefinition.icon` stays with the item-system ticket.