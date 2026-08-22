using System;
using System.Collections.Generic;

namespace ShadowInfection.UI.LoadoutWindow
{
    internal sealed class DatabaseLoadoutCatalog : ILoadoutCatalog
    {
        public IReadOnlyList<WeaponData> GetPlayerWeapons()
        {
            var weapons = DatabaseManager.Instance?.weaponDatabase?.allWeapons;
            if (weapons == null || weapons.Count == 0)
                return Array.Empty<WeaponData>();

            var result = new List<WeaponData>(weapons.Count);
            for (var i = 0; i < weapons.Count; i++)
            {
                var weapon = weapons[i];
                if (weapon != null && !weapon.npcOnly)
                    result.Add(weapon);
            }

            return result;
        }

        public IReadOnlyList<SkillData> GetPlayerSkills()
        {
            var skills = DatabaseManager.Instance?.skillDatabase?.allSkills;
            if (skills == null || skills.Count == 0)
                return Array.Empty<SkillData>();

            var result = new List<SkillData>(skills.Count);
            for (var i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                if (skill != null && !skill.npcOnly)
                    result.Add(skill);
            }

            return result;
        }

        public WeaponData GetWeapon(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return DatabaseManager.Instance?.weaponDatabase?.GetWeaponByName(id);
        }

        public SkillData GetSkill(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return DatabaseManager.Instance?.skillDatabase?.GetSkillByName(id);
        }
    }
}
