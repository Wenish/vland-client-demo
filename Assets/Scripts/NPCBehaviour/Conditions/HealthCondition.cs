using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Condition based on NPC health percentage.
    /// </summary>
    [CreateAssetMenu(fileName = "HealthCondition", menuName = "Game/NPC Behaviour/Conditions/Health")]
    public class HealthCondition : BehaviourCondition
    {
        public enum ComparisonType
        {
            LessThan,
            GreaterThan,
            Between
        }

        [Header("Health Check")]
        public ComparisonType comparison = ComparisonType.LessThan;

        [Range(0f, 1f)]
        [Tooltip("Health percentage threshold (0 = 0%, 1 = 100%)")]
        public float healthPercent = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Only used for Between comparison")]
        public float maxHealthPercent = 1f;

        public override bool Evaluate(BehaviourContext context)
        {
            float currentHealthPercent = context.HealthPercent;

            switch (comparison)
            {
                case ComparisonType.LessThan:
                    return currentHealthPercent < healthPercent;
                case ComparisonType.GreaterThan:
                    return currentHealthPercent > healthPercent;
                case ComparisonType.Between:
                    return currentHealthPercent >= healthPercent && currentHealthPercent <= maxHealthPercent;
                default:
                    return false;
            }
        }
    }
}
