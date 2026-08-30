using System.Collections.Generic;
using UnityEngine;

namespace ShadowInfection.Items
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Items/Database")]
    public sealed class ItemDatabase : ScriptableObject
    {
        [SerializeField]
        private List<ItemDefinition> items = new List<ItemDefinition>();

        [Tooltip("Shown when an item has no icon assigned.")]
        public Texture2D defaultIcon;

        private Dictionary<string, ItemDefinition> _lookup;
        private List<ItemDefinition> _all;

        private void OnEnable()
        {
            _lookup = null;
            _all = null;
        }

        public IReadOnlyList<ItemDefinition> All
        {
            get
            {
                EnsureLookup();
                return _all;
            }
        }

        public bool TryGet(string itemId, out ItemDefinition item)
        {
            EnsureLookup();
            if (string.IsNullOrWhiteSpace(itemId))
            {
                item = null;
                return false;
            }

            return _lookup.TryGetValue(itemId, out item);
        }

        public bool Contains(string itemId)
        {
            return TryGet(itemId, out _);
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<string, ItemDefinition>();
            _all = new List<ItemDefinition>();
            if (items == null)
                return;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                    continue;

                if (_lookup.ContainsKey(item.itemId))
                {
                    UnityEngine.Debug.LogWarning($"ItemDatabase has duplicate itemId '{item.itemId}'.", this);
                    continue;
                }

                _lookup[item.itemId] = item;
                _all.Add(item);
            }
        }
    }
}
