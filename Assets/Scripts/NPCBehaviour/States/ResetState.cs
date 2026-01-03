using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace NPCBehaviour
{
    /// <summary>
    /// Reset state where NPC returns to spawn position after being pulled too far.
    /// Implements WoW-style leash/reset mechanics.
    /// - Clears threat/aggro
    /// - Heals to full health
    /// - Returns to spawn position
    /// </summary>
    [CreateAssetMenu(fileName = "StateReset", menuName = "Game/NPC Behaviour/States/Reset")]
    public class ResetState : BehaviourState
    {
        [Header("Transitions")]
        [Tooltip("Transitions from this state (typically back to Idle)")]
        public List<BehaviourTransition> transitions = new();

        [Header("Reset Behaviour")]
        [Tooltip("How fast to move back to spawn (units per second)")]
        public float resetSpeed = 5f;

        [Tooltip("Distance to spawn point before reset completes")]
        public float stoppingDistance = 0.5f;

        [Tooltip("Should we clear threat when resetting?")]
        public bool clearThreat = true;

        [Tooltip("Should we heal to full health on reset?")]
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

            // Move back to spawn using NavMesh
            if (distanceToSpawn > stoppingDistance)
            {
                MoveTowardsSpawn(context);
                context.IsMoving = true;
            }
            else
            {
                // Reached spawn - stop moving
                context.Unit.horizontalInput = 0f;
                context.Unit.verticalInput = 0f;
                context.IsMoving = false;
            }

            // Heal to full if configured
            if ( healOnReset && context.Health < context.MaxHealth)
            {
                context.Unit.Heal(context.MaxHealth, context.Unit);
            }

            // Stay in this state - let transitions handle exit
            return true;
        }

        public override void OnExit(BehaviourContext context)
        {
            // Clear threat/aggro
            if (clearThreat && context.HasThreatSystem)
            {
                context.ThreatManager.ClearAllThreat();
            }

            context.Unit.horizontalInput = 0f;
            context.Unit.verticalInput = 0f;
            context.IsMoving = false;
        }

        public override BehaviourTransition EvaluateTransitions(BehaviourContext context)
        {
            foreach (var transition in transitions)
            {
                if (transition != null && transition.CanTransition(context))
                {
                    return transition;
                }
            }

            return null;
        }

        private void MoveTowardsSpawn(BehaviourContext context)
            {
                Vector3 spawnPos = context.SpawnPosition;
                context.CurrentDestination = spawnPos;

                // Calculate path to spawn
                NavMesh.CalculatePath(context.Position, spawnPos, NavMesh.AllAreas, context.CurrentPath);

                if (context.CurrentPath.corners.Length < 2)
                {
                    context.Unit.horizontalInput = 0f;
                    context.Unit.verticalInput = 0f;
                    return;
                }

                // Get next waypoint
                Vector3 nextWaypoint = context.CurrentPath.corners[1];

                // Move towards next waypoint
                Vector3 direction = nextWaypoint - context.Position;
                direction.y = 0f;
                direction.Normalize();

                context.Unit.horizontalInput = direction.x;
                context.Unit.verticalInput = direction.z;

                // Set facing direction
                float moveAngle = -Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg - 90f + 180f;
                context.Unit.angle = moveAngle;
            }
    }
}
