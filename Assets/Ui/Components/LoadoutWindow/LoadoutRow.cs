using System;
using UnityEngine;
using UnityEngine.UIElements;
using Vland.UI;

public sealed class LoadoutRow : VisualElement
{
    public string Id { get; }
    public LoadoutItem Item { get; }

    public event Action<LoadoutRow> Clicked;
    public event Action<LoadoutRow> Hovered;
    public event Action Unhovered;

    private bool _selected;

    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            EnableInClassList("loadout-row--selected", _selected);
        }
    }

    public LoadoutRow(LoadoutItem item)
    {
        Item = item;
        Id = item.id ?? string.Empty;

        AddToClassList("loadout-row");
        pickingMode = PickingMode.Position;
        focusable = false;

        var icon = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore };
        icon.AddToClassList("loadout-row__icon");
        if (item.icon != null)
            icon.style.backgroundImage = new StyleBackground(item.icon);

        var body = new VisualElement { name = "body", pickingMode = PickingMode.Ignore };
        body.AddToClassList("loadout-row__body");

        var name = new Label(item.name ?? string.Empty) { pickingMode = PickingMode.Ignore };
        name.AddToClassList("loadout-row__name");
        body.Add(name);

        var tags = SkillTagUtil.GetLabels(item.tags);
        if (tags.Count > 0 || !string.IsNullOrEmpty(item.meta))
        {
            var metaRow = new VisualElement { pickingMode = PickingMode.Ignore };
            metaRow.AddToClassList("loadout-row__meta-row");

            foreach (var tagLabel in tags)
            {
                var chip = new Label(tagLabel) { pickingMode = PickingMode.Ignore };
                chip.AddToClassList("loadout-chip");
                metaRow.Add(chip);
            }

            if (!string.IsNullOrEmpty(item.meta))
            {
                var meta = new Label(item.meta) { pickingMode = PickingMode.Ignore };
                meta.AddToClassList("loadout-row__meta");
                metaRow.Add(meta);
            }

            body.Add(metaRow);
        }

        if (!string.IsNullOrEmpty(item.summary))
        {
            var summary = new Label(item.summary) { pickingMode = PickingMode.Ignore };
            summary.AddToClassList("loadout-row__summary");
            body.Add(summary);
        }

        Add(icon);
        Add(body);

        RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(this));
        RegisterCallback<PointerEnterEvent>(_ =>
        {
            UiCursorRefresh.PushInteractiveHover();
            Hovered?.Invoke(this);
        }, TrickleDown.TrickleDown);
        RegisterCallback<PointerLeaveEvent>(_ =>
        {
            UiCursorRefresh.PopInteractiveHover();
            Unhovered?.Invoke();
        }, TrickleDown.TrickleDown);
        RegisterCallback<DetachFromPanelEvent>(_ => UiCursorRefresh.PopInteractiveHover());
    }
}
