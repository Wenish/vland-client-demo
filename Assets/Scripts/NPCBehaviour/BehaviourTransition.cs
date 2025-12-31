using System.Collections.Generic;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Defines a transition between behaviour states.
    /// Transitions are data-driven and can have multiple conditions (all must be true).
    /// </summary>
    [CreateAssetMenu(fileName = "NewTransition", menuName = "Game/NPC Behaviour/Transition")]
    public class BehaviourTransition : ScriptableObject
    {
        [Header("Transition")]
        [Tooltip("Target state to transition to")]
        public BehaviourState targetState;

        [Tooltip("All conditions must be true for this transition to trigger")]
        public List<BehaviourCondition> conditions = new();

        [Tooltip("Priority when multiple transitions are valid (higher = more important)")]
        public int priority = 0;

        /// <summary>
        /// Check if all conditions for this transition are met.
        /// </summary>
        public bool CanTransition(BehaviourContext context)
        {
            if (targetState == null) return false;

            foreach (var condition in conditions)
            {
                if (condition == null) continue;
                if (!condition.Evaluate(context))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
