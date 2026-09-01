using System.Collections.Generic;
using System.Threading;
using MessagePipe;
using MyGame.Events.Ui;
using R3;
using ShadowInfection.Input;
using ShadowInfection.Items;
using ShadowInfection.UI;
using ShadowInfection.UI.CharacterWindow;
using UnityEngine;

namespace ShadowInfection.UI.InventoryWindow
{
    internal sealed class InventoryWindowPresenter
    {
        private readonly IItemInventory inventory;
        private readonly IItemCatalog catalog;
        private readonly ICharacterEquipment equipment;
        private readonly IEquipSlotSelection slotSelection;
        private readonly IInputReader input;
        private readonly ISubscriber<InventoryChangedEvent> inventoryChanged;
        private readonly ISubscriber<EquipmentChangedEvent> equipmentChanged;
        private readonly ISubscriber<SetInventoryWindowOpenEvent> setOpen;
        private readonly ISubscriber<SetLoadoutWindowOpenEvent> loadoutOpen;
        private readonly ISubscriber<SetCharacterWindowOpenEvent> characterOpen;
        private readonly ISubscriber<VendorWindowVisibilityChangedEvent> vendorVisibility;
        private readonly IPublisher<SetInventoryWindowOpenEvent> publishOpen;
        private readonly IPublisher<SetLoadoutWindowOpenEvent> publishLoadout;
        private readonly IPublisher<RequestCloseVendorWindowEvent> closeVendor;
        private readonly CharacterInventoryPanelCoordinator panelCoordinator;

        private InventoryView view;
        private CharacterWindowPresenter characterPresenter;
        private R3.DisposableBag subscriptions;
        private InventoryFilter filter = InventoryFilter.All;
        private string search = string.Empty;
        private string selectedRowId;
        private bool characterOpenState;
        private bool sideBySideApplied;
        private readonly List<InventoryRowVm> rows = new();

        public bool IsOpen => view != null && view.IsOpen;
        public UiDraggablePanel Draggable => view?.Draggable;

        public InventoryWindowPresenter(
            IItemInventory inventory,
            IItemCatalog catalog,
            ICharacterEquipment equipment,
            IEquipSlotSelection slotSelection,
            IInputReader input,
            ISubscriber<InventoryChangedEvent> inventoryChanged,
            ISubscriber<EquipmentChangedEvent> equipmentChanged,
            ISubscriber<SetInventoryWindowOpenEvent> setOpen,
            ISubscriber<SetLoadoutWindowOpenEvent> loadoutOpen,
            ISubscriber<SetCharacterWindowOpenEvent> characterOpen,
            ISubscriber<VendorWindowVisibilityChangedEvent> vendorVisibility,
            IPublisher<SetInventoryWindowOpenEvent> publishOpen,
            IPublisher<SetLoadoutWindowOpenEvent> publishLoadout,
            IPublisher<RequestCloseVendorWindowEvent> closeVendor,
            CharacterInventoryPanelCoordinator panelCoordinator)
        {
            this.inventory = inventory;
            this.catalog = catalog;
            this.equipment = equipment;
            this.slotSelection = slotSelection;
            this.input = input;
            this.inventoryChanged = inventoryChanged;
            this.equipmentChanged = equipmentChanged;
            this.setOpen = setOpen;
            this.loadoutOpen = loadoutOpen;
            this.characterOpen = characterOpen;
            this.vendorVisibility = vendorVisibility;
            this.publishOpen = publishOpen;
            this.publishLoadout = publishLoadout;
            this.closeVendor = closeVendor;
            this.panelCoordinator = panelCoordinator;
        }

        internal void LinkCharacterPresenter(CharacterWindowPresenter presenter)
        {
            characterPresenter = presenter;
        }

        public void Bind(InventoryView nextView, CancellationToken token)
        {
            Unbind();
            view = nextView;
            if (view == null)
                return;

            panelCoordinator?.RegisterInventory(this);

            view.CloseClicked += Close;
            view.RowClicked += OnRowClicked;
            view.RowQuickEquip += OnRowQuickEquip;
            view.EquipClicked += OnEquipClicked;
            view.DestroyClicked += OnDestroyClicked;
            view.ConfirmDestroyClicked += OnConfirmDestroy;
            view.CancelDestroyClicked += OnCancelDestroy;
            view.FilterClicked += OnFilterClicked;
            view.SearchChanged += OnSearchChanged;
            view.PositionChanged += PersistPosition;

            view.SetFilter(filter);
            Refresh();

            subscriptions.Add(inventoryChanged.Subscribe(_ => Refresh()));
            subscriptions.Add(equipmentChanged.Subscribe(_ => Refresh()));
            subscriptions.Add(setOpen.Subscribe(OnSetOpen));
            subscriptions.Add(loadoutOpen.Subscribe(OnLoadoutOpen));
            subscriptions.Add(characterOpen.Subscribe(OnCharacterOpen));
            subscriptions.Add(vendorVisibility.Subscribe(OnVendorVisibility));
            if (slotSelection != null)
                slotSelection.Changed += OnSlotSelectionChanged;
            subscriptions.Add(
                Observable.EveryUpdate(UnityFrameProvider.Update, token)
                    .Subscribe(_ => TickToggle()));
        }

        public void Unbind()
        {
            if (view != null)
            {
                view.CloseClicked -= Close;
                view.RowClicked -= OnRowClicked;
                view.RowQuickEquip -= OnRowQuickEquip;
                view.EquipClicked -= OnEquipClicked;
                view.DestroyClicked -= OnDestroyClicked;
                view.ConfirmDestroyClicked -= OnConfirmDestroy;
                view.CancelDestroyClicked -= OnCancelDestroy;
                view.FilterClicked -= OnFilterClicked;
                view.SearchChanged -= OnSearchChanged;
                view.PositionChanged -= PersistPosition;
            }

            if (slotSelection != null)
                slotSelection.Changed -= OnSlotSelectionChanged;

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
            RestorePosition();
            Refresh();
            TryApplySideBySideLayout();
        }

        private void Close()
        {
            PersistPosition();
            view?.SetConfirmVisible(false);
            view?.SetOpen(false);
            publishOpen.Publish(new SetInventoryWindowOpenEvent(false));
            sideBySideApplied = false;
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
                RestorePosition();
                Refresh();
                TryApplySideBySideLayout();
            }
            else
            {
                view.SetConfirmVisible(false);
                view.SetOpen(false);
                sideBySideApplied = false;
            }
        }

        private void OnLoadoutOpen(SetLoadoutWindowOpenEvent evt)
        {
            if (evt != null && evt.IsOpen && view != null && view.IsOpen)
                Close();
        }

        private void OnCharacterOpen(SetCharacterWindowOpenEvent evt)
        {
            characterOpenState = evt != null && evt.IsOpen;
            if (characterOpenState && view != null && view.IsOpen)
                TryApplySideBySideLayout();
        }

        private void OnVendorVisibility(VendorWindowVisibilityChangedEvent evt)
        {
            if (evt != null && evt.IsOpen && view != null && view.IsOpen)
                Close();
        }

        private void OnSlotSelectionChanged()
        {
            Refresh();
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

        private void OnRowQuickEquip(string rowId)
        {
            var row = FindRow(rowId);
            if (row == null || !row.CanQuickEquip || equipment == null)
                return;

            var targetSlot = slotSelection != null && slotSelection.SelectedSlot.HasValue
                ? slotSelection.SelectedSlot.Value
                : row.Definition.slot;

            if (equipment.TryEquip(row.InstanceId, targetSlot))
                view?.SetConfirmVisible(false);
            Refresh();
        }

        private void OnEquipClicked()
        {
            var selected = FindSelected();
            if (selected == null || !selected.CanQuickEquip || equipment == null)
                return;

            var targetSlot = slotSelection != null && slotSelection.SelectedSlot.HasValue
                ? slotSelection.SelectedSlot.Value
                : selected.Definition.slot;

            if (equipment.TryEquip(selected.InstanceId, targetSlot))
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

            var selected = FindSelected();
            view.SetRows(rows, selectedRowId, EmptyMessage());
            view.SetDetail(
                selected,
                BuildDetailNotice(selected),
                selected != null && selected.CanQuickEquip,
                selected != null && CanDestroy(selected));
        }

        private void BuildRows()
        {
            rows.Clear();
            if (inventory == null)
                return;

            var mainHandWeapon = ResolveMainHandWeaponType();
            var equipmentRows = inventory.Equipment;
            if (equipmentRows != null)
            {
                for (var i = 0; i < equipmentRows.Count; i++)
                {
                    var entry = equipmentRows[i];
                    if (entry == null)
                        continue;

                    var def = ResolveItem(entry.itemId);
                    if (!PassesFilter(def, isStack: false) || !PassesSearch(def, entry.itemId))
                        continue;

                    rows.Add(ToEquipmentRow(entry, def, mainHandWeapon));
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

        private InventoryRowVm ToEquipmentRow(InventoryEntry entry, ItemDefinition def, WeaponType? mainHandWeapon)
        {
            var equipped = equipment != null && equipment.IsEquipped(entry.instanceId);
            var targetSlot = slotSelection != null && slotSelection.SelectedSlot.HasValue
                ? slotSelection.SelectedSlot.Value
                : def != null ? def.slot : ItemSlot.None;
            var weightReason = def != null
                ? ItemPresentation.ArmorWeightMismatchReason(def, mainHandWeapon)
                : null;
            var slotMismatch = def != null && def.kind == ItemKind.Equipment && def.slot != targetSlot;
            var canEquip = def != null
                && def.kind == ItemKind.Equipment
                && !equipped
                && string.IsNullOrEmpty(weightReason)
                && !slotMismatch;

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
                Definition = def,
                Dimmed = !canEquip && def != null && def.kind == ItemKind.Equipment && !equipped,
                CanQuickEquip = canEquip,
                EquipBlockReason = equipped
                    ? "Equipped — unequip first"
                    : slotMismatch
                        ? $"Select the {ItemPresentation.SlotLabel(def.slot)} slot on your character."
                        : weightReason
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
                Definition = def,
                Dimmed = false,
                CanQuickEquip = false
            };
        }

        private string BuildDetailNotice(InventoryRowVm selected)
        {
            if (selected == null)
                return null;

            if (!string.IsNullOrWhiteSpace(selected.EquipBlockReason))
                return selected.EquipBlockReason;

            return null;
        }

        private bool CanDestroy(InventoryRowVm selected)
        {
            if (selected == null || selected.IsStack)
                return true;

            return equipment == null || !equipment.IsEquipped(selected.InstanceId);
        }

        private WeaponType? ResolveMainHandWeaponType()
        {
            if (equipment == null || catalog == null)
                return null;
            if (!equipment.TryGetEquippedEntry(ItemSlot.MainHand, out var entry)
                || string.IsNullOrWhiteSpace(entry.itemId))
                return null;
            if (!catalog.TryGet(entry.itemId, out var definition) || definition.weaponData == null)
                return null;

            return definition.weaponData.weaponType;
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

        private InventoryRowVm FindSelected()
        {
            return FindRow(selectedRowId);
        }

        private InventoryRowVm FindRow(string rowId)
        {
            if (string.IsNullOrEmpty(rowId))
                return null;

            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].RowId == rowId)
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

        private void RestorePosition()
        {
            if (view == null)
                return;

            if (FloatingPanelLayout.TryReadPosition(
                    FloatingPanelLayout.InventoryPosX,
                    FloatingPanelLayout.InventoryPosY,
                    out var left,
                    out var top))
                view.ApplyPosition(left, top);
            else
                view.ApplyDefaultPosition();
        }

        private void PersistPosition()
        {
            if (view == null || !view.IsOpen || !view.HasUsableLayout())
                return;

            var pos = view.GetPosition();
            FloatingPanelLayout.WritePosition(
                FloatingPanelLayout.InventoryPosX,
                FloatingPanelLayout.InventoryPosY,
                pos.x,
                pos.y);
        }

        private void TryApplySideBySideLayout()
        {
            if (view == null || !view.IsOpen || !characterOpenState || sideBySideApplied)
                return;
            if (FloatingPanelLayout.HasSavedPosition(FloatingPanelLayout.InventoryPosX, FloatingPanelLayout.InventoryPosY)
                || FloatingPanelLayout.HasSavedPosition(FloatingPanelLayout.CharacterPosX, FloatingPanelLayout.CharacterPosY))
                return;

            var characterDraggable = characterPresenter?.Draggable;
            if (characterDraggable == null || !view.HasUsableLayout() || !characterDraggable.HasUsableLayout())
            {
                view.Draggable?.Panel?.schedule.Execute(TryApplySideBySideLayout);
                return;
            }

            if (FloatingPanelLayout.TryTileSideBySide(characterDraggable, view.Draggable))
                sideBySideApplied = true;
        }
    }
}
