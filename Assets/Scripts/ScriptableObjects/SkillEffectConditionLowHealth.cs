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
            /*
            var health = target.GetComponent<HealthComponent>();
            if (health != null && health.CurrentHealth / health.MaxHealth < thresholdPercent)
            {
                result.Add(target);
            }
            */
            result.Add(target);
        }
        return result;
    }
}