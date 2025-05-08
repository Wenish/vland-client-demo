using System.Collections.Generic;

public abstract class SkillEffectMechanic : SkillEffectData
{
    public override SkillEffectType EffectType { get; } = SkillEffectType.Mechanic;
    public override List<UnitController> Execute(CastContext castContext, List<UnitController> targets) {
        return DoMechanic(castContext, targets);
    }

    public abstract List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets);


}