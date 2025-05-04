using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/Heal")]
public class SkillEffectMechanicHeal : SkillEffectData
{
    public int healAmount = 20;

    public override List<GameObject> Execute(GameObject caster, List<GameObject> targets)
    {
        /*
        foreach (var target in targets)
        {
            var health = target.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.Heal(healAmount);
            }
        }
        */
        return targets;
    }
}