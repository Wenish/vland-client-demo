using UnityEngine.InputSystem;

/// <summary>
/// Client input modifiers for skill targeting.
/// Left Alt forces self-cast for smart-target skills and places mouse-aimed
/// (ground) spells at the caster.
/// </summary>
public static class SkillTargetingInput
{
    public static bool IsLeftAltPressedForSelfTarget()
    {
        return Keyboard.current != null && Keyboard.current.leftAltKey.isPressed;
    }
}
