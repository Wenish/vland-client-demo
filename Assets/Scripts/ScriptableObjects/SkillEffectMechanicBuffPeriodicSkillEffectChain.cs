using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/BuffPeriodicSkillEffectChain")]
public class SkillEffectMechanicBuffPeriodicSkillEffectChain : SkillEffectMechanic
{
    public string buffId;
    public float duration;
    public float tickInterval;
    public SkillEffectChainData effectChainDataOnTick;
    public UniqueMode uniqueMode = UniqueMode.None;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            UnitMediator mediator = target.unitMediator;
            if (mediator == null)
            {
                Debug.LogWarning($"Target {target.name} does not have a UnitMediator component.");
                continue;
            }

            BuffPeriodicSkillEffectChain buff = new BuffPeriodicSkillEffectChain(
                buffId,
                duration,
                tickInterval,
                effectChainDataOnTick,
                castContext,
                uniqueMode,
                castContext.caster.unitMediator
            );
            castContext.skillInstance.ManageBuff(mediator, buff, true);
        }
        return targets;
    }
}