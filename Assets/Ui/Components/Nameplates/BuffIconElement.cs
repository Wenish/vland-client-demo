using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.Nameplates
{
    public sealed class BuffIconElement : VisualElement
    {
        private readonly VisualElement iconImage;
        private readonly Label timerLabel;
        private readonly Label stackLabel;
        private Label runtimeTooltip;
        private string tooltipText = string.Empty;
        private bool interactive;

        public BuffIconElement()
        {
            AddToClassList("buff-icon");
            pickingMode = PickingMode.Ignore;

            iconImage = new VisualElement { name = "buff-icon-image" };
            iconImage.AddToClassList("buff-icon__image");
            iconImage.pickingMode = PickingMode.Ignore;
            Add(iconImage);

            timerLabel = new Label { name = "buff-timer" };
            timerLabel.AddToClassList("buff-icon__timer");
            timerLabel.pickingMode = PickingMode.Ignore;
            Add(timerLabel);

            stackLabel = new Label { name = "buff-stack" };
            stackLabel.AddToClassList("buff-icon__stack");
            stackLabel.pickingMode = PickingMode.Ignore;
            Add(stackLabel);

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        public void SetData(UiBuffData data)
        {
            SetData(data, interactiveTooltip: false);
        }

        public void SetData(UiBuffData data, bool interactiveTooltip)
        {
            if (data == null)
                return;

            interactive = interactiveTooltip;
            pickingMode = interactive ? PickingMode.Position : PickingMode.Ignore;
            tooltipText = interactive ? FormatTooltip(data) : string.Empty;

            if (data.IconTexture != null)
                iconImage.style.backgroundImage = new StyleBackground(data.IconTexture);
            else
                iconImage.style.backgroundImage = StyleKeyword.None;

            if (data.Duration < Mathf.Infinity)
            {
                timerLabel.text = Mathf.CeilToInt(data.TimeRemaining).ToString();
                timerLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                timerLabel.style.display = DisplayStyle.None;
            }

            if (data.StackCount > 1)
            {
                stackLabel.text = data.StackCount.ToString();
                stackLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                stackLabel.style.display = DisplayStyle.None;
            }
        }

        private static string FormatTooltip(UiBuffData data)
        {
            var name = !string.IsNullOrWhiteSpace(data.DisplayName) ? data.DisplayName : data.BuffId;
            if (data.Duration >= Mathf.Infinity)
                return name ?? string.Empty;

            return $"{name}\n{Mathf.CeilToInt(data.TimeRemaining)}s";
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            if (!interactive || string.IsNullOrEmpty(tooltipText) || panel == null)
                return;

            runtimeTooltip = new Label
            {
                name = "buff-runtime-tooltip",
                text = tooltipText,
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    whiteSpace = WhiteSpace.Normal,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 6,
                    paddingBottom = 6,
                    backgroundColor = new Color(0.008f, 0.075f, 0.204f, 0.95f),
                    color = new Color(1f, 0.957f, 0.906f, 1f),
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = new Color(0.792f, 0.624f, 0.424f, 0.85f),
                    borderBottomColor = new Color(0.792f, 0.624f, 0.424f, 0.85f),
                    borderLeftColor = new Color(0.792f, 0.624f, 0.424f, 0.85f),
                    borderRightColor = new Color(0.792f, 0.624f, 0.424f, 0.85f)
                }
            };
            panel.visualTree.Add(runtimeTooltip);
            runtimeTooltip.RegisterCallback<GeometryChangedEvent>(OnTooltipGeometryChanged);
        }

        private void OnTooltipGeometryChanged(GeometryChangedEvent evt)
        {
            if (runtimeTooltip == null)
                return;

            runtimeTooltip.UnregisterCallback<GeometryChangedEvent>(OnTooltipGeometryChanged);
            var world = this.worldBound;
            runtimeTooltip.style.left = world.x;
            runtimeTooltip.style.top = world.y - runtimeTooltip.layout.height - 4f;
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (runtimeTooltip == null)
                return;

            runtimeTooltip.RemoveFromHierarchy();
            runtimeTooltip = null;
        }
    }
}
