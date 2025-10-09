using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectTargetSelf", menuName = "Game/Skills/Effects/Target/Self")]
public class SkillEffectTargetSelf : SkillEffectData
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
    public List<UnitController> GetTargets(CastContext castContext, List<UnitController> targets)
    {
        return new List<UnitController> { castContext.caster };
    }
}