namespace ShadowInfection.Items
{
    public static class HeldWeaponResolver
    {
        public const string FistsWeaponName = "Fists";

        public static WeaponData ResolveMain(ItemDatabase items, WeaponDatabase weapons, string itemId)
        {
            var fromItem = ResolveItemWeapon(items, itemId);
            if (fromItem != null)
                return fromItem;

            return weapons != null ? weapons.GetWeaponByName(FistsWeaponName) : null;
        }

        public static WeaponData ResolveItemWeapon(ItemDatabase items, string itemId)
        {
            if (items == null || string.IsNullOrWhiteSpace(itemId))
                return null;

            if (!items.TryGet(itemId, out var definition) || definition == null)
                return null;

            return definition.weaponData;
        }

        public static WeaponData ResolveOffHandAttackWeapon(
            ItemDatabase items,
            WeaponDatabase weapons,
            string itemId,
            WeaponData mainWeapon)
        {
            var fromItem = ResolveItemWeapon(items, itemId);
            if (fromItem != null)
                return ItemRules.IsDualWieldWeapon(fromItem.weaponType) ? fromItem : null;

            if (mainWeapon == null || mainWeapon.weaponType != WeaponType.Unarmed)
                return null;

            var fists = weapons != null ? weapons.GetWeaponByName(FistsWeaponName) : null;
            return fists != null ? fists : mainWeapon;
        }
    }
}
