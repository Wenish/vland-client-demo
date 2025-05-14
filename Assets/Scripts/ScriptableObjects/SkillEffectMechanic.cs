using System;
using System.Collections;
using System.Collections.Generic;

public abstract class SkillEffectMechanic : SkillEffectData
{
    public override SkillEffectType EffectType { get; } = SkillEffectType.Mechanic;

    public override IEnumerator Execute(
        CastContext castContext,
        List<UnitController> targets,
        Action<List<UnitController>> onComplete
    )
    {
        // Run the old synchronous logic
        var nextTargets = DoMechanic(castContext, targets);

        // Immediately signal completion
        onComplete(nextTargets);

        // End the coroutine
        yield break;
    }

    public abstract List<UnitController> DoMechanic(
        CastContext castContext,
        List<UnitController> targets
    );


}