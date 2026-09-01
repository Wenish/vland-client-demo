using System.Collections.Generic;
using UnityEngine;

namespace ShadowInfection.Items
{
    [CreateAssetMenu(fileName = "Item", menuName = "Game/Items/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string itemId = "item_new";
        public string displayName = "New Item";
        [TextArea]
        public string description;
        public Texture2D icon;

        [Header("Kind")]
        public ItemKind kind = ItemKind.Equipment;
        public ItemSlot slot = ItemSlot.None;
        public ItemRarity rarity = ItemRarity.Common;
        public ArmorWeight armorWeight = ArmorWeight.Leather;

        [Header("Equipment")]
        public WeaponData weaponData;
        public List<StatModifier> statModifiers = new List<StatModifier>();
        public BuffType legendaryBuff;

        [Header("Gem")]
        public string keyword;
        public List<StatModifier> keywordBonus = new List<StatModifier>();

        [Header("Reserved")]
        public SkillData activeSkill;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        public bool IsStackable => ItemRules.IsStackable(kind);

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
                UnityEngine.Debug.LogWarning($"ItemDefinition '{name}' is missing itemId.", this);

            if (kind != ItemKind.Equipment)
                slot = ItemSlot.None;

            if (kind == ItemKind.Equipment && ItemRules.IsWeaponSlot(slot) && weaponData == null)
                UnityEngine.Debug.LogWarning($"ItemDefinition '{itemId}' is a weapon without WeaponData.", this);
        }
    }
}
