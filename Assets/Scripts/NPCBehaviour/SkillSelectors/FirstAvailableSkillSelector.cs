using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Selects the first skill that is off cooldown.
    /// Simple but effective for basic NPCs.
    /// </summary>
    [CreateAssetMenu(fileName = "FirstAvailableSelector", menuName = "Game/NPC Behaviour/Skill Selectors/First Available")]
    public class FirstAvailableSkillSelector : SkillSelector
    {
        public override NetworkedSkillInstance SelectSkill(BehaviourContext context, List<NetworkedSkillInstance> availableSkills)
        {
            if (availableSkills == null || availableSkills.Count == 0) return null;

            foreach (var skill in availableSkills)
            {
                if (skill != null && !skill.IsOnCooldown)
                {
                    return skill;
                }
            }

            return null;
        }
    }
}
