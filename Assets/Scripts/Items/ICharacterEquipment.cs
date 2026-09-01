using System.Collections.Generic;

namespace ShadowInfection.Items
{
    public interface ICharacterEquipment
    {
        IReadOnlyList<EquippedSlotEntry> Equipped { get; }
        bool TryGetEquipped(ItemSlot slot, out string instanceId);
        bool TryGetEquippedEntry(ItemSlot slot, out EquippedSlotEntry entry);
        bool IsEquipped(string instanceId);
        bool TryEquip(string instanceId, ItemSlot slot);
        bool TryUnequip(ItemSlot slot);
    }
}
