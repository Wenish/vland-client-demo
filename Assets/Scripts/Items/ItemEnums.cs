namespace ShadowInfection.Items
{
    public enum ItemKind : byte
    {
        Equipment = 0,
        Gem = 1,
        Material = 2
    }

    public enum ItemSlot : byte
    {
        None = 0,
        Head = 1,
        Shoulder = 2,
        Cape = 3,
        Chest = 4,
        Pants = 5,
        Feet = 6,
        Gloves = 7,
        MainHand = 8,
        OffHand = 9
    }

    public enum ItemRarity : byte
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    public enum ArmorWeight : byte
    {
        Cloth = 0,
        Leather = 1,
        Plate = 2
    }
}
