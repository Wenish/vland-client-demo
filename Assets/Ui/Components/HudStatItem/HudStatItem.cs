using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

public enum HudStatIcon
{
    Clock,
    Skull,
    Trophy,
    Sword,
    ChartUp
}

[UxmlElement]
public partial class HudStatItem : VisualElement
{
    public const string UssClassName = "si-round-stat";
    public const string TimerUssClassName = "si-round-stat--timer";
    public const string IconUssClassName = "si-round-stat__icon";
    public const string ValueUssClassName = "si-round-stat__value";

    private const float ValueSlotEm = 0.8f;
    private const float ValueSlotPaddingPx = 2f;

    private readonly VisualElement iconElement;
    private readonly Label valueLabel;
    private int reservedValueLength;
    private float reservedValueWidth;

    [SerializeField, DontCreateProperty]
    private HudStatIcon iconKind;

    [SerializeField, DontCreateProperty]
    private string valueText = string.Empty;

    [UxmlAttribute, CreateProperty]
    public HudStatIcon Icon
    {
        get => iconKind;
        set
        {
            iconElement.RemoveFromClassList(GetIconClass(iconKind));
            iconKind = value;
            iconElement.AddToClassList(GetIconClass(iconKind));
            EnableInClassList(TimerUssClassName, iconKind == HudStatIcon.Clock);
        }
    }

    [UxmlAttribute, CreateProperty]
    public string Value
    {
        get => valueText;
        set
        {
            valueText = value ?? string.Empty;
            valueLabel.text = valueText;
            UpdateReservedValueWidth();
        }
    }

    public HudStatItem()
    {
        pickingMode = PickingMode.Ignore;
        AddToClassList(UssClassName);

        iconElement = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore };
        iconElement.AddToClassList(IconUssClassName);
        iconElement.AddToClassList(GetIconClass(iconKind));

        valueLabel = new Label { name = "value", pickingMode = PickingMode.Ignore };
        valueLabel.AddToClassList(ValueUssClassName);

        Add(iconElement);
        Add(valueLabel);

        EnableInClassList(TimerUssClassName, iconKind == HudStatIcon.Clock);
        RegisterCallback<AttachToPanelEvent>(_ => UpdateReservedValueWidth());
        RegisterCallback<GeometryChangedEvent>(_ => UpdateReservedValueWidth());
    }

    private void UpdateReservedValueWidth()
    {
        if (valueLabel == null)
            return;

        var length = valueText.Length;
        if (length == 0 || length < reservedValueLength)
            return;

        var fontSize = valueLabel.resolvedStyle.fontSize;
        if (fontSize <= 0f)
            fontSize = 16f;

        var width = Mathf.Ceil(fontSize * ValueSlotEm * length + ValueSlotPaddingPx);
        if (length == reservedValueLength && width <= reservedValueWidth)
            return;

        reservedValueLength = length;
        reservedValueWidth = Mathf.Max(reservedValueWidth, width);

        var size = reservedValueWidth;
        valueLabel.style.width = size;
        valueLabel.style.minWidth = size;
        valueLabel.style.maxWidth = size;
    }

    private static string GetIconClass(HudStatIcon icon)
    {
        return icon switch
        {
            HudStatIcon.Skull => "si-round-stat__icon--skull",
            HudStatIcon.Trophy => "si-round-stat__icon--trophy",
            HudStatIcon.Sword => "si-round-stat__icon--sword",
            HudStatIcon.ChartUp => "si-round-stat__icon--chart-up",
            _ => "si-round-stat__icon--clock"
        };
    }
}
