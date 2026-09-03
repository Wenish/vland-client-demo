namespace ShadowInfection.Items
{
    public static class ItemRules
    {
        public const int SocketsPerPiece = 2;
        public const float OffHandDamageMultiplier = 0.5f;

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

        public static bool IsOneHandMelee(WeaponType weapon)
        {
            switch (weapon)
            {
                case WeaponType.Sword:
                case WeaponType.Daggers:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsDualWieldWeapon(WeaponType weapon)
        {
            return IsOneHandMelee(weapon) || weapon == WeaponType.Pistols;
        }

        public static bool IsPairedTwoModelWeapon(WeaponType weapon)
        {
            return weapon == WeaponType.SwordAndShield;
        }

        public static bool CanDualWieldTogether(WeaponType? mainHandWeapon, WeaponType offHandWeapon)
        {
            if (!IsDualWieldWeapon(offHandWeapon))
                return false;

            if (!mainHandWeapon.HasValue || mainHandWeapon.Value == WeaponType.Unarmed)
                return true;

            if (IsOneHandMelee(offHandWeapon))
                return IsOneHandMelee(mainHandWeapon.Value);

            if (offHandWeapon == WeaponType.Pistols)
                return mainHandWeapon.Value == WeaponType.Pistols;

            return false;
        }

        public static bool IsShieldWeapon(WeaponType weapon)
        {
            return weapon == WeaponType.Shield;
        }

        public static bool CanShieldWithMainHand(WeaponType? mainHandWeapon)
        {
            return mainHandWeapon == WeaponType.Sword
                || mainHandWeapon == WeaponType.SwordAndShield;
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

        public static bool CanEquipToSlot(
            ItemDefinition item,
            ItemSlot targetSlot,
            WeaponType? mainHandWeapon,
            WeaponType? offHandWeapon)
        {
            if (item == null || item.kind != ItemKind.Equipment)
                return false;

            if (IsArmorSlot(item.slot) || item.slot == ItemSlot.Cape)
            {
                if (item.slot != targetSlot)
                    return false;

                return CanEquipWithWeapon(item, mainHandWeapon);
            }

            if (!IsWeaponSlot(targetSlot) || item.weaponData == null)
                return false;

            var weaponType = item.weaponData.weaponType;

            if (IsShieldWeapon(weaponType))
            {
                if (targetSlot != ItemSlot.OffHand)
                    return false;

                return CanShieldWithMainHand(mainHandWeapon);
            }

            if (IsDualWieldWeapon(weaponType))
            {
                if (targetSlot == ItemSlot.MainHand)
                    return true;

                if (targetSlot == ItemSlot.OffHand)
                    return CanDualWieldTogether(mainHandWeapon, weaponType);

                return false;
            }

            return targetSlot == ItemSlot.MainHand;
        }

        public static bool TryResolveEquipSlot(
            CharacterSaveData character,
            IItemCatalog catalog,
            ItemDefinition definition,
            ItemSlot? selectedSlot,
            out ItemSlot slot)
        {
            slot = default;
            if (character == null || definition == null)
                return false;

            var mainHand = CharacterInventoryOperations.ResolveMainHandWeaponType(character, catalog);
            var offHand = CharacterInventoryOperations.ResolveOffHandWeaponType(character, catalog);
            var itemWeapon = definition.weaponData != null
                ? (WeaponType?)definition.weaponData.weaponType
                : null;

            if (selectedSlot.HasValue)
            {
                var projectedMain = selectedSlot.Value == ItemSlot.MainHand ? itemWeapon : mainHand;
                var projectedOff = selectedSlot.Value == ItemSlot.OffHand ? itemWeapon : offHand;
                if (!CanEquipToSlot(definition, selectedSlot.Value, projectedMain, projectedOff))
                    return false;

                slot = selectedSlot.Value;
                return true;
            }

            if (itemWeapon.HasValue && IsShieldWeapon(itemWeapon.Value))
            {
                if (!CanEquipToSlot(definition, ItemSlot.OffHand, mainHand, offHand))
                    return false;

                slot = ItemSlot.OffHand;
                return true;
            }

            if (itemWeapon.HasValue && IsDualWieldWeapon(itemWeapon.Value))
            {
                var mainEmpty = !CharacterInventory.TryGetEquipped(character, ItemSlot.MainHand, out var mainEntry)
                    || string.IsNullOrWhiteSpace(mainEntry.instanceId);
                if (mainEmpty && CanEquipToSlot(definition, ItemSlot.MainHand, null, offHand))
                {
                    slot = ItemSlot.MainHand;
                    return true;
                }

                var offEmpty = !CharacterInventory.TryGetEquipped(character, ItemSlot.OffHand, out var offEntry)
                    || string.IsNullOrWhiteSpace(offEntry.instanceId);
                if (offEmpty && CanEquipToSlot(definition, ItemSlot.OffHand, mainHand, null))
                {
                    slot = ItemSlot.OffHand;
                    return true;
                }

                slot = ItemSlot.MainHand;
                return CanEquipToSlot(definition, ItemSlot.MainHand, itemWeapon, offHand);
            }

            slot = ItemSlot.MainHand;
            return CanEquipToSlot(definition, ItemSlot.MainHand, itemWeapon, offHand);
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
