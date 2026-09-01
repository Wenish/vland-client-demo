using ShadowInfection.Items;

namespace ShadowInfection.Items
{
    public static class CharacterInventoryOperations
    {
        public static bool TryEquip(
            CharacterSaveData character,
            IItemCatalog catalog,
            string instanceId,
            ItemSlot slot)
        {
            if (character == null || catalog == null || string.IsNullOrWhiteSpace(instanceId))
                return false;

            CharacterInventory.EnsureLists(character);
            if (!CharacterInventory.TryFindBagEntry(character, instanceId, out var bagEntry))
                return false;
            if (!catalog.TryGet(bagEntry.itemId, out var definition))
                return false;
            if (definition.kind != ItemKind.Equipment || definition.slot != slot)
                return false;
            if (!ItemRules.CanEquipWithWeapon(definition, ResolveMainHandWeaponType(character, catalog)))
                return false;

            if (CharacterInventory.TryGetEquipped(character, slot, out var incumbent)
                && !string.IsNullOrWhiteSpace(incumbent.instanceId))
            {
                character.InventoryEquipment.Add(new InventoryEntry
                {
                    instanceId = incumbent.instanceId,
                    itemId = incumbent.itemId
                });
                CharacterInventory.RemoveEquipped(character, slot);
            }

            if (!CharacterInventory.TryRemoveBagEntry(character, instanceId))
                return false;

            CharacterInventory.SetEquipped(character, slot, instanceId, bagEntry.itemId);
            return true;
        }

        public static bool TryUnequip(CharacterSaveData character, ItemSlot slot)
        {
            if (character == null)
                return false;

            CharacterInventory.EnsureLists(character);
            if (!CharacterInventory.TryGetEquipped(character, slot, out var entry)
                || string.IsNullOrWhiteSpace(entry.instanceId))
                return false;

            character.InventoryEquipment.Add(new InventoryEntry
            {
                instanceId = entry.instanceId,
                itemId = entry.itemId
            });
            CharacterInventory.RemoveEquipped(character, slot);
            return true;
        }

        public static WeaponType? ResolveMainHandWeaponType(CharacterSaveData character, IItemCatalog catalog)
        {
            if (!CharacterInventory.TryGetEquipped(character, ItemSlot.MainHand, out var entry)
                || string.IsNullOrWhiteSpace(entry.itemId))
                return null;

            if (!catalog.TryGet(entry.itemId, out var definition) || definition.weaponData == null)
                return null;

            return definition.weaponData.weaponType;
        }
    }
}
