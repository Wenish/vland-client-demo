using Mirror;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Server-authoritative NPC behaviour executor.
    /// Manages behaviour states, transitions, and integrates with existing UnitController.
    /// This component should be attached to NPC units and runs only on the server.
    /// </summary>
    [RequireComponent(typeof(UnitController))]
    [RequireComponent(typeof(UnitMediator))]
    public class BehaviourExecutor : NetworkBehaviour
    {
        [Header("Behaviour Configuration")]
        [Tooltip("The behaviour profile to use for this NPC")]
        [SerializeField]
        private BehaviourProfile behaviourProfile;

        [Header("Runtime Settings")]
        [Tooltip("Enable debug logging")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("How often to check for transitions (in seconds). Lower = more responsive but more expensive.")]
        [SerializeField]
        private float transitionCheckInterval = 0.2f;

        // Runtime data
        private BehaviourContext _context;
        private float _lastTransitionCheckTime;
        private bool _isInitialized;

        // Public properties
        public BehaviourProfile CurrentProfile => _context?.CurrentProfile;
        public BehaviourState CurrentState => _context?.CurrentState;
        public BehaviourContext Context => _context;

        #region Unity Lifecycle

        public override void OnStartServer()
        {
            base.OnStartServer();
            Initialize();
        }

        private void Update()
        {
            // Only run on server
            if (!isServer || !_isInitialized) return;

            // Don't run behaviour if unit is dead
            if (_context.IsDead) return;

            // Update current state
            if (_context.CurrentState != null)
            {
                bool continueState = _context.CurrentState.OnUpdate(_context, Time.deltaTime);

                if (!continueState)
                {
                    if (debugMode)
                        Debug.Log($"[BehaviourExecutor] State '{_context.CurrentState.name}' returned false, checking transitions.");
                }
            }

            // Check for transitions periodically
            if (Time.time - _lastTransitionCheckTime >= transitionCheckInterval)
            {
                CheckTransitions();
                _lastTransitionCheckTime = Time.time;
            }
        }

        private void OnDestroy()
        {
            if (isServer && _isInitialized && _context?.CurrentState != null)
            {
                _context.CurrentState.OnExit(_context);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the behaviour executor with the configured profile.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"[BehaviourExecutor] Already initialized on {gameObject.name}");
                return;
            }

            UnitController unit = GetComponent<UnitController>();
            if (unit == null)
            {
                Debug.LogError($"[BehaviourExecutor] No UnitController found on {gameObject.name}!");
                return;
            }

            _context = new BehaviourContext(unit);

            if (behaviourProfile == null)
            {
                Debug.LogWarning($"[BehaviourExecutor] No behaviour profile assigned to {gameObject.name}. Behaviour system disabled.");
                return;
            }

            if (!behaviourProfile.Validate())
            {
                Debug.LogError($"[BehaviourExecutor] Behaviour profile '{behaviourProfile.name}' failed validation!");
                return;
            }

            SetBehaviourProfile(behaviourProfile);
            _isInitialized = true;

            if (debugMode)
                Debug.Log($"[BehaviourExecutor] Initialized on {gameObject.name} with profile '{behaviourProfile.name}'");
        }

        #endregion

        #region Profile Management

        /// <summary>
        /// Switch to a different behaviour profile at runtime.
        /// Useful for boss phase changes or dynamic behaviour modification.
        /// </summary>
        [Server]
        public void SetBehaviourProfile(BehaviourProfile newProfile)
        {
            if (newProfile == null)
            {
                Debug.LogError("[BehaviourExecutor] Attempted to set null behaviour profile!");
                return;
            }

            if (!newProfile.Validate())
            {
                Debug.LogError($"[BehaviourExecutor] Profile '{newProfile.name}' failed validation!");
                return;
            }

            // Exit current state if switching profiles
            if (_context.CurrentState != null && _context.CurrentProfile != newProfile)
            {
                _context.CurrentState.OnExit(_context);
            }

            _context.CurrentProfile = newProfile;
            _context.RefreshAvailableSkills();

            // Enter initial state of new profile
            BehaviourState initialState = newProfile.GetInitialState();
            TransitionToState(initialState);

            if (debugMode)
                Debug.Log($"[BehaviourExecutor] Switched to profile '{newProfile.name}', initial state: '{initialState.name}'");
        }

        /// <summary>
        /// Get the currently active behaviour profile.
        /// </summary>
        public BehaviourProfile GetBehaviourProfile()
        {
            return _context?.CurrentProfile;
        }

        #endregion

        #region State Transitions

        /// <summary>
        /// Check for valid transitions and switch states if needed.
        /// </summary>
        private void CheckTransitions()
        {
            if (_context.CurrentProfile == null || _context.CurrentState == null)
                return;

            BehaviourTransition validTransition = null;

            // First check global transitions
            if (_context.CurrentProfile.globalTransitions != null)
            {
                foreach (var transition in _context.CurrentProfile.globalTransitions)
                {
                    if (transition != null && transition.CanTransition(_context))
                    {
                        if (validTransition == null || transition.priority > validTransition.priority)
                        {
                            validTransition = transition;
                        }
                    }
                }
            }

            // Then check state-specific transitions
            var stateTransition = _context.CurrentState.EvaluateTransitions(_context);
            if (stateTransition != null)
            {
                if (validTransition == null || stateTransition.priority > validTransition.priority)
                {
                    validTransition = stateTransition;
                }
            }

            // Execute transition if found
            if (validTransition != null)
            {
                TransitionToState(validTransition.targetState);
            }
        }

        /// <summary>
        /// Force a transition to a specific state.
        /// </summary>
        [Server]
        public void TransitionToState(BehaviourState newState)
        {
            if (newState == null)
            {
                Debug.LogError("[BehaviourExecutor] Attempted to transition to null state!");
                return;
            }

            // Exit current state
            if (_context.CurrentState != null)
            {
                _context.CurrentState.OnExit(_context);

                if (debugMode)
                    Debug.Log($"[BehaviourExecutor] Exiting state: {_context.CurrentState.name}");
            }

            // Clear state-specific data
            _context.ClearStateData();
            _context.TimeInState = 0f;

            // Enter new state
            _context.CurrentState = newState;
            newState.OnEnter(_context);

            if (debugMode)
                Debug.Log($"[BehaviourExecutor] Entering state: {newState.name}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Check if the behaviour executor is properly initialized and running.
        /// </summary>
        public bool IsInitialized()
        {
            return _isInitialized && _context != null && _context.CurrentProfile != null;
        }

        /// <summary>
        /// Manually trigger a transition check (useful for event-driven transitions).
        /// </summary>
        [Server]
        public void ForceTransitionCheck()
        {
            if (!isServer || !_isInitialized) return;
            CheckTransitions();
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!debugMode || _context == null) return;

            // Draw target connection
            if (_context.CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _context.CurrentTarget.transform.position);
                Gizmos.DrawWireSphere(_context.CurrentTarget.transform.position, 0.5f);
            }

            // Draw current destination
            if (_context.CurrentDestination != Vector3.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_context.CurrentDestination, 0.5f);
                
                // Draw path
                if (_context.CurrentPath != null && _context.CurrentPath.corners.Length > 1)
                {
                    Gizmos.color = Color.cyan;
                    for (int i = 1; i < _context.CurrentPath.corners.Length; i++)
                    {
                        Gizmos.DrawLine(_context.CurrentPath.corners[i - 1], _context.CurrentPath.corners[i]);
                    }
                }
            }
        }

        #endregion
    }
}
