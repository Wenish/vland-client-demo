using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Selects skills based on NPC health percentage.
    /// Useful for bosses that use different abilities at different health ranges.
    /// </summary>
    [CreateAssetMenu(fileName = "HealthBasedSelector", menuName = "Game/NPC Behaviour/Skill Selectors/Health Based")]
    public class HealthBasedSkillSelector : SkillSelector
    {
        [System.Serializable]
        public class HealthSkillMapping
        {
            [Range(0f, 1f)]
            public float minHealthPercent;
            [Range(0f, 1f)]
            public float maxHealthPercent = 1f;
            public List<string> skillNames = new();
            [Tooltip("Priority when multiple ranges match (higher = more important)")]
            public int priority = 0;
        }

        [Header("Health Ranges")]
        public List<HealthSkillMapping> healthMappings = new();

        public override NetworkedSkillInstance SelectSkill(BehaviourContext context, List<NetworkedSkillInstance> availableSkills)
        {
            if (availableSkills == null || availableSkills.Count == 0) return null;

            float healthPercent = context.HealthPercent;

            // Find all mappings that match current health, sorted by priority
            var validMappings = healthMappings
                .Where(m => healthPercent >= m.minHealthPercent && healthPercent <= m.maxHealthPercent)
                .OrderByDescending(m => m.priority);

            foreach (var mapping in validMappings)
            {
                foreach (var skillName in mapping.skillNames)
                {
                    var skill = availableSkills.FirstOrDefault(s => 
                        s != null && 
                        s.skillName == skillName && 
                        !s.IsOnCooldown
                    );

                    if (skill != null) return skill;
                }
            }

            return null;
        }
    }
}
