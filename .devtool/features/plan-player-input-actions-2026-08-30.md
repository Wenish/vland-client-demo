---
id: "plan-player-input-actions-2026-08-30"
status: "review"
priority: "medium"
assignee: null
epic: null
dueDate: null
created: "2026-08-29T23:16:00.000Z"
modified: "2026-08-30T02:09:34.166Z"
completedAt: null
labels: ["Usability"]
order: "a3"
---
# Plan player key bindings

Inventory of every current key / mouse / gamepad bind, so we can plan a real binding map.

Bindings are hardcoded in scripts. No Input Action asset, no remapping in Settings.

## Should

- Settings tab **KEYBINDINGS** (same style as GENERAL / GRAPHICS / AUDIO).
- Layout and remap flow like well-known games (click slot → press key → done), see below.
- One binding table for player keys, separate table for admin / cheat keys.
- Resolve conflicts and decide what stays bound.
- Follow project architecture so a new bind is one catalog entry — not another hardcoded key in a gameplay script. See Architecture below.
- Per skill slot (Skill 1 / 2 / 3 / Ultimate), a **Quick Cast** toggle. New default (no player settings yet): skill key = **preview**, **Cast Modifier** (today `Shift`) + skill key = **quick cast**. See Quick Cast below.
- **Self Target** and **Cast** binds are **modifier keys** (held with a skill key). Settings names must say Modifier. See Modifiers below.
- **Gamepad is not in this ticket.** Catalog / service still has a gamepad slot so it can be added later without a redesign. KEYBINDINGS UI is keyboard/mouse only: **Key** + **Secondary**.
- **Keep today’s combat input priority** (preview over ping, etc.). Remapping keys must not change those rules. See Existing gameplay rules below.
- **Cancel Cast** vs **Interrupt** are different: RMB (Cancel Cast) only aborts **preview**. Interrupt (`H`) cancels a skill that is **already casting**.

## Settings tab: KEYBINDINGS

Row labels as they would appear in Settings (short Title Case, like Nickname / Master / Fullscreen). Each action has **Key** (primary) and **Secondary** (optional extra bind). No Gamepad column in the UI yet.

**Movement**
- Move Forward
- Move Backward
- Move Left
- Move Right

**Combat**
- Attack
- Skill 1
- Skill 2
- Skill 3
- Ultimate
- Cancel Cast (preview only)
- Interrupt (active cast)
- Ping

**Modifiers** (held with a skill key; do nothing on their own)
- Self Target Modifier
- Cast Modifier

**World**
- Interact

**Camera**
- Camera Follow
- Camera Fixed
- Spectate Previous
- Spectate Next

**Interface**
- Loadout
- Leaderboard
- Vendor Tabs
- Menu

Not remappable in this tab (always mouse): Face / Aim, Zoom, edge-pan.

### Layout

Stay inside the existing Settings panel (tab bar + scroll + RESET / BACK). Page is a scrollable bind list, not a grid of random buttons.

```
KEYBINDINGS
Click a key, then press a new one. Right-click to clear. Esc to cancel.

          Action              Key      Secondary
────────────────────────────────────────────────────────
MOVEMENT
Move Forward                  W        ↑
Move Backward                 S        ↓
Move Left                     A        ←
Move Right                    D        →

COMBAT
Attack                        LMB      Space
Skill 1                       Q        —                  ☐ Quick Cast
Skill 2                       E        —                  ☐ Quick Cast
Skill 3                       C        —                  ☐ Quick Cast
Ultimate                      X        —                  ☐ Quick Cast
Cancel Cast                   RMB      —
Interrupt                     H        —

MODIFIERS
Self Target Modifier          LAlt     —
Cast Modifier                 Shift    —
...
```

- Sticky header row: **Action | Key | Secondary**. Action name left, two bind slots right. Empty slot shows `—`. **Secondary** is the optional extra bind (today Attack is LMB + Space). Do **not** label this column Alt — that clashes with Self Target Modifier (`Left Alt`).
- No **Gamepad** column in the UI. Each catalog entry still has a gamepad bind field (empty) so a later ticket can show a third column without changing the data shape.
- Section headers (`MOVEMENT`, `COMBAT`, `MODIFIERS`, `WORLD`, `CAMERA`, `INTERFACE`) as quiet labels, same grouping as the name list above.
- One row per action, same height, hover highlights the whole row. Bind slots look like compact keycaps (same gold border language as other Settings fields).
- One listening session at a time (only one slot is waiting).
- Skill 1 / 2 / 3 / Ultimate rows also have a **Quick Cast** checkbox on the right. Other rows do not.
- Modifier rows (`Self Target Modifier`, `Cast Modifier`) are hold-keys: the slot shows `Shift` / `Left Alt`, not a tap action.
- Short hint under the tab, always visible while this tab is open — no extra help screen.
- RESET on this tab restores default binds **and** Quick Cast toggles (not graphics/audio). BACK / overlay close work as they do on other Settings tabs.

### Modifiers

Self Target and the preview / quick-cast flip are **not** standalone actions. They are modifier keys: **hold + skill key**. Pressing them alone does nothing. Settings labels must include **Modifier** so that is obvious.

| Settings name | Default | Held with a skill key |
| --- | --- | --- |
| **Self Target Modifier** | `Left Alt` | Self-cast / place the spell on the caster |
| **Cast Modifier** | `Shift` | Invert that slot’s Quick Cast mode (preview ↔ quick) |

- Remap still works as click slot → press the modifier (`Shift`, `Left Alt`, `Left Ctrl`, …). The player binds the modifier itself, not a chord.
- Gameplay reads them as **held** (`IsHeld`), not as a one-frame press.
- Catalog flag `isModifier` so the KEYBINDINGS list can group them and the service treats them as hold-keys.

### Quick Cast (per skill slot)

Today (hardcoded, not in Settings): skill key = quick cast, `Shift` + skill key = preview.

**Should:** each of Skill 1, Skill 2, Skill 3, Ultimate has its own **Quick Cast** checkbox on that row in KEYBINDINGS (same pattern as League — bind + mode on one line).

| Quick Cast | Skill key | **Cast Modifier** (`Shift` by default) + skill key |
| --- | --- | --- |
| Off (**default**) | Preview, then confirm | Quick cast |
| On | Quick cast | Preview, then confirm |

- Default for all four slots: **Off** — first time / after RESET, only pressing the skill key opens preview; Shift + skill key quick-casts.
- Toggles are independent (e.g. Ultimate quick-cast on, Skill 1 still preview).
- **Cast Modifier** inverts that slot’s mode for one press. **Self Target Modifier** can be held at the same time (preview on self, or quick-cast on self).
- Skills with no preview indicator still cast immediately either way (same as today).
- Changes apply immediately, same as remaps. Persist with the input service.

Covers `default-behavior-for-casting-skills-2026-08-27` plus per-slot control.

### Cancel Cast vs Interrupt

Two different actions. Do not merge them.

| Settings name | Default | When it does something |
| --- | --- | --- |
| **Cancel Cast** | Right mouse | Only while a **preview** is up: close preview, do not cast. Does nothing to a skill that is already casting. |
| **Interrupt** | `H` | Cancels the skill that is **already casting** (windup / channel). Today it also drops preview if one is open — keep that so the player is not stuck in preview. |

### Existing gameplay rules (keep)

Remapping must not change these. Same priority as `PlayerInput` today.

- **Preview wins over ping and attack.** While preview is up: Left mouse **confirms** the skill (Self Target Modifier may still be held). Do **not** ping. Do **not** start a basic attack.
- **Outside preview:** Self Target Modifier / Alt + Left mouse is **ping**, not attack.
- **Cancel Cast** (RMB) only aborts preview. It does not interrupt a live cast.
- **Same skill key again** while that slot’s preview is up confirms the cast (same as today).
- **Self Target Modifier** still applies during preview and on quick-cast (self / place at caster).
- Skills with **no preview indicator** still cast immediately either way.
- Pointer over blocking UI: ignore **mouse** attack; keyboard binds still work.
- Modal UI (`UiModalInputBlock`): gameplay input stays cancelled.
- Spectate LMB/RMB only when dead; they must not steal Cancel Cast / Attack while you are alive and in preview.

### Remap flow

Same pattern as Overwatch / Valorant / most PC action games. No Apply button — the new key is live as soon as it is accepted.

1. Player opens Settings → **KEYBINDINGS**.
2. Clicks (or activates) one bind slot. That slot enters listening: highlight + label **Press a key**. Other slots are inert until this finishes.
3. Next keyboard key or mouse button becomes the bind. Slot shows the new key and listening ends. (No gamepad capture until gamepad is implemented.)
4. **Escape** while listening **cancels** and keeps the old bind. Does not open/close the menu. (Menu is remapped by clicking the Menu slot, then pressing a key that is not Escape.)
5. **Right-click** a slot **clears** it (`—`). Cannot clear the last remaining bind for Move Forward/Back/Left/Right or Attack — those must keep at least one key so the player is never stuck.
6. If the new key is already used by another action: small prompt, not a full-screen modal.
   - *“[Key] is bound to [Other Action]. Swap keys?”*
   - **Swap** — this slot takes the key, the other action gets this slot’s old key (or empty if there was none).
   - **Cancel** — close the prompt, keep old binds, stop listening.
7. Duplicate on the **same** action (Key and Secondary both `Q`) is rejected: slot flashes, stays listening or reverts; no prompt.
8. Listening also ends if the player clicks elsewhere, changes tab, or closes Settings — old bind kept.

No extra “save keybinds” step. Conflicts are resolved in that one prompt so the player always knows what every key does.

### Architecture

Follow `.cursor/rules/project-architecture.mdc`. New code in `ShadowInfection.Input` (not legacy root namespace). No new singleton. No `FindObjectOfType` from UI.

**Layers**

1. **Data** — Player action catalog (ScriptableObject or equivalent list of definitions). Each entry: id, Settings label, group (`MOVEMENT` / `COMBAT` / `MODIFIERS` / …), default **Key** / **Secondary**, unused **Gamepad** slot (for later), flags (required, show in Settings, **`isModifier`** for hold-with-skill keys). Skill slot entries also carry a **Quick Cast** flag (default off). No gameplay logic in the asset. Cheat / debug actions live in a **separate** catalog so they never appear in KEYBINDINGS.
2. **Service** — `IInputBindings` (name can change): held/pressed/released by action id, remap, swap, persist, reset-to-defaults, Quick Cast get/set per skill slot, `TryGetSnapshot` for UI. Conflict rules live here, not in the Settings view. Register in `GameLifetimeScope` (global, like other app settings). Constructor / `[Inject]` — not a static manager. Gamepad reads are stubbed / unused until a later ticket; do not wire `Gamepad.current` into the new remap UI now.
3. **Gameplay** — `PlayerInput`, camera, interact, HUD presenters ask the service (`WasPressed(Skill1)`, `IsHeld(SelfTargetModifier)`, `IsQuickCast(Skill1)`), they do **not** read `Keyboard.current.qKey` or a hardcoded Shift / Alt rule. **Keep the existing priority rules** (preview over ping, etc.) in that gameplay path — only the *source* of the bind changes. When those files are touched, migrate binds locally; do not rewrite the whole input surface in one go.
4. **Presentation** — KEYBINDINGS tab is UI Toolkit only. It **iterates the catalog** and binds to the snapshot (same idea as match UI `TryGetSnapshot`). Remap / clear / reset / Quick Cast toggle go through the service. No hardcoded row list in the view. Existing Settings `*LifetimeScope` / binder pattern — extend, don’t add a parallel settings stack.

**Adding a keybind later** should be:

1. Add one catalog entry (id, label, group, defaults, flags).
2. Call the service from the feature that needs it.
3. Settings tab shows the row by itself.

No new branch in a central switch, no extra Settings UXML row by hand, no cheat keys mixed into the player catalog.

Admin / cheat keys do **not** show here. If they ever get names in a debug panel:

- Pause Waves
- Damage All
- Heal All
- Open Gates
- Close Gates
- Debug Scenes
- Show FPS
- Show DPS

---

## Player key bindings (current)

| Action | Binding |
| --- | --- |
| Move | `W` `A` `S` `D` or Arrow keys |
| Move (gamepad) | Left stick |
| Face / aim | Mouse position |
| Basic attack (hold) | Left mouse **or** `Space` **or** Gamepad right trigger |
| Skill 1 | `Q` |
| Skill 2 | `E` |
| Skill 3 | `C` |
| Ultimate | `X` |
| Preview vs quick (all slots, hardcoded) | Skill key = **quick cast**. **Cast Modifier** `Shift` + skill key = **preview**. |
| Confirm preview | Left mouse **or** same skill key again (while preview is up) |
| Cancel Cast (preview only) | Right mouse |
| Interrupt (active cast) | `H` (also drops preview if one is up) |
| Self Target Modifier (hold) | `Left Alt` + skill key (or + confirm while previewing) |
| World ping | `Alt` (left or right) + Left mouse |
| Interact (vendor, gate) | `F` |
| Toggle camera follow | `Z` |
| Toggle fixed follow (no look-ahead) | Middle mouse |
| Zoom | Mouse wheel |
| Pan camera (follow off) | Mouse at screen edges |
| Spectate previous / next | Left click / Right click (only while dead) |
| Toggle loadout (lobby only) | `I` |
| Toggle zombie leaderboard | `Tab` |
| Cycle vendor tabs (vendor open) | `Tab` |
| Close vendor / settings / menu | `Escape` |

Gamepad: left stick + right trigger exist in code today. **Out of scope** for KEYBINDINGS this ticket. Keep current gamepad behavior as-is until a later ticket fills the catalog gamepad slot and shows the column.

---

## Admin / cheat key bindings (current)

Server/host keyboard only unless noted. Not gated by an admin role.

| Action | Binding |
| --- | --- |
| Pause / unpause zombie waves | `P` |
| Damage all units (20) | `O` |
| Heal + full shield all units | `L` |
| Open all gates | `N` |
| Close all gates | `M` |
| Toggle map-switch debug window | `F4` (host/server) |
| Toggle FPS overlay | `F2` |
| Toggle DPS overlay | `F3` |

---

## Conflicts / notes

- `Tab` is both leaderboard and vendor tabs.
- `Alt` is both world ping (either Alt + click) and Self Target Modifier (`Left Alt` only). Keep today’s split: preview uses Left Alt as self-target and **does not ping**.
- Cheat keys `O` `L` `P` `N` `M` sit on the same keyboard as gameplay.
- `Z` is camera lock; QWERTZ `Y` is not bound.