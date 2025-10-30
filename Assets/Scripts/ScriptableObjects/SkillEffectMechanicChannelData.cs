using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicChannel",
    menuName = "Game/Skills/Effects/Mechanic/Channel"
)]
public class SkillEffectMechanicChannelData : SkillEffectData
{
    public enum ChannelTickMode
    {
        // Ticks are evenly spaced with the final tick aligned to the channel end: t = (1..N)/N
        EvenlySpacedEndAligned,
        // Ticks include both start (t=0) and end (t=channelDuration). For N>=2, spacing is duration/(N-1).
        // For N==1, a single tick happens at start.
        IncludeStartAndEnd,
    }

    [Tooltip("Seconds to channel before continuing.")]
    public float channelDuration = 2f;

    [Tooltip("Percentage (0–1) of the caster’s base move speed allowed during channel.")]
    [Range(0f, 1f)]
    public float moveSpeedPercent = 0f;

    [Tooltip("Percentage (0-1) of the caster's base turn speed allowed during channel.")]
    [Range(0f, 1f)]
    public float turnSpeedPercent = 0f;

    [Header("Ticking (optional)")]
    [Tooltip("Optional effect chain to execute periodically during the channel.")]
    public SkillEffectChainData tickEffect;

    [Tooltip("Number of evenly spaced ticks to execute within channelDuration. 0 disables ticking.")]
    [Min(0)]
    public int tickCount = 0;

    [Tooltip("How to schedule ticks during the channel.")]
    public ChannelTickMode tickMode = ChannelTickMode.EvenlySpacedEndAligned;

    public override SkillEffectType EffectType => SkillEffectType.Mechanic;

    public override IEnumerator Execute(
        CastContext ctx,
        List<UnitController> targets,
        Action<List<UnitController>> onComplete
    )
    {
        var caster = ctx.caster;
        caster.unitActionState.SetUnitActionState(UnitActionState.ActionType.Channeling, NetworkTime.time, channelDuration, ctx.skillInstance.skillName);

        StatModifier moveSpeedModifier = new StatModifier() {
            Type = StatType.MovementSpeed,
            ModifierType = ModifierType.Percent,
            Value = moveSpeedPercent
        };
        caster.unitMediator.Stats.ApplyModifier(moveSpeedModifier);
        
        StatModifier turnSpeedModifier = new StatModifier() {
            Type = StatType.TurnSpeed,
            ModifierType = ModifierType.Percent,
            Value = turnSpeedPercent
        };
        caster.unitMediator.Stats.ApplyModifier(turnSpeedModifier);

        float elapsed = 0f;

        // Ticking scheduler state
        int ticksDone = 0;
        float tickInterval = 0f;
        bool hasTicks = tickEffect != null && tickCount > 0 && ctx.skillInstance != null;
        if (hasTicks)
        {
            if (tickMode == ChannelTickMode.EvenlySpacedEndAligned)
            {
                // Spread ticks evenly across the duration; final tick at end
                tickInterval = channelDuration > 0f ? channelDuration / tickCount : 0f;
            }
            else // IncludeStartAndEnd
            {
                // Include start and end as tick points. For N==1, single tick at start.
                tickInterval = (tickCount > 1 && channelDuration > 0f) ? channelDuration / (tickCount - 1) : 0f;
                // Fire initial tick at t=0 immediately
                ctx.skillInstance.StartCoroutine(tickEffect.ExecuteCoroutine(ctx, targets));
                ticksDone = 1;
            }
        }

        while (elapsed < channelDuration)
        {
            if (ctx.IsCancelled)
            {
                break;
            }

            // Advance time first
            elapsed += Time.deltaTime;

            // Schedule ticks as we pass their boundaries to ensure we hit the exact tickCount
            if (hasTicks && tickInterval >= 0f)
            {
                const float EPS = 1e-4f;
                if (tickMode == ChannelTickMode.EvenlySpacedEndAligned)
                {
                    // Boundaries at (i+1)*interval, i in [0..N-1]
                    while (ticksDone < tickCount && elapsed + EPS >= (ticksDone + 1) * tickInterval)
                    {
                        ctx.skillInstance.StartCoroutine(tickEffect.ExecuteCoroutine(ctx, targets));
                        ticksDone++;
                    }
                }
                else // IncludeStartAndEnd
                {
                    // Boundaries at i*interval, i in [0..N-1] (we already fired i=0)
                    while (ticksDone < tickCount && elapsed + EPS >= (ticksDone) * tickInterval)
                    {
                        ctx.skillInstance.StartCoroutine(tickEffect.ExecuteCoroutine(ctx, targets));
                        ticksDone++;
                    }
                }
            }

            yield return null;
        }

        // If not cancelled, ensure we executed exactly tickCount ticks (catch up for any rounding/last-frame issues)
        if (!ctx.IsCancelled && tickEffect != null && tickCount > 0 && ctx.skillInstance != null)
        {
            while (ticksDone < tickCount)
            {
                ctx.skillInstance.StartCoroutine(tickEffect.ExecuteCoroutine(ctx, targets));
                ticksDone++;
            }
        }

        caster.unitActionState.SetUnitActionStateToIdle();

        // Remove the move speed modifier
        caster.unitMediator.Stats.RemoveModifier(moveSpeedModifier);
        caster.unitMediator.Stats.RemoveModifier(turnSpeedModifier);
        // Hand back the same targets so the chain continues
        if (ctx.IsCancelled) yield break;

        onComplete(targets);
    }
}
