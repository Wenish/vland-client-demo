using System.Collections.Generic;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Base class for skill selection strategies.
    /// Skill selectors are data-driven and choose which skill to use based on context.
    /// </summary>
    public abstract class SkillSelector : ScriptableObject
    {
        [TextArea(2, 3)]
        public string selectorDescription;

        /// <summary>
        /// Select an appropriate skill from the available skills.
        /// Returns null if no suitable skill is found.
        /// </summary>
        public abstract NetworkedSkillInstance SelectSkill(BehaviourContext context, List<NetworkedSkillInstance> availableSkills);
    }
}
