using System;
using System.Collections.Generic;
using MessagePipe;

namespace ShadowInfection.Items
{
    public sealed class ActiveCharacterInventory : IItemInventory, IDisposable
    {
        private readonly CharacterManager characters;
        private readonly IPublisher<InventoryChangedEvent> changed;

        public ActiveCharacterInventory(
            CharacterManager characters,
            IPublisher<InventoryChangedEvent> changed)
        {
            this.characters = characters;
            this.changed = changed;
            if (this.characters != null)
            {
                this.characters.OnRosterChanged += PublishChanged;
                this.characters.OnActiveCharacterChanged += OnActiveChanged;
            }
        }

        public IReadOnlyList<InventoryEntry> Equipment
        {
            get
            {
                var active = characters != null ? characters.GetActive() : null;
                if (active != null && active.InventoryEquipment != null)
                    return active.InventoryEquipment;
                return Array.Empty<InventoryEntry>();
            }
        }

        public IReadOnlyList<ItemStack> Stacks
        {
            get
            {
                var active = characters != null ? characters.GetActive() : null;
                if (active != null && active.InventoryStacks != null)
                    return active.InventoryStacks;
                return Array.Empty<ItemStack>();
            }
        }

        public bool TryGrantItem(string itemId)
        {
            if (characters == null || !characters.TryGrantItem(itemId))
                return false;

            NetworkInventorySync.NotifyLocalInventoryChanged();
            return true;
        }

        public bool TryDestroyEquipment(string instanceId)
        {
            if (characters == null || !characters.TryDestroyEquipment(instanceId))
                return false;

            NetworkInventorySync.NotifyLocalInventoryChanged();
            return true;
        }

        public bool TryDestroyStack(string itemId, int amount = 1)
        {
            if (characters == null || !characters.TryDestroyStack(itemId, amount))
                return false;

            NetworkInventorySync.NotifyLocalInventoryChanged();
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
        }

        private void PublishChanged()
        {
            changed?.Publish(new InventoryChangedEvent());
        }
    }
}
