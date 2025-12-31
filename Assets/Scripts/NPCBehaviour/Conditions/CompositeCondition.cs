using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Combines multiple conditions with AND or OR logic.
    /// Allows for complex conditional behaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "CompositeCondition", menuName = "Game/NPC Behaviour/Conditions/Composite")]
    public class CompositeCondition : BehaviourCondition
    {
        public enum LogicType
        {
            And,  // All conditions must be true
            Or    // At least one condition must be true
        }

        [Header("Composite Logic")]
        public LogicType logicType = LogicType.And;
        public BehaviourCondition[] conditions;

        public override bool Evaluate(BehaviourContext context)
        {
            if (conditions == null || conditions.Length == 0)
                return false;

            switch (logicType)
            {
                case LogicType.And:
                    foreach (var condition in conditions)
                    {
                        if (condition == null) continue;
                        if (!condition.Evaluate(context))
                            return false;
                    }
                    return true;

                case LogicType.Or:
                    foreach (var condition in conditions)
                    {
                        if (condition == null) continue;
                        if (condition.Evaluate(context))
                            return true;
                    }
                    return false;

                default:
                    return false;
            }
        }
    }
}
