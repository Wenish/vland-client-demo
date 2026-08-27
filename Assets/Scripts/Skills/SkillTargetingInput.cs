using UnityEngine.InputSystem;

/// <summary>
/// Client input modifiers for skill targeting (self-cast, etc.).
/// </summary>
public static class SkillTargetingInput
{
    public static bool IsLeftAltPressedForSelfTarget()
    {
        return Keyboard.current != null && Keyboard.current.leftAltKey.isPressed;
    }
}
