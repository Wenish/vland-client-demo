using MyGame.Events;

public enum PlayerActionFailReason : byte
{
    OnCooldown = 1
}

/// <summary>
/// Gameplay-facing helper for local-player HUD info text.
/// Combat systems call this; they do not talk to UI classes directly.
/// </summary>
public static class PlayerActionFeedback
{
    public static void Notify(PlayerActionFailReason reason, string actionName)
    {
        if (reason == PlayerActionFailReason.OnCooldown)
            ShowCooldown(actionName);
    }

    public static void Show(
        string text,
        string key = "",
        float durationSeconds = 0f,
        PlayerHudInfoKind kind = PlayerHudInfoKind.Info)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        GameEventPublish.ToMessagePipe(new PlayerHudInfoMessageEvent(text, key, durationSeconds, kind));
    }

    public static void ShowCooldown(string actionName)
    {
        var name = string.IsNullOrWhiteSpace(actionName) ? "That action" : actionName;
        Show($"{name} is on cooldown", "cooldown:" + name, kind: PlayerHudInfoKind.Error);
    }

    public static string ResolveSkillName(NetworkedSkillInstance skill)
    {
        if (skill == null)
            return "Ability";

        if (skill.skillData != null && !string.IsNullOrWhiteSpace(skill.skillData.skillName))
            return skill.skillData.skillName;

        return string.IsNullOrWhiteSpace(skill.skillName) ? "Ability" : skill.skillName;
    }

    public static bool TryNotifyAttackCooldown(UnitController unit)
    {
        if (!ShouldShowAttackCooldown(unit))
            return false;

        ShowCooldown("Attack");
        return true;
    }

    public static bool TryNotifySkillCooldown(UnitController unit, SkillSlotType slot, int index)
    {
        if (!TryGetSkillOnCooldown(unit, slot, index, out var skill))
            return false;

        ShowCooldown(ResolveSkillName(skill));
        return true;
    }

    public static bool ShouldShowAttackCooldown(UnitController unit)
    {
        if (unit == null)
            return false;

        var weapon = unit.GetComponent<WeaponController>();
        if (weapon == null || weapon.weaponData == null || !weapon.IsAttackOnCooldown)
            return false;

        return unit.unitActionState == null
            || !unit.unitActionState.IsPerforming(UnitActionState.ActionType.Attacking);
    }

    public static bool ShouldShowSkillCooldown(UnitController unit, SkillSlotType slot, int index)
    {
        return TryGetSkillOnCooldown(unit, slot, index, out _);
    }

    private static bool TryGetSkillOnCooldown(
        UnitController unit,
        SkillSlotType slot,
        int index,
        out NetworkedSkillInstance skill)
    {
        skill = unit != null ? unit.unitMediator?.Skills?.GetSkill(slot, index) : null;
        if (skill == null || !skill.IsOnCooldown || skill.IsRecastWindowOpen)
            return false;

        var actionState = unit.unitActionState;
        if (actionState != null && IsPerformingSkill(actionState, skill))
            return false;

        return true;
    }

    private static bool IsPerformingSkill(UnitActionState actionState, NetworkedSkillInstance skill)
    {
        if (actionState.IsPerformingSkill(skill.skillName))
            return true;

        return skill.skillData != null
            && actionState.IsPerformingSkill(skill.skillData.skillName);
    }
}
