using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace NPCBehaviour
{
    /// <summary>
    /// Patrol state where NPC moves between waypoints or in a random pattern.
    /// </summary>
    [CreateAssetMenu(fileName = "StatePatrol", menuName = "Game/NPC Behaviour/States/Patrol")]
    public class PatrolState : BehaviourState
    {
        [Header("Patrol Behaviour")]
        [Tooltip("Transitions from this state")]
        public List<BehaviourTransition> transitions = new();

        [Header("Patrol Type")]
        public PatrolType patrolType = PatrolType.Random;

        [Header("Random Patrol")]
        [Tooltip("Range for random waypoints")]
        public float randomWaypointRange = 10f;

        [Tooltip("Time to wait at each waypoint")]
        public float waypointWaitTime = 2f;

        [Header("Fixed Waypoints")]
        [Tooltip("Fixed patrol points (only used if patrolType is FixedWaypoints)")]
        public List<Vector3> waypoints = new();

        [Tooltip("Should patrol loop back to start?")]
        public bool loopWaypoints = true;

        public override void OnEnter(BehaviourContext context)
        {
            context.IsMoving = true;
            context.SetStateData("waypointIndex", 0);
            context.SetStateData("waitTimeRemaining", 0f);
            context.SetStateData("isWaiting", false);
            context.SetStateData("patrolOrigin", context.Position);

            GenerateNextWaypoint(context);
        }

        public override bool OnUpdate(BehaviourContext context, float deltaTime)
        {
            context.TimeInState += deltaTime;

            bool isWaiting = context.GetStateData("isWaiting", false);

            if (isWaiting)
            {
                float waitTime = context.GetStateData("waitTimeRemaining", 0f);
                waitTime -= deltaTime;
                context.SetStateData("waitTimeRemaining", waitTime);

                if (waitTime <= 0f)
                {
                    context.SetStateData("isWaiting", false);
                    GenerateNextWaypoint(context);
                }

                return true;
            }

            // Move towards current destination
            if (MoveTowardsDestination(context))
            {
                // Reached waypoint
                context.SetStateData("isWaiting", true);
                context.SetStateData("waitTimeRemaining", waypointWaitTime);
                context.Unit.horizontalInput = 0f;
                context.Unit.verticalInput = 0f;
            }

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

        private void GenerateNextWaypoint(BehaviourContext context)
        {
            switch (patrolType)
            {
                case PatrolType.Random:
                    GenerateRandomWaypoint(context);
                    break;
                case PatrolType.FixedWaypoints:
                    GenerateFixedWaypoint(context);
                    break;
            }
        }

        private void GenerateRandomWaypoint(BehaviourContext context)
        {
            Vector3 origin = context.GetStateData("patrolOrigin", context.Position);
            Vector3 randomPoint = origin + Random.insideUnitSphere * randomWaypointRange;
            randomPoint.y = origin.y; // Keep same Y level

            // Try to find valid NavMesh position
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, randomWaypointRange, NavMesh.AllAreas))
            {
                context.CurrentDestination = hit.position;
            }
            else
            {
                context.CurrentDestination = origin;
            }
        }

        private void GenerateFixedWaypoint(BehaviourContext context)
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                // No waypoints defined, stay in place
                context.CurrentDestination = context.Position;
                return;
            }

            int currentIndex = context.GetStateData("waypointIndex", 0);
            context.CurrentDestination = waypoints[currentIndex];

            // Move to next waypoint
            currentIndex++;
            if (currentIndex >= waypoints.Count)
            {
                currentIndex = loopWaypoints ? 0 : waypoints.Count - 1;
            }
            context.SetStateData("waypointIndex", currentIndex);
        }

        private bool MoveTowardsDestination(BehaviourContext context)
        {
            Vector3 destination = context.CurrentDestination;
            NavMesh.CalculatePath(context.Position, destination, NavMesh.AllAreas, context.CurrentPath);

            if (context.CurrentPath.corners.Length < 2)
            {
                return true; // Already at destination
            }

            Vector3 nextWaypoint = context.CurrentPath.corners[1];
            float distanceToDestination = Vector3.Distance(context.Position, destination);

            // Check if reached destination
            if (distanceToDestination < 1f)
            {
                return true;
            }

            // Move towards next waypoint
            Vector3 direction = nextWaypoint - context.Position;
            direction.y = 0f;
            direction.Normalize();

            context.Unit.horizontalInput = direction.x;
            context.Unit.verticalInput = direction.z;

            // Set facing direction
            float angle = -Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg - 90f;
            context.Unit.angle = angle;

            return false;
        }
    }

    public enum PatrolType
    {
        Random,
        FixedWaypoints
    }
}
