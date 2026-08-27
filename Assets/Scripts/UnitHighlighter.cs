using R3;
using ShadowInfection;
using ShadowInfection.DI;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitHighlighter : MonoBehaviour
{
    public LayerMask unitLayerMask; // Set this to your unit layer in the inspector
    private GameObject lastHighlighted;
    private UnitController _snapHighlightTarget;
    private Camera _mainCamera;
    private R3.DisposableBag subscriptions;

    [ColorUsage(true, true)]
    public Color outlineColorDefault = Color.yellow; // Default highlight color

    public float outlineWidth = 2f; // Default outline width


    void Awake()
    {
        _mainCamera = Camera.main;
    }

    void OnEnable()
    {
        subscriptions.Dispose();
        subscriptions = new R3.DisposableBag();

        if (!GameplayLifetimeScope.TryResolve<ISkillAimPreviewSession>(out var session))
            return;

        subscriptions.Add(session.State.Subscribe(OnAimPreviewStateChanged));
        OnAimPreviewStateChanged(session.State.CurrentValue);
    }

    void OnDisable()
    {
        subscriptions.Dispose();
        subscriptions = new R3.DisposableBag();
        _snapHighlightTarget = null;
    }

    void Update()
    {
        if (_mainCamera == null)
        {
            return;
        }

        if (TryGetSnapHighlightTarget(out GameObject snapTarget))
        {
            SetHighlight(snapTarget);
            return;
        }

        if (!TryGetPointerPosition(out Vector2 pointerPosition))
        {
            SetHighlight(null);
            return;
        }

        Ray ray = _mainCamera.ScreenPointToRay(pointerPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, unitLayerMask))
            SetHighlight(hit.collider.gameObject);
        else
            SetHighlight(null);
    }

    void OnAimPreviewStateChanged(SkillAimPreviewState? state)
    {
        var previous = _snapHighlightTarget;
        _snapHighlightTarget = state?.ShouldOverrideHoverHighlight == true
            ? state.Value.FollowTarget
            : null;

        if (_snapHighlightTarget != null)
        {
            SetHighlight(_snapHighlightTarget.gameObject);
            return;
        }

        if (previous != null)
        {
            RemoveHighlight();
            lastHighlighted = null;
        }
    }

    bool TryGetSnapHighlightTarget(out GameObject snapTarget)
    {
        snapTarget = null;
        if (_snapHighlightTarget == null)
            return false;

        snapTarget = _snapHighlightTarget.gameObject;
        return true;
    }

    static bool TryGetPointerPosition(out Vector2 pointerPosition)
    {
        if (Pointer.current != null)
        {
            pointerPosition = Pointer.current.position.ReadValue();
            return true;
        }

        if (Mouse.current != null)
        {
            pointerPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Pen.current != null)
        {
            pointerPosition = Pen.current.position.ReadValue();
            return true;
        }

        pointerPosition = default;
        return false;
    }

    void SetHighlight(GameObject unit)
    {
        if (lastHighlighted == unit)
            return;

        RemoveHighlight();
        if (unit == null)
        {
            lastHighlighted = null;
            return;
        }

        ApplyHighlight(unit);
        lastHighlighted = unit;
    }

    void ApplyHighlight(GameObject unit)
    {
        var outline = unit.AddComponent<Outline>();

        var hoverUnitControler = unit.GetComponent<UnitController>();
        var outlineColor = outlineColorDefault;
        if (hoverUnitControler != null)
        {
            if (GameLifetimeScope.TryResolve<ITeamColorService>(out var teamColors))
            {
                outlineColor = teamColors.GetColorForTeam(hoverUnitControler.team);
            }
        }
        outlineColor.a = 0.5f; // Set alpha to 50%

        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = outlineWidth;
    }

    void RemoveHighlight()
    {
        if (lastHighlighted != null)
        {

            var outline = lastHighlighted.GetComponent<Outline>();
            if (outline != null)
            {
                Destroy(outline); // Remove the outline component
            }

            return;
        }
    }

    private void OnDestroy()
    {
        subscriptions.Dispose();
        RemoveHighlight(); // Ensure highlight is removed when the script is destroyed
    }
}
