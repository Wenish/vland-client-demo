---
id: "window-close-keys-2026-09-02"
status: "done"
priority: "medium"
assignee: null
epic: null
dueDate: null
created: "2026-09-02T01:36:00.000Z"
modified: "2026-09-02T01:45:00.000Z"
completedAt: "2026-09-02T01:45:00.000Z"
labels: ["Usability"]
order: "Zy"
---
# Window close keys and ESC-before-menu

Player windows toggle shut on the **same remappable action** that opened them. **ESC** (Menu) dismisses those windows **before** the in-game menu opens.

## Feel

WoW-style: `B` / `U` / `I` / Interact are on/off for bag, character, loadout, and vendor. Escape never jumps straight to Game Menu if a window is still up. One Escape clears **all** player windows; the next press opens the menu.

## Should

- Each player window **closes** on the same catalog action that opens it (defaults below).
- **ESC** (`PlayerActionId.Menu`, default Escape), in order:
  1. If a text field is focused (inventory search), **blur only**. Do not close windows or open the menu on that press (`UiTextInputFocus` already does this).
  2. If any player window is open, **close all of them** on one press. Do **not** open the menu on that press.
  3. If the settings overlay is open, close settings only.
  4. Else toggle the in-game menu (close if visible, otherwise open).
- Opening the in-game menu also closes leftover player windows.
- New player windows register with `IUiOverlayRegistry` so ESC does not grow a special-case list in the menu presenter.

## Windows in this ticket

| Window | Open action | Default |
| --- | --- | --- |
| Inventory | Inventory | `B` |
| Character | Character | `U` |
| Loadout | Loadout | `I` (lobby only, same as today) |
| Vendor | Interact | `F` — press again on the **same** vendor to close |

## Out

- **Leaderboard** — Tab overlay, not a window. ESC does not dismiss it.
- **Host Admin** overlay.
- Main-menu / character-select screens.
- HUD Loadout **button** (keybind toggle only).
- Inventory search typing: while search is focused, `B` types; next ESC blurs, then the next ESC closes windows.

## Input / architecture

- Keep existing `TickToggle` on Inventory / Character / Loadout.
- Vendor toggle: `Open` of an already-open **same** session closes instead of re-opening. Lobby and match both go through `OpenVendorWindowEvent`.
- ESC has **one owner**: `InGameMenuPresenter.TickEscape`. It calls `IUiOverlayRegistry.TryCloseAll()` before settings / menu so overlay close and menu cannot both fire on the same press.
- `IUiOverlay` + `IUiOverlayRegistry` on `GameLifetimeScope`. Presenters register on Bind, unregister on Unbind. `Close()` on the presenter (persist / events unchanged).
