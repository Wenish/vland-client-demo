# Room Lobby UI (UI Toolkit) — Quick Setup

This project includes a lightweight Room Lobby UI implemented with Unity UI Toolkit that integrates with Mirror’s built-in room system.

It:
- Observes `NetworkRoomManager.roomSlots` to display the current room player list.
- Shows each player’s `readyToBegin` state.
- Lets the **local** player toggle ready using Mirror’s supported API: `NetworkRoomPlayer.CmdChangeReadyState(bool)`.

It **does not** modify or subclass Mirror’s `NetworkRoomManager` or `NetworkRoomPlayer`, and does not change any networking/gameplay logic.

---

## Files

UI assets:
- `Assets/Resources/Ui/RoomLobby/RoomLobby.uxml`
- `Assets/Resources/Ui/RoomLobby/RoomLobby.uss`

Code:
- `Assets/Ui/Components/RoomLobby/RoomLobbyController.cs`
- `Assets/Ui/Components/RoomLobby/RoomLobbyView.cs`
- `Assets/Ui/Components/RoomLobby/MirrorRoomLobbyPresenter.cs`

---

## Quick Setup (Room Scene)

1. Open your **Room Scene** (the scene used by your `NetworkRoomManager` as `RoomScene`).
2. Create an empty GameObject named `RoomLobbyUI`.
3. Add component: `RoomLobbyController`.

That’s it. The UI will:
- Create a `UIDocument` automatically if the GameObject doesn’t have one.
- Reuse a `PanelSettings` from any other `UIDocument` already present in the scene.
- Otherwise, create a fallback `PanelSettings` at runtime.

### Recommended (optional, but best practice)
If you already have a shared UI Toolkit Panel Settings asset in the project:
- Add a `UIDocument` component yourself and assign the existing Panel Settings asset (e.g. `Assets/Ui/UI Toolkit/PanelSettings.asset`).

This keeps UI scaling/input consistent across scenes and avoids creating a runtime-only `PanelSettings`.

---

## How It Works (Mirror integration)

- Player list source: `((NetworkRoomManager)NetworkManager.singleton).roomSlots`.
- Ready state: `NetworkRoomPlayer.readyToBegin`.
- Toggle ready (client authoritative request, server authoritative state): `NetworkRoomPlayer.CmdChangeReadyState(!readyToBegin)`.

The UI hides itself automatically when Mirror’s `RoomScene` is not the active scene.

---

## Troubleshooting

**UI doesn’t appear**
- Confirm you’re actually in the Room Scene (the UI hides itself outside the active Room Scene).
- Confirm there is a `NetworkRoomManager` in the scene (or set as `NetworkManager.singleton`).

**Ready button is disabled**
- You must be a connected client and have a local room player (`NetworkClient.active` and `NetworkClient.localPlayer != null`).

**Player names are generic**
- The UI defaults to `Player {index+1}`.
- It tries (non-invasively) to find a string field/property on any component on the room player with common names like `displayName`, `playerName`, `username`, etc.

---

## Tweaks

- Refresh rate: in `RoomLobbyController`, adjust `refreshIntervalSeconds` (default `0.2`).
- Styling: edit `Assets/Resources/Ui/RoomLobby/RoomLobby.uss`.

---

## Removing

To remove the UI:
- Delete/disable the `RoomLobbyUI` GameObject in the room scene.

No other systems are affected.
