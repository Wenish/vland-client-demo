using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ShadowInfection.Items
{
    public static class ItemPresentation
    {
        public static string TypeLine(ItemDefinition item)
        {
            if (item == null)
                return "Unknown";

            switch (item.kind)
            {
                case ItemKind.Gem:
                    return string.IsNullOrWhiteSpace(item.keyword)
                        ? "Gem"
                        : $"Gem · {item.keyword}";
                case ItemKind.Material:
                    return "Material";
                default:
                    if (ItemRules.IsWeaponSlot(item.slot))
                        return item.slot == ItemSlot.OffHand ? "Off Hand" : "Weapon";
                    if (ItemRules.IsArmorSlot(item.slot))
                    {
                        if (item.slot == ItemSlot.Cape)
                            return SlotLabel(item.slot);
                        return $"{item.armorWeight} {SlotLabel(item.slot)}";
                    }
                    return "Equipment";
            }
        }

        public static string SlotLabel(ItemSlot slot)
        {
            switch (slot)
            {
                case ItemSlot.Head: return "Head";
                case ItemSlot.Shoulder: return "Shoulder";
                case ItemSlot.Cape: return "Cape";
                case ItemSlot.Chest: return "Chest";
                case ItemSlot.Pants: return "Pants";
                case ItemSlot.Feet: return "Feet";
                case ItemSlot.Gloves: return "Gloves";
                case ItemSlot.MainHand: return "Weapon";
                case ItemSlot.OffHand: return "Off Hand";
                default: return "Item";
            }
        }

        public static string KindLabel(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Gem: return "Gem";
                case ItemKind.Material: return "Material";
                default: return "Equipment";
            }
        }

        public static string FormatStats(IList<StatModifier> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (var i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier == null)
                    continue;

                if (builder.Length > 0)
                    builder.Append('\n');

                builder.Append(FormatStat(modifier));
            }

            return builder.ToString();
        }

        public static string FormatStat(StatModifier modifier)
        {
            if (modifier == null)
                return string.Empty;

            var sign = modifier.Value >= 0f ? "+" : string.Empty;
            var value = Mathf.Approximately(modifier.Value, Mathf.Round(modifier.Value))
                ? Mathf.RoundToInt(modifier.Value).ToString()
                : modifier.Value.ToString("0.#");

            if (modifier.ModifierType == ModifierType.Percent)
                return $"{sign}{value}% {StatLabel(modifier.Type)}";

            return $"{sign}{value} {StatLabel(modifier.Type)}";
        }

        public static string StatLabel(StatType type)
        {
            switch (type)
            {
                case StatType.Health: return "Health";
                case StatType.MovementSpeed: return "Move Speed";
                case StatType.Shield: return "Shield";
                case StatType.TurnSpeed: return "Turn Speed";
                case StatType.DamageReduction: return "Damage Reduction";
                case StatType.AttackSpeed: return "Attack Speed";
                case StatType.AttackPower: return "Attack Power";
                case StatType.AbilityPower: return "Ability Power";
                case StatType.Armor: return "Armor";
                case StatType.MagicResist: return "Magic Resist";
                case StatType.CritChance: return "Crit Chance";
                default: return type.ToString();
            }
        }

        public static string RarityClass(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return "inventory-icon--uncommon";
                case ItemRarity.Rare: return "inventory-icon--rare";
                case ItemRarity.Epic: return "inventory-icon--epic";
                case ItemRarity.Legendary: return "inventory-icon--legendary";
                default: return "inventory-icon--common";
            }
        }

        public static string ArmorWeightMismatchReason(ItemDefinition piece, WeaponType? mainHandWeapon)
        {
            if (piece == null || !ItemRules.EnforcesArmorWeight(piece.slot))
                return null;
            if (ItemRules.CanEquipWithWeapon(piece, mainHandWeapon))
                return null;
            if (!mainHandWeapon.HasValue
                || !ItemRules.TryGetArmorWeightFor(mainHandWeapon.Value, out var required))
                return "Cannot equip with current main-hand weapon.";

            return $"Requires {required} armor with your equipped weapon.";
        }

        public static string HandEquipBlockReason(
            ItemDefinition piece,
            ItemSlot targetSlot,
            WeaponType? mainHandWeapon,
            WeaponType? offHandWeapon)
        {
            if (piece == null || piece.kind != ItemKind.Equipment)
                return null;

            if (ItemRules.CanEquipToSlot(piece, targetSlot, mainHandWeapon, offHandWeapon))
                return null;

            if (piece.weaponData != null && ItemRules.IsShieldWeapon(piece.weaponData.weaponType))
                return "Requires a one-hand sword in main hand.";

            if (piece.weaponData != null && ItemRules.IsDualWieldWeapon(piece.weaponData.weaponType))
            {
                if (targetSlot == ItemSlot.OffHand)
                    return "Requires an empty main hand or a one-hand sword or dagger in main hand.";
            }

            if (ItemRules.IsWeaponSlot(targetSlot))
                return $"Cannot equip to {SlotLabel(targetSlot).ToLower()}.";

            return $"Select the {SlotLabel(piece.slot)} slot on your character.";
        }
    }
}
