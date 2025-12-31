using System.Collections.Generic;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Defines health thresholds and associated behaviour profiles for boss phases.
    /// Allows bosses to change behaviour patterns as they lose health.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHealthPhaseProfile", menuName = "Game/NPC Behaviour/Health Phase Profile")]
    public class HealthPhaseProfile : ScriptableObject
    {
        [System.Serializable]
        public class HealthPhase
        {
            [Tooltip("Name of this phase (for debugging)")]
            public string phaseName;

            [Range(0f, 1f)]
            [Tooltip("Health percentage threshold to trigger this phase (0 = 0%, 1 = 100%)")]
            public float healthThreshold;

            [Tooltip("Behaviour profile to use during this phase")]
            public BehaviourProfile behaviourProfile;

            [Header("Optional Events")]
            [Tooltip("Skills to add when entering this phase")]
            public List<string> skillsToAdd = new();

            [Tooltip("Skills to remove when entering this phase")]
            public List<string> skillsToRemove = new();

            [Header("Visual/Audio (Optional)")]
            [Tooltip("Play an effect when this phase begins")]
            public GameObject phaseTransitionEffect;

            [TextArea(2, 3)]
            [Tooltip("Debug/designer notes about this phase")]
            public string notes;
        }

        [Header("Phase Configuration")]
        [Tooltip("Phases ordered by health threshold (highest to lowest)")]
        public List<HealthPhase> phases = new();

        [Header("Settings")]
        [Tooltip("Can phases be repeated? If false, each phase can only trigger once.")]
        public bool allowPhaseRepeat = false;

        /// <summary>
        /// Get the appropriate phase for the given health percentage.
        /// Returns null if no phase matches.
        /// </summary>
        public HealthPhase GetPhaseForHealth(float healthPercent, HashSet<int> completedPhases = null)
        {
            // Sort phases by threshold (descending) and find first match
            for (int i = 0; i < phases.Count; i++)
            {
                var phase = phases[i];
                
                // Skip if this phase was already completed and repeats aren't allowed
                if (!allowPhaseRepeat && completedPhases != null && completedPhases.Contains(i))
                    continue;

                // Check if health has dropped below this threshold
                if (healthPercent <= phase.healthThreshold)
                {
                    return phase;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the index of a specific phase.
        /// </summary>
        public int GetPhaseIndex(HealthPhase phase)
        {
            return phases.IndexOf(phase);
        }

        /// <summary>
        /// Validate the phase profile configuration.
        /// </summary>
        public bool Validate()
        {
            if (phases == null || phases.Count == 0)
            {
                Debug.LogError($"HealthPhaseProfile '{name}' has no phases defined!", this);
                return false;
            }

            for (int i = 0; i < phases.Count; i++)
            {
                var phase = phases[i];
                if (phase.behaviourProfile == null)
                {
                    Debug.LogError($"HealthPhaseProfile '{name}' phase {i} has no behaviour profile!", this);
                    return false;
                }

                if (!phase.behaviourProfile.Validate())
                {
                    Debug.LogError($"HealthPhaseProfile '{name}' phase {i} has invalid behaviour profile!", this);
                    return false;
                }
            }

            return true;
        }
    }
}
