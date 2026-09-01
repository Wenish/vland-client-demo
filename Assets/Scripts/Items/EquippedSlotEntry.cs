using System;

namespace ShadowInfection.Items
{
    [Serializable]
    public class EquippedSlotEntry
    {
        public ItemSlot slot;
        public string instanceId;
        public string itemId;
    }
}
