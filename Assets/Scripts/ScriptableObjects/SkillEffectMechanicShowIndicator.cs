using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicShowIndicator",
    menuName = "Game/Skills/Effects/Mechanic/Show Indicator")]
public class SkillEffectMechanicShowIndicator : SkillEffectData
{
    public enum IndicatorLifetimeMode : byte
    {
        /// <summary>
        /// Show immediately and complete. Hidden when the cast ends or is cancelled.
        /// </summary>
        UntilCastEnds = 0,

        /// <summary>
        /// Show for a fixed duration, then hide this session and continue.
        /// </summary>
        ForSeconds = 1,
    }

    [Expandable]
    public SkillIndicatorData indicator;

    [Tooltip("How long this indicator session stays visible.")]
    public IndicatorLifetimeMode lifetimeMode = IndicatorLifetimeMode.UntilCastEnds;

    [ShowIf(nameof(lifetimeMode), IndicatorLifetimeMode.ForSeconds)]
    [MinValue(0f)]
    public float durationSeconds = 1f;

    public override SkillEffectType EffectType => SkillEffectType.Mechanic;

    public override IEnumerator Execute(
        CastContext ctx,
        List<UnitController> targets,
        Action<List<UnitController>> onComplete)
    {
        targets ??= new List<UnitController>();

        if (ctx?.skillInstance == null || indicator == null || ctx.caster == null)
        {
            onComplete(targets);
            yield break;
        }

        float castRange = ctx.skillInstance.skillData != null
            ? ctx.skillInstance.skillData.castRange
            : 0f;
        var display = indicator.ToDisplayParams(castRange, forPreview: false);
        Vector3 aim = ctx.aimPoint ?? ctx.caster.transform.position;

        UnitController followTarget = null;
        if (indicator.snapToTarget != null)
        {
            followTarget = SkillIndicatorTargetSnap.Resolve(
                indicator,
                ctx.caster,
                ctx.skillInstance,
                aim);
        }

        int sessionId = ctx.skillInstance.ServerShowSkillIndicator(display, aim, followTarget);

        try
        {
            if (lifetimeMode == IndicatorLifetimeMode.ForSeconds)
            {
                float elapsed = 0f;
                while (elapsed < durationSeconds)
                {
                    if (ctx.IsCancelled)
                        yield break;
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            // UntilCastEnds: complete immediately; hide on cast end or CancelCast.
        }
        finally
        {
            if (lifetimeMode == IndicatorLifetimeMode.ForSeconds && sessionId > 0)
            {
                ctx.skillInstance.ServerHideSkillIndicator(sessionId);
            }
        }

        if (ctx.IsCancelled)
            yield break;

        onComplete(targets);
    }
}
