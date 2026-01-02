using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Condition based on distance to target.
    /// </summary>
    [CreateAssetMenu(fileName = "DistanceCondition", menuName = "Game/NPC Behaviour/Conditions/Distance")]
    public class DistanceCondition : BehaviourCondition
    {
        public enum ComparisonType
        {
            LessThan,
            GreaterThan,
            Between
        }

        [Header("Distance Check")]
        public ComparisonType comparison = ComparisonType.LessThan;
        public float distance = 5f;
        
        [Tooltip("Only used for Between comparison")]
        public float maxDistance = 10f;

        [Tooltip("Check distance to current target (if false, checks if ANY enemy is in range)")]
        public bool useCurrentTarget = true;

        public override bool Evaluate(BehaviourContext context)
        {
            float dist;

            if (useCurrentTarget)
            {
                if (context.CurrentTarget == null) return false;
                dist = context.DistanceToTarget();
            }
            else
            {
                // Find closest enemy
                var allUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
                float closestDist = float.MaxValue;

                foreach (var unit in allUnits)
                {
                    if (unit == null || unit == context.Unit || unit.team == context.Team || unit.IsDead)
                        continue;

                    float d = Vector3.Distance(context.Position, unit.transform.position);
                    if (d < closestDist)
                        closestDist = d;
                }

                dist = closestDist;
            }

            switch (comparison)
            {
                case ComparisonType.LessThan:
                    return dist < distance;
                case ComparisonType.GreaterThan:
                    return dist > distance;
                case ComparisonType.Between:
                    return dist >= distance && dist <= maxDistance;
                default:
                    return false;
            }
        }
    }
}
