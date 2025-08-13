using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class SkillEffectTarget : SkillEffectData
{
    public override SkillEffectType EffectType { get; } = SkillEffectType.Target;

    [System.Flags]
    public enum TargetTeam
    {
        Self = 1 << 0,
        Allies = 1 << 1,
        Enemies = 1 << 2,
    }

    [Header("General Target Filters")]
    [Tooltip("Which relative teams are allowed (relative to caster).")]
    public TargetTeam teamMask = TargetTeam.Enemies; // default like before

    [Tooltip("Include dead units.")]
    public bool includeDead = false;

    [Tooltip("Ensure returned list contains unique units.")]
    public bool distinct = true;

    [Tooltip("Maximum number of targets to keep (0 = unlimited).")]
    public int maxTargets = 0;

    [Tooltip("If true and maxTargets > 0, randomize final order before trimming.")]
    public bool randomizeOrder = false;

    [Tooltip("Optional: sort by distance to caster before trimming (ignored if randomizeOrder).")]
    public bool sortByDistance = true;

    protected bool PassesTeamMask(CastContext context, UnitController candidate)
    {
        if (candidate == null) return false;
        var caster = context.caster;
        if (candidate == caster)
        {
            return (teamMask & TargetTeam.Self) != 0;
        }
        bool isAlly = candidate.team == caster.team;
        if (isAlly) return (teamMask & TargetTeam.Allies) != 0;
        return (teamMask & TargetTeam.Enemies) != 0;
    }

    protected IEnumerable<UnitController> ApplyCommonFilters(CastContext context, IEnumerable<UnitController> raw)
    {
        var caster = context.caster;
        IEnumerable<UnitController> seq = raw;

        if (distinct)
        {
            var seen = new HashSet<UnitController>();
            seq = seq.Where(u => u != null && seen.Add(u));
        }

        seq = seq.Where(u => u != null);
        if (!includeDead)
        {
            seq = seq.Where(u => !u.IsDead);
        }
        seq = seq.Where(u => PassesTeamMask(context, u));

        if (randomizeOrder && maxTargets > 0)
        {
            // simple Fisher-Yates via OrderBy Guid (fine for small lists)
            seq = seq.OrderBy(_ => UnityEngine.Random.value);
        }
        else if (sortByDistance)
        {
            Vector3 origin = caster.transform.position;
            seq = seq.OrderBy(u => (u.transform.position - origin).sqrMagnitude);
        }

        if (maxTargets > 0)
        {
            seq = seq.Take(maxTargets);
        }
        return seq;
    }

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