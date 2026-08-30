using System.Collections.Generic;
using UnityEngine;

namespace ShadowInfection.Input
{
    public interface IInputReader
    {
        bool IsListening { get; }

        bool WasPressed(PlayerActionId id);

        bool WasReleased(PlayerActionId id);

        bool IsHeld(PlayerActionId id);

        bool WasMousePressed(PlayerActionId id);

        bool WasPressedExcludingMouse(PlayerActionId id);

        bool IsQuickCast(PlayerActionId id);

        Vector2 ReadMoveAxis();

        bool IsMoveOrAttackKeyboardKey(KeyCode keyCode);
    }

    public interface IInputBindingSession
    {
        bool TryGetSnapshot(out InputBindingsSnapshot snapshot);

        bool TryGetDisplayBind(PlayerActionId id, out InputBindingKey key);
    }

    public interface IInputBindingCommands
    {
        void BeginListen(PlayerActionId id, InputBindingSlot slot);

        void CancelListen();

        void ConfirmSwap();

        void DismissConflict();

        bool TryClear(PlayerActionId id, InputBindingSlot slot);

        void SetQuickCast(PlayerActionId id, bool enabled);

        void ResetToDefaults();
    }

    public enum BindResultKind
    {
        Applied = 0,
        DuplicateOnSameAction = 1,
        Conflict = 2,
        RequiredBind = 3,
        Unchanged = 4
    }

    public readonly struct InputActionRowSnapshot
    {
        public readonly PlayerActionId Id;
        public readonly string Label;
        public readonly PlayerActionGroup Group;
        public readonly string PrimaryLabel;
        public readonly string SecondaryLabel;
        public readonly bool IsModifier;
        public readonly bool IsRequired;
        public readonly bool HasQuickCast;
        public readonly bool QuickCast;

        public InputActionRowSnapshot(
            PlayerActionId id,
            string label,
            PlayerActionGroup group,
            string primaryLabel,
            string secondaryLabel,
            bool isModifier,
            bool isRequired,
            bool hasQuickCast,
            bool quickCast)
        {
            Id = id;
            Label = label ?? string.Empty;
            Group = group;
            PrimaryLabel = primaryLabel ?? InputBindingDisplay.EmptySlot;
            SecondaryLabel = secondaryLabel ?? InputBindingDisplay.EmptySlot;
            IsModifier = isModifier;
            IsRequired = isRequired;
            HasQuickCast = hasQuickCast;
            QuickCast = quickCast;
        }
    }

    public readonly struct InputBindingsSnapshot
    {
        public readonly int Revision;
        public readonly bool IsListening;
        public readonly PlayerActionId ListeningAction;
        public readonly InputBindingSlot ListeningSlot;
        public readonly bool HasConflict;
        public readonly int ListenRejectPulse;
        public readonly string ConflictKeyLabel;
        public readonly string ConflictActionLabel;
        public readonly IReadOnlyList<InputActionRowSnapshot> Rows;

        public InputBindingsSnapshot(
            int revision,
            bool isListening,
            PlayerActionId listeningAction,
            InputBindingSlot listeningSlot,
            bool hasConflict,
            int listenRejectPulse,
            string conflictKeyLabel,
            string conflictActionLabel,
            IReadOnlyList<InputActionRowSnapshot> rows)
        {
            Revision = revision;
            IsListening = isListening;
            ListeningAction = listeningAction;
            ListeningSlot = listeningSlot;
            HasConflict = hasConflict;
            ListenRejectPulse = listenRejectPulse;
            ConflictKeyLabel = conflictKeyLabel ?? string.Empty;
            ConflictActionLabel = conflictActionLabel ?? string.Empty;
            Rows = rows ?? System.Array.Empty<InputActionRowSnapshot>();
        }

        public string Signature =>
            $"{Revision}|{IsListening}|{(int)ListeningAction}|{(int)ListeningSlot}|{HasConflict}|{ListenRejectPulse}|{ConflictKeyLabel}|{ConflictActionLabel}";
    }
}
