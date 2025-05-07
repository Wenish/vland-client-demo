using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Condition/Low Health")]
public class SkillEffectConditionLowHealthData : SkillEffectData
{
    public float thresholdPercent = 0.5f;

    public override List<UnitController> Execute(UnitController caster, List<UnitController> targets)
    {
        List<UnitController> result = new List<UnitController>();
        foreach (var target in targets)
        {
            var health = target.health;
            var maxHealth = target.maxHealth;
            float healthPercent = (float)health / (float)maxHealth;
            var isBelowThreshold = healthPercent < thresholdPercent;
            if (isBelowThreshold)
            {
                result.Add(target);
            }
        }
        return result;
    }
}