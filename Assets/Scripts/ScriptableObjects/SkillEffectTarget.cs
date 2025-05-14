using System;
using System.Collections;
using System.Collections.Generic;

public abstract class SkillEffectTarget : SkillEffectData
{
    public override SkillEffectType EffectType { get; } = SkillEffectType.Target;
    public override IEnumerator Execute(
            CastContext castContext,
            List<UnitController> targets,
            Action<List<UnitController>> onComplete
        )
    {
        var nextTargets = GetTargets(castContext, targets);
        // Signal that we’re done and hand back the found units
        onComplete(nextTargets);
        // End the coroutine
        yield break;
    }

    public abstract List<UnitController> GetTargets(
        CastContext castContext,
        List<UnitController> targets
    );
}