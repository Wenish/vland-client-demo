# Items

Catalog + character bag. Combat, wear, and loot are later tickets.

## Layers

- **Data** — `ItemDefinition` ScriptableObjects in `ItemDatabase` (`IGameDatabases.Items`). One type, no subclasses. `ItemKind` chooses bag behavior.
- **Persist** — `CharacterSaveData.InventoryEquipment` (`instanceId` + `itemId`) and `InventoryStacks` (`itemId` + `count`). Saved with the roster in `CharacterManager` PlayerPrefs.
- **Services** — `IItemCatalog` (lookup) and `IItemInventory` (active character grant/destroy). Registered on `GameLifetimeScope`.
- **UI** — Inventory window is a sibling of loadout. Debug lobby stall reuses `VendorWindow` with `VendorCatalogSource.ItemDatabase` and grants locally (do not `EquipWeapon`).

## Rules

- Equipment grant always adds a new row. Gems and materials increment a stack.
- `TryGrantItem` / destroy are the only mutation path. Future loot tables call the same methods.
- Do not put bags on `UnitController`. Wear later reads this bag; it does not replace it.
- `ItemRules.ArmorWeightFor` and `SocketsPerPiece` exist for the wear ticket. Unused in combat now.

## Save shape later

Sockets hang off `instanceId`, not `itemId`. Equipped paper-doll fields stay off this ticket (`ArmorSlotIds` is still an unused stub).
