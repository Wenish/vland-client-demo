using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class LoadoutTile : VisualElement
{
    [SerializeField, DontCreateProperty]
    private Texture2D _icon;

    [UxmlAttribute, CreateProperty]
    public Texture2D Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            if (_iconEl != null)
                _iconEl.style.backgroundImage = _icon != null ? new StyleBackground(_icon) : StyleKeyword.None;
        }
    }

    [SerializeField, DontCreateProperty]
    private string _displayName;

    [UxmlAttribute, CreateProperty]
    public string DisplayName
    {
        get => _displayName;
        set => _displayName = value;
    }

    [SerializeField, DontCreateProperty]
    private string _id;

    [UxmlAttribute, CreateProperty]
    public string Id
    {
        get => _id;
        set => _id = value;
    }

    [SerializeField, DontCreateProperty]
    private bool _selected;

    [UxmlAttribute, CreateProperty]
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            EnableInClassList("selected", _selected);
        }
    }

    [SerializeField, DontCreateProperty]
    private string _tooltipText;

    [UxmlAttribute, CreateProperty]
    public string TooltipText
    {
        get => _tooltipText;
        set => _tooltipText = value;
    }

    public event Action<LoadoutTile> Clicked;

    private VisualElement _iconEl;
    private Label _runtimeTooltip;

    public LoadoutTile()
    {
        AddToClassList("loadout-tile");
        style.flexDirection = FlexDirection.Column;
        style.alignItems = Align.Stretch;
        style.justifyContent = Justify.Center;

        // ensure pointer events for hover
        pickingMode = PickingMode.Position;

        _iconEl = new VisualElement { name = "icon" };
        _iconEl.AddToClassList("loadout-tile__icon");
        _iconEl.style.backgroundImage = _icon != null ? new StyleBackground(_icon) : StyleKeyword.None;
        _iconEl.style.flexGrow = 1;
        _iconEl.style.width = Length.Percent(100);
        _iconEl.style.height = Length.Percent(100);
        Add(_iconEl);

        RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(this));

        // hover callbacks for tooltip (mirrors AbilityCooldownElement behavior)
        RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

        // Keep square aspect: set height equal to width when layout changes
        RegisterCallback<GeometryChangedEvent>(evt =>
        {
            style.height = evt.newRect.width;
        });
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        // derive tooltip text: prefer explicit TooltipText, otherwise DisplayName
        var text = string.IsNullOrEmpty(_tooltipText) ? _displayName : _tooltipText;
        if (string.IsNullOrEmpty(text))
            return;

        StyleColor backgroundColor = new StyleColor(new Color(0.051f, 0.051f, 0.051f, 0.995f));
        StyleColor borderColor = new StyleColor(new Color(0.29f, 0.29f, 0.29f, 0.8f));
        StyleColor textColor = new StyleColor(new Color(0.98f, 0.98f, 0.98f, 1f));

        _runtimeTooltip = new Label
        {
            name = "runtime-tooltip",
            style =
            {
                position = Position.Absolute,
                unityTextAlign = TextAnchor.UpperLeft,
                minWidth = 180,
                maxWidth = 300,
                whiteSpace = WhiteSpace.Normal,
                paddingLeft = 8,
                paddingRight = 8,
                paddingTop = 6,
                paddingBottom = 6,
                backgroundColor = backgroundColor,
                color = textColor,
                borderTopLeftRadius = 3,
                borderTopRightRadius = 3,
                borderBottomLeftRadius = 3,
                borderBottomRightRadius = 3,
                borderTopWidth = 2,
                borderBottomWidth = 2,
                borderLeftWidth = 2,
                borderRightWidth = 2,
                borderTopColor = borderColor,
                borderBottomColor = borderColor,
                borderLeftColor = borderColor,
                borderRightColor = borderColor
            }
        };
        _runtimeTooltip.enableRichText = true;
        _runtimeTooltip.text = text;
        panel.visualTree.Add(_runtimeTooltip);
        _runtimeTooltip.RegisterCallback<GeometryChangedEvent>(OnTooltipGeometryChanged);
    }

    private void OnTooltipGeometryChanged(GeometryChangedEvent geometryEvt)
    {
        if (_runtimeTooltip == null)
            return;
        _runtimeTooltip.UnregisterCallback<GeometryChangedEvent>(OnTooltipGeometryChanged);
        PositionTooltipFixed();
    }

    private void PositionTooltipFixed()
    {
        if (_runtimeTooltip == null)
            return;
        var root = panel.visualTree;
        var tileRect = worldBound; // in panel space
        float panelW = root.layout.width;
        float panelH = root.layout.height;
        float tipW = _runtimeTooltip.layout.width;
        float tipH = _runtimeTooltip.layout.height;

        const float margin = 8f;

        // Default position: to the left of the tile, vertically centered
        float left = tileRect.x - tipW - margin;
        float top = tileRect.y + (tileRect.height - tipH) * 0.5f;

        // Clamp vertically within panel
        top = Mathf.Clamp(top, 0, Mathf.Max(0, panelH - tipH));

        // If not enough space on the left, place to the right as a fallback
        if (left < 0)
        {
            left = Mathf.Min(tileRect.xMax + margin, Mathf.Max(0, panelW - tipW));
        }
        else
        {
            // Ensure it doesn't exceed panel on the left
            left = Mathf.Max(0, left);
        }

        _runtimeTooltip.style.left = left;
        _runtimeTooltip.style.top = top;
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        if (_runtimeTooltip != null)
        {
            panel.visualTree.Remove(_runtimeTooltip);
            _runtimeTooltip = null;
        }
    }
}
