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
                CharacterInventory.EnsureLists(active);
                return active != null && active.InventoryEquipment != null
                    ? active.InventoryEquipment
                    : Array.Empty<InventoryEntry>();
            }
        }

        public IReadOnlyList<ItemStack> Stacks
        {
            get
            {
                var active = characters != null ? characters.GetActive() : null;
                CharacterInventory.EnsureLists(active);
                return active != null && active.InventoryStacks != null
                    ? active.InventoryStacks
                    : Array.Empty<ItemStack>();
            }
        }

        public bool TryGrantItem(string itemId)
        {
            return characters != null && characters.TryGrantItem(itemId);
        }

        public bool TryDestroyEquipment(string instanceId)
        {
            return characters != null && characters.TryDestroyEquipment(instanceId);
        }

        public bool TryDestroyStack(string itemId, int amount = 1)
        {
            return characters != null && characters.TryDestroyStack(itemId, amount);
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
