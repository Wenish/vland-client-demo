using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicGrantMarkerStacksToCaster",
    menuName = "Game/Skills/Effects/Mechanic/Grant Marker Stacks To Caster")]
public class SkillEffectMechanicGrantMarkerStacksToCasterData : SkillEffectMechanic
{
    private class MarkerBuff : Buff
    {
        public MarkerBuff(string buffId, float duration, UnitMediator caster)
            : base(buffId, duration, UniqueMode.None, caster, null)
        {
        }

        public override void OnApply(UnitMediator mediator) { }
        public override void OnRemove(UnitMediator mediator) { }
    }

    [Tooltip("Buff id used as stack token on the caster.")]
    public string buffId = "echo_cleave_stack";

    [Tooltip("Duration of each stack token in seconds.")]
    [Min(0.1f)]
    public float stackDuration = 3f;

    [Tooltip("Maximum number of stacks the caster can hold.")]
    [Min(1)]
    public int maxStacks = 3;

    [Tooltip("How many stacks to grant for each valid target in this effect.")]
    [Min(1)]
    public int stacksPerTarget = 1;

    [Tooltip("If true, the caster cannot gain stacks from hitting themselves.")]
    public bool ignoreCasterInTargets = true;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        if (castContext?.caster?.unitMediator?.Buffs == null || string.IsNullOrWhiteSpace(buffId))
        {
            return targets;
        }

        var caster = castContext.caster;
        var eligibleTargetCount = 0;

        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null)
                {
                    continue;
                }

                if (ignoreCasterInTargets && target == caster)
                {
                    continue;
                }

                eligibleTargetCount++;
            }
        }

        if (eligibleTargetCount <= 0)
        {
            return targets;
        }

        var existing = caster.unitMediator.Buffs.ActiveBuffs.Count(b => b.BuffId == buffId);
        var remainingCapacity = Mathf.Max(0, maxStacks - existing);
        if (remainingCapacity <= 0)
        {
            return targets;
        }

        var desiredStacks = eligibleTargetCount * stacksPerTarget;
        var stacksToAdd = Mathf.Min(remainingCapacity, desiredStacks);

        for (int i = 0; i < stacksToAdd; i++)
        {
            var stackBuff = new MarkerBuff(buffId, stackDuration, caster.unitMediator)
            {
                SkillName = castContext.skillInstance?.skillData?.skillName
            };
            caster.unitMediator.Buffs.AddBuff(stackBuff);
        }

        return targets;
    }
}