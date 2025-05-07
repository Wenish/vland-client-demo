using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Condition/Low Health")]
public class SkillEffectConditionLowHealthData : SkillEffectCondition
{
    public float thresholdPercent = 0.5f;

    public override bool IsConditionMet(UnitController caster, UnitController target)
    {
        var health = target.health;
        var maxHealth = target.maxHealth;
        float healthPercent = (float)health / (float)maxHealth;
        return healthPercent < thresholdPercent;
    }
}