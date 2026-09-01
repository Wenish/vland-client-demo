using System;

namespace ShadowInfection.Items
{
    public interface IEquipSlotSelection
    {
        ItemSlot? SelectedSlot { get; }
        event Action Changed;
        void Select(ItemSlot slot);
        void Clear();
    }
}
