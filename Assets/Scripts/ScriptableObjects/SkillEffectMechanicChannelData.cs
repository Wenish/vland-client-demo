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
    [Tooltip("Seconds to channel before continuing.")]
    public float channelDuration = 2f;

    [Tooltip("Percentage (0–1) of the caster’s base move speed allowed during channel.")]
    [Range(0f, 1f)]
    public float moveSpeedPercent = 0f;

    [Tooltip("Percentage (0-1) of the caster's base turn speed allowed during channel.")]
    [Range(0f, 1f)]
    public float turnSpeedPercent = 0f;

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
        while (elapsed < channelDuration)
        {
            if (ctx.IsCancelled)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
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
