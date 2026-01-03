using System.Collections.Generic;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Idle state where NPC does nothing and waits.
    /// Can be used as a default state or for stationary NPCs.
    /// </summary>
    [CreateAssetMenu(fileName = "StateIdle", menuName = "Game/NPC Behaviour/States/Idle")]
    public class IdleState : BehaviourState
    {
        [Header("Idle Behaviour")]
        [Tooltip("Transitions from this state")]
        public List<BehaviourTransition> transitions = new();

        [Tooltip("Should the NPC look around while idle?")]
        public bool lookAround = false;

        [Tooltip("Random rotation speed when looking around")]
        public float rotationSpeed = 30f;

        public override void OnEnter(BehaviourContext context)
        {
            // Stop movement
            context.Unit.horizontalInput = 0f;
            context.Unit.verticalInput = 0f;
            context.IsMoving = false;
        }

        public override bool OnUpdate(BehaviourContext context, float deltaTime)
        {
            context.TimeInState += deltaTime;

            // Optional: Make NPC look around while idle
            if (lookAround)
            {
                float randomRotation = Mathf.Sin(Time.time * rotationSpeed * Mathf.Deg2Rad) * 45f;
                context.Unit.angle = randomRotation;
            }

            return true;
        }

        public override void OnExit(BehaviourContext context)
        {
            // Nothing to clean up
        }

        public override BehaviourTransition EvaluateTransitions(BehaviourContext context)
        {
            // Evaluate state-specific transitions
            foreach (var transition in transitions)
            {
                if (transition != null && transition.CanTransition(context))
                {
                    return transition;
                }
            }

            return null;
        }
    }
}
