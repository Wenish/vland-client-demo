using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/Heal")]
public class SkillEffectMechanicHeal : SkillEffectMechanic
{
    public int healAmount = 20;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        Debug.Log($"Executing Heal Effect on {targets.Count} targets.");
        foreach (var target in targets)
        {
            target.Heal(healAmount);
            Debug.Log($"Healed {target.name} for {healAmount} health.");
        }
        return targets;
    }
}