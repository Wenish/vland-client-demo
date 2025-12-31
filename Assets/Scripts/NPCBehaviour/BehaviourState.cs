using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Base ScriptableObject representing a single behaviour state.
    /// States are stateless and data-driven - all runtime data is stored in BehaviourContext.
    /// States decide what the NPC should do but don't execute game logic directly.
    /// </summary>
    public abstract class BehaviourState : ScriptableObject
    {
        [Header("State Info")]
        [Tooltip("Unique identifier for this state")]
        public string stateId;
        
        [TextArea(2, 4)]
        [Tooltip("Description of what this state does")]
        public string description;

        /// <summary>
        /// Called when this state becomes active. Use to initialize state-specific data.
        /// </summary>
        public virtual void OnEnter(BehaviourContext context) { }

        /// <summary>
        /// Called every frame while this state is active.
        /// Returns true if the state wants to continue, false to trigger exit.
        /// </summary>
        public virtual bool OnUpdate(BehaviourContext context, float deltaTime)
        {
            return true;
        }

        /// <summary>
        /// Called when this state is being exited.
        /// </summary>
        public virtual void OnExit(BehaviourContext context) { }

        /// <summary>
        /// Evaluate all transitions for this state and return the first valid one.
        /// </summary>
        public virtual BehaviourTransition EvaluateTransitions(BehaviourContext context)
        {
            return null;
        }
    }
}
