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
    private Label _keyLabel; // displays the activation key

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
            _cooldownLabel.text = _cooldownRemaining < 1
                ? _cooldownRemaining.ToString("F1")
                : Mathf.FloorToInt(_cooldownRemaining).ToString();
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

    [SerializeField, DontCreateProperty]
    private string _activationKey;
    [UxmlAttribute, CreateProperty]
    public string ActivationKey
    {
        get => _activationKey;
        set
        {
            _activationKey = value;
            if (_keyLabel != null) {
                _keyLabel.text = _activationKey;
                _keyLabel.style.visibility = string.IsNullOrEmpty(_activationKey) ? Visibility.Hidden : Visibility.Visible;
            }

        }
    }

    private Label _runtimeTooltip;

    public AbilityCooldownElement()
    {
        AddToClassList("ability-container");

        // ensure pointer events
        this.pickingMode = PickingMode.Position;

        // hover callbacks
        RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

        // icon
        _iconImage = new Image { name = "Icon" };
        _iconImage.AddToClassList("ability-icon");
        Add(_iconImage);

        // disabled overlay
        _iconOverlay = new VisualElement { name = "IconOverlay" };
        _iconOverlay.AddToClassList("icon-overlay");
        Add(_iconOverlay);

        // cooldown fill
        _cooldownOverlay = new VisualElement { name = "CooldownOverlay" };
        _cooldownOverlay.AddToClassList("cooldown-overlay");
        Add(_cooldownOverlay);

        // cooldown label container
        var cooldownLabelContainer = new VisualElement { name = "CooldownLabelContainer" };
        cooldownLabelContainer.AddToClassList("cooldown-label-container");
        Add(cooldownLabelContainer);

        // cooldown label
        _cooldownLabel = new Label { name = "CooldownLabel" };
        _cooldownLabel.AddToClassList("cooldown-label");
        cooldownLabelContainer.Add(_cooldownLabel);

        // activation key label
        _keyLabel = new Label { name = "KeyLabel" };
        _keyLabel.AddToClassList("key-label");
        Add(_keyLabel);
        _keyLabel.style.visibility = string.IsNullOrEmpty(_activationKey) ? Visibility.Hidden : Visibility.Visible;
    }

    public void SetIcon(Texture2D texture)
    {
        _iconImage.image = texture;
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        if (string.IsNullOrEmpty(_tooltipText))
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
        _runtimeTooltip.text = _tooltipText;
        panel.visualTree.Add(_runtimeTooltip);
        _runtimeTooltip.RegisterCallback<GeometryChangedEvent>(OnTooltipGeometryChanged);
    }

    private void OnTooltipGeometryChanged(GeometryChangedEvent geometryEvt)
    {
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
