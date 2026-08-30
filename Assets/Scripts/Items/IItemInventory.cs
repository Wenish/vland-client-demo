using System.Collections.Generic;

namespace ShadowInfection.Items
{
    public interface IItemInventory
    {
        IReadOnlyList<InventoryEntry> Equipment { get; }
        IReadOnlyList<ItemStack> Stacks { get; }
        bool TryGrantItem(string itemId);
        bool TryDestroyEquipment(string instanceId);
        bool TryDestroyStack(string itemId, int amount = 1);
    }
}
