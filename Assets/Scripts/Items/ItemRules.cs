namespace ShadowInfection.Items
{
    public static class ItemRules
    {
        public const int SocketsPerPiece = 2;

        public static bool IsStackable(ItemKind kind)
        {
            return kind == ItemKind.Gem || kind == ItemKind.Material;
        }

        public static bool IsArmorSlot(ItemSlot slot)
        {
            return slot >= ItemSlot.Head && slot <= ItemSlot.Gloves;
        }

        public static bool IsWeaponSlot(ItemSlot slot)
        {
            return slot == ItemSlot.MainHand || slot == ItemSlot.OffHand;
        }

        public static bool EnforcesArmorWeight(ItemSlot slot)
        {
            return IsArmorSlot(slot) && slot != ItemSlot.Cape;
        }

        public static bool CanEquipWithWeapon(ItemDefinition piece, WeaponType? mainHandWeapon)
        {
            if (piece == null)
                return false;

            if (!EnforcesArmorWeight(piece.slot))
                return true;

            if (!mainHandWeapon.HasValue)
                return true;

            if (!TryGetArmorWeightFor(mainHandWeapon.Value, out var required))
                return true;

            return piece.armorWeight == required;
        }

        public static bool TryGetArmorWeightFor(WeaponType weapon, out ArmorWeight weight)
        {
            switch (weapon)
            {
                case WeaponType.Staff:
                    weight = ArmorWeight.Cloth;
                    return true;
                case WeaponType.Daggers:
                case WeaponType.Bow:
                case WeaponType.Pistols:
                case WeaponType.Gun:
                    weight = ArmorWeight.Leather;
                    return true;
                case WeaponType.Sword:
                case WeaponType.SwordAndShield:
                    weight = ArmorWeight.Plate;
                    return true;
                default:
                    weight = default;
                    return false;
            }
        }
    }
}
