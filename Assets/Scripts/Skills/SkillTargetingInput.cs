using ShadowInfection.DI;
using ShadowInfection.Input;
using UnityEngine.InputSystem;

/// <summary>
/// Client input modifiers for skill targeting.
/// Self Target Modifier forces self-cast for smart-target skills and places mouse-aimed
/// (ground) spells at the caster.
/// </summary>
public static class SkillTargetingInput
{
    public static bool IsLeftAltPressedForSelfTarget()
    {
        var reader = GameServices.Input;
        if (reader != null)
            return reader.IsHeld(PlayerActionId.SelfTargetModifier);

        return Keyboard.current != null && Keyboard.current.leftAltKey.isPressed;
    }
}
