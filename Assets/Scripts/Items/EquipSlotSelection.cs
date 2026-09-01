using System;

namespace ShadowInfection.Items
{
    public sealed class EquipSlotSelection : IEquipSlotSelection
    {
        public ItemSlot? SelectedSlot { get; private set; }
        public event Action Changed;

        public void Select(ItemSlot slot)
        {
            SelectedSlot = slot;
            Changed?.Invoke();
        }

        public void Clear()
        {
            if (!SelectedSlot.HasValue)
                return;

            SelectedSlot = null;
            Changed?.Invoke();
        }
    }
}
