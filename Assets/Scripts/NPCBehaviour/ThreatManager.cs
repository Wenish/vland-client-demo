using Mirror;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Manages threat (aggro) for an NPC unit.
    /// Automatically tracks damage dealt to this NPC and generates threat.
    /// Can be configured to respond to healing, taunts, and other threat modifiers.
    /// </summary>
    [RequireComponent(typeof(UnitController))]
    public class ThreatManager : NetworkBehaviour
    {
        [Header("Threat Configuration")]
        [Tooltip("Enable threat system for this NPC")]
        [SerializeField]
        private bool enableThreat = true;

        [Tooltip("Threat decay per second (0 = no decay)")]
        [SerializeField]
        private float threatDecayRate = 1f;

        [Tooltip("Maximum threat value per target")]
        [SerializeField]
        private float maxThreat = 1000f;

        [Tooltip("Maximum range to maintain threat (targets beyond this are removed)")]
        [SerializeField]
        private float threatRange = 50f;

        [Header("Threat Generation")]
        [Tooltip("Threat multiplier for damage taken")]
        [SerializeField]
        private float damageThreatMultiplier = 1f;

        [Tooltip("Should healing generate threat?")]
        [SerializeField]
        private bool healingGeneratesThreat = false;

        [Tooltip("Threat multiplier for healing (if enabled)")]
        [SerializeField]
        private float healingThreatMultiplier = 0.5f;

        [Header("Debug")]
        [Tooltip("Show threat values in console")]
        [SerializeField]
        private bool debugMode = false;

        // Runtime
        private ThreatTable _threatTable;
        private UnitController _unit;
        private float _lastHealthValue;
        private bool _isInitialized;

        // Public properties
        public ThreatTable ThreatTable => _threatTable;
        public bool IsEnabled => enableThreat && _isInitialized;
        public int ThreatTargetCount => _threatTable?.GetThreatCount() ?? 0;

        #region Unity Lifecycle

        public override void OnStartServer()
        {
            base.OnStartServer();
            Initialize();
        }

        private void Update()
        {
            if (!isServer || !_isInitialized || !enableThreat) return;

            // Update threat table (decay, cleanup)
            _threatTable.Update(Time.deltaTime, transform.position);

            // Track damage/healing
            TrackHealthChanges();
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                _threatTable?.ClearAll();
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (_isInitialized) return;

            _unit = GetComponent<UnitController>();
            if (_unit == null)
            {
                Debug.LogError($"[ThreatManager] No UnitController found on {gameObject.name}!");
                return;
            }

            _threatTable = new ThreatTable(threatDecayRate, maxThreat, threatRange);
            _lastHealthValue = _unit.health;
            _isInitialized = true;

            if (debugMode)
                Debug.Log($"[ThreatManager] Initialized on {gameObject.name}");
        }

        #endregion

        #region Threat Management

        /// <summary>
        /// Add threat to a specific unit.
        /// </summary>
        [Server]
        public void AddThreat(UnitController target, float amount)
        {
            if (!enableThreat || !_isInitialized || target == null) return;
            if (target.team == _unit.team) return; // No threat for allies

            _threatTable.AddThreat(target, amount);

            if (debugMode)
                Debug.Log($"[ThreatManager] {target.name} gained {amount} threat (Total: {_threatTable.GetThreat(target)})");
        }

        /// <summary>
        /// Remove threat from a unit.
        /// </summary>
        [Server]
        public void RemoveThreat(UnitController target, float amount)
        {
            if (!_isInitialized || target == null) return;

            _threatTable.RemoveThreat(target, amount);

            if (debugMode)
                Debug.Log($"[ThreatManager] {target.name} lost {amount} threat (Total: {_threatTable.GetThreat(target)})");
        }

        /// <summary>
        /// Clear all threat from a specific target.
        /// </summary>
        [Server]
        public void ClearThreat(UnitController target)
        {
            if (!_isInitialized || target == null) return;

            _threatTable.ClearThreat(target);

            if (debugMode)
                Debug.Log($"[ThreatManager] Cleared all threat from {target.name}");
        }

        /// <summary>
        /// Clear entire threat table.
        /// </summary>
        [Server]
        public void ClearAllThreat()
        {
            if (!_isInitialized) return;

            _threatTable.ClearAll();

            if (debugMode)
                Debug.Log($"[ThreatManager] Cleared all threat");
        }

        /// <summary>
        /// Get current threat value for a target.
        /// </summary>
        public float GetThreat(UnitController target)
        {
            if (!_isInitialized || target == null) return 0f;
            return _threatTable.GetThreat(target);
        }

        /// <summary>
        /// Get the target with highest threat.
        /// </summary>
        public UnitController GetHighestThreatTarget()
        {
            if (!_isInitialized || !enableThreat) return null;
            return _threatTable.GetHighestThreatTarget();
        }

        /// <summary>
        /// Taunt: Force this NPC to focus on a specific target by maximizing their threat.
        /// </summary>
        [Server]
        public void Taunt(UnitController taunter, float duration = 0f)
        {
            if (!_isInitialized || taunter == null) return;

            // Set threat to max for the taunter
            _threatTable.SetThreat(taunter, maxThreat);

            // Optionally: Lock threat for duration (would need additional state tracking)

            if (debugMode)
                Debug.Log($"[ThreatManager] {taunter.name} taunted {gameObject.name}!");
        }

        /// <summary>
        /// Transfer threat from one unit to another (useful for tank swaps).
        /// </summary>
        [Server]
        public void TransferThreat(UnitController from, UnitController to, float percentage = 1f)
        {
            if (!_isInitialized) return;

            _threatTable.TransferThreat(from, to, percentage);

            if (debugMode)
                Debug.Log($"[ThreatManager] Transferred {percentage * 100}% threat from {from.name} to {to.name}");
        }

        /// <summary>
        /// Scale all threat by a multiplier (for AoE threat reduction).
        /// </summary>
        [Server]
        public void ScaleAllThreat(float multiplier)
        {
            if (!_isInitialized) return;

            _threatTable.ScaleAllThreat(multiplier);

            if (debugMode)
                Debug.Log($"[ThreatManager] Scaled all threat by {multiplier}");
        }

        #endregion

        #region Automatic Threat Generation

        private void TrackHealthChanges()
        {
            float currentHealth = _unit.health;
            float healthDelta = currentHealth - _lastHealthValue;

            if (Mathf.Abs(healthDelta) > 0.1f)
            {
                if (healthDelta < 0f) // Damage taken
                {
                    OnDamageTaken(Mathf.Abs(healthDelta));
                }
                else if (healthDelta > 0f && healingGeneratesThreat) // Healing received
                {
                    OnHealingReceived(healthDelta);
                }
            }

            _lastHealthValue = currentHealth;
        }

        private void OnDamageTaken(float damageAmount)
        {
            // Try to find who damaged us
            // Note: For a more robust system, you'd want to track the damage source
            // This is a simplified version that adds threat to the nearest enemy
            var attacker = FindNearestEnemy();
            if (attacker != null)
            {
                float threat = damageAmount * damageThreatMultiplier;
                AddThreat(attacker, threat);
            }
        }

        private void OnHealingReceived(float healAmount)
        {
            // Add threat to the healer (if we could track them)
            // For now, this is a placeholder
            var nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                float threat = healAmount * healingThreatMultiplier;
                AddThreat(nearestEnemy, threat);
            }
        }

        private UnitController FindNearestEnemy()
        {
            var allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            UnitController nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var unit in allUnits)
            {
                if (unit == null || unit == _unit || unit.IsDead) continue;
                if (unit.team == _unit.team) continue;

                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance < nearestDistance && distance <= threatRange)
                {
                    nearest = unit;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        #endregion

        #region Public API for Skills/Abilities

        /// <summary>
        /// Call this when a skill/ability deals damage to generate appropriate threat.
        /// </summary>
        [Server]
        public void OnDamageDealt(UnitController target, float damage, float threatMultiplier = 1f)
        {
            if (!_isInitialized || target == null) return;

            float threat = damage * damageThreatMultiplier * threatMultiplier;
            
            // The target's threat manager should receive this
            var targetThreatManager = target.GetComponent<ThreatManager>();
            if (targetThreatManager != null && targetThreatManager.IsEnabled)
            {
                targetThreatManager.AddThreat(_unit, threat);
            }
        }

        /// <summary>
        /// Call this when healing an ally to generate threat on nearby enemies.
        /// </summary>
        [Server]
        public void OnHealingDealt(UnitController healTarget, float healAmount)
        {
            if (!_isInitialized || !healingGeneratesThreat) return;

            // Find nearby enemies and add threat
            var enemies = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy == _unit || enemy.IsDead) continue;
                if (enemy.team == _unit.team) continue;

                var enemyThreatManager = enemy.GetComponent<ThreatManager>();
                if (enemyThreatManager != null && enemyThreatManager.IsEnabled)
                {
                    float threat = healAmount * healingThreatMultiplier;
                    enemyThreatManager.AddThreat(_unit, threat);
                }
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Enable or disable the threat system at runtime.
        /// </summary>
        public void SetThreatEnabled(bool enabled)
        {
            enableThreat = enabled;
            if (!enabled && _isInitialized)
            {
                _threatTable.ClearAll();
            }
        }

        /// <summary>
        /// Update threat configuration at runtime.
        /// </summary>
        public void ConfigureThreat(float decayRate, float maxThreat, float range)
        {
            if (!_isInitialized) return;

            _threatTable.DecayRate = decayRate;
            _threatTable.MaxThreat = maxThreat;
            _threatTable.ThreatRange = range;
        }

        #endregion
    }
}
