using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicHeal", menuName = "Game/Skills/Effects/Mechanic/Heal")]
public class SkillEffectMechanicHeal : SkillEffectMechanic
{
    public enum HealAmountMode
    {
        // Heals a flat amount of HP equal to healAmount
        Flat = 0,
        // Heals healAmount percent of the target's max health
        PercentMaxHealth = 1,
        // Heals healAmount percent of the target's missing health
        PercentMissingHealth = 2,
    }

    [Header("Heal Settings")]
    [Tooltip("How to apply the heal amount: Flat points, % of Max Health, or % of Missing Health.")]
    public HealAmountMode mode = HealAmountMode.Flat;

    [Tooltip("Flat: heals this many HP. Percent modes: this is the percentage value (e.g., 20 = 20%).")]
    [Min(0)]
    public int healAmount = 20;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            int amount = CalculateHealAmount(target);
            if (amount <= 0) continue;
            target.Heal(amount, castContext.caster);
        }
        return targets;
    }

    private int CalculateHealAmount(UnitController target)
    {
        switch (mode)
        {
            case HealAmountMode.Flat:
                return Mathf.Max(0, healAmount);
            case HealAmountMode.PercentMaxHealth:
                return Mathf.Max(0, Mathf.CeilToInt(target.maxHealth * (healAmount / 100f)));
            case HealAmountMode.PercentMissingHealth:
                int missing = Mathf.Max(0, target.maxHealth - target.health);
                return Mathf.Max(0, Mathf.CeilToInt(missing * (healAmount / 100f)));
            default:
                return Mathf.Max(0, healAmount);
        }
    }
}