using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vland.UI
{
    public enum LoadoutSlot
    {
        Weapon,
        Passive,
        Normal1,
        Normal2,
        Normal3,
        Ultimate
    }

    public struct LoadoutItem
    {
        public string id;         // skill/weapon name
        public string name;       // display name
        public Texture2D icon;    // icon texture
        public LoadoutSlot slot;  // compatible slot type
    }

    [UxmlElement]
    public partial class LoadoutWindow : VisualElement
    {
        // UXML attributes for sizes
        [SerializeField, DontCreateProperty]
        private int _columns = 4;
        [UxmlAttribute, CreateProperty]
        public int Columns { get => _columns; set { _columns = Mathf.Max(1, value); ApplyGridStyle(); } }

        [SerializeField, DontCreateProperty]
        private int _rowBeforeScroll = 2;
        [UxmlAttribute, CreateProperty]
        public int RowsBeforeScroll { get => _rowBeforeScroll; set { _rowBeforeScroll = Mathf.Max(1, value); ApplyGridStyle(); } }

        [SerializeField, DontCreateProperty]
        private Vector2 _tileSize = new Vector2(72, 72);
        [UxmlAttribute, CreateProperty]
        public Vector2 TileSize { get => _tileSize; set { _tileSize = value; ApplyGridStyle(); } }

        private VisualElement _slotsBar;
        private ScrollView _scroll;
        private VisualElement _grid;
        private Label _subheading;

        private readonly Dictionary<LoadoutSlot, LoadoutTile> _selectedForSlot = new();
        private readonly Dictionary<LoadoutSlot, VisualElement> _slotContainers = new();
        private readonly Dictionary<LoadoutSlot, string> _slotDefaultLabels = new();

        public event Action<LoadoutSlot, string> OnSelectionChanged; // (slot, id)
        public event Action<LoadoutSlot> OnActiveSlotChanged; // notify controller to refresh grid

        public LoadoutWindow()
        {
            AddToClassList("loadout-window");

            // Top slot bar: 1 weapon, 1 passive, 3 normal, 1 ultimate
            _slotsBar = new VisualElement { name = "slotsBar" };
            _slotsBar.AddToClassList("slots-bar");
            Add(_slotsBar);

            // Scrollable grid container
            _subheading = new Label();
            _subheading.AddToClassList("loadout-subheading");
            Add(_subheading);

            _scroll = new ScrollView(ScrollViewMode.Vertical) { name = "scroll" };
            _scroll.AddToClassList("loadout-scroll");
            Add(_scroll);

            _grid = new VisualElement { name = "grid" };
            _grid.AddToClassList("loadout-grid");
            _scroll.Add(_grid);

            BuildSlotBar();
            ApplyGridStyle();

            // Ensure Weapon is active when the window first opens (after attaching to panel)
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
        }

        private bool _didInitActive;
        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            if (_didInitActive) return;
            _didInitActive = true;
            // Schedule to next tick so external listeners (controller) are subscribed
            this.schedule.Execute(() =>
            {
                SetActiveSlot(LoadoutSlot.Weapon);
                UpdateSubheading();
            }).ExecuteLater(0);
        }

        private void BuildSlotBar()
        {
            _slotsBar.Clear();
            AddSlot(LoadoutSlot.Passive, "Passive");
            AddSlot(LoadoutSlot.Weapon, "Weapon");
            AddSlot(LoadoutSlot.Normal1, "Skill 1");
            AddSlot(LoadoutSlot.Normal2, "Skill 2");
            AddSlot(LoadoutSlot.Normal3, "Skill 3");
            AddSlot(LoadoutSlot.Ultimate, "Ultimate");
        }

        private void AddSlot(LoadoutSlot slot, string label)
        {
            var container = new VisualElement();
            container.AddToClassList("slot");
            var icon = new VisualElement();
            icon.AddToClassList("slot__icon");
            var text = new Label(label);
            text.AddToClassList("slot__label");
            container.Add(icon);
            container.Add(text);
            _slotsBar.Add(container);

            // cache a tile-like placeholder to update preview when selection changes
            var tile = new LoadoutTile { DisplayName = label };
            _selectedForSlot[slot] = tile;
            _slotDefaultLabels[slot] = label;
            container.userData = slot;
            _slotContainers[slot] = container;
            container.RegisterCallback<ClickEvent>(_ =>
            {
                SetActiveSlot(slot);
            });
        }

        private void ApplyGridStyle()
        {
            if (_grid == null) return;
            int cols = Mathf.Max(1, _columns);
            // Slightly reduce percent to avoid 1px wrap due to scrollbar/rounding and guarantee 4 per row
            float epsilon = 0.2f; // percent points to shave off collectively
            float percent = Mathf.Max(0f, (100f / cols) - (epsilon / cols));

            _grid.style.flexDirection = FlexDirection.Row;
            _grid.style.flexWrap = Wrap.Wrap;
            _grid.style.justifyContent = Justify.FlexStart;
            _grid.style.alignContent = Align.FlexStart;

            foreach (var child in _grid.Children())
            {
                child.style.flexBasis = new StyleLength(Length.Percent(percent));
                child.style.width = StyleKeyword.Auto; // let basis control width
                // no fixed height; tiles square themselves on geometry change
                // no external horizontal margins; use internal tile padding for uniform gaps
                child.style.marginRight = 0;
                child.style.marginBottom = 0; // use internal tile padding for uniform vertical gaps
                child.style.flexShrink = 0;
            }

            // height of 2 rows + gaps -> enable scroll after
            float rowsVisible = Mathf.Max(1, _rowBeforeScroll);
            // vertical gaps are created via tile inner padding, not layout margins
            float visibleHeight = rowsVisible * (_tileSize.y) + 8; // padding
            _scroll.style.maxHeight = visibleHeight;
        }

        public void SetItems(IEnumerable<LoadoutItem> items, LoadoutSlot filter)
        {
            _grid.Clear();
            _currentFilter = filter;
            if (items == null) return;
            foreach (var it in items)
            {
                if (it.slot != filter) continue;
                var tile = new LoadoutTile
                {
                    Icon = it.icon,
                    DisplayName = it.name,
                    Id = it.id
                };
                tile.Clicked += OnTileClicked;
                _grid.Add(tile);
            }
            ApplyGridStyle();
            HighlightSelectionInGridForActiveSlot();
        }

        private void OnTileClicked(LoadoutTile tile)
        {
            // Which slot is active? We can expose an API to set current target slot; for simplicity, default to first empty, else last used.
            var target = GetActiveSlot();
            if (!IsCompatible(_currentFilter, target))
            {
                // ignore mismatched assignment
                // optional: could flash a feedback element here
                return;
            }
            // Swap logic (especially for normal skills): if this id is already in another slot of same group, swap selections
            var newId = tile.Id;
            var groupSlots = GetGroupSlots(target);
            LoadoutSlot? otherWithSame = null;
            foreach (var s in groupSlots)
            {
                if (s.Equals(target)) continue;
                if (GetSelectedId(s) == newId)
                {
                    otherWithSame = s;
                    break;
                }
            }

            if (otherWithSame.HasValue)
            {
                var other = otherWithSame.Value;
                var currentTargetId = GetSelectedId(target);

                // assign newId to target (clicked tile)
                SelectForSlot(target, tile);
                OnSelectionChanged?.Invoke(target, newId);

                if (!string.IsNullOrEmpty(currentTargetId))
                {
                    // find tile for the previous target id within current grid (same filter group)
                    LoadoutTile prevTile = null;
                    foreach (var child in _grid.Children())
                    {
                        if (child is LoadoutTile t && t.Id == currentTargetId)
                        {
                            prevTile = t;
                            break;
                        }
                    }

                    if (prevTile != null)
                    {
                        SelectForSlot(other, prevTile);
                        OnSelectionChanged?.Invoke(other, currentTargetId);
                    }
                    else
                    {
                        // If not found in current grid (shouldn't happen for normals), fallback to label-only update
                        SetSlotPreviewLabel(other, currentTargetId);
                        _selectedForSlot[other] = new LoadoutTile { Id = currentTargetId, DisplayName = currentTargetId, Icon = null };
                        OnSelectionChanged?.Invoke(other, currentTargetId);
                    }
                }
                else
                {
                    // Moving selection from 'other' to target, clear the other slot
                    ClearSlotSelection(other);
                    OnSelectionChanged?.Invoke(other, null);
                }

                // Keep highlight for active slot only
                HighlightSelectionInGridForActiveSlot();
            }
            else
            {
                // simple assignment when not duplicating
                SelectForSlot(target, tile);
                OnSelectionChanged?.Invoke(target, tile.Id);
                HighlightSelectionInGridForActiveSlot();
            }
        }

        private LoadoutSlot _lastActive = LoadoutSlot.Weapon;
        public void SetActiveSlot(LoadoutSlot slot)
        {
            _lastActive = slot;
            UpdateActiveSlotVisuals();
            OnActiveSlotChanged?.Invoke(_lastActive);
            UpdateSubheading();
            HighlightSelectionInGridForActiveSlot();
        }

        private LoadoutSlot GetActiveSlot() => _lastActive;

        private void UpdateActiveSlotVisuals()
        {
            foreach (var kv in _slotContainers)
            {
                kv.Value.EnableInClassList("active", kv.Key == _lastActive);
            }
        }

        private void UpdateSubheading()
        {
            if (_subheading == null) return;
            string text = _lastActive switch
            {
                LoadoutSlot.Weapon => "Select Weapon:",
                LoadoutSlot.Passive => "Select Passive Skill:",
                LoadoutSlot.Normal1 => "Select Normal Skill 1:",
                LoadoutSlot.Normal2 => "Select Normal Skill 2:",
                LoadoutSlot.Normal3 => "Select Normal Skill 3:",
                LoadoutSlot.Ultimate => "Select Ultimate:",
                _ => "Select"
            };
            _subheading.text = text;
        }

        public void SelectForSlot(LoadoutSlot slot, LoadoutTile tile)
        {
            // unselect others in grid
            foreach (var child in _grid.Children())
            {
                if (child is LoadoutTile t) t.Selected = false;
            }
            tile.Selected = true;

            // update slot preview icon/label
            // slotsBar children map order: 0 Weapon,1 Passive,2 N1,3 N2,4 N3,5 Ult
            int idx = slot switch
            {
                LoadoutSlot.Passive => 0,
                LoadoutSlot.Weapon => 1,
                LoadoutSlot.Normal1 => 2,
                LoadoutSlot.Normal2 => 3,
                LoadoutSlot.Normal3 => 4,
                LoadoutSlot.Ultimate => 5,
                _ => 0
            };
            if (idx >= 0 && idx < _slotsBar.childCount)
            {
                var cont = _slotsBar[idx];
                var icon = cont.Q<VisualElement>(className: "slot__icon");
                var label = cont.Q<Label>(className: "slot__label");
                if (icon != null) icon.style.backgroundImage = tile.Icon != null ? new StyleBackground(tile.Icon) : StyleKeyword.None;
                if (label != null) label.text = tile.DisplayName;
            }
            _selectedForSlot[slot] = tile;
        }

        public string GetSelectedId(LoadoutSlot slot) => _selectedForSlot.TryGetValue(slot, out var t) ? t?.Id : null;

        // Programmatic selection by id (used for initializing from saved loadout)
        // Does not fire OnSelectionChanged (reserved for user interactions)
        public void SelectById(LoadoutSlot slot, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                ClearSlotSelection(slot);
                // If this is the active slot, ensure grid highlight clears too
                HighlightSelectionInGridForActiveSlot();
                return;
            }

            // Update internal selection state and preview label immediately
            _selectedForSlot[slot] = new LoadoutTile { Id = id, DisplayName = id, Icon = null };
            SetSlotPreviewLabel(slot, id);

            // If this slot is currently active and the grid contains the tile, highlight it
            if (GetActiveSlot() == slot && IsCompatible(_currentFilter, slot))
            {
                HighlightSelectionInGridForActiveSlot();
            }
        }

        // ---------- internals ----------
        private LoadoutSlot _currentFilter = LoadoutSlot.Weapon;

        private static bool IsCompatible(LoadoutSlot filter, LoadoutSlot target)
        {
            if (filter == LoadoutSlot.Weapon) return target == LoadoutSlot.Weapon;
            if (filter == LoadoutSlot.Passive) return target == LoadoutSlot.Passive;
            if (filter == LoadoutSlot.Ultimate) return target == LoadoutSlot.Ultimate;
            // Normals: filter is Normal1 for all normals in our usage
            bool filterIsNormal = filter == LoadoutSlot.Normal1 || filter == LoadoutSlot.Normal2 || filter == LoadoutSlot.Normal3;
            bool targetIsNormal = target == LoadoutSlot.Normal1 || target == LoadoutSlot.Normal2 || target == LoadoutSlot.Normal3;
            return filterIsNormal && targetIsNormal;
        }

        private void HighlightSelectionInGridForActiveSlot()
        {
            var active = GetActiveSlot();
            if (!IsCompatible(_currentFilter, active))
                return;

            string selectedId = GetSelectedId(active);
            foreach (var child in _grid.Children())
            {
                if (child is LoadoutTile t)
                {
                    t.Selected = !string.IsNullOrEmpty(selectedId) && t.Id == selectedId;
                }
            }
        }

        private IEnumerable<LoadoutSlot> GetGroupSlots(LoadoutSlot slot)
        {
            if (slot == LoadoutSlot.Normal1 || slot == LoadoutSlot.Normal2 || slot == LoadoutSlot.Normal3)
            {
                yield return LoadoutSlot.Normal1;
                yield return LoadoutSlot.Normal2;
                yield return LoadoutSlot.Normal3;
                yield break;
            }

            // single-slot groups
            yield return slot;
        }

        private void ClearSlotSelection(LoadoutSlot slot)
        {
            // Reset slot preview to default label and no icon
            if (_slotContainers.TryGetValue(slot, out var cont))
            {
                var icon = cont.Q<VisualElement>(className: "slot__icon");
                var label = cont.Q<Label>(className: "slot__label");
                if (icon != null) icon.style.backgroundImage = StyleKeyword.None;
                if (label != null && _slotDefaultLabels.TryGetValue(slot, out var defLabel)) label.text = defLabel;
            }
            _selectedForSlot[slot] = new LoadoutTile { Id = null, DisplayName = _slotDefaultLabels.TryGetValue(slot, out var l) ? l : string.Empty, Icon = null };
        }

        private void SetSlotPreviewLabel(LoadoutSlot slot, string text)
        {
            if (_slotContainers.TryGetValue(slot, out var cont))
            {
                var label = cont.Q<Label>(className: "slot__label");
                if (label != null) label.text = text ?? (_slotDefaultLabels.TryGetValue(slot, out var def) ? def : string.Empty);
            }
        }
    }
}
