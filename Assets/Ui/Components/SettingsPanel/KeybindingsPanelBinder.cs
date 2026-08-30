using System;
using System.Collections.Generic;
using ShadowInfection.Input;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.SettingsPanel
{
    public sealed class KeybindingsPanelBinder
    {
        private readonly SettingsPanelView view;
        private readonly Dictionary<string, BindSlotElement> slots = new();
        private readonly Dictionary<PlayerActionId, QuickCastToggle> quickCastToggles = new();

        private IInputBindingSession session;
        private IInputBindingCommands commands;
        private IVisualElementScheduledItem tick;
        private EventCallback<PointerDownEvent> pointerDownCallback;
        private string lastLayoutSignature;
        private int lastRejectPulse = -1;
        private bool isBound;
        private bool rebuilding;

        public KeybindingsPanelBinder(SettingsPanelView view)
        {
            this.view = view;
        }

        public void Bind(IInputBindingSession nextSession, IInputBindingCommands nextCommands)
        {
            Unbind();
            session = nextSession;
            commands = nextCommands;
            if (view?.Root == null || session == null || commands == null)
                return;

            if (view.KeybindingsConflictSwap != null)
                view.KeybindingsConflictSwap.clicked += OnSwapClicked;
            if (view.KeybindingsConflictCancel != null)
                view.KeybindingsConflictCancel.clicked += OnConflictCancelClicked;

            pointerDownCallback = OnRootPointerDown;
            view.Root.RegisterCallback(pointerDownCallback, TrickleDown.NoTrickleDown);
            view.TabChanged += OnTabChanged;

            RebuildFromSnapshot();
            tick = view.Root.schedule.Execute(OnTick).Every(16);
            isBound = true;
        }

        public void Unbind()
        {
            if (!isBound)
                return;

            tick?.Pause();
            tick = null;
            commands?.CancelListen();

            if (view?.Root != null && pointerDownCallback != null)
                view.Root.UnregisterCallback(pointerDownCallback);

            if (view != null)
            {
                view.TabChanged -= OnTabChanged;
                if (view.KeybindingsConflictSwap != null)
                    view.KeybindingsConflictSwap.clicked -= OnSwapClicked;
                if (view.KeybindingsConflictCancel != null)
                    view.KeybindingsConflictCancel.clicked -= OnConflictCancelClicked;
            }

            session = null;
            commands = null;
            lastLayoutSignature = null;
            isBound = false;
        }

        public void Refresh()
        {
            lastLayoutSignature = null;
            RebuildFromSnapshot();
        }

        public bool HandleResetIfActive()
        {
            if (view == null || view.ActiveTab != SettingsTab.Keybindings || commands == null)
                return false;

            commands.ResetToDefaults();
            Refresh();
            return true;
        }

        private void OnTabChanged(SettingsTab tab)
        {
            if (tab != SettingsTab.Keybindings)
                commands?.CancelListen();
            else
                Refresh();
        }

        private void OnTick()
        {
            if (view == null || view.ActiveTab != SettingsTab.Keybindings)
                return;
            RebuildFromSnapshot();
        }

        private void RebuildFromSnapshot()
        {
            if (session == null || !session.TryGetSnapshot(out var snapshot))
                return;

            var layoutSignature = LayoutSignature(snapshot);
            if (layoutSignature == lastLayoutSignature)
            {
                SyncQuickCastToggles(snapshot);
                ApplyListeningAndConflict(snapshot);
                return;
            }

            lastLayoutSignature = layoutSignature;
            rebuilding = true;
            BuildRows(snapshot);
            ApplyListeningAndConflict(snapshot);
            rebuilding = false;
        }

        private void BuildRows(InputBindingsSnapshot snapshot)
        {
            var list = view.KeybindingsList;
            if (list == null)
                return;

            list.Clear();
            slots.Clear();
            quickCastToggles.Clear();

            var lastGroup = (PlayerActionGroup)(-1);
            var rows = snapshot.Rows;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Group != lastGroup)
                {
                    lastGroup = row.Group;
                    list.Add(new Label(GroupLabel(row.Group)) { pickingMode = PickingMode.Ignore });
                    list[list.childCount - 1].AddToClassList("keybind-section");
                }

                list.Add(CreateRow(row, snapshot));
            }
        }

        private VisualElement CreateRow(InputActionRowSnapshot row, InputBindingsSnapshot snapshot)
        {
            var element = new VisualElement();
            element.AddToClassList("keybind-row");

            var label = new Label(row.Label);
            label.AddToClassList("keybind-row__label");
            label.pickingMode = PickingMode.Ignore;
            element.Add(label);

            element.Add(CreateSlot(row, InputBindingSlot.Primary, row.PrimaryLabel, snapshot));
            element.Add(CreateSlot(row, InputBindingSlot.Secondary, row.SecondaryLabel, snapshot));
            element.Add(CreateQuickCastCell(row));
            return element;
        }

        private VisualElement CreateQuickCastCell(InputActionRowSnapshot row)
        {
            var cell = new VisualElement();
            cell.AddToClassList("keybind-quick-cell");
            if (!row.HasQuickCast)
            {
                cell.pickingMode = PickingMode.Ignore;
                return cell;
            }

            var toggle = new QuickCastToggle();
            toggle.SetValueWithoutNotify(row.QuickCast);
            var id = row.Id;
            toggle.ValueChanged += enabled =>
            {
                if (rebuilding)
                    return;
                commands?.SetQuickCast(id, enabled);
            };
            quickCastToggles[row.Id] = toggle;
            cell.Add(toggle);
            return cell;
        }

        private BindSlotElement CreateSlot(
            InputActionRowSnapshot row,
            InputBindingSlot slot,
            string label,
            InputBindingsSnapshot snapshot)
        {
            var button = new BindSlotElement(row.Id, slot);
            button.text = label;
            button.AddToClassList("keybind-slot");
            if (label == InputBindingDisplay.EmptySlot)
                button.AddToClassList("keybind-slot--empty");

            var listening = snapshot.IsListening
                && snapshot.ListeningAction == row.Id
                && snapshot.ListeningSlot == slot;
            if (listening)
            {
                button.text = "Press a key";
                button.AddToClassList("keybind-slot--listening");
                button.RemoveFromClassList("keybind-slot--empty");
            }

            button.clicked += () =>
            {
                if (snapshot.HasConflict)
                    return;
                commands?.BeginListen(row.Id, slot);
                lastLayoutSignature = null;
            };
            button.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 1)
                    return;
                evt.StopImmediatePropagation();
                if (commands == null)
                    return;
                if (!commands.TryClear(row.Id, slot))
                    FlashReject(button);
                else
                    lastLayoutSignature = null;
            });

            slots[SlotKey(row.Id, slot)] = button;
            return button;
        }

        private void ApplyListeningAndConflict(InputBindingsSnapshot snapshot)
        {
            if (snapshot.ListenRejectPulse != lastRejectPulse)
            {
                lastRejectPulse = snapshot.ListenRejectPulse;
                if (snapshot.IsListening)
                {
                    var key = SlotKey(snapshot.ListeningAction, snapshot.ListeningSlot);
                    if (slots.TryGetValue(key, out var slot))
                        FlashReject(slot);
                }
            }

            var conflict = view.KeybindingsConflict;
            if (conflict == null)
                return;

            conflict.style.display = snapshot.HasConflict ? DisplayStyle.Flex : DisplayStyle.None;
            if (view.KeybindingsConflictText != null && snapshot.HasConflict)
            {
                view.KeybindingsConflictText.text =
                    $"{snapshot.ConflictKeyLabel} is bound to {snapshot.ConflictActionLabel}. Swap keys?";
            }
        }

        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (commands == null || view.ActiveTab != SettingsTab.Keybindings)
                return;
            if (IsBindControl(evt.target as VisualElement))
                return;
            commands.CancelListen();
            lastLayoutSignature = null;
        }

        private void OnSwapClicked()
        {
            commands?.ConfirmSwap();
            lastLayoutSignature = null;
        }

        private void OnConflictCancelClicked()
        {
            commands?.DismissConflict();
            lastLayoutSignature = null;
        }

        private static void FlashReject(VisualElement slot)
        {
            if (slot == null)
                return;
            slot.AddToClassList("keybind-slot--reject");
            slot.schedule.Execute(() => slot.RemoveFromClassList("keybind-slot--reject")).StartingIn(280);
        }

        private static bool IsBindControl(VisualElement target)
        {
            for (var current = target; current != null; current = current.parent)
            {
                if (current.ClassListContains("keybind-slot")
                    || current.ClassListContains("keybind-conflict-dialog")
                    || current.ClassListContains("keybind-quick")
                    || current.ClassListContains("keybind-quick-cell"))
                    return true;
            }

            return false;
        }

        private void SyncQuickCastToggles(InputBindingsSnapshot snapshot)
        {
            var rows = snapshot.Rows;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (!row.HasQuickCast || !quickCastToggles.TryGetValue(row.Id, out var toggle))
                    continue;
                if (toggle.Value != row.QuickCast)
                    toggle.SetValueWithoutNotify(row.QuickCast);
            }
        }

        private static string LayoutSignature(InputBindingsSnapshot snapshot)
        {
            var builder = new System.Text.StringBuilder();
            builder.Append(snapshot.IsListening ? '1' : '0');
            builder.Append('|');
            builder.Append((int)snapshot.ListeningAction);
            builder.Append('|');
            builder.Append((int)snapshot.ListeningSlot);
            builder.Append('|');
            builder.Append(snapshot.HasConflict ? '1' : '0');
            builder.Append('|');
            builder.Append(snapshot.ListenRejectPulse);
            builder.Append('|');
            builder.Append(snapshot.ConflictKeyLabel);
            builder.Append('|');
            builder.Append(snapshot.ConflictActionLabel);
            var rows = snapshot.Rows;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                builder.Append('|');
                builder.Append((int)row.Id);
                builder.Append(':');
                builder.Append(row.PrimaryLabel);
                builder.Append('/');
                builder.Append(row.SecondaryLabel);
            }

            return builder.ToString();
        }

        private static string SlotKey(PlayerActionId id, InputBindingSlot slot)
        {
            return ((int)id) + ":" + ((int)slot);
        }

        private static string GroupLabel(PlayerActionGroup group)
        {
            return group switch
            {
                PlayerActionGroup.Movement => "MOVEMENT",
                PlayerActionGroup.Combat => "COMBAT",
                PlayerActionGroup.Modifiers => "MODIFIERS",
                PlayerActionGroup.World => "WORLD",
                PlayerActionGroup.Camera => "CAMERA",
                PlayerActionGroup.Interface => "INTERFACE",
                _ => group.ToString().ToUpperInvariant()
            };
        }

        private sealed class QuickCastToggle : VisualElement
        {
            public bool Value { get; private set; }
            public event Action<bool> ValueChanged;

            public QuickCastToggle()
            {
                AddToClassList("keybind-quick");
                focusable = false;

                var box = new VisualElement { pickingMode = PickingMode.Ignore };
                box.AddToClassList("keybind-quick__box");
                Add(box);

                var label = new Label("Quick Cast") { pickingMode = PickingMode.Ignore };
                label.AddToClassList("keybind-quick__label");
                Add(label);

                RegisterCallback<PointerDownEvent>(OnPointerDown);
            }

            public void SetValueWithoutNotify(bool value)
            {
                Value = value;
                EnableInClassList("keybind-quick--on", value);
            }

            private void OnPointerDown(PointerDownEvent evt)
            {
                evt.StopPropagation();
                if (evt.button != 0)
                    return;
                SetValueWithoutNotify(!Value);
                ValueChanged?.Invoke(Value);
            }
        }

        private sealed class BindSlotElement : Button
        {
            public PlayerActionId ActionId { get; }
            public InputBindingSlot Slot { get; }

            public BindSlotElement(PlayerActionId actionId, InputBindingSlot slot)
            {
                ActionId = actionId;
                Slot = slot;
                focusable = false;
            }
        }
    }
}
