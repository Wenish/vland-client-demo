using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Selects skills based on distance to target.
    /// Uses skill names or tags to identify which skills work at which ranges.
    /// </summary>
    [CreateAssetMenu(fileName = "DistanceBasedSelector", menuName = "Game/NPC Behaviour/Skill Selectors/Distance Based")]
    public class DistanceBasedSkillSelector : SkillSelector
    {
        [System.Serializable]
        public class DistanceSkillMapping
        {
            public float minDistance;
            public float maxDistance;
            public List<string> skillNames = new();
        }

        [Header("Distance Ranges")]
        public List<DistanceSkillMapping> distanceMappings = new();

        [Header("Fallback")]
        [Tooltip("If true, will select any available skill if no distance match is found")]
        public bool allowFallback = true;

        public override NetworkedSkillInstance SelectSkill(BehaviourContext context, List<NetworkedSkillInstance> availableSkills)
        {
            if (availableSkills == null || availableSkills.Count == 0) return null;
            if (context.CurrentTarget == null) return null;

            float distance = context.DistanceToTarget();

            // Find skills that match the current distance
            foreach (var mapping in distanceMappings.OrderBy(m => m.minDistance))
            {
                if (distance >= mapping.minDistance && distance <= mapping.maxDistance)
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
            }

            // Fallback to any available skill
            if (allowFallback)
            {
                return availableSkills.FirstOrDefault(s => s != null && !s.IsOnCooldown);
            }

            return null;
        }
    }
}
