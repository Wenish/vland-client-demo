using Mirror;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(DestructibleObjective))]
public class DestructibleNavMeshBlocker : NetworkBehaviour
{
    [Header("Blockers")]
    [Tooltip("NavMesh obstacles toggled by destructible state.")]
    [SerializeField] private NavMeshObstacle[] navMeshObstacles;

    [Tooltip("Optional gameplay colliders to toggle with path blocking.")]
    [SerializeField] private Collider[] blockingColliders;

    [Header("Behavior")]
    [Tooltip("When true: object blocks while intact and opens path when destroyed.")]
    [SerializeField] private bool blockWhenIntact = true;

    private DestructibleObjective _objective;

    private void Awake()
    {
        _objective = GetComponent<DestructibleObjective>();
    }

    private void Start()
    {
        if (_objective == null)
        {
            _objective = GetComponent<DestructibleObjective>();
        }

        if (_objective == null)
        {
            Debug.LogWarning($"[{nameof(DestructibleNavMeshBlocker)}] Missing DestructibleObjective on {name}.", this);
            return;
        }

        _objective.OnDestroyedStateChanged += OnDestroyedStateChanged;
        ApplyState(_objective.IsDestroyed);
    }

    private void OnDestroy()
    {
        if (_objective != null)
        {
            _objective.OnDestroyedStateChanged -= OnDestroyedStateChanged;
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (navMeshObstacles == null || navMeshObstacles.Length == 0)
        {
            var obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle != null)
            {
                navMeshObstacles = new[] { obstacle };
            }
        }

        if (blockingColliders == null || blockingColliders.Length == 0)
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                blockingColliders = new[] { col };
            }
        }
    }

    private void OnDestroyedStateChanged(bool isDestroyed)
    {
        ApplyState(isDestroyed);
    }

    private void ApplyState(bool isDestroyed)
    {
        bool shouldBlock = blockWhenIntact ? !isDestroyed : isDestroyed;

        if (navMeshObstacles != null)
        {
            for (int i = 0; i < navMeshObstacles.Length; i++)
            {
                var obstacle = navMeshObstacles[i];
                if (obstacle == null) continue;
                obstacle.enabled = shouldBlock;
            }
        }

        if (blockingColliders != null)
        {
            for (int i = 0; i < blockingColliders.Length; i++)
            {
                var blocker = blockingColliders[i];
                if (blocker == null) continue;
                blocker.enabled = shouldBlock;
            }
        }
    }
}