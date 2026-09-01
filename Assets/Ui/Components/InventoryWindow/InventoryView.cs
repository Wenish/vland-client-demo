using System;
using System.Collections.Generic;
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

        private readonly VisualElement overlay;
        private readonly VisualElement panel;
        private readonly VisualElement filterBar;
        private readonly VisualElement itemList;
        private readonly VisualElement detailIcon;
        private readonly VisualElement confirmHost;
        private readonly Label detailName;
        private readonly Label detailMeta;
        private readonly Label detailDescription;
        private readonly Label detailStats;
        private readonly Label detailEmpty;
        private readonly Label confirmMessage;
        private readonly Button closeButton;
        private readonly Button destroyButton;
        private readonly Button confirmYes;
        private readonly Button confirmNo;
        private readonly TextField searchField;
        private readonly Dictionary<InventoryFilter, Button> filterButtons = new();

        private bool isOpen;
        private bool searchInputActive;
        private bool modalInputPushed;

        public event Action CloseClicked;
        public event Action OverlayClicked;
        public event Action<string> RowClicked;
        public event Action DestroyClicked;
        public event Action ConfirmDestroyClicked;
        public event Action CancelDestroyClicked;
        public event Action<InventoryFilter> FilterClicked;
        public event Action<string> SearchChanged;

        public bool IsOpen => isOpen;
        public bool IsSearchFocused => searchInputActive;

        public InventoryView(VisualElement root)
        {
            overlay = root.Q<VisualElement>("inventoryOverlay");
            panel = root.Q<VisualElement>("InventoryPanel");
            filterBar = root.Q<VisualElement>("filterBar");
            itemList = root.Q<VisualElement>("itemList");
            detailIcon = root.Q<VisualElement>("detailIcon");
            confirmHost = root.Q<VisualElement>("confirmHost");
            detailName = root.Q<Label>("detailName");
            detailMeta = root.Q<Label>("detailMeta");
            detailDescription = root.Q<Label>("detailDescription");
            detailStats = root.Q<Label>("detailStats");
            detailEmpty = root.Q<Label>("detailEmpty");
            confirmMessage = root.Q<Label>("confirmMessage");
            closeButton = root.Q<OrnateButton>("closeButton") ?? root.Q<Button>("closeButton");
            destroyButton = root.Q<OrnateButton>("destroyButton") ?? root.Q<Button>("destroyButton");
            confirmYes = root.Q<OrnateButton>("confirmYes") ?? root.Q<Button>("confirmYes");
            confirmNo = root.Q<OrnateButton>("confirmNo") ?? root.Q<Button>("confirmNo");
            searchField = root.Q<TextField>("searchField");

            if (overlay == null || panel == null)
            {
                UnityEngine.Debug.LogError("InventoryView: overlay or panel was not found in the inventory UI.");
                return;
            }

            overlay.pickingMode = PickingMode.Position;
            panel.pickingMode = PickingMode.Position;
            UiGameplayInputGuard.Apply(overlay);
            UiPointerState.RegisterBlockingElement(overlay);

            overlay.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == overlay)
                    OverlayClicked?.Invoke();
            });
            panel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            if (closeButton != null)
                closeButton.clicked += () => CloseClicked?.Invoke();
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
            UiPointerState.UnregisterBlockingElement(overlay);
        }

        public void SetOpen(bool open)
        {
            isOpen = open;
            if (overlay != null)
                overlay.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open)
            {
                PrepareSearchForOpen();
            }
            else
            {
                searchField?.Blur();
                ReleaseSearchInput();
                SetConfirmVisible(false);
            }

            RefreshModalInputBlock();
        }

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
                itemList.Add(row);
            }
        }

        public void SetDetail(InventoryRowVm row)
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

            if (destroyButton != null)
                destroyButton.SetEnabled(has);
        }

        public void ClearDetail()
        {
            SetDetail(null);
        }

        public void SetConfirmVisible(bool visible, string message = null)
        {
            if (confirmHost != null)
                confirmHost.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (confirmMessage != null && message != null)
                confirmMessage.text = message;
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
            overlay?.schedule.Execute(() =>
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
