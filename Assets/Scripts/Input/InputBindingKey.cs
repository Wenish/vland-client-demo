using System;
using UnityEngine.InputSystem;

namespace ShadowInfection.Input
{
    public enum InputBindingDevice
    {
        None = 0,
        Keyboard = 1,
        Mouse = 2,
        Gamepad = 3
    }

    public enum InputMouseButton
    {
        None = 0,
        Left = 1,
        Right = 2,
        Middle = 3,
        Forward = 4,
        Back = 5
    }

    public enum InputGamepadButton
    {
        None = 0,
        DpadUp = 1,
        DpadDown = 2,
        DpadLeft = 3,
        DpadRight = 4,
        North = 5,
        East = 6,
        South = 7,
        West = 8,
        LeftShoulder = 9,
        RightShoulder = 10,
        LeftTrigger = 11,
        RightTrigger = 12,
        LeftStickPress = 13,
        RightStickPress = 14,
        Start = 15,
        Select = 16
    }

    [Serializable]
    public struct InputBindingKey : IEquatable<InputBindingKey>
    {
        public InputBindingDevice device;
        public Key keyboardKey;
        public InputMouseButton mouseButton;
        public InputGamepadButton gamepadButton;

        public bool IsEmpty => device == InputBindingDevice.None;

        public static InputBindingKey None => default;

        public static InputBindingKey Keyboard(Key key)
        {
            return new InputBindingKey
            {
                device = InputBindingDevice.Keyboard,
                keyboardKey = key
            };
        }

        public static InputBindingKey Mouse(InputMouseButton button)
        {
            return new InputBindingKey
            {
                device = InputBindingDevice.Mouse,
                mouseButton = button
            };
        }

        public static InputBindingKey Gamepad(InputGamepadButton button)
        {
            return new InputBindingKey
            {
                device = InputBindingDevice.Gamepad,
                gamepadButton = button
            };
        }

        public bool Equals(InputBindingKey other)
        {
            if (device != other.device)
                return false;
            if (device == InputBindingDevice.None)
                return true;
            if (device == InputBindingDevice.Keyboard)
                return keyboardKey == other.keyboardKey;
            if (device == InputBindingDevice.Mouse)
                return mouseButton == other.mouseButton;
            return gamepadButton == other.gamepadButton;
        }

        public override bool Equals(object obj)
        {
            return obj is InputBindingKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return device switch
                {
                    InputBindingDevice.Keyboard => ((int)device * 397) ^ (int)keyboardKey,
                    InputBindingDevice.Mouse => ((int)device * 397) ^ (int)mouseButton,
                    InputBindingDevice.Gamepad => ((int)device * 397) ^ (int)gamepadButton,
                    _ => 0
                };
            }
        }

        public static bool operator ==(InputBindingKey left, InputBindingKey right) => left.Equals(right);

        public static bool operator !=(InputBindingKey left, InputBindingKey right) => !left.Equals(right);

        public string ToToken()
        {
            if (IsEmpty)
                return string.Empty;
            return device switch
            {
                InputBindingDevice.Keyboard => "k:" + (int)keyboardKey,
                InputBindingDevice.Mouse => "m:" + (int)mouseButton,
                InputBindingDevice.Gamepad => "g:" + (int)gamepadButton,
                _ => string.Empty
            };
        }

        public static bool TryParseToken(string token, out InputBindingKey key)
        {
            key = None;
            if (string.IsNullOrEmpty(token) || token.Length < 3 || token[1] != ':')
                return token == string.Empty;

            if (!int.TryParse(token.Substring(2), out var code))
                return false;

            switch (token[0])
            {
                case 'k':
                    key = Keyboard((Key)code);
                    return true;
                case 'm':
                    key = Mouse((InputMouseButton)code);
                    return true;
                case 'g':
                    key = Gamepad((InputGamepadButton)code);
                    return true;
                default:
                    return false;
            }
        }
    }
}
