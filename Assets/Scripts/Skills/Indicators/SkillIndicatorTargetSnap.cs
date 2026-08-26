using System.Collections.Generic;
using UnityEngine;

public static class SkillIndicatorTargetSnap
{
    public static UnitController Resolve(
        SkillIndicatorData indicator,
        UnitController caster,
        NetworkedSkillInstance skillInstance,
        Vector3 aimPoint)
    {
        if (indicator == null || indicator.snapToTarget == null || caster == null)
            return null;

        var ctx = new CastContext(caster, skillInstance)
        {
            aimPoint = aimPoint,
            aimRotation = SkillAimUtil.GetAimRotation(caster, aimPoint),
        };

        var seeds = new List<UnitController> { caster };
        var targets = indicator.snapToTarget.GetTargets(ctx, seeds);
        if (targets == null || targets.Count == 0)
            return null;

        return targets[0];
    }
}
