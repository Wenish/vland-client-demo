using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/Damage")]
public class SkillEffectMechanicDamageData : SkillEffectMechanic
{
    public int amount = 20;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            target.TakeDamage(amount, castContext.caster);
        }
        return targets;
    }
}