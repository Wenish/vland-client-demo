using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MessagePipe;
using MyGame.Events.Ui;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using Vland.UI;

namespace ShadowInfection.UI.LoadoutWindow
{
    internal sealed class LoadoutWindowPresenter
    {
        private readonly ILoadoutStore store;
        private readonly ILoadoutCatalog catalog;
        private readonly ISubscriber<SetLoadoutWindowOpenEvent> setOpen;

        private LoadoutView view;
        private R3.DisposableBag subscriptions;
        private CancellationToken bindToken;
        private LoadoutSlot activeSlot = LoadoutSlot.Weapon;
        private SkillTag? tagFilter;
        private readonly Dictionary<LoadoutSlot, LoadoutItem> selected = new();
        private bool applyPending;

        public LoadoutWindowPresenter(
            ILoadoutStore store,
            ILoadoutCatalog catalog,
            ISubscriber<SetLoadoutWindowOpenEvent> setOpen)
        {
            this.store = store;
            this.catalog = catalog;
            this.setOpen = setOpen;
        }

        public void Bind(LoadoutView nextView, CancellationToken token)
        {
            Unbind();
            view = nextView;
            bindToken = token;
            if (view == null)
                return;

            foreach (var slot in LoadoutSlots.All)
                selected[slot] = LoadoutItem.Empty;

            view.CloseClicked += Close;
            view.OpenClicked += Open;
            view.OverlayClicked += Close;
            view.SlotClicked += HandleSlotClicked;
            view.ItemClicked += HandleItemClicked;
            view.FilterClicked += HandleFilterClicked;

            TryInitializeFromSavedLoadout();
            view.SetActiveSlot(activeSlot);
            view.SetFilter(tagFilter, activeSlot != LoadoutSlot.Weapon);
            ClearIncompatibleSkillSelections();
            RefreshList();
            ApplyCurrentLoadout();

            subscriptions.Add(setOpen.Subscribe(OnSetOpen));
            subscriptions.Add(
                Observable.EveryUpdate(UnityFrameProvider.Update, token)
                    .Subscribe(_ => TickToggle()));
        }

        public void Unbind()
        {
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
            activeSlot = LoadoutSlot.Weapon;
            tagFilter = null;
            selected.Clear();
        }

        private void Open()
        {
            view?.SetOpen(true);
        }

        private void Close()
        {
            view?.SetOpen(false);
        }

        private void OnSetOpen(SetLoadoutWindowOpenEvent evt)
        {
            view?.SetOpen(evt != null && evt.IsOpen);
        }

        private void TickToggle()
        {
            if (view == null || Keyboard.current == null || !Keyboard.current.iKey.wasPressedThisFrame)
                return;

            view.SetOpen(!view.IsOpen);
        }

        private void HandleSlotClicked(LoadoutSlot slot)
        {
            activeSlot = slot;
            view.SetActiveSlot(slot);
            view.SetFilter(tagFilter, slot != LoadoutSlot.Weapon);
            RefreshList();
        }

        private void HandleFilterClicked(SkillTag? tag)
        {
            tagFilter = tag;
            view.SetFilter(tagFilter, activeSlot != LoadoutSlot.Weapon);
            RefreshList();
        }

        private void HandleItemClicked(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            var items = BuildItemsForActiveSlot();
            var clicked = items.FirstOrDefault(item => item.id == id);
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

            if (activeSlot == LoadoutSlot.Weapon)
            {
                ClearIncompatibleSkillSelections();
                RefreshList();
            }
            else
            {
                RefreshList();
            }

            ScheduleApply();
        }

        private void TryInitializeFromSavedLoadout()
        {
            var saved = store.Get();
            if (saved == null)
                return;

            AssignSlot(LoadoutSlot.Weapon, MakeWeaponItem(saved.WeaponId));
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

            var items = BuildItemsForActiveSlot();
            var selectedId = GetSelectedId(activeSlot);
            view.SetItems(items, selectedId, BuildEmptyMessage());
            view.SetSubheading(BuildSubheading(items.Count));
            view.SetFilter(tagFilter, activeSlot != LoadoutSlot.Weapon);
        }

        private List<LoadoutItem> BuildItemsForActiveSlot()
        {
            var items = new List<LoadoutItem>();
            var selectedWeaponType = GetSelectedWeaponType();

            if (activeSlot == LoadoutSlot.Weapon)
            {
                var weapons = catalog.GetPlayerWeapons();
                for (var i = 0; i < weapons.Count; i++)
                    items.Add(ToWeaponItem(weapons[i]));
                return items;
            }

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
                if (!skill.CanBeUsedWithWeapon(selectedWeaponType))
                    continue;
                if (tagFilter.HasValue && !skill.HasTag(tagFilter.Value))
                    continue;

                items.Add(ToSkillItem(skill, expectedType == SkillType.Normal ? LoadoutSlot.Normal1 : activeSlot));
            }

            return items;
        }

        private string BuildSubheading(int visibleCount)
        {
            var choosing = LoadoutSlots.ChoosingLabel(activeSlot);
            if (activeSlot == LoadoutSlot.Weapon)
                return $"{choosing} · {visibleCount} weapons";

            var typeLabel = LoadoutSlots.SlotTypeLabel(activeSlot);
            var weapon = GetSelectedWeapon();
            var weaponText = weapon != null
                ? $"compatible with {weapon.weaponName}"
                : "that work with any weapon";
            var tagText = tagFilter.HasValue ? $"{SkillTagUtil.GetLabel(tagFilter.Value)} · " : string.Empty;
            return $"{choosing} · {tagText}{typeLabel} {weaponText}";
        }

        private string BuildEmptyMessage()
        {
            if (activeSlot == LoadoutSlot.Weapon)
                return "No weapons available.";

            var weapon = GetSelectedWeapon();
            var weaponText = weapon != null ? weapon.weaponName : "this slot";
            if (tagFilter.HasValue)
                return $"No {SkillTagUtil.GetLabel(tagFilter.Value)} skills for this slot with {weaponText}.";

            return weapon != null
                ? $"No skills for this slot with {weapon.weaponName}."
                : "No skills for this slot. Choose a weapon to unlock more.";
        }

        private LoadoutItem MakeWeaponItem(string id)
        {
            var weapon = catalog.GetWeapon(id);
            return weapon != null ? ToWeaponItem(weapon) : LoadoutItem.Empty;
        }

        private LoadoutItem MakeSkillItem(string id, LoadoutSlot slot)
        {
            var skill = catalog.GetSkill(id);
            return skill != null ? ToSkillItem(skill, slot) : LoadoutItem.Empty;
        }

        private static LoadoutItem ToWeaponItem(WeaponData weapon)
        {
            return new LoadoutItem
            {
                id = weapon.weaponName,
                name = weapon.weaponName,
                icon = weapon.iconTexture,
                slot = LoadoutSlot.Weapon,
                isWeapon = true,
                summary = $"Type: {weapon.weaponType}",
                meta = $"Damage: +{weapon.attackPower} · Range: {weapon.attackRange}",
                description = $"Type: {weapon.weaponType}\nDamage: +{weapon.attackPower}\nRange: {weapon.attackRange}",
            };
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

        private WeaponData GetSelectedWeapon()
        {
            return catalog.GetWeapon(GetSelectedId(LoadoutSlot.Weapon));
        }

        private WeaponType? GetSelectedWeaponType()
        {
            return GetSelectedWeapon()?.weaponType;
        }

        private void ClearIncompatibleSkillSelections()
        {
            var weaponType = GetSelectedWeaponType();
            if (!weaponType.HasValue)
                return;

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
            var newLocalLoadout = new LocalLoadout
            {
                UnitName = ApplicationSettings.GetEffectiveNickname(ApplicationSettings.Instance?.Nickname),
                WeaponId = GetSelectedId(LoadoutSlot.Weapon) ?? string.Empty,
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
