using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.Nameplates
{
    public sealed class BuffIconElement : VisualElement
    {
        private readonly VisualElement iconImage;
        private readonly Label timerLabel;
        private readonly Label stackLabel;

        public BuffIconElement()
        {
            AddToClassList("buff-icon");
            pickingMode = PickingMode.Ignore;
            style.width = UnitNameplateMetrics.BuffSize;
            style.height = UnitNameplateMetrics.BuffSize;
            style.minWidth = UnitNameplateMetrics.BuffSize;
            style.maxWidth = UnitNameplateMetrics.BuffSize;
            style.minHeight = UnitNameplateMetrics.BuffSize;
            style.flexShrink = 0;
            style.overflow = Overflow.Hidden;

            iconImage = new VisualElement { name = "buff-icon-image" };
            iconImage.AddToClassList("buff-icon__image");
            iconImage.pickingMode = PickingMode.Ignore;
            Add(iconImage);

            timerLabel = new Label { name = "buff-timer" };
            timerLabel.AddToClassList("buff-icon__timer");
            timerLabel.pickingMode = PickingMode.Ignore;
            timerLabel.style.marginTop = 0;
            timerLabel.style.marginRight = 0;
            timerLabel.style.marginBottom = 0;
            timerLabel.style.marginLeft = 0;
            timerLabel.style.paddingTop = 0;
            timerLabel.style.paddingRight = 0;
            timerLabel.style.paddingBottom = 0;
            timerLabel.style.paddingLeft = 0;
            timerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(timerLabel);

            stackLabel = new Label { name = "buff-stack" };
            stackLabel.AddToClassList("buff-icon__stack");
            stackLabel.pickingMode = PickingMode.Ignore;
            Add(stackLabel);
        }

        public void SetData(UiBuffData data)
        {
            if (data == null)
                return;

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
    }
}
