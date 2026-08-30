using System.Collections.Generic;
using UnityEngine;

namespace ShadowInfection.Items
{
    public interface IItemCatalog
    {
        IReadOnlyList<ItemDefinition> All { get; }
        Texture2D DefaultIcon { get; }
        bool TryGet(string itemId, out ItemDefinition item);
        Texture2D ResolveIcon(ItemDefinition item);
    }
}
