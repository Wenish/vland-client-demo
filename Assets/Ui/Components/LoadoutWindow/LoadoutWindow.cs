using UnityEngine;

namespace Vland.UI
{
    public enum LoadoutSlot
    {
        Weapon,
        Passive,
        Normal1,
        Normal2,
        Normal3,
        Ultimate
    }

    public struct LoadoutItem
    {
        public string id;
        public string name;
        public string description;
        public string summary;
        public string meta;
        public Texture2D icon;
        public LoadoutSlot slot;
        public SkillTag tags;
        public bool isWeapon;

        public static LoadoutItem Empty => new LoadoutItem
        {
            id = string.Empty,
            name = string.Empty,
            description = string.Empty,
            summary = string.Empty,
            meta = string.Empty,
        };

        public bool HasId => !string.IsNullOrEmpty(id);
    }

    public static class LoadoutSlots
    {
        public static readonly LoadoutSlot[] All =
        {
            LoadoutSlot.Weapon,
            LoadoutSlot.Passive,
            LoadoutSlot.Normal1,
            LoadoutSlot.Normal2,
            LoadoutSlot.Normal3,
            LoadoutSlot.Ultimate,
        };

        public static bool IsNormal(LoadoutSlot slot)
        {
            return slot == LoadoutSlot.Normal1
                || slot == LoadoutSlot.Normal2
                || slot == LoadoutSlot.Normal3;
        }

        public static string RoleLabel(LoadoutSlot slot)
        {
            return slot switch
            {
                LoadoutSlot.Weapon => "Weapon",
                LoadoutSlot.Passive => "Passive",
                LoadoutSlot.Normal1 => "Skill 1 (Q)",
                LoadoutSlot.Normal2 => "Skill 2 (E)",
                LoadoutSlot.Normal3 => "Skill 3 (C)",
                LoadoutSlot.Ultimate => "Ultimate (X)",
                _ => slot.ToString(),
            };
        }

        public static string ChoosingLabel(LoadoutSlot slot)
        {
            return slot switch
            {
                LoadoutSlot.Weapon => "Choosing: Weapon",
                LoadoutSlot.Passive => "Choosing: Passive",
                LoadoutSlot.Normal1 => "Choosing: Skill 1 (Q)",
                LoadoutSlot.Normal2 => "Choosing: Skill 2 (E)",
                LoadoutSlot.Normal3 => "Choosing: Skill 3 (C)",
                LoadoutSlot.Ultimate => "Choosing: Ultimate (X)",
                _ => "Choosing",
            };
        }

        public static string SlotTypeLabel(LoadoutSlot slot)
        {
            return slot switch
            {
                LoadoutSlot.Weapon => "Weapons",
                LoadoutSlot.Passive => "Passive skills",
                LoadoutSlot.Ultimate => "Ultimate skills",
                _ => "Normal skills",
            };
        }
    }
}
