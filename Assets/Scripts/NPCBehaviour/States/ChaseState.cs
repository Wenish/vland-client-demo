using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace NPCBehaviour
{
    /// <summary>
    /// Chase state where NPC pursues a target.
    /// Automatically finds and follows enemies.
    /// </summary>
    [CreateAssetMenu(fileName = "ChaseState", menuName = "Game/NPC Behaviour/States/Chase")]
    public class ChaseState : BehaviourState
    {
        [Header("Chase Behaviour")]
        [Tooltip("Transitions from this state")]
        public List<BehaviourTransition> transitions = new();

        [Header("Target Selection")]
        [Tooltip("How often to update target (in seconds)")]
        public float targetUpdateInterval = 0.5f;

        [Tooltip("Maximum distance to detect enemies")]
        public float detectionRange = 30f;

        [Tooltip("Prioritize closest target")]
        public bool prioritizeClosest = true;

        [Tooltip("Use threat-based targeting if ThreatManager is available")]
        public bool useThreatTargeting = true;

        [Header("Movement")]
        [Tooltip("How close to get to target before stopping")]
        public float stoppingDistance = 2f;

        public override void OnEnter(BehaviourContext context)
        {
            context.IsMoving = true;
            context.LastTargetUpdateTime = 0f;
            FindTarget(context);
        }

        public override bool OnUpdate(BehaviourContext context, float deltaTime)
        {
            context.TimeInState += deltaTime;

            // Update target periodically
            if (Time.time - context.LastTargetUpdateTime > targetUpdateInterval)
            {
                FindTarget(context);
                context.LastTargetUpdateTime = Time.time;
            }

            // If we have no target, exit state
            if (context.CurrentTarget == null || context.CurrentTarget.IsDead)
            {
                return false;
            }

            // Move towards target
            MoveTowardsTarget(context);

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

        private void FindTarget(BehaviourContext context)
        {
            // Try threat-based targeting first if enabled and available
            if (useThreatTargeting && context.HasThreatSystem)
            {
                var threatTarget = context.GetHighestThreatTarget();
                if (threatTarget != null && !threatTarget.IsDead)
                {
                    float distance = Vector3.Distance(context.Position, threatTarget.transform.position);
                    if (distance <= detectionRange)
                    {
                        context.CurrentTarget = threatTarget;
                        return;
                    }
                }
            }

            // Fallback to standard targeting
            var allUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            var enemies = allUnits
                .Where(u => u != null && u != context.Unit && u.team != context.Team && !u.IsDead)
                .Where(u => Vector3.Distance(context.Position, u.transform.position) <= detectionRange)
                .ToList();

            if (enemies.Count == 0)
            {
                context.CurrentTarget = null;
                return;
            }

            if (prioritizeClosest)
            {
                context.CurrentTarget = enemies
                    .OrderBy(e => Vector3.Distance(context.Position, e.transform.position))
                    .First();
            }
            else
            {
                context.CurrentTarget = enemies[Random.Range(0, enemies.Count)];
            }
        }

        private void MoveTowardsTarget(BehaviourContext context)
        {
            if (context.CurrentTarget == null) return;

            Vector3 targetPos = context.CurrentTarget.transform.position;
            context.CurrentDestination = targetPos;

            // Calculate path
            NavMesh.CalculatePath(context.Position, targetPos, NavMesh.AllAreas, context.CurrentPath);

            if (context.CurrentPath.corners.Length < 2)
            {
                context.Unit.horizontalInput = 0f;
                context.Unit.verticalInput = 0f;
                return;
            }

            // Get next waypoint
            Vector3 nextWaypoint = context.CurrentPath.corners[1];
            float distance = Vector3.Distance(context.Position, targetPos);

            // Stop if close enough
            if (distance < stoppingDistance)
            {
                context.Unit.horizontalInput = 0f;
                context.Unit.verticalInput = 0f;
                
                // Face target
                Vector3 directionToTarget = targetPos - context.Position;
                directionToTarget.y = 0f;
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    float angle = -Mathf.Atan2(directionToTarget.z, directionToTarget.x) * Mathf.Rad2Deg - 90f + 180f;
                    context.Unit.angle = angle;
                }
                return;
            }

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
