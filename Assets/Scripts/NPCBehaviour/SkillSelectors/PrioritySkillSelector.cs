using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Prioritizes skills based on a defined order.
    /// Useful for creating intelligent AI that uses optimal skill rotations.
    /// </summary>
    [CreateAssetMenu(fileName = "PrioritySelector", menuName = "Game/NPC Behaviour/Skill Selectors/Priority Based")]
    public class PrioritySkillSelector : SkillSelector
    {
        [System.Serializable]
        public class PrioritySkill
        {
            public string skillName;
            [Tooltip("Lower priority number = higher priority (1 is highest)")]
            public int priority = 1;
        }

        [Header("Priority List")]
        [Tooltip("Skills ordered by priority (top = highest priority)")]
        public List<PrioritySkill> prioritySkills = new();

        public override NetworkedSkillInstance SelectSkill(BehaviourContext context, List<NetworkedSkillInstance> availableSkills)
        {
            if (availableSkills == null || availableSkills.Count == 0) return null;

            // Sort by priority (lower number = higher priority)
            var sortedPriorities = prioritySkills.OrderBy(p => p.priority);

            foreach (var prioritySkill in sortedPriorities)
            {
                var skill = availableSkills.FirstOrDefault(s => 
                    s != null && 
                    s.skillName == prioritySkill.skillName && 
                    !s.IsOnCooldown
                );

                if (skill != null) return skill;
            }

            return null;
        }
    }
}
