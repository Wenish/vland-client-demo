using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Condition that checks if a specific target has the highest threat.
    /// Useful for transitions like "Switch to attack state if current target has highest threat".
    /// </summary>
    [CreateAssetMenu(fileName = "HighestThreatCondition", menuName = "Game/NPC Behaviour/Conditions/Highest Threat")]
    public class HighestThreatCondition : BehaviourCondition
    {
        [Header("Highest Threat Check")]
        [Tooltip("If true, checks if current target has highest threat. If false, gets highest threat target.")]
        public bool checkCurrentTarget = true;

        [Tooltip("Store the highest threat target in context for use by states")]
        public bool updateCurrentTarget = true;

        public override bool Evaluate(BehaviourContext context)
        {
            // Requires threat system
            if (!context.HasThreatSystem)
                return false;

            // No targets in threat table
            if (context.GetThreatTargetCount() == 0)
                return false;

            var highestThreatTarget = context.GetHighestThreatTarget();
            
            if (highestThreatTarget == null)
                return false;

            // Update context if requested
            if (updateCurrentTarget)
            {
                context.CurrentTarget = highestThreatTarget;
            }

            // Check mode
            if (checkCurrentTarget)
            {
                // Does current target have the highest threat?
                return context.CurrentTarget != null && 
                       context.CurrentTarget == highestThreatTarget;
            }
            else
            {
                // Just check if we have any target with threat
                return true;
            }
        }
    }
}
