using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectConditionHasBuff",
    menuName = "Game/Skills/Effects/Condition/Has Buff")]
public class SkillEffectConditionHasBuff : SkillEffectCondition
{
    [Tooltip("The buffId to check for. The condition passes if the target currently has this buff active.")]
    public string buffId;

    [Tooltip("If true, the condition passes when the buff is NOT present instead.")]
    public bool negate = false;

    public override bool IsConditionMet(CastContext castContext, UnitController target)
    {
        if (target == null) return false;
        var mediator = target.unitMediator;
        if (mediator == null) return false;

        bool hasBuff = mediator.Buffs.GetBuffById(buffId) != null;
        return negate ? !hasBuff : hasBuff;
    }
}
