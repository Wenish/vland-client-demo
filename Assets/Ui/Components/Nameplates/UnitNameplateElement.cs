using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.Nameplates
{
    public sealed class UnitNameplateElement : VisualElement
    {
        private readonly VisualElement healthRow;
        private readonly VisualElement vitals;
        private readonly VisualElement shieldTrack;
        private readonly VisualElement shieldFill;
        private readonly VisualElement healthFill;
        private readonly Label nameLabel;
        private readonly VisualElement buffRow;
        private readonly VisualElement castBarRoot;
        private readonly VisualElement castIcon;
        private readonly VisualElement castFill;
        private readonly List<BuffIconElement> buffIcons = new();
        private readonly Queue<BuffIconElement> buffIconPool = new();
        private float cachedWidth = UnitNameplateMetrics.EstimatedPlateWidth;
        private float cachedHeight = UnitNameplateMetrics.EstimatedPlateHeight;
        private Vector2 lastPanelPosition;
        private bool isShown;
        private bool hasBeenPositioned;

        public UnitNameplateElement()
        {
            pickingMode = PickingMode.Ignore;
            usageHints = UsageHints.DynamicTransform;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.display = DisplayStyle.None;
            style.visibility = Visibility.Hidden;
            AddToClassList("unit-nameplate");
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            var column = CreateColumn();
            Add(column);

            buffRow = CreateBuffRow();
            column.Add(buffRow);

            vitals = CreateVitals();
            column.Add(vitals);

            shieldTrack = CreateBarTrack("unit-nameplate__bar-fill--shield", out shieldFill);
            shieldTrack.AddToClassList("unit-nameplate__bar-track--shield");
            vitals.Add(shieldTrack);

            healthRow = CreateHealthRow(out healthFill, out nameLabel);
            vitals.Add(healthRow);

            castBarRoot = CreateCastBar(out castIcon, out castFill);
            column.Add(castBarRoot);
        }

        public void HideAndReset()
        {
            Hide();
            transform.position = Vector3.zero;
        }

        public void Apply(in UnitNameplateSnapshot snapshot)
        {
            var shouldShow = snapshot.ShowRoot
                && (snapshot.ShowHealth
                    || snapshot.ShowShield
                    || snapshot.ShowCastBar
                    || snapshot.Buffs.Count > 0);

            if (!shouldShow)
            {
                Hide();
                return;
            }

            SetVisible(shieldTrack, snapshot.ShowShield);
            SetVisible(healthRow, snapshot.ShowHealth);
            SetVisible(nameLabel, snapshot.ShowName);
            SetVisible(vitals, snapshot.ShowHealth || snapshot.ShowShield);

            if (snapshot.ShowShield)
                SetFill(shieldFill, snapshot.ShieldFill);

            if (snapshot.ShowHealth)
            {
                SetFill(healthFill, snapshot.HealthFill);
                healthFill.style.backgroundColor = snapshot.HealthColor;
            }

            if (snapshot.ShowName)
                nameLabel.text = snapshot.UnitName ?? string.Empty;

            castBarRoot.style.visibility = snapshot.ShowCastBar
                ? Visibility.Visible
                : Visibility.Hidden;
            if (snapshot.ShowCastBar)
            {
                SetFill(castFill, snapshot.CastProgress);
                if (snapshot.CastIcon != null)
                    castIcon.style.backgroundImage = new StyleBackground(snapshot.CastIcon);
                else
                    castIcon.style.backgroundImage = StyleKeyword.None;
            }

            ApplyBuffs(snapshot.Buffs);

            var showingNow = !isShown;
            style.display = DisplayStyle.Flex;
            if (showingNow)
            {
                style.visibility = Visibility.Hidden;
                hasBeenPositioned = false;
            }

            isShown = true;
        }

        public void SetScreenPosition(Vector2 panelPosition)
        {
            lastPanelPosition = panelPosition;
            ApplyTransform(panelPosition);
            hasBeenPositioned = true;

            if (isShown)
                style.visibility = Visibility.Visible;
        }

        private void Hide()
        {
            style.display = DisplayStyle.None;
            style.visibility = Visibility.Hidden;
            isShown = false;
            hasBeenPositioned = false;
        }

        private void ApplyTransform(Vector2 panelPosition)
        {
            transform.position = new Vector3(
                Mathf.Round(panelPosition.x - cachedWidth * 0.5f),
                Mathf.Round(panelPosition.y - cachedHeight),
                0f);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.width <= 0f || evt.newRect.height <= 0f)
                return;

            if (!IsPlausibleLayout(evt.newRect))
                return;

            cachedWidth = evt.newRect.width;
            cachedHeight = evt.newRect.height;

            if (hasBeenPositioned)
                ApplyTransform(lastPanelPosition);
        }

        private static bool IsPlausibleLayout(Rect rect)
        {
            return rect.width <= UnitNameplateMetrics.MaxLayoutWidth
                && rect.height <= UnitNameplateMetrics.MaxLayoutHeight;
        }

        private static VisualElement CreateColumn()
        {
            var column = new VisualElement { name = "column" };
            column.AddToClassList("unit-nameplate__column");
            column.pickingMode = PickingMode.Ignore;
            return column;
        }

        private static VisualElement CreateBuffRow()
        {
            var row = new VisualElement { name = "buff-row" };
            row.AddToClassList("unit-nameplate__buff-row");
            row.pickingMode = PickingMode.Ignore;
            row.style.display = DisplayStyle.None;
            return row;
        }

        private static VisualElement CreateVitals()
        {
            var vitals = new VisualElement { name = "vitals" };
            vitals.AddToClassList("unit-nameplate__vitals");
            vitals.pickingMode = PickingMode.Ignore;
            return vitals;
        }

        private static VisualElement CreateBarTrack(string fillModifierClass, out VisualElement fill)
        {
            var track = new VisualElement();
            track.AddToClassList("unit-nameplate__bar-track");
            track.pickingMode = PickingMode.Ignore;

            fill = new VisualElement();
            fill.AddToClassList("unit-nameplate__bar-fill");
            if (!string.IsNullOrEmpty(fillModifierClass))
                fill.AddToClassList(fillModifierClass);
            fill.pickingMode = PickingMode.Ignore;

            track.Add(fill);
            return track;
        }

        private static VisualElement CreateHealthRow(
            out VisualElement healthFill,
            out Label nameLabel)
        {
            var row = new VisualElement { name = "health-row" };
            row.AddToClassList("unit-nameplate__health-row");
            row.pickingMode = PickingMode.Ignore;

            var healthTrack = CreateBarTrack(string.Empty, out healthFill);
            row.Add(healthTrack);

            AddTick(row, "unit-nameplate__tick--25");
            AddTick(row, "unit-nameplate__tick--50");
            AddTick(row, "unit-nameplate__tick--75");

            nameLabel = new Label { name = "name-label" };
            nameLabel.AddToClassList("unit-nameplate__name");
            nameLabel.pickingMode = PickingMode.Ignore;
            row.Add(nameLabel);

            return row;
        }

        private static void AddTick(VisualElement parent, string modifierClass)
        {
            var tick = new VisualElement();
            tick.AddToClassList("unit-nameplate__tick");
            tick.AddToClassList(modifierClass);
            tick.pickingMode = PickingMode.Ignore;
            parent.Add(tick);
        }

        private static VisualElement CreateCastBar(out VisualElement icon, out VisualElement fill)
        {
            var root = new VisualElement { name = "cast-bar" };
            root.AddToClassList("unit-nameplate__cast-bar");
            root.pickingMode = PickingMode.Ignore;
            root.style.visibility = Visibility.Hidden;

            icon = new VisualElement { name = "cast-icon" };
            icon.AddToClassList("unit-nameplate__cast-icon");
            icon.pickingMode = PickingMode.Ignore;
            root.Add(icon);

            var track = new VisualElement { name = "cast-track" };
            track.AddToClassList("unit-nameplate__cast-track");
            track.pickingMode = PickingMode.Ignore;
            root.Add(track);

            fill = new VisualElement { name = "cast-fill" };
            fill.AddToClassList("unit-nameplate__cast-fill");
            fill.pickingMode = PickingMode.Ignore;
            track.Add(fill);

            return root;
        }

        private void ApplyBuffs(IReadOnlyList<UiBuffData> buffs)
        {
            while (buffIcons.Count > buffs.Count)
            {
                var index = buffIcons.Count - 1;
                var icon = buffIcons[index];
                buffIcons.RemoveAt(index);
                buffRow.Remove(icon);
                buffIconPool.Enqueue(icon);
            }

            for (var i = 0; i < buffs.Count; i++)
            {
                BuffIconElement icon;
                if (i < buffIcons.Count)
                {
                    icon = buffIcons[i];
                }
                else
                {
                    icon = buffIconPool.Count > 0 ? buffIconPool.Dequeue() : new BuffIconElement();
                    buffIcons.Add(icon);
                    buffRow.Add(icon);
                }

                icon.SetData(buffs[i]);
            }

            buffRow.style.display = buffs.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetFill(VisualElement fill, float normalized)
        {
            fill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element == null)
                return;

            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
