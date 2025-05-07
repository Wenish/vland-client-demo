using System.Collections.Generic;

public abstract class SkillEffectMechanic : SkillEffectData
{
    public override SkillEffectType EffectType { get; } = SkillEffectType.Mechanic;
    public override List<UnitController> Execute(UnitController caster, List<UnitController> targets) {
        return DoMechanic(caster, targets);
    }

    public abstract List<UnitController> DoMechanic(UnitController caster, List<UnitController> targets);


}