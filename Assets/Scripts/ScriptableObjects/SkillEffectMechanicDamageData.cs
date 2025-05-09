using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/Damage")]
public class SkillEffectMechanicDamageData : SkillEffectMechanic
{
    public int amount = 20;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        Debug.Log($"Executing Damage Effect on {targets.Count} targets.");
        foreach (var target in targets)
        {
            target.TakeDamage(amount, castContext.caster);
            Debug.Log($"Damaged {target.name} for {amount} health.");
        }
        return targets;
    }
}