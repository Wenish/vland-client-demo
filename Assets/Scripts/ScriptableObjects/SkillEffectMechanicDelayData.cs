using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicDelay",
    menuName = "Game/Skills/Effects/Mechanic/Delay"
)]
public class SkillEffectMechanicDelayData : SkillEffectData
{
    [Tooltip("Seconds to wait before continuing the chain.")]
    public float delaySeconds = 1f;

    [Tooltip("Percentage (0–1) of the caster's base move speed allowed during delay.")]
    [Range(0f, 1f)]
    public float moveSpeedPercent = 1f;

    [Tooltip("Percentage (0–1) of the caster's base turn speed allowed during delay.")]
    [Range(0f, 1f)]
    public float turnSpeedPercent = 1f;

    public override SkillEffectType EffectType => SkillEffectType.Mechanic;

    public override IEnumerator Execute(
        CastContext castContext,
        List<UnitController> targets,
        Action<List<UnitController>> onComplete
    )
    {
        var caster = castContext.caster;

        var moveSpeedModifier = new StatModifier
        {
            Type = StatType.MovementSpeed,
            ModifierType = ModifierType.Percent,
            Value = moveSpeedPercent
        };
        caster.unitMediator.Stats.ApplyModifier(moveSpeedModifier);

        var turnSpeedModifier = new StatModifier
        {
            Type = StatType.TurnSpeed,
            ModifierType = ModifierType.Percent,
            Value = turnSpeedPercent
        };
        caster.unitMediator.Stats.ApplyModifier(turnSpeedModifier);

        yield return new WaitForSeconds(delaySeconds);

        caster.unitMediator.Stats.RemoveModifier(moveSpeedModifier);
        caster.unitMediator.Stats.RemoveModifier(turnSpeedModifier);

        onComplete(targets);
    }
}