using System;
using System.Collections.Generic;
using MessagePipe;
using ShadowInfection.Items;

namespace ShadowInfection.Items
{
    public sealed class ActiveCharacterEquipment : ICharacterEquipment, IDisposable
    {
        private readonly CharacterManager characters;
        private readonly IItemCatalog catalog;
        private readonly IPublisher<EquipmentChangedEvent> equipmentChanged;
        private readonly IPublisher<InventoryChangedEvent> inventoryChanged;

        public ActiveCharacterEquipment(
            CharacterManager characters,
            IItemCatalog catalog,
            IPublisher<EquipmentChangedEvent> equipmentChanged,
            IPublisher<InventoryChangedEvent> inventoryChanged)
        {
            this.characters = characters;
            this.catalog = catalog;
            this.equipmentChanged = equipmentChanged;
            this.inventoryChanged = inventoryChanged;
            if (this.characters != null)
            {
                this.characters.OnRosterChanged += PublishChanged;
                this.characters.OnActiveCharacterChanged += OnActiveChanged;
            }
        }

        public IReadOnlyList<EquippedSlotEntry> Equipped
        {
            get
            {
                var active = characters != null ? characters.GetActive() : null;
                if (active != null && active.EquippedSlots != null)
                    return active.EquippedSlots;
                return Array.Empty<EquippedSlotEntry>();
            }
        }

        public bool TryGetEquipped(ItemSlot slot, out string instanceId)
        {
            instanceId = null;
            var active = characters != null ? characters.GetActive() : null;
            if (!CharacterInventory.TryGetEquipped(active, slot, out var entry))
                return false;

            instanceId = entry.instanceId;
            return !string.IsNullOrWhiteSpace(instanceId);
        }

        public bool TryGetEquippedEntry(ItemSlot slot, out EquippedSlotEntry entry)
        {
            var active = characters != null ? characters.GetActive() : null;
            return CharacterInventory.TryGetEquipped(active, slot, out entry);
        }

        public bool IsEquipped(string instanceId)
        {
            var active = characters != null ? characters.GetActive() : null;
            return CharacterInventory.IsEquipped(active, instanceId);
        }

        public bool TryEquip(string instanceId, ItemSlot slot)
        {
            var active = characters != null ? characters.GetActive() : null;
            if (active == null)
                return false;

            if (!CharacterInventoryOperations.TryEquip(active, catalog, instanceId, slot))
                return false;

            characters.PersistRoster();
            PublishBoth();
            SyncToServerIfNeeded();
            return true;
        }

        public bool TryUnequip(ItemSlot slot)
        {
            var active = characters != null ? characters.GetActive() : null;
            if (active == null)
                return false;

            if (!CharacterInventoryOperations.TryUnequip(active, slot))
                return false;

            characters.PersistRoster();
            PublishBoth();
            SyncToServerIfNeeded();
            return true;
        }

        public void Dispose()
        {
            if (characters == null)
                return;

            characters.OnRosterChanged -= PublishChanged;
            characters.OnActiveCharacterChanged -= OnActiveChanged;
        }

        private void OnActiveChanged(CharacterSaveData _)
        {
            PublishChanged();
            SyncToServerIfNeeded();
        }

        private static void SyncToServerIfNeeded()
        {
            NetworkInventorySync.NotifyLocalInventoryChanged();
        }

        private void PublishChanged()
        {
            equipmentChanged?.Publish(new EquipmentChangedEvent());
        }

        private void PublishBoth()
        {
            equipmentChanged?.Publish(new EquipmentChangedEvent());
            inventoryChanged?.Publish(new InventoryChangedEvent());
        }
    }
}
