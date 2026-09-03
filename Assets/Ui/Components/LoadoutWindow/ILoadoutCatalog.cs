using System.Collections.Generic;

namespace ShadowInfection.UI.LoadoutWindow
{
    internal interface ILoadoutCatalog
    {
        IReadOnlyList<SkillData> GetPlayerSkills();
        SkillData GetSkill(string id);
    }
}
