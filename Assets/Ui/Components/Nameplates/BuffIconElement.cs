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
