using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Condition based on time spent in current state.
    /// </summary>
    [CreateAssetMenu(fileName = "TimeInStateCondition", menuName = "Game/NPC Behaviour/Conditions/Time In State")]
    public class TimeInStateCondition : BehaviourCondition
    {
        [Header("Time Check")]
        [Tooltip("Minimum time in seconds that must pass in current state")]
        public float minTimeInState = 5f;

        public override bool Evaluate(BehaviourContext context)
        {
            return context.TimeInState >= minTimeInState;
        }
    }
}
