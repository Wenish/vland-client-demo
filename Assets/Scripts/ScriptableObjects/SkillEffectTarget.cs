using System.Collections.Generic;

public abstract class SkillEffectTarget : SkillEffectData
{
    public override SkillEffectType EffectType { get; } = SkillEffectType.Target;
    public override List<UnitController> Execute(UnitController caster, List<UnitController> targets) {
        return GetTargets(caster, targets);
    }

    public abstract List<UnitController> GetTargets(UnitController caster, List<UnitController> targets);
}