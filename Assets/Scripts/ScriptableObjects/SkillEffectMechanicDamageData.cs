using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/Damage")]
public class SkillEffectMechanicDamageData : SkillEffectData
{
    public int amount = 20;

    
    public override List<GameObject> Execute(GameObject caster, List<GameObject> targets)
    {
        var casterUnit = caster.GetComponent<UnitController>();
        Debug.Log($"Executing Damage Effect on {targets.Count} targets.");
        foreach (var target in targets)
        {
            var unitController = target.GetComponent<UnitController>();
            if (unitController == null)
            {
                Debug.LogWarning($"Target {target.name} does not have a UnitController component.");
                continue;
            }
            unitController.TakeDamage(amount, casterUnit);
            Debug.Log($"Damaged {target.name} for {amount} health.");
        }
        return targets;
    }
}