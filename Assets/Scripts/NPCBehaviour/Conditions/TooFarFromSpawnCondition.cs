using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Condition that checks if NPC is too far from spawn point.
    /// Used to trigger reset/leash behavior when enemies are kited too far.
    /// </summary>
    [CreateAssetMenu(fileName = "TooFarFromSpawnCondition", menuName = "Game/NPC Behaviour/Conditions/Too Far From Spawn")]
    public class TooFarFromSpawnCondition : BehaviourCondition
    {
        [Header("Reset Distance")]
        [Tooltip("Distance from spawn point before reset triggers")]
        public float resetDistance = 50f;

        public override bool Evaluate(BehaviourContext context)
        {
            float distanceFromSpawn = Vector3.Distance(context.Position, context.SpawnPosition);
            return distanceFromSpawn > resetDistance;
        }
    }
}
