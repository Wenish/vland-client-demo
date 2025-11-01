using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/HealOverTime")]
public class SkillEffectMechanicHealOverTime : SkillEffectMechanic
{
    public string buffId;
    public BuffType buffType;
    public ModifierType modifierType;
    public int healAmount;
    public float duration;
    public  float tickInterval;
    public UniqueMode uniqueMode = UniqueMode.None;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        Debug.Log($"Executing Heal Over Time Effect on {targets.Count} targets.");
        foreach (var target in targets)
        {
            UnitMediator mediator = target.unitMediator;
            if (mediator == null)
            {
                Debug.LogWarning($"Target {target.name} does not have a UnitMediator component.");
                continue;
            }

            BuffHealOverTime buff = new BuffHealOverTime(
                buffId,
                duration,
                tickInterval,
                healAmount,
                modifierType,
                uniqueMode,
                castContext.caster.unitMediator,
                buffType
            );
            castContext.skillInstance.ManageBuff(mediator, buff, true);
            Debug.Log($"Applied Heal Over Time to {target.name}: {healAmount} every {tickInterval} seconds.");
        }
        return targets;
    }
}