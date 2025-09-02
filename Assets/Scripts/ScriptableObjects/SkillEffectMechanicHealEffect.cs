using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/Heal")]
public class SkillEffectMechanicHeal : SkillEffectMechanic
{
    public int healAmount = 20;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            target.Heal(healAmount, castContext.caster);
        }
        return targets;
    }
}