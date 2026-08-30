using System;
using ShadowInfection.Items;
using UnityEngine;

namespace ShadowInfection.UI.InventoryWindow
{
    internal enum InventoryFilter
    {
        All = 0,
        Head,
        Shoulder,
        Cape,
        Chest,
        Pants,
        Feet,
        Gloves,
        Weapon,
        Gem,
        Material
    }

    internal sealed class InventoryRowVm
    {
        public string RowId;
        public string InstanceId;
        public string ItemId;
        public bool IsStack;
        public int Count;
        public string Name;
        public string Meta;
        public string Summary;
        public Texture2D Icon;
        public string RarityClass;
        public ItemDefinition Definition;
    }
}
