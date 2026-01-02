using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Soft leash condition - triggers when NPC is too far from spawn but hasn't reset yet.
    /// Can be used to slow movement or begin returning without full reset.
    /// </summary>
    [CreateAssetMenu(fileName = "LeashCondition", menuName = "Game/NPC Behaviour/Conditions/Leash")]
    public class LeashCondition : BehaviourCondition
    {
        [Header("Leash Distance")]
        [Tooltip("Distance from spawn before leash triggers (should be less than reset distance)")]
        public float leashDistance = 35f;

        public override bool Evaluate(BehaviourContext context)
        {
            float distanceFromSpawn = Vector3.Distance(context.Position, context.SpawnPosition);
            return distanceFromSpawn > leashDistance;
        }
    }
}
