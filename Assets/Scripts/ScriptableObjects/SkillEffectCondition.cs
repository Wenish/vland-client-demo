using System.Collections.Generic;

public abstract class SkillEffectCondition : SkillEffectData
{
    public override SkillEffectType EffectType { get; } = SkillEffectType.Condition;
    public override List<UnitController> Execute(UnitController caster, List<UnitController> targets) {
        List<UnitController> result = new List<UnitController>();
        foreach (var target in targets)
        {
            var isConditionMet = IsConditionMet(caster, target);
            if (isConditionMet)
            {
                result.Add(target);
            }
        }
        return result;
    }

    public abstract bool IsConditionMet(UnitController caster, UnitController target);
}