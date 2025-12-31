using System.Linq;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Condition based on number of nearby enemies.
    /// Useful for triggering flee or aggressive behaviour based on threat level.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyCountCondition", menuName = "Game/NPC Behaviour/Conditions/Enemy Count")]
    public class EnemyCountCondition : BehaviourCondition
    {
        public enum ComparisonType
        {
            LessThan,
            GreaterThan,
            Equals
        }

        [Header("Enemy Count Check")]
        public ComparisonType comparison = ComparisonType.GreaterThan;
        public int enemyCount = 3;

        [Tooltip("Range to detect enemies")]
        public float detectionRange = 15f;

        public override bool Evaluate(BehaviourContext context)
        {
            var allUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            int count = allUnits
                .Count(u => u != null && 
                           u != context.Unit && 
                           u.team != context.Team && 
                           !u.IsDead &&
                           Vector3.Distance(context.Position, u.transform.position) <= detectionRange);

            switch (comparison)
            {
                case ComparisonType.LessThan:
                    return count < enemyCount;
                case ComparisonType.GreaterThan:
                    return count > enemyCount;
                case ComparisonType.Equals:
                    return count == enemyCount;
                default:
                    return false;
            }
        }
    }
}
