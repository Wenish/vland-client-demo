using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Randomly selects from available skills, with optional weighting.
    /// Good for adding unpredictability to boss fights or advanced NPCs.
    /// </summary>
    [CreateAssetMenu(fileName = "RandomWeightedSelector", menuName = "Game/NPC Behaviour/Skill Selectors/Random Weighted")]
    public class RandomWeightedSkillSelector : SkillSelector
    {
        [System.Serializable]
        public class WeightedSkill
        {
            public string skillName;
            [Tooltip("Higher weight = more likely to be selected")]
            public float weight = 1f;
        }

        [Header("Weighted Skills")]
        public List<WeightedSkill> weightedSkills = new();

        [Header("Options")]
        [Tooltip("If true, only considers skills that are off cooldown")]
        public bool respectCooldowns = true;

        public override NetworkedSkillInstance SelectSkill(BehaviourContext context, List<NetworkedSkillInstance> availableSkills)
        {
            if (availableSkills == null || availableSkills.Count == 0) return null;

            // Build list of valid weighted skills
            var validSkills = new List<(NetworkedSkillInstance skill, float weight)>();

            foreach (var weighted in weightedSkills)
            {
                var skill = availableSkills.FirstOrDefault(s => s != null && s.skillName == weighted.skillName);
                if (skill != null && (!respectCooldowns || !skill.IsOnCooldown))
                {
                    validSkills.Add((skill, weighted.weight));
                }
            }

            if (validSkills.Count == 0) return null;

            // Calculate total weight
            float totalWeight = validSkills.Sum(s => s.weight);
            if (totalWeight <= 0) return validSkills[0].skill;

            // Random selection based on weight
            float randomValue = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            foreach (var (skill, weight) in validSkills)
            {
                cumulativeWeight += weight;
                if (randomValue <= cumulativeWeight)
                {
                    return skill;
                }
            }

            return validSkills[validSkills.Count - 1].skill;
        }
    }
}
