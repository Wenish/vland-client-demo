using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Condition that randomly evaluates to true based on chance.
    /// Useful for adding unpredictability to AI behaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "RandomChanceCondition", menuName = "Game/NPC Behaviour/Conditions/Random Chance")]
    public class RandomChanceCondition : BehaviourCondition
    {
        [Header("Chance")]
        [Range(0f, 1f)]
        [Tooltip("Probability of condition being true (0 = never, 1 = always)")]
        public float chance = 0.5f;

        [Tooltip("Minimum time between evaluations (prevents spam)")]
        public float cooldown = 1f;

        private float _lastEvaluationTime = -999f;

        public override bool Evaluate(BehaviourContext context)
        {
            if (Time.time - _lastEvaluationTime < cooldown)
            {
                return false;
            }

            _lastEvaluationTime = Time.time;
            return Random.value < chance;
        }
    }
}
