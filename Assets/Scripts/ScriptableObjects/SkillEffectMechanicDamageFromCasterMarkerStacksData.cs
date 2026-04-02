using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicDamageFromCasterMarkerStacks",
    menuName = "Game/Skills/Effects/Mechanic/Damage From Caster Marker Stacks")]
public class SkillEffectMechanicDamageFromCasterMarkerStacksData : SkillEffectMechanic
{
    [Tooltip("Buff id used as stack token on the caster.")]
    public string buffId = "echo_cleave_stack";

    [Min(0)]
    public int baseDamage = 20;

    [Min(0)]
    public int bonusDamagePerStack = 8;

    [Tooltip("Maximum amount of stacks that can contribute to one hit.")]
    [Min(1)]
    public int maxStacksUsed = 3;

    [Tooltip("If true, stacks used for this attack are removed from the caster.")]
    public bool consumeStacks = true;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        if (castContext?.caster?.unitMediator?.Buffs == null)
        {
            return targets;
        }

        var buffSystem = castContext.caster.unitMediator.Buffs;
        var stacks = buffSystem.ActiveBuffs.Where(b => b.BuffId == buffId).ToList();
        var stacksUsed = Mathf.Min(maxStacksUsed, stacks.Count);
        var damage = baseDamage + (bonusDamagePerStack * stacksUsed);

        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null)
                {
                    continue;
                }

                target.TakeDamage(damage, castContext.caster);
            }
        }

        if (consumeStacks && stacksUsed > 0)
        {
            for (int i = 0; i < stacksUsed; i++)
            {
                buffSystem.RemoveBuff(stacks[i]);
            }
        }

        return targets;
    }
}