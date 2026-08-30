using UnityEngine;
using UnityEngine.InputSystem;

namespace ShadowInfection.Input
{
    public static class InputKeyCodeMap
    {
        public static bool TryToKeyCode(Key key, out KeyCode keyCode)
        {
            switch (key)
            {
                case Key.A: keyCode = KeyCode.A; return true;
                case Key.B: keyCode = KeyCode.B; return true;
                case Key.C: keyCode = KeyCode.C; return true;
                case Key.D: keyCode = KeyCode.D; return true;
                case Key.E: keyCode = KeyCode.E; return true;
                case Key.F: keyCode = KeyCode.F; return true;
                case Key.G: keyCode = KeyCode.G; return true;
                case Key.H: keyCode = KeyCode.H; return true;
                case Key.I: keyCode = KeyCode.I; return true;
                case Key.J: keyCode = KeyCode.J; return true;
                case Key.K: keyCode = KeyCode.K; return true;
                case Key.L: keyCode = KeyCode.L; return true;
                case Key.M: keyCode = KeyCode.M; return true;
                case Key.N: keyCode = KeyCode.N; return true;
                case Key.O: keyCode = KeyCode.O; return true;
                case Key.P: keyCode = KeyCode.P; return true;
                case Key.Q: keyCode = KeyCode.Q; return true;
                case Key.R: keyCode = KeyCode.R; return true;
                case Key.S: keyCode = KeyCode.S; return true;
                case Key.T: keyCode = KeyCode.T; return true;
                case Key.U: keyCode = KeyCode.U; return true;
                case Key.V: keyCode = KeyCode.V; return true;
                case Key.W: keyCode = KeyCode.W; return true;
                case Key.X: keyCode = KeyCode.X; return true;
                case Key.Y: keyCode = KeyCode.Y; return true;
                case Key.Z: keyCode = KeyCode.Z; return true;
                case Key.Space: keyCode = KeyCode.Space; return true;
                case Key.LeftArrow: keyCode = KeyCode.LeftArrow; return true;
                case Key.RightArrow: keyCode = KeyCode.RightArrow; return true;
                case Key.UpArrow: keyCode = KeyCode.UpArrow; return true;
                case Key.DownArrow: keyCode = KeyCode.DownArrow; return true;
                case Key.LeftShift: keyCode = KeyCode.LeftShift; return true;
                case Key.RightShift: keyCode = KeyCode.RightShift; return true;
                case Key.LeftAlt: keyCode = KeyCode.LeftAlt; return true;
                case Key.RightAlt: keyCode = KeyCode.RightAlt; return true;
                case Key.LeftCtrl: keyCode = KeyCode.LeftControl; return true;
                case Key.RightCtrl: keyCode = KeyCode.RightControl; return true;
                case Key.Tab: keyCode = KeyCode.Tab; return true;
                case Key.Escape: keyCode = KeyCode.Escape; return true;
                case Key.Enter: keyCode = KeyCode.Return; return true;
                case Key.Backspace: keyCode = KeyCode.Backspace; return true;
                case Key.Delete: keyCode = KeyCode.Delete; return true;
                case Key.Insert: keyCode = KeyCode.Insert; return true;
                case Key.Home: keyCode = KeyCode.Home; return true;
                case Key.End: keyCode = KeyCode.End; return true;
                case Key.PageUp: keyCode = KeyCode.PageUp; return true;
                case Key.PageDown: keyCode = KeyCode.PageDown; return true;
                case Key.Digit0: keyCode = KeyCode.Alpha0; return true;
                case Key.Digit1: keyCode = KeyCode.Alpha1; return true;
                case Key.Digit2: keyCode = KeyCode.Alpha2; return true;
                case Key.Digit3: keyCode = KeyCode.Alpha3; return true;
                case Key.Digit4: keyCode = KeyCode.Alpha4; return true;
                case Key.Digit5: keyCode = KeyCode.Alpha5; return true;
                case Key.Digit6: keyCode = KeyCode.Alpha6; return true;
                case Key.Digit7: keyCode = KeyCode.Alpha7; return true;
                case Key.Digit8: keyCode = KeyCode.Alpha8; return true;
                case Key.Digit9: keyCode = KeyCode.Alpha9; return true;
                case Key.F1: keyCode = KeyCode.F1; return true;
                case Key.F2: keyCode = KeyCode.F2; return true;
                case Key.F3: keyCode = KeyCode.F3; return true;
                case Key.F4: keyCode = KeyCode.F4; return true;
                case Key.F5: keyCode = KeyCode.F5; return true;
                case Key.F6: keyCode = KeyCode.F6; return true;
                case Key.F7: keyCode = KeyCode.F7; return true;
                case Key.F8: keyCode = KeyCode.F8; return true;
                case Key.F9: keyCode = KeyCode.F9; return true;
                case Key.F10: keyCode = KeyCode.F10; return true;
                case Key.F11: keyCode = KeyCode.F11; return true;
                case Key.F12: keyCode = KeyCode.F12; return true;
                default:
                    keyCode = KeyCode.None;
                    return false;
            }
        }
    }
}
