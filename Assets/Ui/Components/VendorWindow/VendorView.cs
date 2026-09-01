using System;
using System.Collections.Generic;
using ShadowInfection.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vland.UI
{
    public sealed class VendorView
    {
        public const int RowsPerPage = 7;
        private const float TooltipDelayMs = 150f;
        private const float TooltipGap = 8f;
        private const float TooltipWidth = 240f;

        private readonly VisualElement host;
        private readonly VisualElement panel;
        private readonly VisualElement titleBar;
        private readonly VisualElement portrait;
        private readonly VisualElement rowList;
        private readonly VisualElement pageControl;
        private readonly VisualElement tooltip;
        private readonly VisualElement tooltipSeparator;
        private readonly Label vendorName;
        private readonly Label vendorSubtitle;
        private readonly Label hint;
        private readonly Label emptyLabel;
        private readonly Label goldValue;
        private readonly Label pageLabel;
        private readonly Label tooltipName;
        private readonly Label tooltipType;
        private readonly Label tooltipStats;
        private readonly Label tooltipFlavour;
        private readonly Label tooltipPermanent;
        private readonly Label tooltipAction;
        private readonly Label tooltipPrice;
        private readonly Label buybackLabel;
        private readonly Button closeButton;
        private readonly Button tabBuy;
        private readonly Button tabSell;
        private readonly Button tabUpgrades;
        private readonly Button pagePrev;
        private readonly Button pageNext;
        private readonly Button buybackButton;

        private VendorRowVm pendingTooltip;
        private int tooltipToken;
        private bool isOpen;
        private UiDraggablePanel draggable;

        public event Action CloseClicked;
        public event Action PositionChanged;
        public event Action<VendorTab> TabClicked;
        public event Action PagePrevClicked;
        public event Action PageNextClicked;
        public event Action<string> RowSelected;
        public event Action<string> RowTransactRequested;

        public bool IsOpen => isOpen;
        public VisualElement Panel => panel;

        public VendorView(VisualElement root)
        {
            host = root.Q<VisualElement>("vendorHost");
            panel = root.Q<VisualElement>("VendorPanel");
            titleBar = root.Q<VisualElement>("titleBar");
            portrait = root.Q<VisualElement>("vendorPortrait");
            rowList = root.Q<VisualElement>("rowList");
            pageControl = root.Q<VisualElement>("pageControl");
            tooltip = root.Q<VisualElement>("vendorTooltip");
            tooltipSeparator = root.Q<VisualElement>("tooltipSeparator");
            vendorName = root.Q<Label>("vendorName");
            vendorSubtitle = root.Q<Label>("vendorSubtitle");
            hint = root.Q<Label>("hintLine");
            emptyLabel = root.Q<Label>("emptyLabel");
            goldValue = root.Q<Label>("vendorGoldValue");
            pageLabel = root.Q<Label>("pageLabel");
            tooltipName = root.Q<Label>("tooltipName");
            tooltipType = root.Q<Label>("tooltipType");
            tooltipStats = root.Q<Label>("tooltipStats");
            tooltipFlavour = root.Q<Label>("tooltipFlavour");
            tooltipPermanent = root.Q<Label>("tooltipPermanent");
            tooltipAction = root.Q<Label>("tooltipAction");
            tooltipPrice = root.Q<Label>("tooltipPrice");
            buybackLabel = root.Q<Label>("buybackLabel");
            closeButton = root.Q<OrnateButton>("closeButton") ?? root.Q<Button>("closeButton");
            tabBuy = root.Q<Button>("tabBuy");
            tabSell = root.Q<Button>("tabSell");
            tabUpgrades = root.Q<Button>("tabUpgrades");
            pagePrev = root.Q<Button>("pagePrev");
            pageNext = root.Q<Button>("pageNext");
            buybackButton = root.Q<Button>("buybackButton");

            if (host == null || panel == null)
            {
                Debug.LogError("VendorView: host or panel was not found.");
                return;
            }

            host.pickingMode = PickingMode.Ignore;
            panel.pickingMode = PickingMode.Position;
            UiGameplayInputGuard.Apply(panel);
            UiPointerState.RegisterBlockingElement(panel);
            if (tooltip != null)
                UiPointerState.RegisterBlockingElement(tooltip);

            panel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            if (closeButton != null)
                closeButton.clicked += () => CloseClicked?.Invoke();

            WireTab(tabBuy, VendorTab.Buy);
            WireTab(tabSell, VendorTab.Sell);
            WireTab(tabUpgrades, VendorTab.Upgrades);

            WirePageButton(pagePrev, () => PagePrevClicked?.Invoke());
            WirePageButton(pageNext, () => PageNextClicked?.Invoke());

            if (buybackButton != null)
            {
                buybackButton.SetEnabled(false);
                buybackButton.focusable = false;
            }

            if (tooltip != null)
                tooltip.pickingMode = PickingMode.Position;

            draggable = new UiDraggablePanel(host, panel, titleBar);
            draggable.PositionChanged += () => PositionChanged?.Invoke();

            if (rowList != null)
            {
                rowList.RegisterCallback<WheelEvent>(evt =>
                {
                    if (evt.delta.y > 0)
                        PageNextClicked?.Invoke();
                    else if (evt.delta.y < 0)
                        PagePrevClicked?.Invoke();
                    evt.StopPropagation();
                });
            }

            HideTooltip();
            SetOpen(false);
        }

        public void Dispose()
        {
            UiPointerState.UnregisterBlockingElement(panel);
            UiPointerState.UnregisterBlockingElement(tooltip);
        }

        public void SetOpen(bool open)
        {
            isOpen = open;
            draggable?.SetOpen(open);
            if (host != null)
                host.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (!open)
                HideTooltip();
        }

        public void SetVendor(string name, string subtitle, Texture2D portraitTexture)
        {
            if (vendorName != null)
                vendorName.text = name ?? string.Empty;
            if (vendorSubtitle != null)
            {
                vendorSubtitle.text = subtitle ?? string.Empty;
                vendorSubtitle.style.display = string.IsNullOrEmpty(subtitle) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (portrait == null)
                return;

            portrait.style.backgroundImage = portraitTexture != null
                ? new StyleBackground(portraitTexture)
                : StyleKeyword.None;
            portrait.EnableInClassList("vendor-portrait--fallback", portraitTexture == null);
        }

        public void SetActiveTab(VendorTab tab)
        {
            SetTabActive(tabBuy, tab == VendorTab.Buy);
            SetTabActive(tabSell, tab == VendorTab.Sell);
            SetTabActive(tabUpgrades, tab == VendorTab.Upgrades);
        }

        public void SetTabVisibility(bool buy, bool sell, bool upgrades)
        {
            SetDisplayed(tabBuy, buy);
            SetDisplayed(tabSell, sell);
            SetDisplayed(tabUpgrades, upgrades);
            tabBuy?.EnableInClassList("vendor-tab--last", buy && !sell && !upgrades);
            tabSell?.EnableInClassList("vendor-tab--last", sell && !upgrades);
            tabUpgrades?.EnableInClassList("vendor-tab--last", upgrades);
        }

        public void SetHint(string text)
        {
            if (hint != null)
                hint.text = text ?? string.Empty;
        }

        public void SetGold(int gold)
        {
            if (goldValue != null)
                goldValue.text = gold.ToString();
        }

        public void SetPage(int pageIndex, int pageCount, bool show)
        {
            if (pageControl != null)
                pageControl.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (buybackButton != null)
                buybackButton.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;

            var safeCount = Mathf.Max(1, pageCount);
            var safePage = Mathf.Clamp(pageIndex, 0, safeCount - 1);
            if (pageLabel != null)
                pageLabel.text = $"PAGE {safePage + 1} / {safeCount}";
            if (pagePrev != null)
                pagePrev.SetEnabled(safePage > 0);
            if (pageNext != null)
                pageNext.SetEnabled(safePage < safeCount - 1);
        }

        public void SetBuybackVisible(bool visible)
        {
            if (buybackButton != null)
                buybackButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (pageControl != null && visible)
                pageControl.style.display = DisplayStyle.None;
            if (buybackLabel != null)
                buybackLabel.text = "BUYBACK (0)";
        }

        public void SetRows(IReadOnlyList<VendorRowVm> rows, string selectedId, string emptyMessage)
        {
            if (rowList == null)
                return;

            HideTooltip();
            rowList.Clear();

            if (rows == null || rows.Count == 0)
            {
                if (emptyLabel != null)
                {
                    emptyLabel.text = emptyMessage ?? "Nothing here.";
                    emptyLabel.style.display = DisplayStyle.Flex;
                    rowList.Add(emptyLabel);
                }
                return;
            }

            if (emptyLabel != null)
                emptyLabel.style.display = DisplayStyle.None;

            for (var i = 0; i < rows.Count; i++)
            {
                var row = new VendorRow(rows[i]);
                if (i == rows.Count - 1)
                    row.AddToClassList("vendor-row--last");
                row.SelectedState = !string.IsNullOrEmpty(selectedId) && row.Id == selectedId;
                row.Selected += clicked => RowSelected?.Invoke(clicked.Id);
                row.TransactRequested += clicked => RowTransactRequested?.Invoke(clicked.Id);
                row.Hovered += (hovered, position) => ScheduleTooltip(hovered.Model, position);
                row.Unhovered += HideTooltip;
                rowList.Add(row);
            }
        }

        public void ApplyPosition(float left, float top) => draggable?.ApplyPosition(left, top);

        public void ApplyDefaultPosition() => draggable?.ApplyDefaultPosition();

        public Vector2 GetPosition() => draggable != null ? draggable.GetPosition() : Vector2.zero;

        public bool HasUsableLayout() => draggable != null && draggable.HasUsableLayout();

        public void ClampToViewport() => draggable?.ClampToViewport();

        private void WireTab(Button button, VendorTab tab)
        {
            if (button == null)
                return;

            button.focusable = false;
            button.clicked += () => TabClicked?.Invoke(tab);
        }

        private static void WirePageButton(Button button, Action clicked)
        {
            if (button == null)
                return;

            button.focusable = false;
            button.clicked += () => clicked?.Invoke();
        }

        private static void SetTabActive(Button button, bool active)
        {
            button?.EnableInClassList("vendor-tab--active", active);
        }

        private static void SetDisplayed(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ScheduleTooltip(VendorRowVm model, Vector2 pointerPosition)
        {
            pendingTooltip = model;
            var token = ++tooltipToken;
            panel.schedule.Execute(() =>
            {
                if (token != tooltipToken)
                    return;
                ShowPendingTooltip();
            }).StartingIn((long)TooltipDelayMs);
            _ = pointerPosition;
        }

        private void ShowPendingTooltip()
        {
            if (pendingTooltip == null || tooltip == null || panel == null)
                return;

            var model = pendingTooltip;
            if (tooltipName != null)
                tooltipName.text = model.Name ?? string.Empty;
            SetOptionalLabel(tooltipType, model.TypeLine);
            SetOptionalLabel(tooltipStats, model.StatBlock);
            SetOptionalLabel(tooltipFlavour, model.Flavour);

            if (tooltipPermanent != null)
            {
                var showPermanent = model.Tab == VendorTab.Upgrades;
                tooltipPermanent.style.display = showPermanent ? DisplayStyle.Flex : DisplayStyle.None;
                if (showPermanent)
                    tooltipPermanent.text = "Permanent. Cannot be sold back.";
            }

            if (tooltipSeparator != null)
                tooltipSeparator.style.display = DisplayStyle.Flex;
            if (tooltipAction != null)
                tooltipAction.text = model.TooltipAction ?? "Right-click to buy";
            if (tooltipPrice != null)
                tooltipPrice.text = model.PriceGold.ToString();

            tooltip.style.display = DisplayStyle.Flex;
            tooltip.schedule.Execute(PositionTooltip);
        }

        private void PositionTooltip()
        {
            if (tooltip == null || panel == null || host == null)
                return;

            var panelLeft = panel.resolvedStyle.left;
            var panelTop = panel.resolvedStyle.top;
            var panelWidth = panel.resolvedStyle.width;
            var tooltipWidth = tooltip.resolvedStyle.width > 0f ? tooltip.resolvedStyle.width : TooltipWidth;
            var tooltipHeight = tooltip.resolvedStyle.height;
            var hostWidth = host.resolvedStyle.width;
            var hostHeight = host.resolvedStyle.height;

            var placeLeft = panelLeft + panelWidth * 0.5f > hostWidth * 0.5f;
            float left;
            if (placeLeft)
                left = panelLeft - tooltipWidth - TooltipGap;
            else
                left = panelLeft + panelWidth + TooltipGap;

            left = Mathf.Clamp(left, 0f, Mathf.Max(0f, hostWidth - tooltipWidth));
            var top = Mathf.Clamp(panelTop, 0f, Mathf.Max(0f, hostHeight - tooltipHeight));
            tooltip.style.left = left;
            tooltip.style.top = top;
        }

        private void HideTooltip()
        {
            tooltipToken++;
            pendingTooltip = null;
            if (tooltip != null)
                tooltip.style.display = DisplayStyle.None;
        }

        private static void SetOptionalLabel(Label label, string text)
        {
            if (label == null)
                return;

            var hasText = !string.IsNullOrEmpty(text);
            label.text = text ?? string.Empty;
            label.style.display = hasText ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
