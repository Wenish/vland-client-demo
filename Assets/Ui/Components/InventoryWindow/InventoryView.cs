using System;
using System.Collections.Generic;
using ShadowInfection.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Vland.UI;

namespace ShadowInfection.UI.InventoryWindow
{
    internal sealed class InventoryView
    {
        private static readonly InventoryFilter[] Filters =
        {
            InventoryFilter.All,
            InventoryFilter.Head,
            InventoryFilter.Shoulder,
            InventoryFilter.Cape,
            InventoryFilter.Chest,
            InventoryFilter.Pants,
            InventoryFilter.Feet,
            InventoryFilter.Gloves,
            InventoryFilter.Weapon,
            InventoryFilter.Gem,
            InventoryFilter.Material
        };

        private readonly VisualElement host;
        private readonly VisualElement panel;
        private readonly VisualElement headerRow;
        private readonly VisualElement filterBar;
        private readonly VisualElement itemList;
        private readonly VisualElement detailIcon;
        private readonly VisualElement confirmHost;
        private readonly Label detailName;
        private readonly Label detailMeta;
        private readonly Label detailDescription;
        private readonly Label detailStats;
        private readonly Label detailNotice;
        private readonly Label detailEmpty;
        private readonly Label confirmMessage;
        private readonly Button closeButton;
        private readonly Button equipButton;
        private readonly Button destroyButton;
        private readonly Button confirmYes;
        private readonly Button confirmNo;
        private readonly TextField searchField;
        private readonly Dictionary<InventoryFilter, Button> filterButtons = new();
        private readonly UiDraggablePanel draggable;

        private bool isOpen;
        private bool searchInputActive;
        private bool modalInputPushed;

        public event Action CloseClicked;
        public event Action<string> RowClicked;
        public event Action<string> RowQuickEquip;
        public event Action EquipClicked;
        public event Action DestroyClicked;
        public event Action ConfirmDestroyClicked;
        public event Action CancelDestroyClicked;
        public event Action<InventoryFilter> FilterClicked;
        public event Action<string> SearchChanged;
        public event Action PositionChanged;

        public bool IsOpen => isOpen;
        public bool IsSearchFocused => searchInputActive;
        public UiDraggablePanel Draggable => draggable;

        public InventoryView(VisualElement root)
        {
            host = root.Q<VisualElement>("inventoryHost");
            panel = root.Q<VisualElement>("InventoryPanel");
            headerRow = root.Q<VisualElement>("headerRow");
            filterBar = root.Q<VisualElement>("filterBar");
            itemList = root.Q<VisualElement>("itemList");
            detailIcon = root.Q<VisualElement>("detailIcon");
            confirmHost = root.Q<VisualElement>("confirmHost");
            detailName = root.Q<Label>("detailName");
            detailMeta = root.Q<Label>("detailMeta");
            detailDescription = root.Q<Label>("detailDescription");
            detailStats = root.Q<Label>("detailStats");
            detailNotice = root.Q<Label>("detailNotice");
            detailEmpty = root.Q<Label>("detailEmpty");
            confirmMessage = root.Q<Label>("confirmMessage");
            closeButton = root.Q<OrnateButton>("closeButton") ?? root.Q<Button>("closeButton");
            equipButton = root.Q<OrnateButton>("equipButton") ?? root.Q<Button>("equipButton");
            destroyButton = root.Q<OrnateButton>("destroyButton") ?? root.Q<Button>("destroyButton");
            confirmYes = root.Q<OrnateButton>("confirmYes") ?? root.Q<Button>("confirmYes");
            confirmNo = root.Q<OrnateButton>("confirmNo") ?? root.Q<Button>("confirmNo");
            searchField = root.Q<TextField>("searchField");

            if (host == null || panel == null)
            {
                UnityEngine.Debug.LogError("InventoryView: host or panel was not found in the inventory UI.");
                return;
            }

            host.pickingMode = PickingMode.Ignore;
            panel.pickingMode = PickingMode.Position;
            UiGameplayInputGuard.Apply(panel);
            UiPointerState.RegisterBlockingElement(panel);
            panel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            draggable = new UiDraggablePanel(host, panel, headerRow, ComputeDefaultPosition);
            draggable.PositionChanged += () => PositionChanged?.Invoke();

            if (closeButton != null)
                closeButton.clicked += () => CloseClicked?.Invoke();
            if (equipButton != null)
                equipButton.clicked += () => EquipClicked?.Invoke();
            if (destroyButton != null)
                destroyButton.clicked += () => DestroyClicked?.Invoke();
            if (confirmYes != null)
                confirmYes.clicked += () => ConfirmDestroyClicked?.Invoke();
            if (confirmNo != null)
                confirmNo.clicked += () => CancelDestroyClicked?.Invoke();
            if (searchField != null)
            {
                searchField.label = string.Empty;
                searchField.focusable = false;
                searchField.RegisterValueChangedCallback(evt => SearchChanged?.Invoke(evt.newValue));
                searchField.RegisterCallback<PointerDownEvent>(OnSearchPointerDown, TrickleDown.TrickleDown);
                searchField.RegisterCallback<FocusInEvent>(_ => CaptureSearchInput());
                searchField.RegisterCallback<FocusOutEvent>(OnSearchFocusOut);
                searchField.RegisterCallback<DetachFromPanelEvent>(_ => ReleaseSearchInput());
                searchField.RegisterCallback<KeyDownEvent>(OnSearchKeyDown, TrickleDown.TrickleDown);
            }

            BuildFilters();
            ClearDetail();
            SetConfirmVisible(false);
            SetOpen(false);
        }

        public void Dispose()
        {
            ReleaseModalInputBlock();
            ReleaseSearchInput();
            UiPointerState.UnregisterBlockingElement(panel);
        }

        public void SetOpen(bool open)
        {
            isOpen = open;
            draggable?.SetOpen(open);
            if (host != null)
                host.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open)
                PrepareSearchForOpen();
            else
            {
                searchField?.Blur();
                ReleaseSearchInput();
                SetConfirmVisible(false);
            }

            RefreshModalInputBlock();
        }

        public void ApplyPosition(float left, float top) => draggable?.ApplyPosition(left, top);

        public void ApplyDefaultPosition() => draggable?.ApplyDefaultPosition();

        public Vector2 GetPosition() => draggable != null ? draggable.GetPosition() : Vector2.zero;

        public bool HasUsableLayout() => draggable != null && draggable.HasUsableLayout();

        public void ClampToViewport() => draggable?.ClampToViewport();

        public void SetFilter(InventoryFilter filter)
        {
            foreach (var pair in filterButtons)
                pair.Value.EnableInClassList("inventory-filter--active", pair.Key == filter);
        }

        public void SetSearch(string text)
        {
            if (searchField != null && searchField.value != (text ?? string.Empty))
                searchField.SetValueWithoutNotify(text ?? string.Empty);
        }

        public void SetRows(IReadOnlyList<InventoryRowVm> rows, string selectedId, string emptyMessage)
        {
            if (itemList == null)
                return;

            itemList.Clear();
            if (rows == null || rows.Count == 0)
            {
                var empty = new Label(emptyMessage ?? "Your bag is empty.") { pickingMode = PickingMode.Ignore };
                empty.AddToClassList("inventory-empty");
                itemList.Add(empty);
                return;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var row = new InventoryRow(rows[i]);
                if (i == rows.Count - 1)
                    row.AddToClassList("inventory-row--last");
                row.Selected = row.Id == selectedId;
                row.Clicked += HandleRowClicked;
                row.QuickEquipRequested += HandleRowQuickEquip;
                itemList.Add(row);
            }
        }

        public void SetDetail(InventoryRowVm row, string notice, bool canEquip, bool canDestroy)
        {
            var has = row != null;
            if (detailEmpty != null)
                detailEmpty.style.display = has ? DisplayStyle.None : DisplayStyle.Flex;
            if (detailName != null)
            {
                detailName.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
                detailName.text = has ? row.Name : string.Empty;
            }

            if (detailMeta != null)
            {
                detailMeta.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
                detailMeta.text = has ? row.Meta : string.Empty;
            }

            if (detailDescription != null)
            {
                var description = has ? row.Description : string.Empty;
                detailDescription.style.display = has && !string.IsNullOrWhiteSpace(description)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                detailDescription.text = description ?? string.Empty;
            }

            if (detailStats != null)
            {
                var stats = has ? row.Summary : string.Empty;
                detailStats.style.display = has && !string.IsNullOrEmpty(stats) ? DisplayStyle.Flex : DisplayStyle.None;
                detailStats.text = stats ?? string.Empty;
            }

            if (detailNotice != null)
            {
                detailNotice.style.display = has && !string.IsNullOrWhiteSpace(notice)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                detailNotice.text = notice ?? string.Empty;
            }

            if (detailIcon != null)
            {
                detailIcon.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
                detailIcon.ClearClassList();
                detailIcon.AddToClassList("inventory-detail__icon");
                if (has && !string.IsNullOrEmpty(row.RarityClass))
                    detailIcon.AddToClassList(row.RarityClass);
                detailIcon.style.backgroundImage = has && row.Icon != null
                    ? new StyleBackground(row.Icon)
                    : StyleKeyword.None;
            }

            if (equipButton != null)
            {
                equipButton.style.display = has && canEquip ? DisplayStyle.Flex : DisplayStyle.None;
                equipButton.SetEnabled(canEquip);
            }

            if (destroyButton != null)
            {
                destroyButton.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
                destroyButton.SetEnabled(has && canDestroy);
            }
        }

        public void ClearDetail()
        {
            SetDetail(null, null, false, false);
        }

        public void SetConfirmVisible(bool visible, string message = null)
        {
            if (confirmHost != null)
                confirmHost.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (confirmMessage != null && message != null)
                confirmMessage.text = message;
        }

        private (float left, float top) ComputeDefaultPosition(bool _)
        {
            if (draggable == null || !draggable.TryGetLayoutSize(out var hostWidth, out var hostHeight, out var width, out var height))
                return (96f, 120f);

            var left = Mathf.Clamp(hostWidth * 0.52f, 360f, hostWidth - width - 24f);
            var top = (hostHeight - height) * 0.5f;
            return (left, top);
        }

        private void BuildFilters()
        {
            if (filterBar == null)
                return;

            filterBar.Clear();
            filterButtons.Clear();
            foreach (var filter in Filters)
            {
                var button = new Button { text = FilterLabel(filter) };
                button.AddToClassList("inventory-filter");
                var captured = filter;
                button.clicked += () => FilterClicked?.Invoke(captured);
                filterBar.Add(button);
                filterButtons[filter] = button;
            }
        }

        private void HandleRowClicked(InventoryRow row)
        {
            if (row != null)
                RowClicked?.Invoke(row.Id);
        }

        private void HandleRowQuickEquip(InventoryRow row)
        {
            if (row != null)
                RowQuickEquip?.Invoke(row.Id);
        }

        private void OnSearchPointerDown(PointerDownEvent evt)
        {
            if (searchField == null)
                return;

            searchField.focusable = true;
            searchField.Focus();
            evt.StopPropagation();
        }

        private void OnSearchFocusOut(FocusOutEvent _)
        {
            ReleaseSearchInput();
            if (searchField != null)
                searchField.focusable = false;
        }

        private void OnSearchKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
                return;

            searchField?.Blur();
            evt.StopImmediatePropagation();
        }

        private void PrepareSearchForOpen()
        {
            if (searchField == null)
                return;

            searchField.focusable = false;
            searchField.Blur();
            host?.schedule.Execute(() =>
            {
                searchField.focusable = false;
                searchField.Blur();
            });
        }

        private void RefreshModalInputBlock()
        {
            var shouldBlock = isOpen;
            if (shouldBlock == modalInputPushed)
                return;

            if (shouldBlock)
            {
                PlayerInput.CancelLocalGameplayInput();
                UiModalInputBlock.Push();
            }
            else
            {
                UiModalInputBlock.Pop();
            }

            modalInputPushed = shouldBlock;
        }

        private void ReleaseModalInputBlock()
        {
            if (!modalInputPushed)
                return;

            UiModalInputBlock.Pop();
            modalInputPushed = false;
        }

        private void CaptureSearchInput()
        {
            if (searchInputActive)
                return;

            searchInputActive = true;
            UiTextInputFocus.Push();
        }

        private void ReleaseSearchInput()
        {
            if (!searchInputActive)
                return;

            searchInputActive = false;
            UiTextInputFocus.Pop();
        }

        private static string FilterLabel(InventoryFilter filter)
        {
            switch (filter)
            {
                case InventoryFilter.All: return "All";
                case InventoryFilter.Head: return "Head";
                case InventoryFilter.Shoulder: return "Shoulder";
                case InventoryFilter.Cape: return "Cape";
                case InventoryFilter.Chest: return "Chest";
                case InventoryFilter.Pants: return "Pants";
                case InventoryFilter.Feet: return "Feet";
                case InventoryFilter.Gloves: return "Gloves";
                case InventoryFilter.Weapon: return "Weapon";
                case InventoryFilter.Gem: return "Gem";
                case InventoryFilter.Material: return "Material";
                default: return filter.ToString();
            }
        }
    }
}
