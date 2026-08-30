using System.Collections.Generic;
using UnityEngine;

public static class SkillIndicatorTargetSnap
{
    public static UnitController Resolve(
        SkillIndicatorData indicator,
        UnitController caster,
        NetworkedSkillInstance skillInstance,
        Vector3 aimPoint,
        bool forceSelfTarget = false,
        UnitController preferredTarget = null)
    {
        return ResolveInternal(
            indicator,
            caster,
            skillInstance,
            aimPoint,
            forPreview: false,
            forceSelfTarget,
            preferredTarget);
    }

    /// <summary>
    /// Resolves snap for aim preview — may return units beyond cast range.
    /// </summary>
    public static UnitController ResolvePreview(
        SkillIndicatorData indicator,
        UnitController caster,
        NetworkedSkillInstance skillInstance,
        Vector3 aimPoint,
        bool forceSelfTarget = false,
        UnitController preferredTarget = null)
    {
        return ResolveInternal(
            indicator,
            caster,
            skillInstance,
            aimPoint,
            forPreview: true,
            forceSelfTarget,
            preferredTarget);
    }

    public static bool IsUnitInSnapRange(
        UnitController caster,
        UnitController unit,
        SkillEffectTarget snapToTarget,
        SkillData skillData)
    {
        if (caster == null || unit == null)
            return false;

        if (unit == caster)
            return true;

        float maxRange;
        if (snapToTarget is SkillEffectTargetSmart smart)
            maxRange = Mathf.Max(0f, smart.range);
        else if (skillData != null)
            maxRange = Mathf.Max(0f, skillData.castRange);
        else
            return true;

        Vector3 offset = unit.transform.position - caster.transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= maxRange * maxRange;
    }

    /// <summary>
    /// Returns false when a preview-snapped unit exists but is outside cast range.
    /// Empty ground (no snap) returns true so existing self-fallback behavior applies.
    /// </summary>
    public static bool TryValidateSnapCast(
        UnitController caster,
        SkillData skill,
        SkillIndicatorData indicator,
        Vector3 aimPoint,
        NetworkedSkillInstance skillInstance,
        out UnitController snapTarget,
        bool forceSelfTarget = false,
        UnitController preferredTarget = null)
    {
        snapTarget = null;
        if (indicator?.snapToTarget == null || caster == null)
            return true;

        snapTarget = ResolvePreview(indicator, caster, skillInstance, aimPoint, forceSelfTarget, preferredTarget);
        if (snapTarget == null)
            return true;

        return IsUnitInSnapRange(caster, snapTarget, indicator.snapToTarget, skill);
    }

    private static UnitController ResolveInternal(
        SkillIndicatorData indicator,
        UnitController caster,
        NetworkedSkillInstance skillInstance,
        Vector3 aimPoint,
        bool forPreview,
        bool forceSelfTarget,
        UnitController preferredTarget)
    {
        if (indicator == null || indicator.snapToTarget == null || caster == null)
            return null;

        var ctx = new CastContext(caster, skillInstance)
        {
            aimPoint = aimPoint,
            aimRotation = SkillAimUtil.GetAimRotation(caster, aimPoint),
            forceSelfTarget = forceSelfTarget,
            preferredTarget = preferredTarget,
        };

        // Sticky selection wins over mouse/hover for every snap type, not only Smart.
        // Left Alt force-self still goes through GetTargets so Self-mask skills keep current behavior.
        if (!forceSelfTarget
            && preferredTarget != null
            && indicator.snapToTarget.PassesCommonFilters(ctx, preferredTarget))
        {
            return preferredTarget;
        }

        var seeds = new List<UnitController> { caster };
        List<UnitController> targets;
        if (forPreview && indicator.snapToTarget is SkillEffectTargetSmart smart)
            targets = smart.GetTargetsForPreview(ctx, seeds);
        else
            targets = indicator.snapToTarget.GetTargets(ctx, seeds);

        if (targets == null || targets.Count == 0)
            return null;

        return targets[0];
    }
}
