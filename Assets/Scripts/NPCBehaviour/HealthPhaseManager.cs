using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Manages health-based phase transitions for boss fights.
    /// Automatically switches behaviour profiles when health thresholds are crossed.
    /// Works in conjunction with BehaviourExecutor.
    /// </summary>
    [RequireComponent(typeof(BehaviourExecutor))]
    [RequireComponent(typeof(UnitController))]
    public class HealthPhaseManager : NetworkBehaviour
    {
        [Header("Phase Configuration")]
        [Tooltip("Health phase profile defining boss phases")]
        [SerializeField]
        private HealthPhaseProfile phaseProfile;

        [Header("Settings")]
        [Tooltip("Enable debug logging")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Spawn phase transition effects on clients")]
        [SerializeField]
        private bool spawnTransitionEffects = true;

        // Components
        private BehaviourExecutor _behaviourExecutor;
        private UnitController _unit;

        // Runtime state
        private HashSet<int> _completedPhases = new HashSet<int>();
        private int _currentPhaseIndex = -1;
        private float _lastHealthCheck = 1f;
        private bool _isInitialized;

        #region Unity Lifecycle

        public override void OnStartServer()
        {
            base.OnStartServer();
            Initialize();
        }

        private void Update()
        {
            if (!isServer || !_isInitialized) return;
            if (_unit == null || _unit.IsDead) return;

            CheckPhaseTransition();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            _behaviourExecutor = GetComponent<BehaviourExecutor>();
            _unit = GetComponent<UnitController>();

            if (_behaviourExecutor == null)
            {
                Debug.LogError($"[HealthPhaseManager] No BehaviourExecutor found on {gameObject.name}!");
                enabled = false;
                return;
            }

            if (_unit == null)
            {
                Debug.LogError($"[HealthPhaseManager] No UnitController found on {gameObject.name}!");
                enabled = false;
                return;
            }

            if (phaseProfile == null)
            {
                Debug.LogWarning($"[HealthPhaseManager] No phase profile assigned to {gameObject.name}. Component disabled.");
                enabled = false;
                return;
            }

            if (!phaseProfile.Validate())
            {
                Debug.LogError($"[HealthPhaseManager] Phase profile '{phaseProfile.name}' failed validation!");
                enabled = false;
                return;
            }

            _isInitialized = true;

            if (debugMode)
                Debug.Log($"[HealthPhaseManager] Initialized on {gameObject.name} with profile '{phaseProfile.name}'");
        }

        #endregion

        #region Phase Management

        /// <summary>
        /// Check if a phase transition should occur based on current health.
        /// </summary>
        private void CheckPhaseTransition()
        {
            float currentHealthPercent = _unit.maxHealth > 0 ? ((float)_unit.health / _unit.maxHealth) : 0f;

            // Only check when health has decreased
            if (currentHealthPercent >= _lastHealthCheck)
            {
                _lastHealthCheck = currentHealthPercent;
                return;
            }

            _lastHealthCheck = currentHealthPercent;

            // Find appropriate phase for current health
            var targetPhase = phaseProfile.GetPhaseForHealth(currentHealthPercent, _completedPhases);

            if (targetPhase == null) return;

            int phaseIndex = phaseProfile.GetPhaseIndex(targetPhase);

            // Don't re-enter the same phase
            if (phaseIndex == _currentPhaseIndex) return;

            // Trigger phase transition
            EnterPhase(targetPhase, phaseIndex);
        }

        /// <summary>
        /// Enter a new phase.
        /// </summary>
        [Server]
        private void EnterPhase(HealthPhaseProfile.HealthPhase phase, int phaseIndex)
        {
            if (phase == null) return;

            if (debugMode)
                Debug.Log($"[HealthPhaseManager] Entering phase: {phase.phaseName} at {phase.healthThreshold * 100}% health");

            // Mark phase as completed
            if (!phaseProfile.allowPhaseRepeat)
            {
                _completedPhases.Add(phaseIndex);
            }

            _currentPhaseIndex = phaseIndex;

            // Switch behaviour profile
            if (phase.behaviourProfile != null)
            {
                _behaviourExecutor.SetBehaviourProfile(phase.behaviourProfile);
            }

            // Modify skills
            var mediator = _unit.GetComponent<UnitMediator>();
            if (mediator != null && mediator.Skills != null)
            {
                // Remove skills
                foreach (var skillName in phase.skillsToRemove)
                {
                    RemoveSkillByName(mediator.Skills, skillName);
                }

                // Add skills
                foreach (var skillName in phase.skillsToAdd)
                {
                    // Determine slot type based on skill data
                    mediator.Skills.AddSkill(SkillSlotType.Normal, skillName);
                }
            }

            // Spawn transition effect
            if (spawnTransitionEffects && phase.phaseTransitionEffect != null)
            {
                RpcSpawnPhaseEffect(phase.phaseTransitionEffect, transform.position);
            }
        }

        /// <summary>
        /// Remove a skill by name from the skill system.
        /// </summary>
        private void RemoveSkillByName(SkillSystem skillSystem, string skillName)
        {
            // Check normal skills
            for (int i = skillSystem.normalSkills.Count - 1; i >= 0; i--)
            {
                if (skillSystem.normalSkills[i] != null && skillSystem.normalSkills[i].skillName == skillName)
                {
                    skillSystem.RemoveSkill(SkillSlotType.Normal, i);
                    return;
                }
            }

            // Check ultimate skills
            for (int i = skillSystem.ultimateSkills.Count - 1; i >= 0; i--)
            {
                if (skillSystem.ultimateSkills[i] != null && skillSystem.ultimateSkills[i].skillName == skillName)
                {
                    skillSystem.RemoveSkill(SkillSlotType.Ultimate, i);
                    return;
                }
            }
        }

        /// <summary>
        /// Manually force a phase transition (useful for scripted events).
        /// </summary>
        [Server]
        public void ForcePhase(int phaseIndex)
        {
            if (phaseIndex < 0 || phaseIndex >= phaseProfile.phases.Count)
            {
                Debug.LogError($"[HealthPhaseManager] Invalid phase index: {phaseIndex}");
                return;
            }

            var phase = phaseProfile.phases[phaseIndex];
            EnterPhase(phase, phaseIndex);
        }

        #endregion

        #region Network Methods

        [ClientRpc]
        private void RpcSpawnPhaseEffect(GameObject effectPrefab, Vector3 position)
        {
            if (effectPrefab == null) return;

            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            
            // Auto-destroy after a delay if it has a ParticleSystem
            var ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // Default cleanup after 5 seconds
                Destroy(effect, 5f);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the current phase index.
        /// </summary>
        public int GetCurrentPhaseIndex()
        {
            return _currentPhaseIndex;
        }

        /// <summary>
        /// Get the current phase.
        /// </summary>
        public HealthPhaseProfile.HealthPhase GetCurrentPhase()
        {
            if (_currentPhaseIndex < 0 || _currentPhaseIndex >= phaseProfile.phases.Count)
                return null;

            return phaseProfile.phases[_currentPhaseIndex];
        }

        /// <summary>
        /// Check if a specific phase has been completed.
        /// </summary>
        public bool IsPhaseCompleted(int phaseIndex)
        {
            return _completedPhases.Contains(phaseIndex);
        }

        #endregion
    }
}
