using System.Collections.Generic;
using System.Threading;
using MessagePipe;
using MyGame.Events.Ui;
using R3;
using ShadowInfection.Input;
using ShadowInfection.Items;
using UnityEngine;

namespace ShadowInfection.UI.InventoryWindow
{
    internal sealed class InventoryWindowPresenter
    {
        private readonly IItemInventory inventory;
        private readonly IItemCatalog catalog;
        private readonly IInputReader input;
        private readonly ISubscriber<InventoryChangedEvent> inventoryChanged;
        private readonly ISubscriber<SetInventoryWindowOpenEvent> setOpen;
        private readonly ISubscriber<SetLoadoutWindowOpenEvent> loadoutOpen;
        private readonly ISubscriber<VendorWindowVisibilityChangedEvent> vendorVisibility;
        private readonly IPublisher<SetInventoryWindowOpenEvent> publishOpen;
        private readonly IPublisher<SetLoadoutWindowOpenEvent> publishLoadout;
        private readonly IPublisher<RequestCloseVendorWindowEvent> closeVendor;

        private InventoryView view;
        private R3.DisposableBag subscriptions;
        private InventoryFilter filter = InventoryFilter.All;
        private string search = string.Empty;
        private string selectedRowId;
        private readonly List<InventoryRowVm> rows = new();

        public InventoryWindowPresenter(
            IItemInventory inventory,
            IItemCatalog catalog,
            IInputReader input,
            ISubscriber<InventoryChangedEvent> inventoryChanged,
            ISubscriber<SetInventoryWindowOpenEvent> setOpen,
            ISubscriber<SetLoadoutWindowOpenEvent> loadoutOpen,
            ISubscriber<VendorWindowVisibilityChangedEvent> vendorVisibility,
            IPublisher<SetInventoryWindowOpenEvent> publishOpen,
            IPublisher<SetLoadoutWindowOpenEvent> publishLoadout,
            IPublisher<RequestCloseVendorWindowEvent> closeVendor)
        {
            this.inventory = inventory;
            this.catalog = catalog;
            this.input = input;
            this.inventoryChanged = inventoryChanged;
            this.setOpen = setOpen;
            this.loadoutOpen = loadoutOpen;
            this.vendorVisibility = vendorVisibility;
            this.publishOpen = publishOpen;
            this.publishLoadout = publishLoadout;
            this.closeVendor = closeVendor;
        }

        public void Bind(InventoryView nextView, CancellationToken token)
        {
            Unbind();
            view = nextView;
            if (view == null)
                return;

            view.CloseClicked += Close;
            view.OverlayClicked += Close;
            view.RowClicked += OnRowClicked;
            view.DestroyClicked += OnDestroyClicked;
            view.ConfirmDestroyClicked += OnConfirmDestroy;
            view.CancelDestroyClicked += OnCancelDestroy;
            view.FilterClicked += OnFilterClicked;
            view.SearchChanged += OnSearchChanged;

            view.SetFilter(filter);
            Refresh();

            subscriptions.Add(inventoryChanged.Subscribe(_ => Refresh()));
            subscriptions.Add(setOpen.Subscribe(OnSetOpen));
            subscriptions.Add(loadoutOpen.Subscribe(OnLoadoutOpen));
            subscriptions.Add(vendorVisibility.Subscribe(OnVendorVisibility));
            subscriptions.Add(
                Observable.EveryUpdate(UnityFrameProvider.Update, token)
                    .Subscribe(_ => TickToggle()));
        }

        public void Unbind()
        {
            if (view != null)
            {
                view.CloseClicked -= Close;
                view.OverlayClicked -= Close;
                view.RowClicked -= OnRowClicked;
                view.DestroyClicked -= OnDestroyClicked;
                view.ConfirmDestroyClicked -= OnConfirmDestroy;
                view.CancelDestroyClicked -= OnCancelDestroy;
                view.FilterClicked -= OnFilterClicked;
                view.SearchChanged -= OnSearchChanged;
            }

            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            view = null;
        }

        private void TickToggle()
        {
            if (view == null || input == null || view.IsSearchFocused)
                return;

            if (!input.WasPressed(PlayerActionId.Inventory))
                return;

            if (UiModalInputBlock.IsBlocked)
            {
                if (view.IsOpen)
                    Close();
                return;
            }

            if (view.IsOpen)
                Close();
            else
                Open();
        }

        private void Open()
        {
            publishLoadout.Publish(new SetLoadoutWindowOpenEvent(false));
            closeVendor.Publish(new RequestCloseVendorWindowEvent());
            view?.SetOpen(true);
            publishOpen.Publish(new SetInventoryWindowOpenEvent(true));
            Refresh();
        }

        private void Close()
        {
            view?.SetConfirmVisible(false);
            view?.SetOpen(false);
            publishOpen.Publish(new SetInventoryWindowOpenEvent(false));
        }

        private void OnSetOpen(SetInventoryWindowOpenEvent evt)
        {
            if (evt == null || view == null)
                return;

            if (evt.IsOpen)
            {
                if (UiModalInputBlock.IsBlocked)
                    return;
                view.SetOpen(true);
                Refresh();
            }
            else
            {
                view.SetConfirmVisible(false);
                view.SetOpen(false);
            }
        }

        private void OnLoadoutOpen(SetLoadoutWindowOpenEvent evt)
        {
            if (evt != null && evt.IsOpen && view != null && view.IsOpen)
                Close();
        }

        private void OnVendorVisibility(VendorWindowVisibilityChangedEvent evt)
        {
            if (evt != null && evt.IsOpen && view != null && view.IsOpen)
                Close();
        }

        private void OnFilterClicked(InventoryFilter next)
        {
            filter = next;
            view?.SetFilter(filter);
            selectedRowId = null;
            Refresh();
        }

        private void OnSearchChanged(string value)
        {
            search = value ?? string.Empty;
            selectedRowId = null;
            Refresh();
        }

        private void OnRowClicked(string rowId)
        {
            selectedRowId = rowId;
            view?.SetConfirmVisible(false);
            Refresh();
        }

        private void OnDestroyClicked()
        {
            var selected = FindSelected();
            if (selected == null)
                return;

            var label = selected.IsStack && selected.Count > 1
                ? $"Remove 1 {selected.Name}?"
                : $"Destroy {selected.Name}?";
            view?.SetConfirmVisible(true, label);
        }

        private void OnConfirmDestroy()
        {
            var selected = FindSelected();
            if (selected == null || inventory == null)
            {
                view?.SetConfirmVisible(false);
                return;
            }

            var destroyed = selected.IsStack
                ? inventory.TryDestroyStack(selected.ItemId)
                : inventory.TryDestroyEquipment(selected.InstanceId);

            view?.SetConfirmVisible(false);
            if (destroyed)
                selectedRowId = null;
            Refresh();
        }

        private void OnCancelDestroy()
        {
            view?.SetConfirmVisible(false);
        }

        private void Refresh()
        {
            if (view == null)
                return;

            BuildRows();
            if (FindSelected() == null)
                selectedRowId = rows.Count > 0 ? rows[0].RowId : null;

            view.SetRows(rows, selectedRowId, EmptyMessage());
            view.SetDetail(FindSelected());
        }

        private void BuildRows()
        {
            rows.Clear();
            if (inventory == null)
                return;

            var equipment = inventory.Equipment;
            if (equipment != null)
            {
                for (var i = 0; i < equipment.Count; i++)
                {
                    var entry = equipment[i];
                    if (entry == null)
                        continue;

                    var def = ResolveItem(entry.itemId);
                    if (!PassesFilter(def, isStack: false) || !PassesSearch(def, entry.itemId))
                        continue;

                    rows.Add(ToEquipmentRow(entry, def));
                }
            }

            var stacks = inventory.Stacks;
            if (stacks != null)
            {
                for (var i = 0; i < stacks.Count; i++)
                {
                    var stack = stacks[i];
                    if (stack == null || stack.count <= 0)
                        continue;

                    var def = ResolveItem(stack.itemId);
                    if (!PassesFilter(def, isStack: true) || !PassesSearch(def, stack.itemId))
                        continue;

                    rows.Add(ToStackRow(stack, def));
                }
            }
        }

        private ItemDefinition ResolveItem(string itemId)
        {
            if (catalog == null || string.IsNullOrWhiteSpace(itemId))
                return null;

            catalog.TryGet(itemId, out var def);
            return def;
        }

        private bool PassesFilter(ItemDefinition def, bool isStack)
        {
            switch (filter)
            {
                case InventoryFilter.All:
                    return true;
                case InventoryFilter.Weapon:
                    return def != null && ItemRules.IsWeaponSlot(def.slot);
                case InventoryFilter.Gem:
                    return def != null && def.kind == ItemKind.Gem;
                case InventoryFilter.Material:
                    return def != null && def.kind == ItemKind.Material;
                case InventoryFilter.Head:
                    return def != null && def.slot == ItemSlot.Head;
                case InventoryFilter.Shoulder:
                    return def != null && def.slot == ItemSlot.Shoulder;
                case InventoryFilter.Cape:
                    return def != null && def.slot == ItemSlot.Cape;
                case InventoryFilter.Chest:
                    return def != null && def.slot == ItemSlot.Chest;
                case InventoryFilter.Pants:
                    return def != null && def.slot == ItemSlot.Pants;
                case InventoryFilter.Feet:
                    return def != null && def.slot == ItemSlot.Feet;
                case InventoryFilter.Gloves:
                    return def != null && def.slot == ItemSlot.Gloves;
                default:
                    return !isStack;
            }
        }

        private bool PassesSearch(ItemDefinition def, string itemId)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;

            var name = def != null ? def.DisplayName : itemId;
            return name != null && name.IndexOf(search.Trim(), System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private InventoryRowVm ToEquipmentRow(InventoryEntry entry, ItemDefinition def)
        {
            return new InventoryRowVm
            {
                RowId = "eq:" + entry.instanceId,
                InstanceId = entry.instanceId,
                ItemId = entry.itemId,
                IsStack = false,
                Count = 1,
                Name = def != null ? def.DisplayName : "Unknown item",
                Meta = def != null ? $"{def.rarity} · {ItemPresentation.TypeLine(def)}" : entry.itemId,
                Description = def != null ? def.description : string.Empty,
                Summary = def != null ? ItemPresentation.FormatStats(def.statModifiers) : string.Empty,
                Icon = catalog != null ? catalog.ResolveIcon(def) : null,
                RarityClass = def != null ? ItemPresentation.RarityClass(def.rarity) : ItemPresentation.RarityClass(ItemRarity.Common),
                Definition = def
            };
        }

        private InventoryRowVm ToStackRow(ItemStack stack, ItemDefinition def)
        {
            return new InventoryRowVm
            {
                RowId = "st:" + stack.itemId,
                ItemId = stack.itemId,
                IsStack = true,
                Count = stack.count,
                Name = def != null ? def.DisplayName : "Unknown item",
                Meta = def != null
                    ? $"{def.rarity} · {ItemPresentation.TypeLine(def)} · ×{stack.count}"
                    : $"{stack.itemId} · ×{stack.count}",
                Description = def != null ? def.description : string.Empty,
                Summary = def != null ? ItemPresentation.FormatStats(def.statModifiers) : string.Empty,
                Icon = catalog != null ? catalog.ResolveIcon(def) : null,
                RarityClass = def != null ? ItemPresentation.RarityClass(def.rarity) : ItemPresentation.RarityClass(ItemRarity.Common),
                Definition = def
            };
        }

        private InventoryRowVm FindSelected()
        {
            if (string.IsNullOrEmpty(selectedRowId))
                return null;

            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].RowId == selectedRowId)
                    return rows[i];
            }

            return null;
        }

        private string EmptyMessage()
        {
            if (!string.IsNullOrWhiteSpace(search) || filter != InventoryFilter.All)
                return "No items match that filter.";
            return "Your bag is empty.";
        }
    }
}
