using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(DestructibleObjective))]
public class DestructibleVisualState : NetworkBehaviour
{
    [Header("Visual Groups")]
    [Tooltip("Enabled when the objective is NOT destroyed.")]
    [SerializeField] private GameObject[] intactVisuals;

    [Tooltip("Enabled when the objective IS destroyed.")]
    [SerializeField] private GameObject[] destroyedVisuals;

    [Header("Renderer Fallback")]
    [Tooltip("If visual arrays are empty, toggle these renderers on destroy/rebuild.")]
    [SerializeField] private Renderer[] fallbackRenderers;

    [Tooltip("When using fallback renderers, disable colliders along with visuals.")]
    [SerializeField] private bool disableCollidersWithFallbackVisuals;

    private DestructibleObjective _objective;
    private Collider[] _cachedColliders;

    private void Awake()
    {
        _objective = GetComponent<DestructibleObjective>();
        _cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    private void Start()
    {
        if (_objective == null)
        {
            _objective = GetComponent<DestructibleObjective>();
        }

        if (_objective == null)
        {
            Debug.LogWarning($"[{nameof(DestructibleVisualState)}] Missing DestructibleObjective on {name}.", this);
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

    private new void OnValidate()
    {
        if ((intactVisuals == null || intactVisuals.Length == 0) &&
            (destroyedVisuals == null || destroyedVisuals.Length == 0))
        {
            AutoAssignFallbackRenderers();
        }
    }

    private void OnDestroyedStateChanged(bool isDestroyed)
    {
        ApplyState(isDestroyed);
    }

    private void ApplyState(bool isDestroyed)
    {
        bool usedVisualArrays = (intactVisuals != null && intactVisuals.Length > 0) ||
                                (destroyedVisuals != null && destroyedVisuals.Length > 0);

        if (usedVisualArrays)
        {
            SetGroupActive(intactVisuals, !isDestroyed);
            SetGroupActive(destroyedVisuals, isDestroyed);
            return;
        }

        bool showFallback = !isDestroyed;
        SetFallbackRenderersEnabled(showFallback);

        if (disableCollidersWithFallbackVisuals)
        {
            SetChildCollidersEnabled(showFallback);
        }
    }

    private void SetGroupActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            var go = objects[i];
            if (go == null) continue;
            go.SetActive(active);
        }
    }

    private void SetFallbackRenderersEnabled(bool enabled)
    {
        if (fallbackRenderers == null)
        {
            return;
        }

        for (int i = 0; i < fallbackRenderers.Length; i++)
        {
            var rend = fallbackRenderers[i];
            if (rend == null) continue;
            rend.enabled = enabled;
        }
    }

    private void SetChildCollidersEnabled(bool enabled)
    {
        if (_cachedColliders == null)
        {
            _cachedColliders = GetComponentsInChildren<Collider>(true);
        }

        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            var col = _cachedColliders[i];
            if (col == null) continue;
            if (_objective != null && _objective.ObjectiveUnit != null)
            {
                var objectiveCollider = _objective.ObjectiveUnit.GetComponent<Collider>();
                if (objectiveCollider != null && col == objectiveCollider)
                {
                    continue;
                }
            }
            col.enabled = enabled;
        }
    }

    private void AutoAssignFallbackRenderers()
    {
        fallbackRenderers = GetComponentsInChildren<Renderer>(true);
    }
}