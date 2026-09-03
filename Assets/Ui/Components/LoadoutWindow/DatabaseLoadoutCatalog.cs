using System;
using System.Collections.Generic;
using ShadowInfection.DI;

namespace ShadowInfection.UI.LoadoutWindow
{
    internal sealed class DatabaseLoadoutCatalog : ILoadoutCatalog
    {
        private readonly IGameDatabases databases;

        public DatabaseLoadoutCatalog(IGameDatabases databases)
        {
            this.databases = databases;
        }

        public IReadOnlyList<SkillData> GetPlayerSkills()
        {
            var all = databases != null && databases.Skills != null ? databases.Skills.allSkills : null;
            if (all == null || all.Count == 0)
                return Array.Empty<SkillData>();

            var result = new List<SkillData>(all.Count);
            for (var i = 0; i < all.Count; i++)
            {
                var skill = all[i];
                if (skill != null && !skill.npcOnly)
                    result.Add(skill);
            }

            return result;
        }

        public SkillData GetSkill(string id)
        {
            if (string.IsNullOrEmpty(id) || databases == null || databases.Skills == null)
                return null;

            return databases.Skills.GetSkillByName(id);
        }
    }
}
