using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Editor/Debug helper that displays current behaviour state information.
    /// Attach this to NPCs during development to visualize behaviour in the Inspector.
    /// </summary>
    [RequireComponent(typeof(BehaviourExecutor))]
    public class BehaviourDebugDisplay : MonoBehaviour
    {
        private BehaviourExecutor _executor;

        [Header("Current State (Runtime)")]
        [SerializeField, Tooltip("Current active state")]
        private string currentState = "None";

        [SerializeField, Tooltip("Time in current state")]
        private float timeInState = 0f;

        [SerializeField, Tooltip("Current target")]
        private string currentTarget = "None";

        [SerializeField, Tooltip("Distance to target")]
        private float distanceToTarget = 0f;

        [SerializeField, Tooltip("Current health percentage")]
        private float healthPercent = 1f;

        [SerializeField, Tooltip("Is moving")]
        private bool isMoving = false;

        [SerializeField, Tooltip("Available skills count")]
        private int availableSkillCount = 0;

        [Header("Threat Info (if ThreatManager exists)")]
        [SerializeField, Tooltip("Threat system enabled")]
        private bool hasThreatSystem = false;

        [SerializeField, Tooltip("Number of threat targets")]
        private int threatTargetCount = 0;

        [SerializeField, Tooltip("Highest threat target")]
        private string highestThreatTarget = "None";

        [SerializeField, Tooltip("Highest threat value")]
        private float highestThreatValue = 0f;

        [SerializeField, Tooltip("Current target threat value")]
        private float currentTargetThreat = 0f;

        [Header("Settings")]
        [SerializeField]
        private bool showGizmos = true;

        [SerializeField]
        private bool showLabels = true;

        private void Start()
        {
            _executor = GetComponent<BehaviourExecutor>();
        }

        private void Update()
        {
            if (_executor == null || _executor.Context == null)
            {
                currentState = "Not Initialized";
                return;
            }

            var context = _executor.Context;

            // Update display fields
            currentState = context.CurrentState != null ? context.CurrentState.name : "None";
            timeInState = context.TimeInState;
            currentTarget = context.CurrentTarget != null ? context.CurrentTarget.name : "None";
            distanceToTarget = context.CurrentTarget != null ? context.DistanceToTarget() : 0f;
            healthPercent = context.HealthPercent;
            isMoving = context.IsMoving;
            availableSkillCount = context.AvailableSkills != null ? context.AvailableSkills.Count : 0;

            // Update threat info
            hasThreatSystem = context.HasThreatSystem;
            if (hasThreatSystem)
            {
                threatTargetCount = context.GetThreatTargetCount();
                var topTarget = context.GetHighestThreatTarget();
                if (topTarget != null)
                {
                    highestThreatTarget = topTarget.name;
                    highestThreatValue = context.GetThreat(topTarget);
                }
                else
                {
                    highestThreatTarget = "None";
                    highestThreatValue = 0f;
                }

                if (context.CurrentTarget != null)
                {
                    currentTargetThreat = context.GetThreat(context.CurrentTarget);
                }
                else
                {
                    currentTargetThreat = 0f;
                }
            }
            else
            {
                threatTargetCount = 0;
                highestThreatTarget = "None";
                highestThreatValue = 0f;
                currentTargetThreat = 0f;
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || _executor == null || _executor.Context == null) return;

            var context = _executor.Context;

            // Draw target line
            if (context.CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, context.CurrentTarget.transform.position);
            }

            // Draw destination
            if (context.CurrentDestination != Vector3.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(context.CurrentDestination, 1f);
                Gizmos.DrawLine(transform.position, context.CurrentDestination);
            }

            // Draw path
            if (context.CurrentPath != null && context.CurrentPath.corners.Length > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 1; i < context.CurrentPath.corners.Length; i++)
                {
                    Gizmos.DrawLine(context.CurrentPath.corners[i - 1], context.CurrentPath.corners[i]);
                    Gizmos.DrawWireSphere(context.CurrentPath.corners[i], 0.3f);
                }
            }

#if UNITY_EDITOR
            // Draw labels
            if (showLabels)
            {
                Vector3 labelPos = transform.position + Vector3.up * 3f;
                string label = $"State: {currentState}\nTime: {timeInState:F1}s\nHealth: {healthPercent * 100:F0}%";
                
                if (context.CurrentTarget != null)
                {
                    label += $"\nTarget: {context.CurrentTarget.name}";
                    label += $"\nDist: {distanceToTarget:F1}";
                }

                // Add threat info if available
                if (hasThreatSystem)
                {
                    label += $"\n--- Threat ---";
                    label += $"\nTargets: {threatTargetCount}";
                    if (context.CurrentTarget != null)
                    {
                        label += $"\nCur Threat: {currentTargetThreat:F0}";
                    }
                    if (highestThreatTarget != "None")
                    {
                        label += $"\nTop: {highestThreatTarget}";
                        label += $"\nTop Threat: {highestThreatValue:F0}";
                    }
                }

                UnityEditor.Handles.Label(labelPos, label);
            }
#endif
        }
    }
}
