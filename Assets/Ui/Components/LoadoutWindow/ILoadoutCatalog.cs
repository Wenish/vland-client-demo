using System.Collections.Generic;

namespace ShadowInfection.UI.LoadoutWindow
{
    internal interface ILoadoutCatalog
    {
        IReadOnlyList<WeaponData> GetPlayerWeapons();
        IReadOnlyList<SkillData> GetPlayerSkills();
        WeaponData GetWeapon(string id);
        SkillData GetSkill(string id);
    }
}
