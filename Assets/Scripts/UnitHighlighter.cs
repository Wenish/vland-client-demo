using UnityEngine;

public class UnitHighlighter : MonoBehaviour
{
    public LayerMask unitLayerMask; // Set this to your unit layer in the inspector
    private GameObject lastHighlighted;
    private Camera _mainCamera;

    void Awake()
    {
        _mainCamera = Camera.main;
        Debug.Log("[UnitHighlighter] Awake: Main camera assigned.");
    }

    void Update()
    {
        if (_mainCamera == null)
        {
            Debug.LogWarning("[UnitHighlighter] Update: Main camera is null.");
            return;
        }
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, unitLayerMask))
        {
            GameObject unit = hit.collider.gameObject;
            if (lastHighlighted != unit)
            {
                Debug.Log($"[UnitHighlighter] Highlighting new unit: {unit.name}");
                RemoveHighlight();
                ApplyHighlight(unit);
                lastHighlighted = unit;
            }
        }
        else
        {
            if (lastHighlighted != null)
            {
                Debug.Log("[UnitHighlighter] No unit under cursor, removing highlight.");
                RemoveHighlight();
                lastHighlighted = null;
            }
        }
    }


    void ApplyHighlight(GameObject unit)
    {
        var outline = unit.GetComponentInChildren<Outline>();
        outline.enabled = true;
            // Set emission color to a bright color (e.g., yellow)

    }

    void RemoveHighlight()
    {
        if (lastHighlighted != null)
        {
            var outline = lastHighlighted.GetComponentInChildren<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
                Debug.Log($"[UnitHighlighter] Disable highlight from unit: {lastHighlighted.name}");
            }
        }
    }

    private void OnDestroy()
    {
        Debug.Log("[UnitHighlighter] OnDestroy: Removing highlight.");
        RemoveHighlight(); // Ensure highlight is removed when the script is destroyed
    }
}