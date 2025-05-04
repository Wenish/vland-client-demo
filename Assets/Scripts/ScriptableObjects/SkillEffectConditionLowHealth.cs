using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Condition/Low Health")]
public class SkillEffectConditionLowHealthData : SkillEffectData
{
    public float thresholdPercent = 0.5f;

    public override List<GameObject> Execute(GameObject caster, List<GameObject> targets)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (var target in targets)
        {
            var unitController = target.GetComponent<UnitController>();
            if (unitController == null)
            {
                Debug.LogWarning($"Target {target.name} does not have a UnitController component.");
                continue;
            }
            var health = unitController.health;
            var maxHealth = unitController.maxHealth;
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