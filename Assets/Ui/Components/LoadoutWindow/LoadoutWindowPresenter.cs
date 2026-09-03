using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MessagePipe;
using Mirror;
using MyGame.Events.Ui;
using R3;
using ShadowInfection.DI;
using ShadowInfection.Input;
using ShadowInfection.Items;
using ShadowInfection.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Vland.UI;

namespace ShadowInfection.UI.LoadoutWindow
{
    internal sealed class LoadoutWindowPresenter : IUiOverlay
    {
        private readonly ILoadoutStore store;
        private readonly ILoadoutCatalog catalog;
        private readonly ICharacterEquipment equipment;
        private readonly IItemCatalog items;
        private readonly ISubscriber<SetLoadoutWindowOpenEvent> setOpen;
        private readonly ISubscriber<EquipmentChangedEvent> equipmentChanged;
        private readonly IPublisher<SetInventoryWindowOpenEvent> inventoryOpen;
        private readonly IPublisher<SetCharacterWindowOpenEvent> characterOpen;
        private readonly ApplicationSettings settings;
        private readonly IInputReader input;
        private readonly IUiOverlayRegistry overlays;

        private LoadoutView view;
        private R3.DisposableBag subscriptions;
        private CancellationToken bindToken;
        private LoadoutSlot activeSlot = LoadoutSlot.Passive;
        private SkillTag? tagFilter;
        private readonly Dictionary<LoadoutSlot, LoadoutItem> selected = new();
        private bool applyPending;

        public LoadoutWindowPresenter(
            ILoadoutStore store,
            ILoadoutCatalog catalog,
            ICharacterEquipment equipment,
            IItemCatalog items,
            ISubscriber<SetLoadoutWindowOpenEvent> setOpen,
            ISubscriber<EquipmentChangedEvent> equipmentChanged,
            IPublisher<SetInventoryWindowOpenEvent> inventoryOpen,
            IPublisher<SetCharacterWindowOpenEvent> characterOpen,
            ApplicationSettings settings,
            IInputReader input,
            IUiOverlayRegistry overlays)
        {
            this.store = store;
            this.catalog = catalog;
            this.equipment = equipment;
            this.items = items;
            this.setOpen = setOpen;
            this.equipmentChanged = equipmentChanged;
            this.inventoryOpen = inventoryOpen;
            this.characterOpen = characterOpen;
            this.settings = settings;
            this.input = input;
            this.overlays = overlays;
        }

        public bool IsOpen => view != null && view.IsOpen;

        public void Bind(LoadoutView nextView, CancellationToken token)
        {
            Unbind();
            view = nextView;
            bindToken = token;
            if (view == null)
                return;

            foreach (var slot in LoadoutSlots.All)
                selected[slot] = LoadoutItem.Empty;

            overlays?.Register(this);

            view.CloseClicked += Close;
            view.OpenClicked += Open;
            view.OverlayClicked += Close;
            view.SlotClicked += HandleSlotClicked;
            view.ItemClicked += HandleItemClicked;
            view.FilterClicked += HandleFilterClicked;

            TryInitializeFromSavedLoadout();
            view.SetActiveSlot(activeSlot);
            view.SetFilter(tagFilter, true);
            ClearIncompatibleSkillSelections();
            RefreshList();
            ApplyCurrentLoadout();

            subscriptions.Add(setOpen.Subscribe(OnSetOpen));
            subscriptions.Add(equipmentChanged.Subscribe(_ => OnEquipmentChanged()));
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
                view.OpenClicked -= Open;
                view.OverlayClicked -= Close;
                view.SlotClicked -= HandleSlotClicked;
                view.ItemClicked -= HandleItemClicked;
                view.FilterClicked -= HandleFilterClicked;
            }

            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            view = null;
            bindToken = default;
            applyPending = false;
            activeSlot = LoadoutSlot.Passive;
            tagFilter = null;
            selected.Clear();
        }

        private void Open()
        {
            inventoryOpen?.Publish(new SetInventoryWindowOpenEvent(false));
            characterOpen?.Publish(new SetCharacterWindowOpenEvent(false));
            view?.SetOpen(true);
        }

        public void Close()
        {
            view?.SetOpen(false);
        }

        private void OnSetOpen(SetLoadoutWindowOpenEvent evt)
        {
            var wantOpen = evt != null && evt.IsOpen;
            if (wantOpen && !IsInRoomLobby())
                wantOpen = false;

            if (wantOpen)
            {
                inventoryOpen?.Publish(new SetInventoryWindowOpenEvent(false));
                characterOpen?.Publish(new SetCharacterWindowOpenEvent(false));
            }

            view?.SetOpen(wantOpen);
        }

        private void OnEquipmentChanged()
        {
            ClearIncompatibleSkillSelections();
            RefreshList();
            ScheduleApply();
        }

        private void TickToggle()
        {
            if (view == null || input == null || !input.WasPressed(PlayerActionId.Loadout))
                return;

            if (UiModalInputBlock.IsBlocked || !IsInRoomLobby())
            {
                if (view.IsOpen)
                    view.SetOpen(false);
                return;
            }

            view.SetOpen(!view.IsOpen);
            if (view.IsOpen)
            {
                inventoryOpen?.Publish(new SetInventoryWindowOpenEvent(false));
                characterOpen?.Publish(new SetCharacterWindowOpenEvent(false));
            }
        }

        private static bool IsInRoomLobby()
        {
            return NetworkManager.singleton is NetworkRoomManager room
                && !string.IsNullOrWhiteSpace(room.RoomScene)
                && Utils.IsSceneActive(room.RoomScene);
        }

        private void HandleSlotClicked(LoadoutSlot slot)
        {
            activeSlot = slot;
            view.SetActiveSlot(slot);
            view.SetFilter(tagFilter, true);
            RefreshList();
        }

        private void HandleFilterClicked(SkillTag? tag)
        {
            tagFilter = tag;
            view.SetFilter(tagFilter, true);
            RefreshList();
        }

        private void HandleItemClicked(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            var itemsForSlot = BuildItemsForActiveSlot();
            var clicked = itemsForSlot.FirstOrDefault(item => item.id == id);
            if (!clicked.HasId)
                return;

            if (LoadoutSlots.IsNormal(activeSlot))
            {
                LoadoutSlot? otherWithSame = null;
                foreach (var slot in LoadoutSlots.All)
                {
                    if (!LoadoutSlots.IsNormal(slot) || slot == activeSlot)
                        continue;
                    if (selected.TryGetValue(slot, out var existing) && existing.id == id)
                    {
                        otherWithSame = slot;
                        break;
                    }
                }

                if (otherWithSame.HasValue)
                {
                    var other = otherWithSame.Value;
                    var previous = selected[activeSlot];
                    AssignSlot(activeSlot, clicked);
                    if (previous.HasId)
                        AssignSlot(other, previous);
                    else
                        AssignSlot(other, LoadoutItem.Empty);

                    ScheduleApply();
                    RefreshList();
                    return;
                }
            }

            AssignSlot(activeSlot, clicked);
            RefreshList();
            ScheduleApply();
        }

        private void TryInitializeFromSavedLoadout()
        {
            var saved = store.Get();
            if (saved == null)
                return;

            AssignSlot(LoadoutSlot.Passive, MakeSkillItem(saved.PassiveId, LoadoutSlot.Passive));
            AssignSlot(LoadoutSlot.Normal1, MakeSkillItem(saved.Normal1Id, LoadoutSlot.Normal1));
            AssignSlot(LoadoutSlot.Normal2, MakeSkillItem(saved.Normal2Id, LoadoutSlot.Normal2));
            AssignSlot(LoadoutSlot.Normal3, MakeSkillItem(saved.Normal3Id, LoadoutSlot.Normal3));
            AssignSlot(LoadoutSlot.Ultimate, MakeSkillItem(saved.UltimateId, LoadoutSlot.Ultimate));
        }

        private void AssignSlot(LoadoutSlot slot, LoadoutItem item)
        {
            selected[slot] = item.HasId ? item : LoadoutItem.Empty;
            view?.SetSlot(slot, selected[slot]);
        }

        private void RefreshList()
        {
            if (view == null)
                return;

            var list = BuildItemsForActiveSlot();
            var selectedId = GetSelectedId(activeSlot);
            view.SetItems(list, selectedId, BuildEmptyMessage());
            view.SetSubheading(BuildSubheading(list.Count));
            view.SetFilter(tagFilter, true);
        }

        private List<LoadoutItem> BuildItemsForActiveSlot()
        {
            var list = new List<LoadoutItem>();
            var equippedWeaponType = GetEquippedWeaponType();

            var expectedType = activeSlot == LoadoutSlot.Passive
                ? SkillType.Passive
                : activeSlot == LoadoutSlot.Ultimate
                    ? SkillType.Ultimate
                    : SkillType.Normal;

            var skills = catalog.GetPlayerSkills();
            for (var i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                if (skill.skillType != expectedType)
                    continue;
                if (!skill.CanBeUsedWithWeapon(equippedWeaponType))
                    continue;
                if (tagFilter.HasValue && !skill.HasTag(tagFilter.Value))
                    continue;

                list.Add(ToSkillItem(skill, expectedType == SkillType.Normal ? LoadoutSlot.Normal1 : activeSlot));
            }

            return list;
        }

        private string BuildSubheading(int visibleCount)
        {
            var choosing = LoadoutSlots.ChoosingLabel(activeSlot);
            var typeLabel = LoadoutSlots.SlotTypeLabel(activeSlot);
            var weaponText = DescribeEquippedWeapon();
            var tagText = tagFilter.HasValue ? $"{SkillTagUtil.GetLabel(tagFilter.Value)} · " : string.Empty;
            return $"{choosing} · {tagText}{typeLabel} compatible with {weaponText}";
        }

        private string BuildEmptyMessage()
        {
            var weaponText = DescribeEquippedWeapon();
            if (tagFilter.HasValue)
                return $"No {SkillTagUtil.GetLabel(tagFilter.Value)} skills for this slot with {weaponText}.";

            return $"No skills for this slot with {weaponText}.";
        }

        private LoadoutItem MakeSkillItem(string id, LoadoutSlot slot)
        {
            var skill = catalog.GetSkill(id);
            return skill != null ? ToSkillItem(skill, slot) : LoadoutItem.Empty;
        }

        private static LoadoutItem ToSkillItem(SkillData skill, LoadoutSlot slot)
        {
            var cooldownText = skill.cooldown > 0 ? $"{skill.cooldown}s cooldown" : skill.skillType.ToString();
            var required = skill.GetRequiredWeaponLabel();
            var meta = skill.RequiredWeapon.HasValue
                ? $"{cooldownText} · Requires {required}"
                : cooldownText;

            return new LoadoutItem
            {
                id = skill.skillName,
                name = skill.skillName,
                icon = skill.iconTexture,
                slot = slot,
                tags = skill.tags,
                summary = skill.GetOneLineSummary(),
                meta = meta,
                description = $"{skill.description}\n\nRequired weapon: {required}",
            };
        }

        private string GetSelectedId(LoadoutSlot slot)
        {
            return selected.TryGetValue(slot, out var item) ? item.id : null;
        }

        private WeaponType GetEquippedWeaponType()
        {
            if (equipment != null
                && equipment.TryGetEquippedEntry(ItemSlot.MainHand, out var entry)
                && !string.IsNullOrWhiteSpace(entry.itemId)
                && items != null
                && items.TryGet(entry.itemId, out var definition)
                && definition.weaponData != null)
            {
                return definition.weaponData.weaponType;
            }

            return WeaponType.Unarmed;
        }

        private string DescribeEquippedWeapon()
        {
            if (equipment != null
                && equipment.TryGetEquippedEntry(ItemSlot.MainHand, out var entry)
                && !string.IsNullOrWhiteSpace(entry.itemId)
                && items != null
                && items.TryGet(entry.itemId, out var definition)
                && definition != null)
            {
                return definition.DisplayName;
            }

            return "Unarmed";
        }

        private void ClearIncompatibleSkillSelections()
        {
            var weaponType = GetEquippedWeaponType();
            var slotsToCheck = new[]
            {
                LoadoutSlot.Passive,
                LoadoutSlot.Normal1,
                LoadoutSlot.Normal2,
                LoadoutSlot.Normal3,
                LoadoutSlot.Ultimate,
            };

            foreach (var slot in slotsToCheck)
            {
                var selectedId = GetSelectedId(slot);
                if (string.IsNullOrWhiteSpace(selectedId))
                    continue;

                var skill = catalog.GetSkill(selectedId);
                if (skill == null || !skill.CanBeUsedWithWeapon(weaponType))
                    AssignSlot(slot, LoadoutItem.Empty);
            }
        }

        private void ScheduleApply()
        {
            if (applyPending)
                return;

            applyPending = true;
            subscriptions.Add(
                Observable.NextFrame(UnityFrameProvider.Update, bindToken)
                    .Subscribe(_ =>
                    {
                        applyPending = false;
                        ApplyCurrentLoadout();
                    }));
        }

        private void ApplyCurrentLoadout()
        {
            var characterName = GameServices.Characters?.GetActive()?.Name;
            if (string.IsNullOrWhiteSpace(characterName))
                characterName = store.Get()?.UnitName;

            var newLocalLoadout = new LocalLoadout
            {
                UnitName = ApplicationSettings.SanitizeNickname(characterName),
                PassiveId = GetSelectedId(LoadoutSlot.Passive) ?? string.Empty,
                Normal1Id = GetSelectedId(LoadoutSlot.Normal1) ?? string.Empty,
                Normal2Id = GetSelectedId(LoadoutSlot.Normal2) ?? string.Empty,
                Normal3Id = GetSelectedId(LoadoutSlot.Normal3) ?? string.Empty,
                UltimateId = GetSelectedId(LoadoutSlot.Ultimate) ?? string.Empty
            };

            store.Set(newLocalLoadout);
        }
    }
}
