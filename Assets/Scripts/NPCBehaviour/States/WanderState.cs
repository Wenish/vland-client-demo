using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace NPCBehaviour
{
    /// <summary>
    /// Wander state where NPC casually roams around near their spawn location.
    /// More natural and organic than patrol, with random pauses and casual movement.
    /// Stays within leash range of spawn position.
    /// </summary>
    [CreateAssetMenu(fileName = "StateWander", menuName = "Game/NPC Behaviour/States/Wander")]
    public class WanderState : BehaviourState
    {
        [Header("Wander Behaviour")]
        [Tooltip("Transitions from this state")]
        public List<BehaviourTransition> transitions = new();

        [Header("Wander Range")]
        [Tooltip("Maximum distance from spawn position to wander")]
        [Range(1f, 50f)]
        public float wanderRadius = 10f;

        [Tooltip("Minimum distance to move per wander (prevents tiny movements)")]
        [Range(0.5f, 10f)]
        public float minWanderDistance = 2f;

        [Header("Movement Speed")]
        [Tooltip("Speed multiplier for wandering (slower than combat/patrol)")]
        [Range(0.1f, 1f)]
        public float wanderSpeedMultiplier = 0.5f;

        [Header("Pause Behavior")]
        [Tooltip("Minimum time to pause at each destination")]
        [Range(0f, 10f)]
        public float minPauseTime = 1f;

        [Tooltip("Maximum time to pause at each destination")]
        [Range(0f, 20f)]
        public float maxPauseTime = 5f;

        [Tooltip("Chance to pause when reaching destination (0-1)")]
        [Range(0f, 1f)]
        public float pauseChance = 0.7f;

        [Header("Look Around")]
        [Tooltip("Should the NPC look around while paused?")]
        public bool lookAroundWhilePaused = true;

        [Tooltip("Random rotation speed when looking around")]
        [Range(10f, 90f)]
        public float lookAroundSpeed = 30f;

        [Header("State Duration")]
        [Tooltip("Optional: Maximum time to stay in wander state (0 = infinite)")]
        public float maxWanderTime = 0f;

        public override void OnEnter(BehaviourContext context)
        {
            context.IsMoving = false;
            context.SetStateData("isPaused", false);
            context.SetStateData("pauseTimeRemaining", 0f);
            context.SetStateData("wanderOrigin", context.SpawnPosition);
            context.SetStateData("lookAroundOffset", Random.Range(0f, 360f));

            // Start with a pause or immediately pick a destination
            if (Random.value < pauseChance)
            {
                StartPause(context);
            }
            else
            {
                PickNewDestination(context);
            }
        }

        public override bool OnUpdate(BehaviourContext context, float deltaTime)
        {
            context.TimeInState += deltaTime;

            // Optional: Exit if max wander time exceeded
            if (maxWanderTime > 0f && context.TimeInState >= maxWanderTime)
            {
                return false;
            }

            bool isPaused = context.GetStateData("isPaused", false);

            if (isPaused)
            {
                UpdatePause(context, deltaTime);
            }
            else
            {
                UpdateMovement(context);
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

        private void PickNewDestination(BehaviourContext context)
        {
            Vector3 wanderOrigin = context.GetStateData("wanderOrigin", context.SpawnPosition);
            Vector3 randomPoint = Vector3.zero;
            bool foundValidPoint = false;
            int attempts = 0;
            const int maxAttempts = 10;

            // Try to find a valid wander point
            while (!foundValidPoint && attempts < maxAttempts)
            {
                attempts++;

                // Generate random point within wander radius
                Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
                randomPoint = wanderOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);

                // Ensure minimum distance
                float distance = Vector3.Distance(context.Position, randomPoint);
                if (distance < minWanderDistance)
                {
                    continue;
                }

                // Try to find valid NavMesh position
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                {
                    // Ensure it's still within wander radius from origin
                    float distanceFromOrigin = Vector3.Distance(wanderOrigin, hit.position);
                    if (distanceFromOrigin <= wanderRadius)
                    {
                        // Verify the destination is reachable via NavMesh
                        NavMeshPath testPath = new NavMeshPath();
                        if (NavMesh.CalculatePath(context.Position, hit.position, NavMesh.AllAreas, testPath))
                        {
                            // Check if path is complete (not partial)
                            if (testPath.status == NavMeshPathStatus.PathComplete)
                            {
                                randomPoint = hit.position;
                                foundValidPoint = true;
                            }
                        }
                    }
                }
            }

            if (foundValidPoint)
            {
                context.CurrentDestination = randomPoint;
                context.IsMoving = true;
            }
            else
            {
                // Couldn't find valid point, just pause
                StartPause(context);
            }
        }

        private void UpdateMovement(BehaviourContext context)
        {
            Vector3 destination = context.CurrentDestination;
            
            // Calculate path
            NavMesh.CalculatePath(context.Position, destination, NavMesh.AllAreas, context.CurrentPath);

            if (context.CurrentPath.corners.Length < 2)
            {
                // Already at destination or no path
                ReachedDestination(context);
                return;
            }

            Vector3 nextWaypoint = context.CurrentPath.corners[1];
            float distanceToDestination = Vector3.Distance(context.Position, destination);

            // Check if reached destination
            if (distanceToDestination < 1f)
            {
                ReachedDestination(context);
                return;
            }

            // Move towards next waypoint with reduced speed
            Vector3 direction = nextWaypoint - context.Position;
            direction.y = 0f;
            direction.Normalize();

            context.Unit.horizontalInput = direction.x * wanderSpeedMultiplier;
            context.Unit.verticalInput = direction.z * wanderSpeedMultiplier;

            // Set facing direction
            float angle = -Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg - 90f + 180f;
            context.Unit.angle = angle;
        }

        private void ReachedDestination(BehaviourContext context)
        {
            // Stop movement
            context.Unit.horizontalInput = 0f;
            context.Unit.verticalInput = 0f;
            context.IsMoving = false;

            // Decide whether to pause or immediately move to next destination
            if (Random.value < pauseChance)
            {
                StartPause(context);
            }
            else
            {
                PickNewDestination(context);
            }
        }

        private void StartPause(BehaviourContext context)
        {
            float pauseTime = Random.Range(minPauseTime, maxPauseTime);
            context.SetStateData("isPaused", true);
            context.SetStateData("pauseTimeRemaining", pauseTime);
            context.SetStateData("pauseStartTime", Time.time);
            context.IsMoving = false;

            // Stop movement
            context.Unit.horizontalInput = 0f;
            context.Unit.verticalInput = 0f;
        }

        private void UpdatePause(BehaviourContext context, float deltaTime)
        {
            float pauseTime = context.GetStateData("pauseTimeRemaining", 0f);
            pauseTime -= deltaTime;
            context.SetStateData("pauseTimeRemaining", pauseTime);

            // Optional: Look around while paused
            if (lookAroundWhilePaused)
            {
                float pauseStartTime = context.GetStateData("pauseStartTime", Time.time);
                float lookAroundOffset = context.GetStateData("lookAroundOffset", 0f);
                float randomRotation = Mathf.Sin((Time.time - pauseStartTime) * lookAroundSpeed * Mathf.Deg2Rad) * 45f;
                context.Unit.angle = randomRotation + lookAroundOffset;
            }

            // Check if pause is over
            if (pauseTime <= 0f)
            {
                context.SetStateData("isPaused", false);
                PickNewDestination(context);
            }
        }
    }
}
