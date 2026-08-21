using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vland.UI
{
    public sealed class VendorRowVm
    {
        public string Id;
        public VendorTab Tab;
        public Texture2D Icon;
        public string IconClass;
        public string Name;
        public string Subtitle;
        public string Flavour;
        public string TypeLine;
        public string StatBlock;
        public int PriceGold;
        public int StackCount;
        public bool Dimmed;
        public bool Locked;
        public bool CanTransact;
        public string PriceNote;
        public string TooltipAction;
    }

    public sealed class VendorRow : VisualElement
    {
        public string Id { get; }
        public VendorRowVm Model { get; }

        public event Action<VendorRow> Selected;
        public event Action<VendorRow> TransactRequested;
        public event Action<VendorRow, Vector2> Hovered;
        public event Action Unhovered;

        private bool _selected;

        public bool SelectedState
        {
            get => _selected;
            set
            {
                _selected = value;
                EnableInClassList("vendor-row--selected", _selected);
            }
        }

        public VendorRow(VendorRowVm model)
        {
            Model = model ?? new VendorRowVm();
            Id = Model.Id ?? string.Empty;

            AddToClassList("vendor-row");
            pickingMode = PickingMode.Position;
            focusable = false;

            if (Model.Dimmed)
                AddToClassList("vendor-row--disabled");
            if (Model.Locked)
                AddToClassList("vendor-row--locked");

            var icon = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore };
            icon.AddToClassList("vendor-row__icon");
            if (Model.Icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(Model.Icon);
                icon.style.unityBackgroundImageTintColor = Color.white;
            }
            if (!string.IsNullOrEmpty(Model.IconClass))
                icon.AddToClassList(Model.IconClass);

            if (Model.StackCount > 1)
            {
                var badge = new Label(Model.StackCount.ToString()) { pickingMode = PickingMode.Ignore };
                badge.AddToClassList("vendor-row__stack");
                icon.Add(badge);
            }

            var body = new VisualElement { name = "body", pickingMode = PickingMode.Ignore };
            body.AddToClassList("vendor-row__body");

            var name = new Label(Model.Name ?? string.Empty) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("vendor-row__name");
            body.Add(name);

            var subtitle = new Label(Model.Subtitle ?? string.Empty) { pickingMode = PickingMode.Ignore };
            subtitle.AddToClassList("vendor-row__subtitle");
            body.Add(subtitle);

            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList("vendor-row__spacer");

            var price = new VisualElement { pickingMode = PickingMode.Ignore };
            price.AddToClassList("vendor-row__price");

            var priceRow = new VisualElement { pickingMode = PickingMode.Ignore };
            priceRow.AddToClassList("vendor-gold");

            var coin = new VisualElement { pickingMode = PickingMode.Ignore };
            coin.AddToClassList("vendor-gold__icon");
            var value = new Label(Model.PriceGold.ToString()) { pickingMode = PickingMode.Ignore };
            value.AddToClassList("vendor-gold__value");
            priceRow.Add(coin);
            priceRow.Add(value);
            price.Add(priceRow);

            if (!string.IsNullOrEmpty(Model.PriceNote))
            {
                var note = new Label(Model.PriceNote) { pickingMode = PickingMode.Ignore };
                note.AddToClassList("vendor-row__price-note");
                price.Add(note);
            }

            if (Model.CanTransact && !Model.Locked)
                AddToClassList("vendor-row--buyable");

            Add(icon);
            Add(body);
            Add(spacer);
            Add(price);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (Model.Locked)
                return;

            if (evt.button == 0)
            {
                Selected?.Invoke(this);
                return;
            }

            if (evt.button == 1)
            {
                evt.StopPropagation();
                TransactRequested?.Invoke(this);
            }
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            Hovered?.Invoke(this, evt.position);
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            Unhovered?.Invoke();
        }
    }
}
