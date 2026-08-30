using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ShadowInfection.Input
{
    public static class InputControlResolver
    {
        public static ButtonControl Resolve(in InputBindingKey key)
        {
            if (key.IsEmpty)
                return null;

            switch (key.device)
            {
                case InputBindingDevice.Keyboard:
                    return Keyboard.current != null ? Keyboard.current[key.keyboardKey] : null;
                case InputBindingDevice.Mouse:
                    return ResolveMouse(key.mouseButton);
                case InputBindingDevice.Gamepad:
                    return ResolveGamepad(key.gamepadButton);
                default:
                    return null;
            }
        }

        public static bool TryCapture(out InputBindingKey key)
        {
            key = InputBindingKey.None;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    key = InputBindingKey.Keyboard(Key.Escape);
                    return true;
                }

                foreach (Key value in System.Enum.GetValues(typeof(Key)))
                {
                    if (value == Key.None || value == Key.Escape)
                        continue;
                    ButtonControl control;
                    try
                    {
                        control = keyboard[value];
                    }
                    catch (System.Exception)
                    {
                        continue;
                    }

                    if (control != null && control.wasPressedThisFrame)
                    {
                        key = InputBindingKey.Keyboard(value);
                        return true;
                    }
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    key = InputBindingKey.Mouse(InputMouseButton.Left);
                    return true;
                }

                if (mouse.rightButton.wasPressedThisFrame)
                {
                    key = InputBindingKey.Mouse(InputMouseButton.Right);
                    return true;
                }

                if (mouse.middleButton.wasPressedThisFrame)
                {
                    key = InputBindingKey.Mouse(InputMouseButton.Middle);
                    return true;
                }

                if (mouse.forwardButton.wasPressedThisFrame)
                {
                    key = InputBindingKey.Mouse(InputMouseButton.Forward);
                    return true;
                }

                if (mouse.backButton.wasPressedThisFrame)
                {
                    key = InputBindingKey.Mouse(InputMouseButton.Back);
                    return true;
                }
            }

            return false;
        }

        private static ButtonControl ResolveMouse(InputMouseButton button)
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return null;

            return button switch
            {
                InputMouseButton.Left => mouse.leftButton,
                InputMouseButton.Right => mouse.rightButton,
                InputMouseButton.Middle => mouse.middleButton,
                InputMouseButton.Forward => mouse.forwardButton,
                InputMouseButton.Back => mouse.backButton,
                _ => null
            };
        }

        private static ButtonControl ResolveGamepad(InputGamepadButton button)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null || button == InputGamepadButton.None)
                return null;

            return button switch
            {
                InputGamepadButton.DpadUp => gamepad.dpad.up,
                InputGamepadButton.DpadDown => gamepad.dpad.down,
                InputGamepadButton.DpadLeft => gamepad.dpad.left,
                InputGamepadButton.DpadRight => gamepad.dpad.right,
                InputGamepadButton.North => gamepad.buttonNorth,
                InputGamepadButton.East => gamepad.buttonEast,
                InputGamepadButton.South => gamepad.buttonSouth,
                InputGamepadButton.West => gamepad.buttonWest,
                InputGamepadButton.LeftShoulder => gamepad.leftShoulder,
                InputGamepadButton.RightShoulder => gamepad.rightShoulder,
                InputGamepadButton.LeftTrigger => gamepad.leftTrigger,
                InputGamepadButton.RightTrigger => gamepad.rightTrigger,
                InputGamepadButton.LeftStickPress => gamepad.leftStickButton,
                InputGamepadButton.RightStickPress => gamepad.rightStickButton,
                InputGamepadButton.Start => gamepad.startButton,
                InputGamepadButton.Select => gamepad.selectButton,
                _ => null
            };
        }
    }
}
