using UnityEngine.InputSystem;

namespace ShadowInfection.Input
{
    public static class InputBindingDisplay
    {
        public const string EmptySlot = "—";

        public static string ToLabel(InputBindingKey key)
        {
            if (key.IsEmpty)
                return EmptySlot;

            return key.device switch
            {
                InputBindingDevice.Keyboard => KeyboardLabel(key.keyboardKey),
                InputBindingDevice.Mouse => MouseLabel(key.mouseButton),
                InputBindingDevice.Gamepad => GamepadLabel(key.gamepadButton),
                _ => EmptySlot
            };
        }

        public static string KeyboardLabel(Key key)
        {
            switch (key)
            {
                case Key.None:
                    return EmptySlot;
                case Key.Space:
                    return "Space";
                case Key.Enter:
                    return "Enter";
                case Key.Tab:
                    return "Tab";
                case Key.Escape:
                    return "Esc";
                case Key.LeftShift:
                    return "Shift";
                case Key.RightShift:
                    return "Right Shift";
                case Key.LeftAlt:
                    return "Left Alt";
                case Key.RightAlt:
                    return "Right Alt";
                case Key.LeftCtrl:
                    return "Ctrl";
                case Key.RightCtrl:
                    return "Right Ctrl";
                case Key.LeftArrow:
                    return "←";
                case Key.RightArrow:
                    return "→";
                case Key.UpArrow:
                    return "↑";
                case Key.DownArrow:
                    return "↓";
                case Key.Backspace:
                    return "Backspace";
                case Key.Delete:
                    return "Del";
                case Key.Insert:
                    return "Ins";
                case Key.Home:
                    return "Home";
                case Key.End:
                    return "End";
                case Key.PageUp:
                    return "PgUp";
                case Key.PageDown:
                    return "PgDn";
                case Key.LeftBracket:
                    return "[";
                case Key.RightBracket:
                    return "]";
                case Key.Semicolon:
                    return ";";
                case Key.Quote:
                    return "'";
                case Key.Comma:
                    return ",";
                case Key.Period:
                    return ".";
                case Key.Slash:
                    return "/";
                case Key.Backslash:
                    return "\\";
                case Key.Minus:
                    return "-";
                case Key.Equals:
                    return "=";
                case Key.Backquote:
                    return "`";
                default:
                    if (key >= Key.A && key <= Key.Z)
                        return ((char)('A' + (key - Key.A))).ToString();
                    if (key >= Key.Digit1 && key <= Key.Digit9)
                        return ((char)('1' + (key - Key.Digit1))).ToString();
                    if (key == Key.Digit0)
                        return "0";
                    if (key >= Key.F1 && key <= Key.F12)
                        return "F" + (1 + (key - Key.F1));
                    return key.ToString();
            }
        }

        public static string MouseLabel(InputMouseButton button)
        {
            return button switch
            {
                InputMouseButton.Left => "LMB",
                InputMouseButton.Right => "RMB",
                InputMouseButton.Middle => "MMB",
                InputMouseButton.Forward => "Mouse4",
                InputMouseButton.Back => "Mouse5",
                _ => EmptySlot
            };
        }

        public static string GamepadLabel(InputGamepadButton button)
        {
            return button switch
            {
                InputGamepadButton.RightTrigger => "RT",
                InputGamepadButton.LeftTrigger => "LT",
                InputGamepadButton.RightShoulder => "RB",
                InputGamepadButton.LeftShoulder => "LB",
                InputGamepadButton.South => "A",
                InputGamepadButton.East => "B",
                InputGamepadButton.West => "X",
                InputGamepadButton.North => "Y",
                InputGamepadButton.Start => "Start",
                InputGamepadButton.Select => "Select",
                InputGamepadButton.DpadUp => "D-Pad ↑",
                InputGamepadButton.DpadDown => "D-Pad ↓",
                InputGamepadButton.DpadLeft => "D-Pad ←",
                InputGamepadButton.DpadRight => "D-Pad →",
                InputGamepadButton.LeftStickPress => "L3",
                InputGamepadButton.RightStickPress => "R3",
                _ => EmptySlot
            };
        }
    }
}
