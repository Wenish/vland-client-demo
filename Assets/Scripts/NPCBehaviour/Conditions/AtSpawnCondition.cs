using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Condition that checks if NPC is at or near spawn position.
    /// Used to determine when reset is complete.
    /// </summary>
    [CreateAssetMenu(fileName = "AtSpawnCondition", menuName = "Game/NPC Behaviour/Conditions/At Spawn")]
    public class AtSpawnCondition : BehaviourCondition
    {
        [Header("Spawn Check")]
        [Tooltip("Distance from spawn to be considered 'at spawn'")]
        public float threshold = 0.5f;

        public override bool Evaluate(BehaviourContext context)
        {
            float distanceFromSpawn = Vector3.Distance(context.Position, context.SpawnPosition);
            return distanceFromSpawn <= threshold;
        }
    }
}
