---
id: "humanoid-weapon-animation-refactor-2026-09-04"
status: "in-progress"
priority: "high"
assignee: null
epic: null
dueDate: null
created: "2026-09-04T06:40:00.000Z"
modified: "2026-09-04T06:40:00.000Z"
completedAt: null
labels: []
order: "Zz"
---
# Finish humanoid weapon animation refactor

Shared `Humanoid.controller` + `Stance` instead of swapping a controller per weapon. **Attacks currently play for no weapon.** Finish the runtime path, then delete leftover animation-set assets/code so the old per-weapon override system is gone.

## Goal (after this ticket)

Humanoid units keep **one** animator graph for their whole life. Equipping a weapon only changes **stance**, never `runtimeAnimatorController`.

| Piece | Target |
| --- | --- |
| Graph | `Assets/Units/Animations/Humanoid.controller` |
| Bind | Once from `ModelData.defaultAnimationSet` |
| Weapon change | `Stance` (int = `WeaponType`) + `StanceBlend` (same value, for 1D loco blend) |
| Auto-attack | AnyState: `Attack` trigger + `AttackVersion` 0 main / 1 off + matching `Stance` + `Health > 0` |
| Split hands | Unarmed, Sword, Daggers, Pistols — version 0 = main, 1 = off |
| Two-hand / ranged | Shared pool, still version 0/1 from `attackIndex % 2` |
| Skills | Cast layer (`Cast` / `CastEnd` / `IsCasting`) — not the Attack trigger |
| Unit-specific clips | Sparse `AnimatorOverrideController` of Humanoid only (zombie, crawler) |

Do **not** go back to `ModelData.weaponAnimationOverrides` or swapping `Sword.overrideController` / `Bow.overrideController` onto the Animator at runtime.

Clip meaning (easy to get wrong):

- `WeaponType.Sword` = **one-hand sword** clips (historically on `SwordAndShield.overrideController`).
- `WeaponType.TwoHandSword` = **two-hand** pack (`Sword.overrideController` / `2Hand-Sword-*`).
- No `WeaponType.SwordAndShield`. Shield is `WeaponType.Shield` and does not swing.

Runtime drive stays the old Unarmed pattern — **no** `Animator.Play("Daggers_Main_0")` / generated state names. Extra random attack variants are **out of scope** until the default two clips per stance play.

## Current state (broken)

Auto-attack **never** plays an attack clip, for any weapon. Combat still hits; the Attack layer stays on `None`.

What went wrong: extra-clip work added `AttackHand`, nested stance state machines, and `Animator.Play` by name. Unity logged `Animator.GotoState: State could not be found`. Code was reverted to `SetTrigger("Attack")` + `AttackVersion = attackIndex % 2`, but the swing still does not play.

Likely remaining causes to check (not assumed fixed):

- Attack-layer AnyState conditions still not matching runtime (`Stance`, `Health` default 0, leftover `AttackHand`).
- Animator not actually on `Humanoid.controller` (prefab or `AnimationSetData` still pointing at an old override).
- `Stance` not set / wrong `WeaponType` vs baked transitions.
- Unity not reimporting the YAML controller after edits — rebuild via `Tools/Animation/Rebuild Humanoid Controller`.

## Clean assets + code (no leftover animation-set path)

After attacks work, the repo should not still look like the old “one AnimationSet per weapon” system.

**Keep (runtime)**

- `Humanoid.controller`
- `HumanoidUnarmed.asset` → Humanoid (player / ninja / shadow warrior default)
- `HumanoidZombieUnarmed.asset` / `HumanoidZombieCrawlerUnarmed.asset` → sparse Humanoid overrides
- `UnitAnimationController` bind-once + stance
- `HumanoidAnimatorBuilder` as the way to regenerate the graph

**Delete or stop assigning (unused duplicates)**

Per-weapon `AnimationSetData` assets that only exist to point at a controller — they are unused (`ModelData` has no weapon override list anymore):

- `HumanoidSword.asset`, `HumanoidTwoHandSword.asset`
- `HumanoidDaggers.asset`, `HumanoidBow.asset`, `HumanoidGun.asset`, `HumanoidPistols.asset`

Some of these still point at **old** override controllers (e.g. `HumanoidBow` → `Bow.overrideController`), which is exactly the leftover to remove.

**Build-time clip sources vs runtime**

Override controllers (`Daggers`, `Bow`, `Gun`, `Pistols`, `Sword`, `SwordAndShield`, `TwoHandSword` if present) and `Unarmed.controller` are **clip catalogues for the builder**, not something a living unit should wear. When the Humanoid graph is the source of truth:

- Prefabs / `AnimationSetData` must not reference them at runtime.
- If clips are fully baked into Humanoid, decide whether those overrides stay as editor-only sources or get deleted. Do not leave both “assigned on models” and “baked in Humanoid”.
- `SwordAndShield.overrideController` is **not** a weapon type — one-hand sword clip source only. Do not bring `WeaponType.SwordAndShield` back.

**Code leftovers to drop**

- Unused `AttackHand` parameter / conditions
- `Animator.Play` by generated attack state name
- Dead `HumanoidAttackVariants.PickRandom` path until extras are a real follow-up
- Any remaining `weaponAnimationOverrides` / per-weapon controller swap

Empty `HumanoidAttackClips.asset` can stay as an optional extras hook, or be removed if it stays unused. Do not let it drive runtime until default attacks work.

## Out of scope

- Re-adding `WeaponType.SwordAndShield` ([separate leftover ticket](remove-legacy-sword-and-shield-weapon-type-2026-09-04.md) — enum is already gone)
- Random extra attack clips / `AttackHand`
- Dual-wield remapping Sword+Sword → Daggers stance
- New animation content (staff/shield can keep placeholders as long as **some** attack state plays)

## Done when

- Auto-attack plays the correct clip for every `WeaponType` (main and off where split hands apply).
- Weapon swap changes loco + attack stance without swapping the controller.
- Player models use Humanoid; zombies keep sparse overrides of Humanoid.
- No unused per-weapon `AnimationSetData`; no unit/prefab still assigned an old weapon override as its runtime controller.
- No `GotoState: State could not be found` on attack.
- `Tools/Animation/Rebuild Humanoid Controller` can regenerate the graph without bringing the dead path back.
