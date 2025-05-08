using System.Collections.Generic;

public abstract class SkillEffectTarget : SkillEffectData
{
    public override SkillEffectType EffectType { get; } = SkillEffectType.Target;
    public override List<UnitController> Execute(CastContext castContext, List<UnitController> targets) {
        return GetTargets(castContext, targets);
    }

    public abstract List<UnitController> GetTargets(CastContext castContext, List<UnitController> targets);
}