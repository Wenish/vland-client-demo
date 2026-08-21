using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Vland.UI;

public sealed class LoadoutView
{
    private readonly VisualElement overlay;
    private readonly VisualElement panel;
    private readonly VisualElement openHost;
    private readonly VisualElement slotsBar;
    private readonly VisualElement filterBar;
    private readonly VisualElement itemList;
    private readonly VisualElement detailIcon;
    private readonly VisualElement detailTags;
    private readonly Label subheading;
    private readonly Label detailName;
    private readonly Label detailMeta;
    private readonly Label detailDescription;
    private readonly Label detailEmpty;
    private readonly Button closeButton;
    private readonly Button openButton;

    private readonly Dictionary<LoadoutSlot, VisualElement> slotElements = new();
    private readonly Dictionary<SkillTag, Button> filterButtons = new();

    private bool isOpen;
    private LoadoutItem displayedDetail;
    private LoadoutItem selectedDetail;
    private bool hasDisplayedDetail;
    private bool hasSelectedDetail;

    public event Action CloseClicked;
    public event Action OpenClicked;
    public event Action OverlayClicked;
    public event Action<LoadoutSlot> SlotClicked;
    public event Action<string> ItemClicked;
    public event Action<SkillTag?> FilterClicked;

    public bool IsOpen => isOpen;

    public LoadoutView(VisualElement root)
    {
        overlay = root.Q<VisualElement>("loadoutOverlay");
        panel = root.Q<VisualElement>("LoadoutPanel");
        openHost = root.Q<VisualElement>("loadoutOpenHost");
        slotsBar = root.Q<VisualElement>("slotsBar");
        filterBar = root.Q<VisualElement>("filterBar");
        itemList = root.Q<VisualElement>("itemList");
        detailIcon = root.Q<VisualElement>("detailIcon");
        detailTags = root.Q<VisualElement>("detailTags");
        subheading = root.Q<Label>("subheading");
        detailName = root.Q<Label>("detailName");
        detailMeta = root.Q<Label>("detailMeta");
        detailDescription = root.Q<Label>("detailDescription");
        detailEmpty = root.Q<Label>("detailEmpty");
        closeButton = root.Q<OrnateButton>("closeButton") ?? root.Q<Button>("closeButton");
        openButton = root.Q<OrnateButton>("openButton") ?? root.Q<Button>("openButton");

        if (overlay == null || panel == null)
        {
            Debug.LogError("LoadoutView: overlay or panel was not found in the loadout UI.");
            return;
        }

        overlay.pickingMode = PickingMode.Position;
        panel.pickingMode = PickingMode.Position;
        UiGameplayInputGuard.Apply(overlay);
        if (openHost != null)
            UiGameplayInputGuard.Apply(openHost);
        UiPointerState.RegisterBlockingElement(overlay);
        if (openButton != null)
            UiPointerState.RegisterBlockingElement(openButton);

        overlay.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == overlay)
                OverlayClicked?.Invoke();
        });
        panel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

        if (closeButton != null)
            closeButton.clicked += () => CloseClicked?.Invoke();
        else
            Debug.LogError("LoadoutView: closeButton was not found in the loadout UI.");

        if (openButton != null)
            openButton.clicked += () => OpenClicked?.Invoke();

        BuildSlots();
        BuildFilters();
        ClearDetail();
        SetOpen(false);
    }

    public void Dispose()
    {
        UiPointerState.UnregisterBlockingElement(overlay);
        if (openButton != null)
            UiPointerState.UnregisterBlockingElement(openButton);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (overlay != null)
            overlay.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        if (openHost != null)
            openHost.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
    }

    public void SetActiveSlot(LoadoutSlot slot)
    {
        foreach (var pair in slotElements)
            pair.Value.EnableInClassList("loadout-slot--active", pair.Key == slot);
    }

    public void SetSlot(LoadoutSlot slot, LoadoutItem item)
    {
        if (!slotElements.TryGetValue(slot, out var element))
            return;

        var icon = element.Q<VisualElement>(className: "loadout-slot__icon");
        var name = element.Q<Label>(className: "loadout-slot__name");
        var role = element.Q<Label>(className: "loadout-slot__role");
        var roleLabel = LoadoutSlots.RoleLabel(slot);

        if (icon != null)
        {
            icon.style.backgroundImage = item.icon != null
                ? new StyleBackground(item.icon)
                : StyleKeyword.None;
        }

        if (name != null)
            name.text = item.HasId ? item.name : "Empty";
        if (role != null)
            role.text = roleLabel;
    }

    public void SetSubheading(string text)
    {
        if (subheading != null)
            subheading.text = text ?? string.Empty;
    }

    public void SetFilter(SkillTag? selectedTag, bool visible)
    {
        if (filterBar != null)
            filterBar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        var activeTag = selectedTag ?? SkillTag.None;
        foreach (var pair in filterButtons)
            pair.Value.EnableInClassList("loadout-filter--active", pair.Key == activeTag);
    }

    public void SetItems(IReadOnlyList<LoadoutItem> items, string selectedId, string emptyMessage)
    {
        if (itemList == null)
            return;

        itemList.Clear();
        hasSelectedDetail = false;

        if (items == null || items.Count == 0)
        {
            var empty = new Label(emptyMessage ?? "No items to show.");
            empty.AddToClassList("loadout-empty");
            itemList.Add(empty);
            ClearDetail();
            return;
        }

        LoadoutItem fallback = default;
        var hasFallback = false;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var row = new LoadoutRow(item);
            if (i == items.Count - 1)
                row.AddToClassList("loadout-row--last");

            var isSelected = !string.IsNullOrEmpty(selectedId) && item.id == selectedId;
            row.Selected = isSelected;
            if (isSelected)
            {
                selectedDetail = item;
                hasSelectedDetail = true;
            }
            else if (!hasFallback)
            {
                fallback = item;
                hasFallback = true;
            }

            row.Clicked += clicked => ItemClicked?.Invoke(clicked.Id);
            row.Hovered += hovered => ShowDetail(hovered.Item);
            row.Unhovered += ShowSelectedOrClearDetail;
            itemList.Add(row);
        }

        if (hasSelectedDetail)
            ShowDetail(selectedDetail);
        else if (hasFallback)
            ShowDetail(fallback);
        else
            ClearDetail();
    }

    private void BuildSlots()
    {
        if (slotsBar == null)
            return;

        slotsBar.Clear();
        slotElements.Clear();

        var slots = LoadoutSlots.All;
        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            var element = new VisualElement { pickingMode = PickingMode.Position };
            element.AddToClassList("loadout-slot");
            if (i == slots.Length - 1)
                element.AddToClassList("loadout-slot--last");
            element.userData = slot;

            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("loadout-slot__icon");

            var name = new Label("Empty") { pickingMode = PickingMode.Ignore };
            name.AddToClassList("loadout-slot__name");

            var role = new Label(LoadoutSlots.RoleLabel(slot)) { pickingMode = PickingMode.Ignore };
            role.AddToClassList("loadout-slot__role");

            element.Add(icon);
            element.Add(name);
            element.Add(role);

            var captured = slot;
            element.RegisterCallback<ClickEvent>(_ => SlotClicked?.Invoke(captured));
            element.RegisterCallback<PointerEnterEvent>(_ => UiCursorRefresh.PushInteractiveHover(), TrickleDown.TrickleDown);
            element.RegisterCallback<PointerLeaveEvent>(_ => UiCursorRefresh.PopInteractiveHover(), TrickleDown.TrickleDown);
            element.RegisterCallback<DetachFromPanelEvent>(_ => UiCursorRefresh.PopInteractiveHover());

            slotsBar.Add(element);
            slotElements[slot] = element;
            SetSlot(slot, LoadoutItem.Empty);
        }
    }

    private void BuildFilters()
    {
        if (filterBar == null)
            return;

        filterBar.Clear();
        filterButtons.Clear();

        AddFilterButton(SkillTag.None, "All");
        foreach (var tag in SkillTagUtil.FilterTags)
            AddFilterButton(tag, SkillTagUtil.GetLabel(tag));
    }

    private void AddFilterButton(SkillTag tag, string label)
    {
        var button = new Button { text = label, focusable = false };
        button.AddToClassList("loadout-filter");
        var clickedTag = tag == SkillTag.None ? (SkillTag?)null : tag;
        button.clicked += () => FilterClicked?.Invoke(clickedTag);
        filterBar.Add(button);
        filterButtons[tag] = button;
    }

    private void ShowSelectedOrClearDetail()
    {
        if (hasSelectedDetail)
            ShowDetail(selectedDetail);
        else if (hasDisplayedDetail)
            ShowDetail(displayedDetail);
    }

    private void ShowDetail(LoadoutItem item)
    {
        displayedDetail = item;
        hasDisplayedDetail = item.HasId;

        var hasItem = item.HasId;
        SetDetailVisible(hasItem);
        if (!hasItem)
            return;

        if (detailIcon != null)
        {
            detailIcon.style.backgroundImage = item.icon != null
                ? new StyleBackground(item.icon)
                : StyleKeyword.None;
        }

        if (detailName != null)
            detailName.text = item.name ?? string.Empty;
        if (detailMeta != null)
            detailMeta.text = item.meta ?? string.Empty;
        if (detailDescription != null)
            detailDescription.text = item.description ?? string.Empty;

        if (detailTags == null)
            return;

        detailTags.Clear();
        foreach (var tagLabel in SkillTagUtil.GetLabels(item.tags))
        {
            var chip = new Label(tagLabel) { pickingMode = PickingMode.Ignore };
            chip.AddToClassList("loadout-chip");
            detailTags.Add(chip);
        }

        detailTags.style.display = detailTags.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ClearDetail()
    {
        hasDisplayedDetail = false;
        displayedDetail = LoadoutItem.Empty;
        SetDetailVisible(false);
        if (detailTags != null)
            detailTags.Clear();
        if (detailName != null)
            detailName.text = string.Empty;
        if (detailMeta != null)
            detailMeta.text = string.Empty;
        if (detailDescription != null)
            detailDescription.text = string.Empty;
        if (detailIcon != null)
            detailIcon.style.backgroundImage = StyleKeyword.None;
    }

    private void SetDetailVisible(bool hasItem)
    {
        var contentDisplay = hasItem ? DisplayStyle.Flex : DisplayStyle.None;
        var emptyDisplay = hasItem ? DisplayStyle.None : DisplayStyle.Flex;
        if (detailIcon != null)
            detailIcon.style.display = contentDisplay;
        if (detailName != null)
            detailName.style.display = contentDisplay;
        if (detailTags != null)
            detailTags.style.display = contentDisplay;
        if (detailMeta != null)
            detailMeta.style.display = contentDisplay;
        if (detailDescription != null)
            detailDescription.style.display = contentDisplay;
        if (detailEmpty != null)
            detailEmpty.style.display = emptyDisplay;
    }
}
