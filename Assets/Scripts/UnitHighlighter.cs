using MyGame.Events;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitHighlighter : MonoBehaviour
{
    public LayerMask unitLayerMask; // Set this to your unit layer in the inspector
    private GameObject lastHighlighted;
    private Camera _mainCamera;

    [ColorUsage(true, true)]
    public Color outlineColorDefault = Color.yellow; // Default highlight color

    public float outlineWidth = 2f; // Default outline width


    void Awake()
    {
        _mainCamera = Camera.main;
    }

    void Update()
    {
        if (_mainCamera == null)
        {
            return;
        }
        // Use the new Unity Input System for pointer position
        Vector2 pointerPosition;
        if (Pointer.current != null)
        {
            pointerPosition = Pointer.current.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            pointerPosition = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Pen.current != null)
        {
            pointerPosition = Pen.current.position.ReadValue();
        }
        else
        {
            // No pointer available; clear highlight and skip
            if (lastHighlighted != null)
            {
                RemoveHighlight();
                lastHighlighted = null;
            }
            return;
        }

        Ray ray = _mainCamera.ScreenPointToRay(pointerPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, unitLayerMask))
        {
            GameObject unit = hit.collider.gameObject;
            if (lastHighlighted != unit)
            {
                RemoveHighlight();
                ApplyHighlight(unit);
                lastHighlighted = unit;
            }
        }
        else
        {
            if (lastHighlighted != null)
            {
                RemoveHighlight();
                lastHighlighted = null;
            }
        }
    }


    void ApplyHighlight(GameObject unit)
    {
        var outline = unit.AddComponent<Outline>();

        var hoverUnitControler = unit.GetComponent<UnitController>();
        var outlineColor = outlineColorDefault;
        if (hoverUnitControler != null)
        {
            outlineColor = TeamColorManager.Instance.GetColorForTeam(hoverUnitControler.team);
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
        RemoveHighlight(); // Ensure highlight is removed when the script is destroyed
    }
}