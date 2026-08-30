using System.Collections.Generic;
using UnityEngine;

namespace ShadowInfection.Items
{
    public sealed class DatabaseItemCatalog : IItemCatalog
    {
        private readonly ItemDatabase database;

        public DatabaseItemCatalog(ItemDatabase database)
        {
            this.database = database;
        }

        public IReadOnlyList<ItemDefinition> All =>
            database != null ? database.All : System.Array.Empty<ItemDefinition>();

        public Texture2D DefaultIcon => database != null ? database.defaultIcon : null;

        public bool TryGet(string itemId, out ItemDefinition item)
        {
            if (database == null)
            {
                item = null;
                return false;
            }

            return database.TryGet(itemId, out item);
        }

        public Texture2D ResolveIcon(ItemDefinition item)
        {
            if (item != null && item.icon != null)
                return item.icon;

            return DefaultIcon;
        }
    }
}
