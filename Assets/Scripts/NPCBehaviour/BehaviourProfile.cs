using System.Collections.Generic;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// A behaviour profile defines which states an NPC can use and the transitions between them.
    /// Profiles are data-driven and can be swapped at runtime for dynamic behaviour changes.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBehaviourProfile", menuName = "Game/NPC Behaviour/Behaviour Profile")]
    public class BehaviourProfile : ScriptableObject
    {
        [Header("Profile Info")]
        public string profileName;

        [TextArea(2, 4)]
        public string profileDescription;

        [Header("States")]
        [Tooltip("Initial state when this profile becomes active")]
        public BehaviourState initialState;

        [Tooltip("All available states in this profile")]
        public List<BehaviourState> availableStates = new();

        [Header("Global Transitions")]
        [Tooltip("Transitions that can trigger from any state")]
        public List<BehaviourTransition> globalTransitions = new();

        /// <summary>
        /// Get the initial state for this profile.
        /// </summary>
        public BehaviourState GetInitialState()
        {
            return initialState;
        }

        /// <summary>
        /// Validate that the profile is properly configured.
        /// </summary>
        public bool Validate()
        {
            if (initialState == null)
            {
                Debug.LogError($"BehaviourProfile '{name}' has no initial state!", this);
                return false;
            }

            if (!availableStates.Contains(initialState))
            {
                Debug.LogWarning($"BehaviourProfile '{name}' initial state is not in available states list!", this);
            }

            return true;
        }
    }
}
