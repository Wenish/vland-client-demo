using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.InventoryWindow
{
    internal sealed class InventoryRow : VisualElement
    {
        public string Id { get; }
        public InventoryRowVm Model { get; }

        public event Action<InventoryRow> Clicked;

        private bool _selected;

        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                EnableInClassList("inventory-row--selected", _selected);
            }
        }

        public InventoryRow(InventoryRowVm model)
        {
            Model = model ?? new InventoryRowVm();
            Id = Model.RowId ?? string.Empty;

            AddToClassList("inventory-row");
            pickingMode = PickingMode.Position;
            focusable = false;

            var icon = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore };
            icon.AddToClassList("inventory-row__icon");
            if (!string.IsNullOrEmpty(Model.RarityClass))
                icon.AddToClassList(Model.RarityClass);
            if (Model.Icon != null)
                icon.style.backgroundImage = new StyleBackground(Model.Icon);

            if (Model.IsStack && Model.Count > 1)
            {
                var badge = new Label(Model.Count.ToString()) { pickingMode = PickingMode.Ignore };
                badge.AddToClassList("inventory-row__stack");
                icon.Add(badge);
            }

            var body = new VisualElement { name = "body", pickingMode = PickingMode.Ignore };
            body.AddToClassList("inventory-row__body");

            var name = new Label(Model.Name ?? string.Empty) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("inventory-row__name");
            body.Add(name);

            if (!string.IsNullOrEmpty(Model.Meta))
            {
                var meta = new Label(Model.Meta) { pickingMode = PickingMode.Ignore };
                meta.AddToClassList("inventory-row__meta");
                body.Add(meta);
            }

            if (!string.IsNullOrEmpty(Model.Summary))
            {
                var summary = new Label(Model.Summary) { pickingMode = PickingMode.Ignore };
                summary.AddToClassList("inventory-row__summary");
                body.Add(summary);
            }

            Add(icon);
            Add(body);

            RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(this));
            RegisterCallback<PointerEnterEvent>(_ => UiCursorRefresh.PushInteractiveHover(), TrickleDown.TrickleDown);
            RegisterCallback<PointerLeaveEvent>(_ => UiCursorRefresh.PopInteractiveHover(), TrickleDown.TrickleDown);
            RegisterCallback<DetachFromPanelEvent>(_ => UiCursorRefresh.PopInteractiveHover());
        }
    }
}
