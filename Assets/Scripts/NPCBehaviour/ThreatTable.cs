using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Manages threat values for an NPC. Tracks how much "aggro" each enemy has.
    /// Higher threat = more likely to be targeted.
    /// </summary>
    public class ThreatTable
    {
        // Threat values per unit
        private Dictionary<UnitController, float> _threatValues = new Dictionary<UnitController, float>();
        
        // Configuration
        private float _decayRate;
        private float _maxThreat;
        private float _threatRange;
        
        /// <summary>
        /// Creates a new threat table.
        /// </summary>
        /// <param name="decayRate">How much threat decays per second (0 = no decay)</param>
        /// <param name="maxThreat">Maximum threat value allowed</param>
        /// <param name="threatRange">Maximum distance to maintain threat (units beyond this are removed)</param>
        public ThreatTable(float decayRate = 1f, float maxThreat = 1000f, float threatRange = 50f)
        {
            _decayRate = decayRate;
            _maxThreat = maxThreat;
            _threatRange = threatRange;
        }
        
        /// <summary>
        /// Add threat to a specific unit.
        /// </summary>
        public void AddThreat(UnitController unit, float amount)
        {
            if (unit == null || unit.IsDead) return;
            
            if (!_threatValues.ContainsKey(unit))
            {
                _threatValues[unit] = 0f;
            }
            
            _threatValues[unit] = Mathf.Min(_threatValues[unit] + amount, _maxThreat);
        }
        
        /// <summary>
        /// Set threat value for a unit directly.
        /// </summary>
        public void SetThreat(UnitController unit, float amount)
        {
            if (unit == null || unit.IsDead) return;
            
            _threatValues[unit] = Mathf.Clamp(amount, 0f, _maxThreat);
        }
        
        /// <summary>
        /// Remove threat from a unit.
        /// </summary>
        public void RemoveThreat(UnitController unit, float amount)
        {
            if (unit == null || !_threatValues.ContainsKey(unit)) return;
            
            _threatValues[unit] = Mathf.Max(0f, _threatValues[unit] - amount);
            
            if (_threatValues[unit] <= 0f)
            {
                _threatValues.Remove(unit);
            }
        }
        
        /// <summary>
        /// Clear all threat from a specific unit.
        /// </summary>
        public void ClearThreat(UnitController unit)
        {
            if (unit != null)
            {
                _threatValues.Remove(unit);
            }
        }
        
        /// <summary>
        /// Clear all threat values.
        /// </summary>
        public void ClearAll()
        {
            _threatValues.Clear();
        }
        
        /// <summary>
        /// Get threat value for a specific unit.
        /// </summary>
        public float GetThreat(UnitController unit)
        {
            if (unit == null || !_threatValues.ContainsKey(unit))
                return 0f;
            
            return _threatValues[unit];
        }
        
        /// <summary>
        /// Get the unit with the highest threat.
        /// </summary>
        /// <param name="requiresLOS">If true, only considers units with line of sight</param>
        /// <param name="origin">Origin point for LOS checks (typically NPC position)</param>
        /// <param name="losLayerMask">Layer mask for LOS raycast</param>
        public UnitController GetHighestThreatTarget(bool requiresLOS = false, Vector3 origin = default, LayerMask losLayerMask = default)
        {
            CleanupDeadUnits();
            
            if (_threatValues.Count == 0) return null;
            
            var validTargets = _threatValues;
            
            // Filter by LOS if required
            if (requiresLOS && origin != default)
            {
                validTargets = _threatValues
                    .Where(kvp => HasLineOfSight(origin, kvp.Key.transform.position, losLayerMask))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            
            if (validTargets.Count == 0) return null;
            
            return validTargets.OrderByDescending(kvp => kvp.Value).First().Key;
        }
        
        /// <summary>
        /// Get all units with threat above a threshold.
        /// </summary>
        public List<UnitController> GetUnitsAboveThreat(float threshold)
        {
            CleanupDeadUnits();
            
            return _threatValues
                .Where(kvp => kvp.Value >= threshold)
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();
        }
        
        /// <summary>
        /// Get all units in the threat table sorted by threat (highest first).
        /// </summary>
        public List<UnitController> GetAllTargetsByThreat()
        {
            CleanupDeadUnits();
            
            return _threatValues
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();
        }
        
        /// <summary>
        /// Get threat table as a list of (unit, threat) pairs.
        /// </summary>
        public List<(UnitController unit, float threat)> GetThreatList()
        {
            CleanupDeadUnits();
            
            return _threatValues
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }
        
        /// <summary>
        /// Check if a unit is in the threat table.
        /// </summary>
        public bool HasThreat(UnitController unit)
        {
            return unit != null && _threatValues.ContainsKey(unit) && _threatValues[unit] > 0f;
        }
        
        /// <summary>
        /// Get the total number of units with threat.
        /// </summary>
        public int GetThreatCount()
        {
            CleanupDeadUnits();
            return _threatValues.Count;
        }
        
        /// <summary>
        /// Update threat table (apply decay, cleanup).
        /// Call this regularly, typically once per frame.
        /// </summary>
        public void Update(float deltaTime, Vector3 npcPosition)
        {
            CleanupDeadUnits();
            
            // Apply decay
            if (_decayRate > 0f)
            {
                var units = _threatValues.Keys.ToList();
                foreach (var unit in units)
                {
                    _threatValues[unit] = Mathf.Max(0f, _threatValues[unit] - _decayRate * deltaTime);
                    
                    if (_threatValues[unit] <= 0f)
                    {
                        _threatValues.Remove(unit);
                    }
                }
            }
            
            // Remove units that are too far away
            if (_threatRange > 0f)
            {
                var units = _threatValues.Keys.ToList();
                foreach (var unit in units)
                {
                    if (unit == null) continue;
                    
                    float distance = Vector3.Distance(npcPosition, unit.transform.position);
                    if (distance > _threatRange)
                    {
                        _threatValues.Remove(unit);
                    }
                }
            }
        }
        
        /// <summary>
        /// Multiply all threat values by a factor.
        /// Useful for AoE threat reduction abilities.
        /// </summary>
        public void ScaleAllThreat(float multiplier)
        {
            var units = _threatValues.Keys.ToList();
            foreach (var unit in units)
            {
                _threatValues[unit] *= multiplier;
                if (_threatValues[unit] <= 0f)
                {
                    _threatValues.Remove(unit);
                }
            }
        }
        
        /// <summary>
        /// Transfer threat from one unit to another (for taunt mechanics).
        /// </summary>
        public void TransferThreat(UnitController from, UnitController to, float percentage = 1f)
        {
            if (from == null || to == null || !_threatValues.ContainsKey(from)) return;
            
            float threatAmount = _threatValues[from] * Mathf.Clamp01(percentage);
            RemoveThreat(from, threatAmount);
            AddThreat(to, threatAmount);
        }
        
        // Helper methods
        
        private void CleanupDeadUnits()
        {
            var deadUnits = _threatValues.Keys.Where(u => u == null || u.IsDead).ToList();
            foreach (var unit in deadUnits)
            {
                _threatValues.Remove(unit);
            }
        }
        
        private bool HasLineOfSight(Vector3 from, Vector3 to, LayerMask layerMask)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            
            if (Physics.Raycast(from, direction.normalized, out RaycastHit hit, distance, layerMask))
            {
                // Hit something - check if it's the target
                return hit.collider.GetComponent<UnitController>() != null;
            }
            
            return true; // No obstruction
        }
        
        /// <summary>
        /// Configuration properties
        /// </summary>
        public float DecayRate
        {
            get => _decayRate;
            set => _decayRate = Mathf.Max(0f, value);
        }
        
        public float MaxThreat
        {
            get => _maxThreat;
            set => _maxThreat = Mathf.Max(1f, value);
        }
        
        public float ThreatRange
        {
            get => _threatRange;
            set => _threatRange = Mathf.Max(0f, value);
        }
    }
}
