using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Condition based on whether a target exists and is valid.
    /// </summary>
    [CreateAssetMenu(fileName = "HasTargetCondition", menuName = "Game/NPC Behaviour/Conditions/Has Target")]
    public class HasTargetCondition : BehaviourCondition
    {
        [Header("Target Check")]
        [Tooltip("If true, condition passes when target exists. If false, passes when no target.")]
        public bool shouldHaveTarget = true;

        [Tooltip("If true, also checks that target is alive")]
        public bool requireAlive = true;

        public override bool Evaluate(BehaviourContext context)
        {
            bool hasValidTarget = context.CurrentTarget != null && 
                                 (!requireAlive || !context.CurrentTarget.IsDead);

            return shouldHaveTarget ? hasValidTarget : !hasValidTarget;
        }
    }
}
