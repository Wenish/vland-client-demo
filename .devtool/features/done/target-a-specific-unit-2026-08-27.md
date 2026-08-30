---
id: "target-a-specific-unit-2026-08-27"
status: "done"
priority: "medium"
assignee: null
epic: null
dueDate: null
created: "2026-08-27T20:42:20.968Z"
modified: "2026-08-30T05:29:09.568Z"
completedAt: "2026-08-30T05:29:09.568Z"
labels: ["Usability"]
order: "Zp"
---
# Target a specific unit

Sticky unit selection (WoW-style) plus a HUD target frame.

Right-click a unit to select it. A frame shows that unit: name, health, shield, buffs/debuffs, cast bar. A ground circle marks them in the world. Their nameplate stays fully visible while selected.

Skills that snap to a unit prefer the selected target. Ground skills still go to the mouse.

## Should

- **Select** with **Select Target** (default **RMB**) on a unit. Works during skill preview and while you are dead. Does **not** confirm/cast the skill.
- **Clear** with the same bind on **empty ground**, only when **no preview** is open. During preview, RMB empty **cancels preview** and **keeps** the selected unit.
- Select **enemies, allies, yourself, and corpses**. Not gates / vendors / destructibles.
- **HUD frame** bottom band, **up and to the right** of the player health/shield/skill block (not glued to the skill bar). Hidden when nothing is selected.
- **World**: selection circle under the unit + nameplate forced visible. Circle look comes from a **ScriptableObject config** (not hardcoded).
- **Smart / snap-to-unit skills** use the selected unit if it is a legal target for that skill. Out of range: preview still on them, confirm shows the existing out-of-range error, preview stays. Ground / mouse-place skills ignore selection.
- Hover does **not** steal the skill off the selected unit. RMB a different unit during preview to switch.
- **Self Target Modifier** (Left Alt) still wins over selection.
- Basic attack stays mouse-aimed. No Tab-target, focus target, or target-of-target in this ticket.

## Input

Default bind is RMB. That key is already **Cancel Cast** and **Spectate Next**. Same overlap pattern as Attack / Spectate Previous.

Add catalog action **Select Target** (`Combat`), default RMB, `allowedOverlaps`: Cancel Cast + Spectate Next. Settings label: **Select Target**.

When those actions share a key (the default):

| Pointer | Preview | You | Result |
| --- | --- | --- | --- |
| Over a **unit** | any | alive or dead | **Select** that unit. Do not cast. Do not cancel preview. Do not spectate. |
| Empty / not a unit | preview open | any | **Cancel Cast** only. **Keep** selected target. |
| Empty / not a unit | no preview | alive | **Clear** target. Frame hides. |
| Empty / not a unit | no preview | dead | **Spectate Next**. Also clears target (empty click). |

If Select Target is remapped off RMB: that key on a unit selects; on empty (no preview) clears. Cancel Cast / Spectate Next keep their own keys and rules.

**Keep**

- LMB is still Attack / confirm preview. LMB does **not** select.
- Pointer over blocking UI: ignore **mouse** select (same as attack). Keyboard skill binds still work.
- Modal UI (`UiModalInputBlock`): no select.
- Same skill key again while preview is up still **confirms** (on the selected unit if it is legal).
- Raycast: same unit layer / first hit as hover highlight (`UnitHighlighter`). No max click range — any unit the pointer hits.

## Who you can select

You can select any `UnitController` the pointer hits, including:

- Other players (enemy or ally)
- Zombies and other NPC units
- Yourself
- Dead units (corpses), so res / Dead-mask skills still have a target

You cannot select:

- Gates, vendors, or other interactables
- Destructibles / structures (unless that object is a normal unit with `UnitController` on the same layer as hover-highlight)

Selecting yourself is allowed. The target frame then shows you (same info as the player HUD — that is OK). **Self Target Modifier** (Left Alt) still forces self-cast even if someone else is selected.

## Clear / lifetime

- **RMB empty, no preview** → clear.
- **RMB another unit** → replace (including during preview).
- **Target dies** → **keep** them. Frame stays at 0 HP (dead look). Needed for res. Clear with empty click or by selecting someone else.
- **Target despawns** (zombie removed, object destroyed) → clear. Cannot keep a missing unit.
- You die → selection **stays** (you can still select while dead).
- Leaving the match / unit owner gone → clear.

No Esc-to-clear, no range/off-screen auto-clear.

## What selection does to skills

Selection is **local** (each client has their own). Server still validates team, life mask, and range. Never client-authoritative “I hit this unit”.

Applies to skills that **snap to a unit** (smart target / snap indicator). Ground / mouse-position skills **ignore** selection (still placed at mouse; Alt still places at caster).

**Priority** (first match wins)

1. **Self Target Modifier** held → self (if the skill allows Self), same as today.
2. Else if a unit is selected **and** it passes that skill’s team + life filters:
   - **In range** → that unit is the snap / cast target. Mouse position and hover do not override.
   - **Out of range** → still snap preview to them. Confirm / quick-cast: existing **“target out of range”** error, **do not** consume the skill, **keep** preview if one is open, **keep** selection. Same as `point-to-click-spells-should-also-target`.
3. Else (no selection, or selected fails team/life) → today’s mouse snap / aim.

**Preview**

- Opening preview with a legal selected unit: indicator and highlight follow **them**, not the hovered unit (`unit-highlight-when-using-spell-preview-indicator`).
- RMB another unit: selection switches, preview follows the new unit. Does not confirm.
- LMB / same skill key: confirm on the selected unit (or error if OOR). Does not change selection.
- Quick-cast (Cast Modifier + skill, or per-slot Quick Cast on): same prefer-selected rules. OOR → error, no cast, no preview to keep.

If the selected unit is the wrong team for this skill (e.g. ally selected, damage skill): treat as “no usable selection” and fall back to mouse snap. HUD still shows the ally.

## HUD target frame

**When:** visible only while a unit is selected. Hide when cleared / despawned.

**Where:** same bottom HUD band as the player combat block, **offset up and to the right** of `characterCombatHud` (vitals + skill row). Classic floating unit frame — not a sibling glued to the skill row.

- Must not sit on the **Loadout** button (that button is already `left: 100%` at skill-bar height). Frame sits **above** that right slot.
- Player’s own cast bar stays where it is (screen center). Target cast bar lives **on the target frame**.
- `picking-mode="Ignore"` — frame is **not** clickable this ticket (no heal-click / click-to-reselect). Must not eat world clicks.

**Contents** (always, while selected)

| Piece | Behavior |
| --- | --- |
| **Name** | Player character name, else `unitName`. |
| **Health** | Bar + numbers (`current / max`), same style as the player HUD vitals. Fill / border **team-colored** (ally vs enemy vs self), same team colors as nameplates. Local player / self uses the local-player health color. |
| **Shield** | Same as health, with numbers. **Hide the whole shield row** when `maxShield == 0`. |
| **Buffs / debuffs** | Two rows (WoW): **buffs** one side, **debuffs** the other (`BuffType.IsNegative`). Icon, duration, stacks — reuse nameplate buff language. Hover **tooltip**: display name + remaining time. Respect `ShowInUnitUiBuffBar`. Cap / wrap if too many (e.g. 8 per row). |
| **Cast bar** | Only while they are casting or channeling. Skill **name + icon + time**, same `CastBar` language as the player. Hide when idle. Interrupt can use the same feedback as the player bar if cheap. |

Dead selected unit: name stays, health 0, shield hidden or 0, no cast bar unless they somehow still cast. Frame stays until cleared.

No portrait this ticket.

## World

**Selection circle** on the ground under the selected unit. Team-colored (hostile / ally / self). Follows the unit; hides when selection clears or the unit despawns. Does not replace hover outline — hover highlight still runs; during snap preview the **snap** unit is highlighted (today’s rule). If snap unit == selected, that is one outline.

**Config is a ScriptableObject** (same idea as `NameplateLayerSettings`). No circle size, texture, material, offset, or color hardcoded in the presenter. Asset lives under `CreateAssetMenu` (e.g. `Game/Targeting/Selection Circle`). Data only — no gameplay logic.

Fields the asset must expose (names can change):

| Field | Purpose |
| --- | --- |
| Prefab / mesh / decal | What is spawned under the unit |
| Texture | Circle texture (optional if the prefab already has one) |
| Material | Optional override |
| Radius | World size. Optional “scale with unit collider” so big bosses get a bigger ring |
| Height offset | Lift off the ground to avoid z-fight |
| Colors | Self / ally / enemy (or tint multipliers on `ITeamColorService`) |
| Opacity | Alpha |

Wire the asset on the targeting LifetimeScope / presenter (`RegisterInstance` / serialized field), same as nameplates. Designers tweak the asset in the Inspector; they do not edit code to change how the circle looks.

**Nameplate:** while selected, force the world nameplate **fully on** (name + health + shield as applicable) even at full HP. Other units keep today’s show-when-damaged policy.

## Out of scope

- Tab / nearest-enemy cycle
- Focus target, target-of-target
- Portrait
- Clickable target frame
- Buff row on **your** player HUD (only the target frame)
- Changing basic attack to chase the selected unit
- Gamepad select
- Selecting non-units

## Architecture

Follow `.cursor/rules/project-architecture.mdc`. New code in `ShadowInfection.Targeting` (or `ShadowInfection.Target`) — not legacy root namespace. No new singleton. HUD does not `FindObjectOfType` a game manager.

**Layers**

1. **Data** — Selection-circle ScriptableObject (prefab, texture, material, radius, height offset, self/ally/enemy colors, opacity). No gameplay logic in the asset. Same pattern as `NameplateLayerSettings`.
2. **Service** — `IPlayerTarget` (name can change): current `UnitController` or none, set/clear, `TryGetSnapshot` for UI. Local client only. Register in `GameplayLifetimeScope` (clears when the match ends). Constructor / `[Inject]`.
3. **Events** — MessagePipe on change (`PlayerTargetChangedEvent` or similar) so HUD, circle, and nameplates can subscribe. No new static C# events.
4. **Input** — `PlayerInput` (or equivalent) raycasts on Select Target. Context table above. Catalog entry as in Input. Gameplay asks the input service by action id, not `Mouse.current.rightButton`.
5. **Skills** — Snap / smart-target path (`SkillEffectTargetSmart`, snap indicator, preview session) reads the local target and applies the priority list. Cast still goes through existing `CmdUseSkill` + server `GetTargets`. Prefer sending enough aim/target identity that the server resolves **that** unit, then **validates**. Do not trust the client for team/range/life.
6. **Presentation** — Target frame is UI Toolkit on the existing Player HUD document / presenter. Reuse nameplate buff + cast drivers / `CastBar` where it stays small. World circle is presentation (decal / projector), **reads the SO**, read-only toward game state.

**Reuse:** `ITeamColorService`, nameplate buff/cast binding, player vital bar USS, `PlayerActionFeedback` out-of-range, `UnitHighlighter` raycast rules.

## Test plan

- RMB enemy / ally / self / corpse → frame + circle + nameplate. RMB another unit replaces.
- RMB empty, no preview → frame hides, circle gone.
- Open preview, RMB empty → preview closes, **target remains**.
- Open preview, RMB a different unit → selection and preview follow the new unit; skill does not fire until confirm.
- With enemy A selected, hover enemy B, press a damage skill → hits **A**. Highlight on **A**.
- A selected and out of range → preview on A, confirm → out-of-range error, preview stays, A still selected.
- Ally selected, press an enemy-only skill → mouse snap (A stays in the HUD).
- Hold Alt → self-cast even with someone else selected.
- Ground skill → still at mouse / Alt at self; selection unchanged.
- Target dies → frame at 0 HP until empty-click or new select. Res skill can still use them if the skill allows Dead.
- Target despawns → frame clears.
- While dead: RMB unit selects; RMB empty spectates next.
- Shield-less zombie: no shield row. Team colors on ally vs enemy.
- Buffs vs debuffs split; tooltip on hover.
- Pointer over Settings / vendor: RMB does not select through UI.
- Loadout button still clickable; target frame does not cover it.
- Changing the selection-circle ScriptableObject (radius, color, texture) changes the ring without a code change.