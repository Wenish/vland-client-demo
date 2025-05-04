using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/Heal")]
public class SkillEffectMechanicHeal : SkillEffectData
{
    public int healAmount = 20;

    public override List<GameObject> Execute(GameObject caster, List<GameObject> targets)
    {
        Debug.Log($"Executing Heal Effect on {targets.Count} targets.");
        foreach (var target in targets)
        {
            var unitController = target.GetComponent<UnitController>();
            if (unitController == null)
            {
                Debug.LogWarning($"Target {target.name} does not have a UnitController component.");
                continue;
            }
            unitController.Heal(healAmount);
            Debug.Log($"Healed {target.name} for {healAmount} health.");
        }
        return targets;
    }
}