using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class AbilityCooldownElement : VisualElement
{
    private Image _iconImage;
    private VisualElement _iconOverlay;
    private VisualElement _cooldownOverlay;
    private Label _cooldownLabel;


    [SerializeField, DontCreateProperty]
    private float _cooldownRemaining;
    [UxmlAttribute, CreateProperty]
    public float CooldownRemaining
    {
        get => _cooldownRemaining;
        set
        {

            _cooldownRemaining = Math.Clamp(value, 0f, float.PositiveInfinity);
            _cooldownLabel.visible = _cooldownRemaining > 0f;
            _cooldownLabel.text = _cooldownRemaining < 1 ? _cooldownRemaining.ToString("F1") : Mathf.FloorToInt(_cooldownRemaining).ToString();
            // MarkDirtyRepaint();
        }
    }

    [SerializeField, DontCreateProperty]
    private float _cooldownProgress;

    [UxmlAttribute, CreateProperty]
    public float CooldownProgress
    {
        get => _cooldownProgress;
        set
        {
            _cooldownProgress = Mathf.Clamp(value, 0f, 100f);
            _cooldownOverlay.style.height = Length.Percent(_cooldownProgress);
            _iconOverlay.style.visibility = _cooldownProgress > 0f ? Visibility.Visible : Visibility.Hidden;
            // MarkDirtyRepaint();
        }
    }

    [SerializeField, DontCreateProperty]
    private Texture2D _iconTexture;
    [UxmlAttribute, CreateProperty]
    public Texture2D IconTexture
    {
        get => _iconTexture;
        set
        {
            _iconTexture = value;
            _iconImage.image = value;
            MarkDirtyRepaint();
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

    private Label _runtimeTooltip;

    public AbilityCooldownElement()
    {
        AddToClassList("ability-container");

        // ensure we get pointer events even if our element has no background
        this.pickingMode = PickingMode.Position;

        // wire up hover/leave
        RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave);


        _iconImage = new Image { name = "Icon" };
        _iconImage.AddToClassList("ability-icon");
        Add(_iconImage);

        _iconOverlay = new VisualElement { name = "IconOverlay" };
        _iconOverlay.AddToClassList("icon-overlay");
        Add(_iconOverlay);

        _cooldownOverlay = new VisualElement { name = "CooldownOverlay" };
        _cooldownOverlay.AddToClassList("cooldown-overlay");
        Add(_cooldownOverlay);

        var cooldownLabelContainer = new VisualElement { name = "CooldownLabelContainer" };
        cooldownLabelContainer.AddToClassList("cooldown-label-container");
        Add(cooldownLabelContainer);


        _cooldownLabel = new Label { name = "CooldownLabel" };
        _cooldownLabel.AddToClassList("cooldown-label");
        cooldownLabelContainer.Add(_cooldownLabel);
    }

    public void SetIcon(Texture2D texture)
    {
        _iconImage.image = texture;
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        if (string.IsNullOrEmpty(_tooltipText))
            return;

        // create the tooltip
        _runtimeTooltip = new Label
        {
            name = "runtime-tooltip",
            style =
            {
                position = Position.Absolute,
                unityTextAlign = TextAnchor.UpperLeft,
                paddingLeft = 6,
                paddingRight = 6,
                paddingTop = 6,
                paddingBottom = 6,
                backgroundColor = new StyleColor(Color.black),
                color = new StyleColor(Color.white),
                borderTopLeftRadius = 3,
                borderTopRightRadius = 3,
                borderBottomLeftRadius = 3,
                borderBottomRightRadius = 3,
                borderTopWidth = 2,
                borderBottomWidth = 2,
                borderLeftWidth = 2,
                borderRightWidth = 2,
                borderTopColor = new StyleColor(Color.yellow),
                borderBottomColor = new StyleColor(Color.yellow),
                borderLeftColor = new StyleColor(Color.yellow),
                borderRightColor = new StyleColor(Color.yellow),
            }
        };
        _runtimeTooltip.enableRichText = true;
        _runtimeTooltip.text = _tooltipText;

        // add it at root so it won’t be clipped
        panel.visualTree.Add(_runtimeTooltip);

        // when its layout is known, position it
        _runtimeTooltip.RegisterCallback<GeometryChangedEvent>(OnTooltipGeometryChanged);
    }

    private void OnTooltipGeometryChanged(GeometryChangedEvent geometryEvt)
    {
        // only run once
        _runtimeTooltip.UnregisterCallback<GeometryChangedEvent>(OnTooltipGeometryChanged);
        PositionTooltipFixed();
    }

    private void PositionTooltipFixed()
    {
        var root = panel.visualTree;
        float panelW = root.layout.width;
        float panelH = root.layout.height;
        float tipW = _runtimeTooltip.layout.width;
        float tipH = _runtimeTooltip.layout.height;

        // center horizontally, 20px above bottom
        _runtimeTooltip.style.left = panelW * 0.5f - tipW * 0.5f;
        _runtimeTooltip.style.top = panelH - tipH - 100f;
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
