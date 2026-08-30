using System;
using System.Collections.Generic;

namespace ShadowInfection.Items
{
    [Serializable]
    public class InventoryEntry
    {
        public string instanceId;
        public string itemId;
    }

    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int count;
    }

    public static class CharacterInventory
    {
        public static void EnsureLists(CharacterSaveData character)
        {
            if (character == null)
                return;

            if (character.UnlockedSkillIds == null)
                character.UnlockedSkillIds = new List<string>();
            if (character.ArmorSlotIds == null)
                character.ArmorSlotIds = new List<string>();
            if (character.InventoryEquipment == null)
                character.InventoryEquipment = new List<InventoryEntry>();
            if (character.InventoryStacks == null)
                character.InventoryStacks = new List<ItemStack>();
        }

        public static bool TryGrant(CharacterSaveData character, ItemDefinition definition)
        {
            if (character == null || definition == null || string.IsNullOrWhiteSpace(definition.itemId))
                return false;

            EnsureLists(character);

            if (definition.IsStackable)
            {
                var stack = FindStack(character, definition.itemId);
                if (stack == null)
                {
                    character.InventoryStacks.Add(new ItemStack
                    {
                        itemId = definition.itemId,
                        count = 1
                    });
                }
                else
                {
                    stack.count += 1;
                }

                return true;
            }

            character.InventoryEquipment.Add(new InventoryEntry
            {
                instanceId = Guid.NewGuid().ToString("N"),
                itemId = definition.itemId
            });
            return true;
        }

        public static bool TryDestroyEquipment(CharacterSaveData character, string instanceId)
        {
            if (character == null || string.IsNullOrWhiteSpace(instanceId))
                return false;

            EnsureLists(character);
            for (var i = 0; i < character.InventoryEquipment.Count; i++)
            {
                var entry = character.InventoryEquipment[i];
                if (entry == null || entry.instanceId != instanceId)
                    continue;

                character.InventoryEquipment.RemoveAt(i);
                return true;
            }

            return false;
        }

        public static bool TryDestroyStack(CharacterSaveData character, string itemId, int amount = 1)
        {
            if (character == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                return false;

            EnsureLists(character);
            var stack = FindStack(character, itemId);
            if (stack == null || stack.count < amount)
                return false;

            stack.count -= amount;
            if (stack.count <= 0)
                character.InventoryStacks.Remove(stack);

            return true;
        }

        private static ItemStack FindStack(CharacterSaveData character, string itemId)
        {
            for (var i = 0; i < character.InventoryStacks.Count; i++)
            {
                var stack = character.InventoryStacks[i];
                if (stack != null && stack.itemId == itemId)
                    return stack;
            }

            return null;
        }
    }
}
