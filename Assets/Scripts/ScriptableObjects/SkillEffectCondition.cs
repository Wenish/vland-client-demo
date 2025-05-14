using System;
using System.Collections;
using System.Collections.Generic;

public abstract class SkillEffectCondition : SkillEffectData
{
    public override SkillEffectType EffectType { get; } = SkillEffectType.Condition;
    public override IEnumerator Execute(
            CastContext castContext,
            List<UnitController> targets,
            Action<List<UnitController>> onComplete
        )
    {
        // Run your existing condition filter
        var result = new List<UnitController>();
        foreach (var target in targets)
        {
            if (IsConditionMet(castContext, target))
                result.Add(target);
        }

        // Hand back the filtered list
        onComplete(result);

        // End the coroutine
        yield break;
    }

    /// <summary>
    /// Returns true if this target should pass through the condition.
    /// </summary>
    public abstract bool IsConditionMet(
        CastContext castContext,
        UnitController target
    );
}