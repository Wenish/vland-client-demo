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
            aimRotation = Quaternion.LookRotation(
                FlatDirection(caster.transform.position, aimPoint),
                Vector3.up),
        };

        var seeds = new List<UnitController> { caster };
        var targets = indicator.snapToTarget.GetTargets(ctx, seeds);
        if (targets == null || targets.Count == 0)
            return null;

        return targets[0];
    }

    private static Vector3 FlatDirection(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return dir.normalized;
    }
}
