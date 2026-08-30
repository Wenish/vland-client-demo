using System.Collections.Generic;
using Cysharp.Text;
using ShadowInfection.UI.Nameplates;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.PlayerHud
{
    internal sealed class TargetFrameView
    {
        private const int MaxIconsPerRow = 8;

        private readonly VisualElement root;
        private readonly Label nameLabel;
        private readonly VisualElement shieldContainer;
        private readonly VisualElement shieldFill;
        private readonly Label shieldValue;
        private readonly VisualElement healthTrack;
        private readonly VisualElement healthFill;
        private readonly Label healthValue;
        private readonly VisualElement buffRow;
        private readonly VisualElement debuffRow;
        private readonly CastBar castBar;
        private readonly List<BuffIconElement> buffIcons = new();
        private readonly List<BuffIconElement> debuffIcons = new();
        private readonly Queue<BuffIconElement> iconPool = new();

        public TargetFrameView(VisualElement hudRoot)
        {
            root = hudRoot.Q<VisualElement>("targetFrame");
            nameLabel = hudRoot.Q<Label>("targetFrameName");
            shieldContainer = hudRoot.Q<VisualElement>("targetFrameShieldContainer");
            shieldFill = hudRoot.Q<VisualElement>("targetFrameShieldFill");
            shieldValue = hudRoot.Q<Label>("targetFrameShieldValue");
            healthTrack = hudRoot.Q<VisualElement>("targetFrameHealthTrack");
            healthFill = hudRoot.Q<VisualElement>("targetFrameHealthFill");
            healthValue = hudRoot.Q<Label>("targetFrameHealthValue");
            buffRow = hudRoot.Q<VisualElement>("targetFrameBuffRow");
            debuffRow = hudRoot.Q<VisualElement>("targetFrameDebuffRow");
            castBar = hudRoot.Q<CastBar>("targetFrameCastbar");

            if (root != null)
                root.pickingMode = PickingMode.Ignore;

            Hide();
        }

        public void Show()
        {
            if (root != null)
                root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (root != null)
                root.style.display = DisplayStyle.None;

            HideCastBar();
        }

        public void SetName(string name)
        {
            if (nameLabel != null)
                nameLabel.text = name ?? string.Empty;
        }

        public void SetHealth(int current, int max, Color fillColor)
        {
            if (healthValue != null)
                healthValue.text = ZString.Format("{0} / {1}", Mathf.Max(0, current), Mathf.Max(0, max));

            SetBarFill(healthFill, current, max);
            if (healthFill != null)
                healthFill.style.backgroundColor = fillColor;

            if (healthTrack != null)
            {
                healthTrack.style.borderTopColor = fillColor;
                healthTrack.style.borderRightColor = fillColor;
                healthTrack.style.borderBottomColor = fillColor;
                healthTrack.style.borderLeftColor = fillColor;
            }
        }

        public void SetShield(int current, int max)
        {
            if (shieldContainer != null)
                shieldContainer.style.display = max > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            if (max <= 0)
                return;

            if (shieldValue != null)
                shieldValue.text = ZString.Format("{0} / {1}", Mathf.Max(0, current), Mathf.Max(0, max));

            SetBarFill(shieldFill, current, max);
        }

        public void SetBuffs(IReadOnlyList<UiBuffData> buffs, IReadOnlyList<UiBuffData> debuffs)
        {
            ApplyRow(buffRow, buffIcons, buffs, isDebuff: false);
            ApplyRow(debuffRow, debuffIcons, debuffs, isDebuff: true);
        }

        public void ShowCastBar()
        {
            if (castBar != null)
                castBar.style.display = DisplayStyle.Flex;
        }

        public void HideCastBar()
        {
            if (castBar != null)
                castBar.style.display = DisplayStyle.None;
        }

        public void SetCastBarOpacity(float opacity)
        {
            if (castBar != null)
                castBar.style.opacity = opacity;
        }

        public void SetCastBarProgress(float progress)
        {
            if (castBar != null)
                castBar.Progress = progress;
        }

        public void SetCastBarTime(string text)
        {
            if (castBar != null)
                castBar.TextTime = text ?? string.Empty;
        }

        public void SetCastBarName(string text)
        {
            if (castBar != null)
                castBar.TextName = text ?? string.Empty;
        }

        public void SetCastBarIcon(Texture2D icon)
        {
            if (castBar != null)
                castBar.IconTexture = icon;
        }

        public void SetCastBarFeedback(Color color, bool visible)
        {
            if (castBar == null)
                return;

            castBar.SetFeedbackColor(color);
            castBar.ShowFeedback(visible);
        }

        private void ApplyRow(
            VisualElement row,
            List<BuffIconElement> icons,
            IReadOnlyList<UiBuffData> data,
            bool isDebuff)
        {
            if (row == null)
                return;

            var count = data != null ? Mathf.Min(data.Count, MaxIconsPerRow) : 0;
            while (icons.Count > count)
            {
                var index = icons.Count - 1;
                var icon = icons[index];
                icons.RemoveAt(index);
                row.Remove(icon);
                iconPool.Enqueue(icon);
            }

            for (var i = 0; i < count; i++)
            {
                BuffIconElement icon;
                if (i < icons.Count)
                {
                    icon = icons[i];
                }
                else
                {
                    icon = iconPool.Count > 0 ? iconPool.Dequeue() : new BuffIconElement();
                    icons.Add(icon);
                    row.Add(icon);
                }

                icon.EnableInClassList("buff-icon--debuff", isDebuff);
                icon.SetData(data[i], interactiveTooltip: true);
            }

            row.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetBarFill(VisualElement fillElement, int current, int max)
        {
            if (fillElement == null)
                return;

            var percent = max <= 0 ? 0f : Mathf.Clamp01((float)current / max) * 100f;
            fillElement.style.width = Length.Percent(percent);
        }
    }
}
