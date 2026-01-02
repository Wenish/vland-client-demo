using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace NPCBehaviour
{
    /// <summary>
    /// Runtime context for NPC behaviour execution.
    /// Contains all mutable state needed during behaviour execution.
    /// This separates runtime data from the data-driven ScriptableObject definitions.
    /// </summary>
    public class BehaviourContext
    {
        // Core references
        public UnitController Unit { get; private set; }
        public UnitMediator Mediator { get; private set; }
        public Transform Transform { get; private set; }
        public ThreatManager ThreatManager { get; private set; }
        
        // Current behaviour state
        public BehaviourState CurrentState { get; set; }
        public BehaviourProfile CurrentProfile { get; set; }
        public float TimeInState { get; set; }

        // Target tracking
        public UnitController CurrentTarget { get; set; }
        public Vector3 TargetPosition { get; set; }
        public float LastTargetUpdateTime { get; set; }

        // Movement data
        public NavMeshPath CurrentPath { get; set; }
        public Vector3 CurrentDestination { get; set; }
        public bool IsMoving { get; set; }

        // Combat data
        public float LastAttackTime { get; set; }
        public float LastSkillUseTime { get; set; }
        public List<NetworkedSkillInstance> AvailableSkills { get; set; }

        // State-specific data storage
        private Dictionary<string, object> _stateData = new Dictionary<string, object>();

        public BehaviourContext(UnitController unit)
        {
            Unit = unit;
            Mediator = unit.GetComponent<UnitMediator>();
            Transform = unit.transform;
            ThreatManager = unit.GetComponent<ThreatManager>();
            CurrentPath = new NavMeshPath();
            AvailableSkills = new List<NetworkedSkillInstance>();
        }

        // Generic state data storage methods
        public void SetStateData<T>(string key, T value)
        {
            _stateData[key] = value;
        }

        public T GetStateData<T>(string key, T defaultValue = default)
        {
            if (_stateData.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public bool HasStateData(string key)
        {
            return _stateData.ContainsKey(key);
        }

        public void ClearStateData()
        {
            _stateData.Clear();
        }

        // Convenience properties
        public Vector3 Position => Transform.position;
        public float Health => Unit.health;
        public float MaxHealth => Unit.maxHealth;
        public float HealthPercent => MaxHealth > 0 ? (Health / MaxHealth) : 0f;
        public bool IsDead => Unit.IsDead;
        public int Team => Unit.team;

        // Distance helpers
        public float DistanceToTarget()
        {
            if (CurrentTarget == null) return float.MaxValue;
            return Vector3.Distance(Position, CurrentTarget.transform.position);
        }

        public float DistanceToPosition(Vector3 position)
        {
            return Vector3.Distance(Position, position);
        }

        // Skill helpers
        public void RefreshAvailableSkills()
        {
            AvailableSkills.Clear();
            if (Mediator?.Skills != null)
            {
                AvailableSkills.AddRange(Mediator.Skills.normalSkills);
                AvailableSkills.AddRange(Mediator.Skills.ultimateSkills);
            }
        }

        public List<NetworkedSkillInstance> GetOffCooldownSkills()
        {
            var result = new List<NetworkedSkillInstance>();
            foreach (var skill in AvailableSkills)
            {
                if (skill != null && !skill.IsOnCooldown)
                {
                    result.Add(skill);
                }
            }
            return result;
        }

        // Threat helpers
        public bool HasThreatSystem => ThreatManager != null && ThreatManager.IsEnabled;

        public UnitController GetHighestThreatTarget()
        {
            if (!HasThreatSystem) return null;
            return ThreatManager.GetHighestThreatTarget();
        }

        public float GetThreat(UnitController target)
        {
            if (!HasThreatSystem || target == null) return 0f;
            return ThreatManager.GetThreat(target);
        }

        public int GetThreatTargetCount()
        {
            if (!HasThreatSystem) return 0;
            return ThreatManager.ThreatTargetCount;
        }
    }
}
