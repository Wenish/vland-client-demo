using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace ShadowInfection.Input
{
    public static class PlayerActionCatalogDefaults
    {
        public static Dictionary<PlayerActionId, PlayerActionDefinition> CreateLookup()
        {
            var list = Create();
            var map = new Dictionary<PlayerActionId, PlayerActionDefinition>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                var def = list[i];
                if (def != null && def.id != PlayerActionId.None)
                    map[def.id] = def;
            }

            return map;
        }

        public static InputBindingKey Coalesce(InputBindingKey catalogValue, InputBindingKey specValue)
        {
            return catalogValue.IsEmpty ? specValue : catalogValue;
        }

        public static List<PlayerActionDefinition> Create()
        {
            return new List<PlayerActionDefinition>
            {
                Move(PlayerActionId.MoveForward, "Move Forward", Key.W, Key.UpArrow),
                Move(PlayerActionId.MoveBackward, "Move Backward", Key.S, Key.DownArrow),
                Move(PlayerActionId.MoveLeft, "Move Left", Key.A, Key.LeftArrow),
                Move(PlayerActionId.MoveRight, "Move Right", Key.D, Key.RightArrow),
                new PlayerActionDefinition
                {
                    id = PlayerActionId.Attack,
                    settingsLabel = "Attack",
                    group = PlayerActionGroup.Combat,
                    defaultPrimary = InputBindingKey.Mouse(InputMouseButton.Left),
                    defaultSecondary = InputBindingKey.Keyboard(Key.Space),
                    defaultGamepad = InputBindingKey.Gamepad(InputGamepadButton.RightTrigger),
                    isRequired = true,
                    showInSettings = true,
                    allowedOverlaps = new List<PlayerActionId> { PlayerActionId.SpectatePrevious }
                },
                Skill(PlayerActionId.Skill1, "Skill 1", Key.Q),
                Skill(PlayerActionId.Skill2, "Skill 2", Key.E),
                Skill(PlayerActionId.Skill3, "Skill 3", Key.C),
                Skill(PlayerActionId.Ultimate, "Ultimate", Key.X),
                new PlayerActionDefinition
                {
                    id = PlayerActionId.CancelCast,
                    settingsLabel = "Cancel Cast",
                    group = PlayerActionGroup.Combat,
                    defaultPrimary = InputBindingKey.Mouse(InputMouseButton.Right),
                    showInSettings = true,
                    allowedOverlaps = new List<PlayerActionId> { PlayerActionId.SpectateNext }
                },
                Bind(PlayerActionId.Interrupt, "Interrupt", PlayerActionGroup.Combat, Key.H),
                new PlayerActionDefinition
                {
                    id = PlayerActionId.Ping,
                    settingsLabel = "Ping",
                    group = PlayerActionGroup.Combat,
                    defaultPrimary = InputBindingKey.Keyboard(Key.G),
                    showInSettings = true
                },
                new PlayerActionDefinition
                {
                    id = PlayerActionId.SelfTargetModifier,
                    settingsLabel = "Self Target Modifier",
                    group = PlayerActionGroup.Modifiers,
                    defaultPrimary = InputBindingKey.Keyboard(Key.LeftAlt),
                    isModifier = true,
                    showInSettings = true
                },
                new PlayerActionDefinition
                {
                    id = PlayerActionId.CastModifier,
                    settingsLabel = "Cast Modifier",
                    group = PlayerActionGroup.Modifiers,
                    defaultPrimary = InputBindingKey.Keyboard(Key.LeftShift),
                    isModifier = true,
                    showInSettings = true
                },
                Bind(PlayerActionId.Interact, "Interact", PlayerActionGroup.World, Key.F),
                Bind(PlayerActionId.CameraFollow, "Camera Follow", PlayerActionGroup.Camera, Key.Z),
                new PlayerActionDefinition
                {
                    id = PlayerActionId.CameraFixed,
                    settingsLabel = "Camera Fixed",
                    group = PlayerActionGroup.Camera,
                    defaultPrimary = InputBindingKey.Mouse(InputMouseButton.Middle),
                    showInSettings = true
                },
                new PlayerActionDefinition
                {
                    id = PlayerActionId.SpectatePrevious,
                    settingsLabel = "Spectate Previous",
                    group = PlayerActionGroup.Camera,
                    defaultPrimary = InputBindingKey.Mouse(InputMouseButton.Left),
                    showInSettings = true,
                    allowedOverlaps = new List<PlayerActionId> { PlayerActionId.Attack }
                },
                new PlayerActionDefinition
                {
                    id = PlayerActionId.SpectateNext,
                    settingsLabel = "Spectate Next",
                    group = PlayerActionGroup.Camera,
                    defaultPrimary = InputBindingKey.Mouse(InputMouseButton.Right),
                    showInSettings = true,
                    allowedOverlaps = new List<PlayerActionId> { PlayerActionId.CancelCast }
                },
                Bind(PlayerActionId.Loadout, "Loadout", PlayerActionGroup.Interface, Key.I),
                new PlayerActionDefinition
                {
                    id = PlayerActionId.Leaderboard,
                    settingsLabel = "Leaderboard",
                    group = PlayerActionGroup.Interface,
                    defaultPrimary = InputBindingKey.Keyboard(Key.Tab),
                    showInSettings = true,
                    allowedOverlaps = new List<PlayerActionId> { PlayerActionId.VendorTabs }
                },
                new PlayerActionDefinition
                {
                    id = PlayerActionId.VendorTabs,
                    settingsLabel = "Vendor Tabs",
                    group = PlayerActionGroup.Interface,
                    defaultPrimary = InputBindingKey.Keyboard(Key.Tab),
                    showInSettings = true,
                    allowedOverlaps = new List<PlayerActionId> { PlayerActionId.Leaderboard }
                },
                Bind(PlayerActionId.Menu, "Menu", PlayerActionGroup.Interface, Key.Escape)
            };
        }

        private static PlayerActionDefinition Move(PlayerActionId id, string label, Key primary, Key secondary)
        {
            return new PlayerActionDefinition
            {
                id = id,
                settingsLabel = label,
                group = PlayerActionGroup.Movement,
                defaultPrimary = InputBindingKey.Keyboard(primary),
                defaultSecondary = InputBindingKey.Keyboard(secondary),
                isRequired = true,
                showInSettings = true
            };
        }

        private static PlayerActionDefinition Skill(PlayerActionId id, string label, Key primary)
        {
            return new PlayerActionDefinition
            {
                id = id,
                settingsLabel = label,
                group = PlayerActionGroup.Combat,
                defaultPrimary = InputBindingKey.Keyboard(primary),
                hasQuickCast = true,
                defaultQuickCast = false,
                showInSettings = true
            };
        }

        private static PlayerActionDefinition Bind(PlayerActionId id, string label, PlayerActionGroup group, Key primary)
        {
            return new PlayerActionDefinition
            {
                id = id,
                settingsLabel = label,
                group = group,
                defaultPrimary = InputBindingKey.Keyboard(primary),
                showInSettings = true
            };
        }
    }
}
