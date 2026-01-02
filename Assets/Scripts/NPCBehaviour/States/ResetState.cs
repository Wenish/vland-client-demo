using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Reset state where NPC returns to spawn position after being pulled too far.
    /// Implements WoW-style leash/reset mechanics.
    /// - Clears threat/aggro
    /// - Heals to full health
    /// - Returns to spawn position
    /// </summary>
    [CreateAssetMenu(fileName = "ResetState", menuName = "Game/NPC Behaviour/States/Reset")]
    public class ResetState : BehaviourState
    {
        [Header("Reset Behaviour")]
        [Tooltip("How fast to move back to spawn (units per second)")]
        public float resetSpeed = 5f;

        [Tooltip("Distance to spawn point before reset completes")]
        public float stoppingDistance = 0.5f;

        [Tooltip("Should we clear threat when resetting?")]
        public bool clearThreat = true;

        [Tooltip("Should we heal to full when reaching spawn?")]
        public bool healOnReset = true;

        public override void OnEnter(BehaviourContext context)
        {
            // Clear current target
            context.CurrentTarget = null;

            // Clear threat/aggro
            if (clearThreat && context.HasThreatSystem)
            {
                context.ThreatManager.ClearAllThreat();
            }

            context.IsMoving = true;
        }

        public override bool OnUpdate(BehaviourContext context, float deltaTime)
        {
            context.TimeInState += deltaTime;

            Vector3 directionToSpawn = (context.SpawnPosition - context.Position).normalized;
            float distanceToSpawn = Vector3.Distance(context.Position, context.SpawnPosition);

            // Move back to spawn
            if (distanceToSpawn > stoppingDistance)
            {
                // Set input for movement
                context.Unit.horizontalInput = directionToSpawn.x;
                context.Unit.verticalInput = directionToSpawn.z;
                context.IsMoving = true;
                return true;
            }

            // Reached spawn - stop moving
            context.Unit.horizontalInput = 0f;
            context.Unit.verticalInput = 0f;
            context.IsMoving = false;

            // Heal to full if configured
            if (healOnReset && context.Health < context.MaxHealth)
            {
                context.Unit.Heal(context.MaxHealth, context.Unit);
            }

            // Exit state to return to Idle
            return false;
        }

        public override void OnExit(BehaviourContext context)
        {
            context.Unit.horizontalInput = 0f;
            context.Unit.verticalInput = 0f;
            context.IsMoving = false;
        }
    }
}
