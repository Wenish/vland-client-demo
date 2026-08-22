using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePipe;
using MyGame.Events.Ui;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;
using Vland.UI;

[DefaultExecutionOrder(100)]
public class LoadoutWindowController : MonoBehaviour
{
    private const int LoadoutSortingOrder = 60;

    public UIDocument uiDocument;
    public VisualTreeAsset loadoutPanelUxml;
    public StyleSheet loadoutWindowUss;
    public StyleSheet loadoutPanelUss;

    private LoadoutView _view;
    private LoadoutSlot _activeSlot = LoadoutSlot.Weapon;
    private SkillTag? _tagFilter;
    private readonly Dictionary<LoadoutSlot, LoadoutItem> _selected = new();
    private bool _applyPending;
    private ISubscriber<SetLoadoutWindowOpenEvent> setOpen;
    private R3.DisposableBag subscriptions;

    private DatabaseManager _db => DatabaseManager.Instance;
    private LoadoutManager _loadoutManager => LoadoutManager.Instance;

    [Inject]
    internal void Construct(ISubscriber<SetLoadoutWindowOpenEvent> injectedSetOpen)
    {
        setOpen = injectedSetOpen;
    }

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("LoadoutWindowController: UIDocument missing.");
            return;
        }

        uiDocument.sortingOrder = LoadoutSortingOrder;

        var root = uiDocument.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        if (loadoutWindowUss != null)
            root.styleSheets.Add(loadoutWindowUss);
        if (loadoutPanelUss != null)
            root.styleSheets.Add(loadoutPanelUss);
        if (loadoutPanelUxml != null)
            loadoutPanelUxml.CloneTree(root);

        var loadoutRoot = root.Q<VisualElement>("loadoutRoot") ?? root;
        UiCursorRefresh.ScheduleForRoot(loadoutRoot, LoadoutSortingOrder);

        _view = new LoadoutView(loadoutRoot);
        _view.CloseClicked += () => _view.SetOpen(false);
        _view.OpenClicked += () => _view.SetOpen(true);
        _view.OverlayClicked += () => _view.SetOpen(false);
        _view.SlotClicked += HandleSlotClicked;
        _view.ItemClicked += HandleItemClicked;
        _view.FilterClicked += HandleFilterClicked;

        foreach (var slot in LoadoutSlots.All)
            _selected[slot] = LoadoutItem.Empty;

        TryInitializeFromSavedLoadout();
        _view.SetActiveSlot(_activeSlot);
        _view.SetFilter(_tagFilter, _activeSlot != LoadoutSlot.Weapon);
        ClearIncompatibleSkillSelections();
        RefreshList();
        ApplyCurrentLoadout();
    }

    public void SetOpen(bool open)
    {
        _view?.SetOpen(open);
    }

    private void OnEnable()
    {
        subscriptions.Dispose();
        subscriptions = new R3.DisposableBag();
        if (setOpen != null)
            subscriptions.Add(setOpen.Subscribe(OnSetOpen));
    }

    private void OnDisable()
    {
        subscriptions.Dispose();
        subscriptions = new R3.DisposableBag();
    }

    private void OnSetOpen(SetLoadoutWindowOpenEvent evt)
    {
        SetOpen(evt != null && evt.IsOpen);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.iKey.wasPressedThisFrame)
            return;
        if (_view == null)
            return;

        _view.SetOpen(!_view.IsOpen);
    }

    private void OnDestroy()
    {
        subscriptions.Dispose();
        _view?.Dispose();
    }

    private void HandleSlotClicked(LoadoutSlot slot)
    {
        _activeSlot = slot;
        _view.SetActiveSlot(slot);
        _view.SetFilter(_tagFilter, slot != LoadoutSlot.Weapon);
        RefreshList();
    }

    private void HandleFilterClicked(SkillTag? tag)
    {
        _tagFilter = tag;
        _view.SetFilter(_tagFilter, _activeSlot != LoadoutSlot.Weapon);
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

        if (LoadoutSlots.IsNormal(_activeSlot))
        {
            LoadoutSlot? otherWithSame = null;
            foreach (var slot in LoadoutSlots.All)
            {
                if (!LoadoutSlots.IsNormal(slot) || slot == _activeSlot)
                    continue;
                if (_selected.TryGetValue(slot, out var existing) && existing.id == id)
                {
                    otherWithSame = slot;
                    break;
                }
            }

            if (otherWithSame.HasValue)
            {
                var other = otherWithSame.Value;
                var previous = _selected[_activeSlot];
                AssignSlot(_activeSlot, clicked);
                if (previous.HasId)
                    AssignSlot(other, previous);
                else
                    AssignSlot(other, LoadoutItem.Empty);

                ScheduleApply();
                RefreshList();
                return;
            }
        }

        AssignSlot(_activeSlot, clicked);

        if (_activeSlot == LoadoutSlot.Weapon)
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
        if (_loadoutManager == null)
            return;

        var saved = _loadoutManager.Get();
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
        _selected[slot] = item.HasId ? item : LoadoutItem.Empty;
        _view?.SetSlot(slot, _selected[slot]);
    }

    private void RefreshList()
    {
        if (_view == null)
            return;

        var items = BuildItemsForActiveSlot();
        var selectedId = GetSelectedId(_activeSlot);
        _view.SetItems(items, selectedId, BuildEmptyMessage());
        _view.SetSubheading(BuildSubheading(items.Count));
        _view.SetFilter(_tagFilter, _activeSlot != LoadoutSlot.Weapon);
    }

    private List<LoadoutItem> BuildItemsForActiveSlot()
    {
        var items = new List<LoadoutItem>();
        if (_db == null)
            return items;

        var selectedWeaponType = GetSelectedWeaponType();

        if (_activeSlot == LoadoutSlot.Weapon)
        {
            if (_db.weaponDatabase == null)
                return items;

            foreach (var weapon in _db.weaponDatabase.allWeapons.Where(weapon => weapon != null && !weapon.npcOnly))
                items.Add(ToWeaponItem(weapon));
            return items;
        }

        if (_db.skillDatabase == null)
            return items;

        var expectedType = _activeSlot == LoadoutSlot.Passive
            ? SkillType.Passive
            : _activeSlot == LoadoutSlot.Ultimate
                ? SkillType.Ultimate
                : SkillType.Normal;

        foreach (var skill in _db.skillDatabase.allSkills)
        {
            if (skill == null || skill.npcOnly || skill.skillType != expectedType)
                continue;
            if (!skill.CanBeUsedWithWeapon(selectedWeaponType))
                continue;
            if (_tagFilter.HasValue && !skill.HasTag(_tagFilter.Value))
                continue;

            items.Add(ToSkillItem(skill, expectedType == SkillType.Normal ? LoadoutSlot.Normal1 : _activeSlot));
        }

        return items;
    }

    private string BuildSubheading(int visibleCount)
    {
        var choosing = LoadoutSlots.ChoosingLabel(_activeSlot);
        if (_activeSlot == LoadoutSlot.Weapon)
            return $"{choosing} · {visibleCount} weapons";

        var typeLabel = LoadoutSlots.SlotTypeLabel(_activeSlot);
        var weapon = GetSelectedWeapon();
        var weaponText = weapon != null
            ? $"compatible with {weapon.weaponName}"
            : "that work with any weapon";
        var tagText = _tagFilter.HasValue ? $"{SkillTagUtil.GetLabel(_tagFilter.Value)} · " : string.Empty;
        return $"{choosing} · {tagText}{typeLabel} {weaponText}";
    }

    private string BuildEmptyMessage()
    {
        if (_activeSlot == LoadoutSlot.Weapon)
            return "No weapons available.";

        var weapon = GetSelectedWeapon();
        var weaponText = weapon != null ? weapon.weaponName : "this slot";
        if (_tagFilter.HasValue)
            return $"No {SkillTagUtil.GetLabel(_tagFilter.Value)} skills for this slot with {weaponText}.";

        return weapon != null
            ? $"No skills for this slot with {weapon.weaponName}."
            : "No skills for this slot. Choose a weapon to unlock more.";
    }

    private LoadoutItem MakeWeaponItem(string id)
    {
        if (string.IsNullOrEmpty(id) || _db?.weaponDatabase == null)
            return LoadoutItem.Empty;

        var weapon = _db.weaponDatabase.GetWeaponByName(id);
        return weapon != null ? ToWeaponItem(weapon) : LoadoutItem.Empty;
    }

    private LoadoutItem MakeSkillItem(string id, LoadoutSlot slot)
    {
        if (string.IsNullOrEmpty(id) || _db?.skillDatabase == null)
            return LoadoutItem.Empty;

        var skill = _db.skillDatabase.GetSkillByName(id);
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
        return _selected.TryGetValue(slot, out var item) ? item.id : null;
    }

    private WeaponData GetSelectedWeapon()
    {
        if (_db?.weaponDatabase == null)
            return null;

        var id = GetSelectedId(LoadoutSlot.Weapon);
        return string.IsNullOrWhiteSpace(id) ? null : _db.weaponDatabase.GetWeaponByName(id);
    }

    private WeaponType? GetSelectedWeaponType()
    {
        return GetSelectedWeapon()?.weaponType;
    }

    private void ClearIncompatibleSkillSelections()
    {
        if (_db?.skillDatabase == null)
            return;

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

            var skill = _db.skillDatabase.GetSkillByName(selectedId);
            if (skill == null || !skill.CanBeUsedWithWeapon(weaponType))
                AssignSlot(slot, LoadoutItem.Empty);
        }
    }

    private void ScheduleApply()
    {
        if (_applyPending)
            return;

        _applyPending = true;
        StartCoroutine(ApplyAtEndOfFrame());
    }

    private IEnumerator ApplyAtEndOfFrame()
    {
        yield return null;
        ApplyCurrentLoadout();
        _applyPending = false;
    }

    private void ApplyCurrentLoadout()
    {
        if (_loadoutManager == null)
            return;

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

        _loadoutManager.Set(newLocalLoadout);
    }
}
