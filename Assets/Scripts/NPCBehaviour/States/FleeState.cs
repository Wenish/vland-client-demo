using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace NPCBehaviour
{
    /// <summary>
    /// Flee state where NPC runs away from threats.
    /// Useful for low-health behaviour or scared NPCs.
    /// </summary>
    [CreateAssetMenu(fileName = "StateFlee", menuName = "Game/NPC Behaviour/States/Flee")]
    public class FleeState : BehaviourState
    {
        [Header("Flee Behaviour")]
        [Tooltip("Transitions from this state")]
        public List<BehaviourTransition> transitions = new();

        [Header("Flee Parameters")]
        [Tooltip("Distance to flee from target")]
        public float fleeDistance = 15f;

        [Tooltip("How often to recalculate flee direction")]
        public float recalculateInterval = 1f;

        [Tooltip("Range to detect threats")]
        public float threatDetectionRange = 20f;

        public override void OnEnter(BehaviourContext context)
        {
            context.IsMoving = true;
            context.SetStateData("lastRecalculateTime", 0f);
            CalculateFleeDestination(context);
        }

        public override bool OnUpdate(BehaviourContext context, float deltaTime)
        {
            context.TimeInState += deltaTime;

            float lastRecalculate = context.GetStateData("lastRecalculateTime", 0f);
            if (Time.time - lastRecalculate > recalculateInterval)
            {
                CalculateFleeDestination(context);
                context.SetStateData("lastRecalculateTime", Time.time);
            }

            MoveTowardsFleePoint(context);

            return true;
        }

        public override void OnExit(BehaviourContext context)
        {
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

        private void CalculateFleeDestination(BehaviourContext context)
        {
            // Find nearby threats
            var allUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            var threats = allUnits
                .Where(u => u != null && u != context.Unit && u.team != context.Team && !u.IsDead)
                .Where(u => Vector3.Distance(context.Position, u.transform.position) <= threatDetectionRange)
                .ToList();

            if (threats.Count == 0)
            {
                // No threats, just move away from current position
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0f;
                randomDirection.Normalize();
                context.CurrentDestination = context.Position + randomDirection * fleeDistance;
                return;
            }

            // Calculate average threat position
            Vector3 averageThreatPos = Vector3.zero;
            foreach (var threat in threats)
            {
                averageThreatPos += threat.transform.position;
            }
            averageThreatPos /= threats.Count;

            // Flee in opposite direction
            Vector3 fleeDirection = (context.Position - averageThreatPos).normalized;
            Vector3 fleePoint = context.Position + fleeDirection * fleeDistance;

            // Try to find valid NavMesh position
            if (NavMesh.SamplePosition(fleePoint, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
            {
                context.CurrentDestination = hit.position;
            }
            else
            {
                context.CurrentDestination = fleePoint;
            }
        }

        private void MoveTowardsFleePoint(BehaviourContext context)
        {
            Vector3 destination = context.CurrentDestination;
            NavMesh.CalculatePath(context.Position, destination, NavMesh.AllAreas, context.CurrentPath);

            if (context.CurrentPath.corners.Length < 2)
            {
                context.Unit.horizontalInput = 0f;
                context.Unit.verticalInput = 0f;
                return;
            }

            Vector3 nextWaypoint = context.CurrentPath.corners[1];
            Vector3 direction = nextWaypoint - context.Position;
            direction.y = 0f;
            direction.Normalize();

            context.Unit.horizontalInput = direction.x;
            context.Unit.verticalInput = direction.z;

            // Set facing direction
            float angle = -Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg - 90f;
            context.Unit.angle = angle;
        }
    }
}
