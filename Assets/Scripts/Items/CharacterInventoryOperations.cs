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

            var mainHand = ResolveMainHandWeaponType(character, catalog);
            var offHand = ResolveOffHandWeaponType(character, catalog);
            var itemWeapon = definition.weaponData != null
                ? (WeaponType?)definition.weaponData.weaponType
                : null;
            var projectedMain = slot == ItemSlot.MainHand ? itemWeapon : mainHand;
            var projectedOff = slot == ItemSlot.OffHand ? itemWeapon : offHand;

            if (definition.kind != ItemKind.Equipment
                || !ItemRules.CanEquipToSlot(definition, slot, projectedMain, projectedOff))
                return false;

            if (!ItemRules.CanEquipWithWeapon(definition, projectedMain))
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

            if (slot == ItemSlot.MainHand)
            {
                UnequipIncompatibleOffHand(character, catalog);
                UnequipIncompatibleArmor(character, catalog);
            }

            return true;
        }

        public static bool TryAutoEquipFirstMainHandWeapon(CharacterSaveData character, IItemCatalog catalog)
        {
            if (character == null || catalog == null)
                return false;

            CharacterInventory.EnsureLists(character);
            if (CharacterInventory.TryGetEquipped(character, ItemSlot.MainHand, out var equipped)
                && !string.IsNullOrWhiteSpace(equipped.instanceId))
            {
                return false;
            }

            for (var i = 0; i < character.InventoryEquipment.Count; i++)
            {
                var entry = character.InventoryEquipment[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.instanceId))
                    continue;
                if (!catalog.TryGet(entry.itemId, out var definition) || definition.weaponData == null)
                    continue;
                if (!ItemRules.CanEquipToSlot(definition, ItemSlot.MainHand, definition.weaponData.weaponType, null))
                    continue;
                return TryEquip(character, catalog, entry.instanceId, ItemSlot.MainHand);
            }

            return false;
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
            return ResolveEquippedWeaponType(character, catalog, ItemSlot.MainHand);
        }

        public static WeaponType? ResolveOffHandWeaponType(CharacterSaveData character, IItemCatalog catalog)
        {
            return ResolveEquippedWeaponType(character, catalog, ItemSlot.OffHand);
        }

        private static WeaponType? ResolveEquippedWeaponType(
            CharacterSaveData character,
            IItemCatalog catalog,
            ItemSlot slot)
        {
            if (!CharacterInventory.TryGetEquipped(character, slot, out var entry)
                || string.IsNullOrWhiteSpace(entry.itemId))
                return null;

            if (catalog == null || !catalog.TryGet(entry.itemId, out var definition) || definition.weaponData == null)
                return null;

            return definition.weaponData.weaponType;
        }

        private static void UnequipIncompatibleOffHand(CharacterSaveData character, IItemCatalog catalog)
        {
            if (!CharacterInventory.TryGetEquipped(character, ItemSlot.OffHand, out var offHandEntry)
                || string.IsNullOrWhiteSpace(offHandEntry.itemId)
                || catalog == null
                || !catalog.TryGet(offHandEntry.itemId, out var offHandDefinition))
                return;

            var mainHand = ResolveMainHandWeaponType(character, catalog);
            var offHand = ResolveOffHandWeaponType(character, catalog);
            if (ItemRules.CanEquipToSlot(offHandDefinition, ItemSlot.OffHand, mainHand, offHand))
                return;

            TryUnequip(character, ItemSlot.OffHand);
        }

        private static void UnequipIncompatibleArmor(CharacterSaveData character, IItemCatalog catalog)
        {
            if (character == null || catalog == null)
                return;

            var mainHand = ResolveMainHandWeaponType(character, catalog);
            if (!mainHand.HasValue)
                return;

            for (var slot = ItemSlot.Head; slot <= ItemSlot.Gloves; slot++)
            {
                if (!ItemRules.EnforcesArmorWeight(slot))
                    continue;

                if (!CharacterInventory.TryGetEquipped(character, slot, out var entry)
                    || string.IsNullOrWhiteSpace(entry.itemId)
                    || !catalog.TryGet(entry.itemId, out var definition))
                    continue;

                if (ItemRules.CanEquipWithWeapon(definition, mainHand))
                    continue;

                TryUnequip(character, slot);
            }
        }
    }
}
