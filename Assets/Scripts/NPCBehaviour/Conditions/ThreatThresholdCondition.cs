using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Condition that checks if threat values meet certain thresholds.
    /// Useful for triggering special behaviors at threat levels.
    /// </summary>
    [CreateAssetMenu(fileName = "ThreatThresholdCondition", menuName = "Game/NPC Behaviour/Conditions/Threat Threshold")]
    public class ThreatThresholdCondition : BehaviourCondition
    {
        public enum ComparisonType
        {
            LessThan,
            LessThanOrEqual,
            Equal,
            GreaterThanOrEqual,
            GreaterThan
        }

        public enum ThresholdMode
        {
            TargetAboveThreshold,    // Specific target's threat > threshold
            AnyAboveThreshold,       // Any target's threat > threshold
            TotalTargetCount,        // Number of targets with threat
            HighestThreatValue       // Highest threat value in table
        }

        [Header("Threshold Settings")]
        [Tooltip("What to check")]
        public ThresholdMode mode = ThresholdMode.TargetAboveThreshold;

        [Tooltip("Comparison type")]
        public ComparisonType comparison = ComparisonType.GreaterThan;

        [Tooltip("Threshold value to compare against")]
        public float threshold = 50f;

        [Header("Target Settings")]
        [Tooltip("Use current target (only for TargetAboveThreshold mode)")]
        public bool useCurrentTarget = true;

        public override bool Evaluate(BehaviourContext context)
        {
            // Requires threat system
            if (!context.HasThreatSystem)
                return false;

            float valueToCheck = 0f;

            switch (mode)
            {
                case ThresholdMode.TargetAboveThreshold:
                    if (!useCurrentTarget || context.CurrentTarget == null)
                        return false;
                    valueToCheck = context.GetThreat(context.CurrentTarget);
                    break;

                case ThresholdMode.AnyAboveThreshold:
                    var threats = context.ThreatManager.ThreatTable.GetThreatList();
                    foreach (var (unit, threat) in threats)
                    {
                        if (CompareValue(threat, threshold, comparison))
                            return true;
                    }
                    return false;

                case ThresholdMode.TotalTargetCount:
                    valueToCheck = context.GetThreatTargetCount();
                    break;

                case ThresholdMode.HighestThreatValue:
                    var highestTarget = context.GetHighestThreatTarget();
                    if (highestTarget == null)
                        return false;
                    valueToCheck = context.GetThreat(highestTarget);
                    break;
            }

            return CompareValue(valueToCheck, threshold, comparison);
        }

        private bool CompareValue(float value, float threshold, ComparisonType comparison)
        {
            switch (comparison)
            {
                case ComparisonType.LessThan:
                    return value < threshold;
                case ComparisonType.LessThanOrEqual:
                    return value <= threshold;
                case ComparisonType.Equal:
                    return Mathf.Approximately(value, threshold);
                case ComparisonType.GreaterThanOrEqual:
                    return value >= threshold;
                case ComparisonType.GreaterThan:
                    return value > threshold;
                default:
                    return false;
            }
        }
    }
}
