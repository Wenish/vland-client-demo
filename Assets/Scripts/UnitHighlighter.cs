using MyGame.Events;
using UnityEngine;

public class UnitHighlighter : MonoBehaviour
{
    public LayerMask unitLayerMask; // Set this to your unit layer in the inspector
    private GameObject lastHighlighted;
    private Camera _mainCamera;

    [ColorUsage(true, true)]
    public Color outlineColorDefault = Color.yellow; // Default highlight color

    [ColorUsage(true, true)]
    public Color outlineColorSameTeam = Color.blue;

    [ColorUsage(true, true)]
    public Color outlineColorOtherTeam = Color.red;

    public float outlineWidth = 2f; // Default outline width

    public UnitController localUnitController; // Reference to the local unit controller


    void Awake()
    {
        _mainCamera = Camera.main;
        Debug.Log("[UnitHighlighter] Awake: Main camera assigned.");
    }

    void Start()
    {
        EventManager.Instance.Subscribe<MyPlayerUnitSpawnedEvent>(OnPlayerUnitSpawned);
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
        var outline = unit.AddComponent<Outline>();

        var hoverUnitControler = unit.GetComponent<UnitController>();
        var outlineColor = outlineColorDefault;
        if (hoverUnitControler != null && localUnitController != null)
        {
            if (hoverUnitControler.team == localUnitController.team)
            {
                outlineColor = outlineColorSameTeam; // Same team
            }
            else
            {
                outlineColor = outlineColorOtherTeam; // Different team
            }
        }

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
                        Debug.Log($"[UnitHighlighter] Removed highlight from unit: {lastHighlighted.name}");
                    }

                    return;
                }
            }

    private void OnPlayerUnitSpawned(MyPlayerUnitSpawnedEvent myPlayerUnitSpawnedEvent)
    {
        localUnitController = myPlayerUnitSpawnedEvent.PlayerCharacter;
    }

    private void OnDestroy()
    {
        EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(OnPlayerUnitSpawned);

        Debug.Log("[UnitHighlighter] OnDestroy: Removing highlight.");
        RemoveHighlight(); // Ensure highlight is removed when the script is destroyed
    }
}