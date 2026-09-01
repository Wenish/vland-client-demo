using System;
using System.Collections.Generic;
using ShadowInfection.Items;

namespace ShadowInfection.Items
{
    [Serializable]
    public struct CharacterInventorySnapshot
    {
        public string characterId;
        public InventoryEntry[] inventoryEquipment;
        public EquippedSlotEntry[] equippedSlots;
        public ItemStack[] inventoryStacks;

        public static CharacterInventorySnapshot From(CharacterSaveData character)
        {
            if (character == null)
                return default;

            CharacterInventory.EnsureLists(character);
            return new CharacterInventorySnapshot
            {
                characterId = character.Id ?? string.Empty,
                inventoryEquipment = character.InventoryEquipment != null
                    ? character.InventoryEquipment.ToArray()
                    : Array.Empty<InventoryEntry>(),
                equippedSlots = character.EquippedSlots != null
                    ? character.EquippedSlots.ToArray()
                    : Array.Empty<EquippedSlotEntry>(),
                inventoryStacks = character.InventoryStacks != null
                    ? character.InventoryStacks.ToArray()
                    : Array.Empty<ItemStack>()
            };
        }

        public static void ApplyTo(CharacterSaveData character, CharacterInventorySnapshot snapshot)
        {
            if (character == null)
                return;

            CharacterInventory.EnsureLists(character);
            character.InventoryEquipment = snapshot.inventoryEquipment != null
                ? new System.Collections.Generic.List<InventoryEntry>(snapshot.inventoryEquipment)
                : new System.Collections.Generic.List<InventoryEntry>();
            character.EquippedSlots = snapshot.equippedSlots != null
                ? new System.Collections.Generic.List<EquippedSlotEntry>(snapshot.equippedSlots)
                : new System.Collections.Generic.List<EquippedSlotEntry>();
            character.InventoryStacks = snapshot.inventoryStacks != null
                ? new System.Collections.Generic.List<ItemStack>(snapshot.inventoryStacks)
                : new System.Collections.Generic.List<ItemStack>();
        }

        public static CharacterSaveData ToCharacterData(CharacterInventorySnapshot snapshot)
        {
            var character = new CharacterSaveData { Id = snapshot.characterId ?? string.Empty };
            ApplyTo(character, snapshot);
            return character;
        }
    }
}
