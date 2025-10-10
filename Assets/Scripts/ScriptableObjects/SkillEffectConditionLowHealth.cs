using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectConditionLowHealth", menuName = "Game/Skills/Effects/Condition/Low Health")]
public class SkillEffectConditionLowHealthData : SkillEffectCondition
{
    [Range(0f, 1f)]
    public float thresholdPercent = 0.5f;

    [Tooltip("If true, compare using <= instead of < (i.e., 50% passes when threshold is 0.5).")]
    public bool inclusive = false;

    public override bool IsConditionMet(CastContext castContext, UnitController target)
    {
        if (target == null) return false;

        int current = target.health;
        int max = target.maxHealth;

        if (max <= 0) return false;

        float percent = (float)current / (float)max;

        bool result = inclusive ? (percent <= thresholdPercent) : (percent < thresholdPercent);
        
        return result;
    }
}