using System;
using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ShadowInfection.Input
{
    public sealed class InputBindingsService : IInputReader, IInputBindingSession, IInputBindingCommands
    {
        private const string PrefsKey = "Input.PlayerBindings.v1";
        private const int SaveVersion = 2;

        private static readonly PlayerActionId[] MoveAndAttack =
        {
            PlayerActionId.MoveForward,
            PlayerActionId.MoveBackward,
            PlayerActionId.MoveLeft,
            PlayerActionId.MoveRight,
            PlayerActionId.Attack
        };

        private readonly IReadOnlyList<PlayerActionDefinition> definitions;
        private readonly PlayerActionDefinition[] definitionById;
        private readonly InputBindingKey[] primary;
        private readonly InputBindingKey[] secondary;
        private readonly InputBindingKey[] gamepad;
        private readonly bool[] quickCast;
        private readonly int count;
        private readonly List<InputActionRowSnapshot> rows = new();

        private int revision = 1;
        private int processedFrame = -1;
        private int listenIgnoreUntilFrame = -1;
        private int listenRejectPulse;
        private bool suppressGameplayThisFrame;
        private bool listening;
        private bool awaitingConflict;
        private PlayerActionId listenAction;
        private InputBindingSlot listenSlot;
        private InputBindingKey pendingKey;
        private PlayerActionId conflictAction;
        private InputBindingsSnapshot cachedSnapshot;
        private bool snapshotDirty = true;

        public InputBindingsService(PlayerActionCatalog catalog)
        {
            var source = catalog != null && catalog.HasActions
                ? catalog
                : PlayerActionCatalog.CreateRuntime();
            definitions = source.Actions;
            count = ComputeCount();
            definitionById = new PlayerActionDefinition[count];
            primary = new InputBindingKey[count];
            secondary = new InputBindingKey[count];
            gamepad = new InputBindingKey[count];
            quickCast = new bool[count];

            ApplyCatalogDefaults();
            LoadOverrides();
            RebuildSnapshot();
        }

        public bool IsListening => listening;

        private bool IsGameplaySuppressed =>
            listening || suppressGameplayThisFrame || UiTextInputFocus.IsBlocking;

        private bool IgnoreMouseGameplay => UiPointerState.IsPointerOverBlockingElement;

        public bool WasPressed(PlayerActionId id)
        {
            EnsureFrame();
            if (IsGameplaySuppressed)
                return false;
            return AnySlot(id, control => control != null && control.wasPressedThisFrame, IgnoreMouseGameplay);
        }

        public bool WasReleased(PlayerActionId id)
        {
            EnsureFrame();
            if (IsGameplaySuppressed)
                return false;
            return AnySlot(id, control => control != null && control.wasReleasedThisFrame, ignoreMouse: false);
        }

        public bool IsHeld(PlayerActionId id)
        {
            EnsureFrame();
            if (IsGameplaySuppressed)
                return false;
            return AnySlot(id, control => control != null && control.isPressed, ignoreMouse: false);
        }

        public bool WasMousePressed(PlayerActionId id)
        {
            EnsureFrame();
            if (IsGameplaySuppressed || IgnoreMouseGameplay)
                return false;
            return SlotPressed(id, InputBindingDevice.Mouse);
        }

        public bool WasPressedExcludingMouse(PlayerActionId id)
        {
            EnsureFrame();
            if (IsGameplaySuppressed)
                return false;
            return SlotPressed(id, InputBindingDevice.Keyboard)
                || SlotPressed(id, InputBindingDevice.Gamepad);
        }

        public bool IsQuickCast(PlayerActionId id)
        {
            var index = ToIndex(id);
            return index >= 0 && quickCast[index];
        }

        public Vector2 ReadMoveAxis()
        {
            EnsureFrame();
            if (IsGameplaySuppressed)
                return Vector2.zero;
            var x = (IsHeld(PlayerActionId.MoveRight) ? 1f : 0f) - (IsHeld(PlayerActionId.MoveLeft) ? 1f : 0f);
            var y = (IsHeld(PlayerActionId.MoveForward) ? 1f : 0f) - (IsHeld(PlayerActionId.MoveBackward) ? 1f : 0f);

            if (Gamepad.current != null)
            {
                var stick = Gamepad.current.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > Mathf.Abs(x))
                    x = stick.x;
                if (Mathf.Abs(stick.y) > Mathf.Abs(y))
                    y = stick.y;
            }

            return new Vector2(x, y);
        }

        public bool IsMoveOrAttackKeyboardKey(KeyCode keyCode)
        {
            for (var i = 0; i < MoveAndAttack.Length; i++)
            {
                if (SlotMatchesKeyCode(MoveAndAttack[i], keyCode))
                    return true;
            }

            return false;
        }

        public bool TryGetSnapshot(out InputBindingsSnapshot snapshot)
        {
            EnsureFrame();
            if (snapshotDirty)
                RebuildSnapshot();
            snapshot = cachedSnapshot;
            return true;
        }

        public bool TryGetDisplayBind(PlayerActionId id, out InputBindingKey key)
        {
            key = InputBindingKey.None;
            var index = ToIndex(id);
            if (index < 0 || definitionById[index] == null)
                return false;
            if (!primary[index].IsEmpty)
            {
                key = primary[index];
                return true;
            }

            if (!secondary[index].IsEmpty)
            {
                key = secondary[index];
                return true;
            }

            return false;
        }

        public void BeginListen(PlayerActionId id, InputBindingSlot slot)
        {
            if (slot == InputBindingSlot.Gamepad || !IsKnown(id))
                return;

            listening = true;
            awaitingConflict = false;
            listenAction = id;
            listenSlot = slot;
            pendingKey = InputBindingKey.None;
            conflictAction = PlayerActionId.None;
            listenIgnoreUntilFrame = Time.frameCount;
            snapshotDirty = true;
        }

        public void CancelListen()
        {
            if (!listening && !awaitingConflict)
                return;

            listening = false;
            awaitingConflict = false;
            listenAction = PlayerActionId.None;
            pendingKey = InputBindingKey.None;
            conflictAction = PlayerActionId.None;
            snapshotDirty = true;
        }

        public void ConfirmSwap()
        {
            if (!awaitingConflict || pendingKey.IsEmpty)
                return;

            ApplySwap(listenAction, listenSlot, pendingKey, conflictAction);
            listening = false;
            awaitingConflict = false;
            pendingKey = InputBindingKey.None;
            conflictAction = PlayerActionId.None;
            Persist();
            NotifyChanged();
        }

        public void DismissConflict()
        {
            CancelListen();
        }

        public bool TryClear(PlayerActionId id, InputBindingSlot slot)
        {
            if (slot == InputBindingSlot.Gamepad || !IsKnown(id))
                return false;

            var index = ToIndex(id);
            var def = definitionById[index];
            if (def != null && def.isRequired && CountKeyboardMouseBinds(index) <= 1)
                return false;

            SetSlot(index, slot, InputBindingKey.None);
            listening = false;
            awaitingConflict = false;
            Persist();
            NotifyChanged();
            return true;
        }

        public void SetQuickCast(PlayerActionId id, bool enabled)
        {
            var index = ToIndex(id);
            if (index < 0)
                return;
            var def = definitionById[index];
            if (def == null || !def.hasQuickCast)
                return;
            if (quickCast[index] == enabled)
                return;

            quickCast[index] = enabled;
            Persist();
            NotifyChanged();
        }

        public void ResetToDefaults()
        {
            ApplyCatalogDefaults();
            listening = false;
            awaitingConflict = false;
            Persist();
            NotifyChanged();
        }

        private void EnsureFrame()
        {
            var frame = Time.frameCount;
            if (processedFrame == frame)
                return;
            processedFrame = frame;
            suppressGameplayThisFrame = false;
            if (listening && !awaitingConflict)
                ProcessListen();
        }

        private void ProcessListen()
        {
            if (Time.frameCount <= listenIgnoreUntilFrame)
                return;
            if (!InputControlResolver.TryCapture(out var captured))
                return;

            suppressGameplayThisFrame = true;

            if (captured.device == InputBindingDevice.Keyboard && captured.keyboardKey == Key.Escape)
            {
                CancelListen();
                return;
            }

            var result = TryAssign(listenAction, listenSlot, captured);
            if (result == BindResultKind.Applied)
            {
                listening = false;
                awaitingConflict = false;
                Persist();
                NotifyChanged();
                return;
            }

            if (result == BindResultKind.Conflict)
            {
                pendingKey = captured;
                awaitingConflict = true;
                snapshotDirty = true;
                return;
            }

            if (result == BindResultKind.DuplicateOnSameAction)
            {
                listenRejectPulse++;
                snapshotDirty = true;
            }
        }

        private BindResultKind TryAssign(PlayerActionId id, InputBindingSlot slot, InputBindingKey key)
        {
            if (key.IsEmpty || !IsKnown(id) || slot == InputBindingSlot.Gamepad)
                return BindResultKind.Unchanged;

            var index = ToIndex(id);
            if (GetSlot(index, slot).Equals(key))
                return BindResultKind.Unchanged;

            var otherSlot = slot == InputBindingSlot.Primary
                ? InputBindingSlot.Secondary
                : InputBindingSlot.Primary;
            if (GetSlot(index, otherSlot).Equals(key))
                return BindResultKind.DuplicateOnSameAction;

            if (TryFindConflict(id, key, out var other))
            {
                conflictAction = other;
                return BindResultKind.Conflict;
            }

            SetSlot(index, slot, key);
            return BindResultKind.Applied;
        }

        private void ApplySwap(PlayerActionId target, InputBindingSlot targetSlot, InputBindingKey newKey, PlayerActionId other)
        {
            var targetIndex = ToIndex(target);
            var otherIndex = ToIndex(other);
            var oldKey = GetSlot(targetIndex, targetSlot);
            var otherSlot = FindSlotWithKey(otherIndex, newKey);
            SetSlot(targetIndex, targetSlot, newKey);
            if (otherSlot.HasValue)
                SetSlot(otherIndex, otherSlot.Value, oldKey);
        }

        private InputBindingSlot? FindSlotWithKey(int index, InputBindingKey key)
        {
            if (primary[index].Equals(key))
                return InputBindingSlot.Primary;
            if (secondary[index].Equals(key))
                return InputBindingSlot.Secondary;
            return null;
        }

        private bool TryFindConflict(PlayerActionId id, InputBindingKey key, out PlayerActionId other)
        {
            other = PlayerActionId.None;
            for (var i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null || def.id == id || def.id == PlayerActionId.None)
                    continue;
                var index = ToIndex(def.id);
                if (index < 0)
                    continue;
                if (!primary[index].Equals(key) && !secondary[index].Equals(key))
                    continue;
                if (AllowsOverlap(id, def.id, def))
                    continue;
                other = def.id;
                return true;
            }

            return false;
        }

        private bool AllowsOverlap(PlayerActionId a, PlayerActionId b, PlayerActionDefinition otherDef)
        {
            if (ListContains(otherDef.allowedOverlaps, a))
                return true;
            var self = definitionById[ToIndex(a)];
            return self != null && ListContains(self.allowedOverlaps, b);
        }

        private static bool ListContains(List<PlayerActionId> list, PlayerActionId id)
        {
            if (list == null)
                return false;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == id)
                    return true;
            }

            return false;
        }

        private bool AnySlot(PlayerActionId id, Func<ButtonControl, bool> predicate, bool ignoreMouse)
        {
            var index = ToIndex(id);
            if (index < 0)
                return false;
            return Matches(primary[index], predicate, ignoreMouse)
                || Matches(secondary[index], predicate, ignoreMouse)
                || Matches(gamepad[index], predicate, ignoreMouse: false);
        }

        private bool SlotPressed(PlayerActionId id, InputBindingDevice device)
        {
            var index = ToIndex(id);
            if (index < 0)
                return false;
            return PressedIfDevice(primary[index], device)
                || PressedIfDevice(secondary[index], device)
                || PressedIfDevice(gamepad[index], device);
        }

        private static bool PressedIfDevice(InputBindingKey key, InputBindingDevice device)
        {
            if (key.device != device)
                return false;
            var control = InputControlResolver.Resolve(key);
            return control != null && control.wasPressedThisFrame;
        }

        private static bool Matches(InputBindingKey key, Func<ButtonControl, bool> predicate, bool ignoreMouse)
        {
            if (ignoreMouse && key.device == InputBindingDevice.Mouse)
                return false;
            var control = InputControlResolver.Resolve(key);
            return predicate(control);
        }

        private bool SlotMatchesKeyCode(PlayerActionId id, KeyCode keyCode)
        {
            var index = ToIndex(id);
            if (index < 0)
                return false;
            return KeyMatches(primary[index], keyCode) || KeyMatches(secondary[index], keyCode);
        }

        private static bool KeyMatches(InputBindingKey key, KeyCode keyCode)
        {
            return key.device == InputBindingDevice.Keyboard
                && InputKeyCodeMap.TryToKeyCode(key.keyboardKey, out var mapped)
                && mapped == keyCode;
        }

        private void ApplyCatalogDefaults()
        {
            Array.Clear(definitionById, 0, definitionById.Length);
            Array.Clear(primary, 0, primary.Length);
            Array.Clear(secondary, 0, secondary.Length);
            Array.Clear(gamepad, 0, gamepad.Length);
            Array.Clear(quickCast, 0, quickCast.Length);

            var spec = PlayerActionCatalogDefaults.CreateLookup();
            for (var i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null || def.id == PlayerActionId.None)
                    continue;
                var index = ToIndex(def.id);
                if (index < 0)
                    continue;
                definitionById[index] = def;
                spec.TryGetValue(def.id, out var specDef);
                primary[index] = PlayerActionCatalogDefaults.Coalesce(
                    def.defaultPrimary,
                    specDef != null ? specDef.defaultPrimary : default);
                secondary[index] = PlayerActionCatalogDefaults.Coalesce(
                    def.defaultSecondary,
                    specDef != null ? specDef.defaultSecondary : default);
                gamepad[index] = PlayerActionCatalogDefaults.Coalesce(
                    def.defaultGamepad,
                    specDef != null ? specDef.defaultGamepad : default);
                quickCast[index] = def.hasQuickCast && def.defaultQuickCast;
            }
        }

        private void LoadOverrides()
        {
            var json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            SavedBindingsFile file;
            try
            {
                file = JsonUtility.FromJson<SavedBindingsFile>(json);
            }
            catch (Exception)
            {
                return;
            }

            if (file?.actions == null)
                return;

            for (var i = 0; i < file.actions.Length; i++)
            {
                var saved = file.actions[i];
                if (saved == null || !Enum.TryParse(saved.id, out PlayerActionId id))
                    continue;
                var index = ToIndex(id);
                if (index < 0 || definitionById[index] == null)
                    continue;
                if (InputBindingKey.TryParseToken(saved.primary, out var p))
                    primary[index] = p;
                if (InputBindingKey.TryParseToken(saved.secondary, out var s))
                    secondary[index] = s;
                if (InputBindingKey.TryParseToken(saved.gamepad, out var g) && !g.IsEmpty)
                    gamepad[index] = g;
                if (definitionById[index].hasQuickCast)
                    quickCast[index] = saved.quickCast;
            }

            if (file.version < 2)
            {
                var pingIndex = ToIndex(PlayerActionId.Ping);
                if (pingIndex >= 0
                    && definitionById[pingIndex] != null
                    && primary[pingIndex].IsEmpty
                    && secondary[pingIndex].IsEmpty)
                    primary[pingIndex] = InputBindingKey.Keyboard(Key.G);
                Persist();
            }
        }

        private void Persist()
        {
            var saved = new List<SavedActionBind>(definitions.Count);
            for (var i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null || def.id == PlayerActionId.None)
                    continue;
                var index = ToIndex(def.id);
                saved.Add(new SavedActionBind
                {
                    id = def.id.ToString(),
                    primary = primary[index].ToToken(),
                    secondary = secondary[index].ToToken(),
                    gamepad = gamepad[index].ToToken(),
                    quickCast = quickCast[index]
                });
            }

            var file = new SavedBindingsFile
            {
                version = SaveVersion,
                actions = saved.ToArray()
            };
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(file));
            PlayerPrefs.Save();
        }

        private void NotifyChanged()
        {
            revision++;
            snapshotDirty = true;
            GameMessages.Publish(new InputBindingsChangedEvent(revision));
        }

        private void RebuildSnapshot()
        {
            rows.Clear();
            for (var i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null || !def.showInSettings || def.id == PlayerActionId.None)
                    continue;
                var index = ToIndex(def.id);
                rows.Add(new InputActionRowSnapshot(
                    def.id,
                    def.settingsLabel,
                    def.group,
                    InputBindingDisplay.ToLabel(primary[index]),
                    InputBindingDisplay.ToLabel(secondary[index]),
                    def.isModifier,
                    def.isRequired,
                    def.hasQuickCast,
                    quickCast[index]));
            }

            var conflictLabel = conflictAction != PlayerActionId.None
                ? LabelFor(conflictAction)
                : string.Empty;
            cachedSnapshot = new InputBindingsSnapshot(
                revision,
                listening,
                listenAction,
                listenSlot,
                awaitingConflict,
                listenRejectPulse,
                awaitingConflict ? InputBindingDisplay.ToLabel(pendingKey) : string.Empty,
                conflictLabel,
                rows);
            snapshotDirty = false;
        }

        private string LabelFor(PlayerActionId id)
        {
            var index = ToIndex(id);
            var def = index >= 0 ? definitionById[index] : null;
            return def != null && !string.IsNullOrEmpty(def.settingsLabel)
                ? def.settingsLabel
                : id.ToString();
        }

        private InputBindingKey GetSlot(int index, InputBindingSlot slot)
        {
            return slot switch
            {
                InputBindingSlot.Secondary => secondary[index],
                InputBindingSlot.Gamepad => gamepad[index],
                _ => primary[index]
            };
        }

        private void SetSlot(int index, InputBindingSlot slot, InputBindingKey key)
        {
            switch (slot)
            {
                case InputBindingSlot.Secondary:
                    secondary[index] = key;
                    break;
                case InputBindingSlot.Gamepad:
                    gamepad[index] = key;
                    break;
                default:
                    primary[index] = key;
                    break;
            }
        }

        private int CountKeyboardMouseBinds(int index)
        {
            var n = 0;
            if (!primary[index].IsEmpty)
                n++;
            if (!secondary[index].IsEmpty)
                n++;
            return n;
        }

        private bool IsKnown(PlayerActionId id)
        {
            var index = ToIndex(id);
            return index >= 0 && definitionById[index] != null;
        }

        private int ToIndex(PlayerActionId id)
        {
            var index = (int)id;
            return index >= 0 && index < count ? index : -1;
        }

        private static int ComputeCount()
        {
            var max = 0;
            foreach (PlayerActionId value in Enum.GetValues(typeof(PlayerActionId)))
            {
                var n = (int)value;
                if (n > max)
                    max = n;
            }

            return max + 1;
        }

        [Serializable]
        private sealed class SavedBindingsFile
        {
            public int version = 1;
            public SavedActionBind[] actions;
        }

        [Serializable]
        private sealed class SavedActionBind
        {
            public string id;
            public string primary;
            public string secondary;
            public string gamepad;
            public bool quickCast;
        }
    }
}
