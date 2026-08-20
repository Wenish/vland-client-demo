using NaughtyAttributes;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Base ScriptableObject for conditions that determine state transitions.
    /// Conditions are stateless and reusable across different transitions.
    /// </summary>
    public abstract class BehaviourCondition : ScriptableObject
    {
        [ResizableTextArea]
        public string description;

        /// <summary>
        /// Evaluate whether this condition is satisfied.
        /// </summary>
        public abstract bool Evaluate(BehaviourContext context);
    }
}
