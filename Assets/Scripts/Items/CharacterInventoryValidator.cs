using System.Collections.Generic;
using ShadowInfection.Items;

namespace ShadowInfection.Items
{
    public static class CharacterInventoryValidator
    {
        public static bool TryValidate(
            CharacterInventorySnapshot snapshot,
            IItemCatalog catalog,
            out string error)
        {
            error = null;
            if (catalog == null)
            {
                error = "Item catalog unavailable.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(snapshot.characterId))
            {
                error = "Missing character id.";
                return false;
            }

            var data = CharacterInventorySnapshot.ToCharacterData(snapshot);
            var seenInstances = new HashSet<string>();

            var bag = data.InventoryEquipment;
            if (bag != null)
            {
                for (var i = 0; i < bag.Count; i++)
                {
                    var entry = bag[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.instanceId))
                    {
                        error = "Invalid bag entry.";
                        return false;
                    }

                    if (!seenInstances.Add(entry.instanceId))
                    {
                        error = "Duplicate item instance.";
                        return false;
                    }

                    if (!catalog.TryGet(entry.itemId, out var definition) || definition == null)
                    {
                        error = $"Unknown item '{entry.itemId}'.";
                        return false;
                    }

                    if (definition.IsStackable)
                    {
                        error = "Stackable items must be in stacks.";
                        return false;
                    }
                }
            }

            var equipped = data.EquippedSlots;
            if (equipped != null)
            {
                var usedSlots = new HashSet<ItemSlot>();
                var mainHandWeapon = CharacterInventoryOperations.ResolveMainHandWeaponType(data, catalog);
                for (var i = 0; i < equipped.Count; i++)
                {
                    var entry = equipped[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.instanceId))
                    {
                        error = "Invalid equipped entry.";
                        return false;
                    }

                    if (!seenInstances.Add(entry.instanceId))
                    {
                        error = "Item equipped and in bag.";
                        return false;
                    }

                    if (!catalog.TryGet(entry.itemId, out var definition) || definition == null)
                    {
                        error = $"Unknown equipped item '{entry.itemId}'.";
                        return false;
                    }

                    if (definition.kind != ItemKind.Equipment || definition.slot != entry.slot)
                    {
                        error = $"Item '{definition.DisplayName}' does not fit slot {entry.slot}.";
                        return false;
                    }

                    if (!usedSlots.Add(entry.slot))
                    {
                        error = $"Duplicate equipped slot {entry.slot}.";
                        return false;
                    }

                    if (!ItemRules.CanEquipWithWeapon(definition, mainHandWeapon))
                    {
                        error = ItemPresentation.ArmorWeightMismatchReason(definition, mainHandWeapon)
                            ?? "Armor weight mismatch.";
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
