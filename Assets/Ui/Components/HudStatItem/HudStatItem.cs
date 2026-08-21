using System.Collections.Generic;
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
    public const string GlyphUssClassName = "si-round-stat__glyph";

    private const float DigitSlotEm = 0.62f;
    private const float OtherSlotEm = 0.34f;

    private readonly VisualElement iconElement;
    private readonly VisualElement valueRow;
    private readonly Label measureProbe;
    private readonly List<Label> glyphLabels = new();

    private float digitSlotWidth;
    private float otherSlotWidth;

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
            RefreshGlyphs();
        }
    }

    public HudStatItem()
    {
        pickingMode = PickingMode.Ignore;
        AddToClassList(UssClassName);

        iconElement = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore };
        iconElement.AddToClassList(IconUssClassName);
        iconElement.AddToClassList(GetIconClass(iconKind));

        valueRow = new VisualElement { name = "value", pickingMode = PickingMode.Ignore };
        valueRow.AddToClassList(ValueUssClassName);

        measureProbe = CreateGlyphLabel();
        measureProbe.style.position = Position.Absolute;
        measureProbe.style.left = -9999;
        measureProbe.style.visibility = Visibility.Hidden;
        valueRow.Add(measureProbe);

        Add(iconElement);
        Add(valueRow);

        EnableInClassList(TimerUssClassName, iconKind == HudStatIcon.Clock);
        RegisterCallback<AttachToPanelEvent>(_ => RefreshGlyphs());
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        var previousDigit = digitSlotWidth;
        var previousOther = otherSlotWidth;
        RefreshSlotWidths();
        if (previousDigit > 0f
            && Mathf.Approximately(previousDigit, digitSlotWidth)
            && Mathf.Approximately(previousOther, otherSlotWidth))
            return;

        ApplyGlyphs();
    }

    private void RefreshGlyphs()
    {
        RefreshSlotWidths();
        ApplyGlyphs();
    }

    private void ApplyGlyphs()
    {
        var text = valueText ?? string.Empty;
        EnsureGlyphCount(text.Length);

        for (var i = 0; i < glyphLabels.Count; i++)
        {
            var glyph = glyphLabels[i];
            if (i >= text.Length)
            {
                glyph.style.display = DisplayStyle.None;
                continue;
            }

            var character = text[i];
            glyph.text = character.ToString();
            glyph.style.display = DisplayStyle.Flex;

            var width = char.IsDigit(character) ? digitSlotWidth : otherSlotWidth;
            glyph.style.width = width;
            glyph.style.minWidth = width;
            glyph.style.maxWidth = width;
        }
    }

    private void RefreshSlotWidths()
    {
        var fontSize = measureProbe.resolvedStyle.fontSize;
        if (fontSize <= 0f)
            fontSize = 16f;

        var digitWidth = MeasureWidestDigit();
        if (digitWidth <= 0f)
            digitWidth = fontSize * DigitSlotEm;

        var otherWidth = MeasureTextWidth(":");
        if (otherWidth <= 0f)
            otherWidth = fontSize * OtherSlotEm;

        digitSlotWidth = Mathf.Ceil(digitWidth);
        otherSlotWidth = Mathf.Ceil(otherWidth);
    }

    private float MeasureWidestDigit()
    {
        var max = 0f;
        for (var digit = '0'; digit <= '9'; digit++)
            max = Mathf.Max(max, MeasureTextWidth(digit.ToString()));
        return max;
    }

    private float MeasureTextWidth(string text)
    {
        if (panel == null)
            return 0f;

        var size = measureProbe.MeasureTextSize(
            text,
            0,
            MeasureMode.Undefined,
            0,
            MeasureMode.Undefined);
        return size.x;
    }

    private void EnsureGlyphCount(int count)
    {
        while (glyphLabels.Count < count)
        {
            var glyph = CreateGlyphLabel();
            valueRow.Add(glyph);
            glyphLabels.Add(glyph);
        }
    }

    private static Label CreateGlyphLabel()
    {
        var glyph = new Label { pickingMode = PickingMode.Ignore };
        glyph.AddToClassList(GlyphUssClassName);
        glyph.style.marginTop = 0;
        glyph.style.marginBottom = 0;
        glyph.style.marginLeft = 0;
        glyph.style.marginRight = 0;
        glyph.style.paddingTop = 0;
        glyph.style.paddingBottom = 0;
        glyph.style.paddingLeft = 0;
        glyph.style.paddingRight = 0;
        glyph.style.unityTextAlign = TextAnchor.MiddleCenter;
        glyph.style.overflow = Overflow.Hidden;
        glyph.style.whiteSpace = WhiteSpace.NoWrap;
        glyph.style.flexShrink = 0;
        return glyph;
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
