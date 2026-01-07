using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectConditionChance", menuName = "Game/Skills/Effects/Condition/Chance")]
public class SkillEffectConditionChance : SkillEffectCondition
{
    [Range(0f, 100f)]
    [Tooltip("The percentage chance (0-100) that the condition will pass and allow subsequent effects to execute.")]
    public float chancePercent = 50f;

    public override bool IsConditionMet(CastContext castContext, UnitController target)
    {
        // Generate a random number between 0 and 100
        float roll = Random.Range(0f, 100f);
        
        // Return true if the roll is less than or equal to the chance percentage
        return roll <= chancePercent;
    }
}
