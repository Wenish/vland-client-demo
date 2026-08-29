---
id: "shared-gold-for-killing-zombies-2026-08-28"
status: "done"
priority: "medium"
assignee: null
epic: null
dueDate: null
created: "2026-08-27T23:29:11.227Z"
modified: "2026-08-29T22:20:00.000Z"
completedAt: "2026-08-29T22:20:00.000Z"
labels: []
order: "Zv"
---
# Shared Gold drops for killing zombies

Zombie mode scales infinitely per wave, so everyone needs gold for upgrades. Last-hit-only gold starved tanks and healers.

## Shipped

**Kill gold:** each **alive** player gets the full drop (default 10), not a split. Dead players get nothing (no mid-run respawn; wipe = game over). Last-hit is not required for gold; a zombie dying with a null/non-player killer still pays alive players. Leaderboard **kills** stay last-hit only.

**Wave payouts** (after the last zombie dies, before the between-waves shop gap). Wipe mid-wave pays nothing.

- Payday: `40 + 10 * wave` (wave 1 = 50)
- Survival: extra `25 + 5 * wave` if nobody died that wave
- Special bounty: extra 50 on **recurring** specials (e.g. Werwolfs), not exact wave overrides

All three use the same “alive players get the full amount” rule as kills. Amounts are on `ZombieModeConfig` Gold settings.

Logic lives in `ZombieGoldService`. Kill popup shows for the local alive player at the corpse. Wave grants show `+N` on the local unit.

## Out of scope (not this ticket)

Wave-scaled kill gold, heal/tank ticks, catch-up equalization.
