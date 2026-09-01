using System.Collections.Generic;
using System.Threading;
using MessagePipe;
using MyGame.Events.Ui;
using R3;
using ShadowInfection.Input;
using ShadowInfection.Items;
using ShadowInfection.UI;
using ShadowInfection.UI.InventoryWindow;
using UnityEngine;

namespace ShadowInfection.UI.CharacterWindow
{
    internal sealed class CharacterWindowPresenter : IUiOverlay
    {
        private static readonly ItemSlot[] PaperDollSlots =
        {
            ItemSlot.Head,
            ItemSlot.Shoulder,
            ItemSlot.Cape,
            ItemSlot.Gloves,
            ItemSlot.Chest,
            ItemSlot.Pants,
            ItemSlot.Feet,
            ItemSlot.MainHand,
            ItemSlot.OffHand
        };

        private readonly ICharacterEquipment equipment;
        private readonly IItemCatalog catalog;
        private readonly IEquipSlotSelection slotSelection;
        private readonly IInputReader input;
        private readonly ISubscriber<InventoryChangedEvent> inventoryChanged;
        private readonly ISubscriber<EquipmentChangedEvent> equipmentChanged;
        private readonly ISubscriber<SetCharacterWindowOpenEvent> setOpen;
        private readonly ISubscriber<SetLoadoutWindowOpenEvent> loadoutOpen;
        private readonly ISubscriber<SetInventoryWindowOpenEvent> inventoryOpen;
        private readonly ISubscriber<VendorWindowVisibilityChangedEvent> vendorVisibility;
        private readonly IPublisher<SetCharacterWindowOpenEvent> publishOpen;
        private readonly IPublisher<SetLoadoutWindowOpenEvent> publishLoadout;
        private readonly IPublisher<RequestCloseVendorWindowEvent> closeVendor;
        private readonly CharacterInventoryPanelCoordinator panelCoordinator;
        private readonly IUiOverlayRegistry overlays;

        private CharacterView view;
        private InventoryWindowPresenter inventoryPresenter;
        private R3.DisposableBag subscriptions;
        private bool inventoryOpenState;
        private bool sideBySideApplied;
        private readonly List<CharacterSlotVm> slotVms = new();

        public bool IsOpen => view != null && view.IsOpen;
        public UiDraggablePanel Draggable => view?.Draggable;

        public CharacterWindowPresenter(
            ICharacterEquipment equipment,
            IItemCatalog catalog,
            IEquipSlotSelection slotSelection,
            IInputReader input,
            ISubscriber<InventoryChangedEvent> inventoryChanged,
            ISubscriber<EquipmentChangedEvent> equipmentChanged,
            ISubscriber<SetCharacterWindowOpenEvent> setOpen,
            ISubscriber<SetLoadoutWindowOpenEvent> loadoutOpen,
            ISubscriber<SetInventoryWindowOpenEvent> inventoryOpen,
            ISubscriber<VendorWindowVisibilityChangedEvent> vendorVisibility,
            IPublisher<SetCharacterWindowOpenEvent> publishOpen,
            IPublisher<SetLoadoutWindowOpenEvent> publishLoadout,
            IPublisher<RequestCloseVendorWindowEvent> closeVendor,
            CharacterInventoryPanelCoordinator panelCoordinator,
            IUiOverlayRegistry overlays)
        {
            this.equipment = equipment;
            this.catalog = catalog;
            this.slotSelection = slotSelection;
            this.input = input;
            this.inventoryChanged = inventoryChanged;
            this.equipmentChanged = equipmentChanged;
            this.setOpen = setOpen;
            this.loadoutOpen = loadoutOpen;
            this.inventoryOpen = inventoryOpen;
            this.vendorVisibility = vendorVisibility;
            this.publishOpen = publishOpen;
            this.publishLoadout = publishLoadout;
            this.closeVendor = closeVendor;
            this.panelCoordinator = panelCoordinator;
            this.overlays = overlays;
        }

        internal void LinkInventoryPresenter(InventoryWindowPresenter presenter)
        {
            inventoryPresenter = presenter;
        }

        public void Bind(CharacterView nextView, CancellationToken token)
        {
            Unbind();
            view = nextView;
            if (view == null)
                return;

            panelCoordinator?.RegisterCharacter(this);
            overlays?.Register(this);

            view.CloseClicked += Close;
            view.SlotClicked += OnSlotClicked;
            view.SlotRightClicked += OnSlotRightClicked;
            view.PositionChanged += PersistPosition;

            Refresh();

            subscriptions.Add(inventoryChanged.Subscribe(_ => Refresh()));
            subscriptions.Add(equipmentChanged.Subscribe(_ => Refresh()));
            subscriptions.Add(setOpen.Subscribe(OnSetOpen));
            subscriptions.Add(loadoutOpen.Subscribe(OnLoadoutOpen));
            subscriptions.Add(inventoryOpen.Subscribe(OnInventoryOpen));
            subscriptions.Add(vendorVisibility.Subscribe(OnVendorVisibility));
            subscriptions.Add(
                Observable.EveryUpdate(UnityFrameProvider.Update, token)
                    .Subscribe(_ => TickToggle()));
        }

        public void Unbind()
        {
            overlays?.Unregister(this);

            if (view != null)
            {
                view.CloseClicked -= Close;
                view.SlotClicked -= OnSlotClicked;
                view.SlotRightClicked -= OnSlotRightClicked;
                view.PositionChanged -= PersistPosition;
            }

            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            view = null;
        }

        private void TickToggle()
        {
            if (view == null || input == null)
                return;

            if (!input.WasPressed(PlayerActionId.Character))
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
            publishOpen.Publish(new SetCharacterWindowOpenEvent(true));
            RestorePosition();
            Refresh();
            TryApplySideBySideLayout();
        }

        public void Close()
        {
            PersistPosition();
            slotSelection?.Clear();
            view?.SetOpen(false);
            publishOpen.Publish(new SetCharacterWindowOpenEvent(false));
            sideBySideApplied = false;
        }

        private void OnSetOpen(SetCharacterWindowOpenEvent evt)
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
                slotSelection?.Clear();
                view.SetOpen(false);
                sideBySideApplied = false;
            }
        }

        private void OnLoadoutOpen(SetLoadoutWindowOpenEvent evt)
        {
            if (evt != null && evt.IsOpen && view != null && view.IsOpen)
                Close();
        }

        private void OnInventoryOpen(SetInventoryWindowOpenEvent evt)
        {
            inventoryOpenState = evt != null && evt.IsOpen;
            if (inventoryOpenState && view != null && view.IsOpen)
                TryApplySideBySideLayout();
        }

        private void OnVendorVisibility(VendorWindowVisibilityChangedEvent evt)
        {
            if (evt != null && evt.IsOpen && view != null && view.IsOpen)
                Close();
        }

        private void OnSlotClicked(ItemSlot slot)
        {
            slotSelection?.Select(slot);
            view?.SetActiveSlot(slot);
            view?.SetSubheading($"Choosing: {ItemPresentation.SlotLabel(slot)}");
        }

        private void OnSlotRightClicked(ItemSlot slot)
        {
            if (equipment == null)
                return;

            if (equipment.TryGetEquipped(slot, out var instanceId) && !string.IsNullOrWhiteSpace(instanceId))
                equipment.TryUnequip(slot);
            Refresh();
        }

        private void Refresh()
        {
            if (view == null)
                return;

            BuildSlots();
            view.SetSlots(slotVms);
            var selected = slotSelection != null ? slotSelection.SelectedSlot : null;
            view.SetActiveSlot(selected);
            view.SetSubheading(selected.HasValue
                ? $"Choosing: {ItemPresentation.SlotLabel(selected.Value)}"
                : null);
        }

        private void BuildSlots()
        {
            slotVms.Clear();
            for (var i = 0; i < PaperDollSlots.Length; i++)
            {
                var slot = PaperDollSlots[i];
                EquippedSlotEntry entry = null;
                var occupied = equipment != null
                    && equipment.TryGetEquippedEntry(slot, out entry)
                    && entry != null
                    && !string.IsNullOrWhiteSpace(entry.instanceId);

                Texture2D icon = null;
                string rarityClass = null;
                if (occupied && catalog.TryGet(entry.itemId, out var definition))
                {
                    icon = catalog.ResolveIcon(definition);
                    rarityClass = ItemPresentation.RarityClass(definition.rarity);
                }

                slotVms.Add(new CharacterSlotVm
                {
                    Slot = slot,
                    Icon = icon,
                    RarityClass = rarityClass,
                    EmptyLabel = ItemPresentation.SlotLabel(slot),
                    Occupied = occupied
                });
            }
        }

        private void RestorePosition()
        {
            if (view == null)
                return;

            if (FloatingPanelLayout.TryReadPosition(
                    FloatingPanelLayout.CharacterPosX,
                    FloatingPanelLayout.CharacterPosY,
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
                FloatingPanelLayout.CharacterPosX,
                FloatingPanelLayout.CharacterPosY,
                pos.x,
                pos.y);
        }

        private void TryApplySideBySideLayout()
        {
            if (view == null || !view.IsOpen || !inventoryOpenState || sideBySideApplied)
                return;
            if (FloatingPanelLayout.HasSavedPosition(FloatingPanelLayout.InventoryPosX, FloatingPanelLayout.InventoryPosY)
                || FloatingPanelLayout.HasSavedPosition(FloatingPanelLayout.CharacterPosX, FloatingPanelLayout.CharacterPosY))
                return;

            var inventoryDraggable = inventoryPresenter?.Draggable ?? panelCoordinator?.InventoryPresenter?.Draggable;
            if (inventoryDraggable == null || !view.HasUsableLayout() || !inventoryDraggable.HasUsableLayout())
            {
                view.Draggable?.Panel?.schedule.Execute(TryApplySideBySideLayout);
                return;
            }

            if (FloatingPanelLayout.TryTileSideBySide(view.Draggable, inventoryDraggable))
                sideBySideApplied = true;
        }
    }
}
