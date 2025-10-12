
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicShield", menuName = "Game/Skills/Effects/Mechanic/Shield")]
public class SkillEffectMechanicShield : SkillEffectMechanic
{
    public enum ShieldAmountMode
    {
        // Grants a flat amount of shield equal to shieldAmount
        Flat = 0,
        // Grants shieldAmount percent of the target's max shield as shield
        PercentMaxShield = 1,
        // Grants shieldAmount percent of the target's missing shield as shield
        PercentMissingShield = 2,
    }

    [Header("Shield Settings")]
    [Tooltip("How to apply the shield amount: Flat points, % of Max Shield, or % of Missing Shield.")]
    public ShieldAmountMode mode = ShieldAmountMode.Flat;

    [Tooltip("Flat: grants this many shield points. Percent modes: this is the percentage value (e.g., 20 = 20%).")]
    [Min(0)]
    public int shieldAmount = 20;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            int amount = CalculateShieldAmount(target);
            if (amount <= 0) continue;
            target.Shield(amount);
        }
        return targets;
    }

    private int CalculateShieldAmount(UnitController target)
    {
        switch (mode)
        {
            case ShieldAmountMode.Flat:
                return Mathf.Max(0, shieldAmount);
            case ShieldAmountMode.PercentMaxShield:
                return Mathf.Max(0, Mathf.CeilToInt(target.maxShield * (shieldAmount / 100f)));
            case ShieldAmountMode.PercentMissingShield:
                int missing = Mathf.Max(0, target.maxShield - target.shield);
                return Mathf.Max(0, Mathf.CeilToInt(missing * (shieldAmount / 100f)));
            default:
                return Mathf.Max(0, shieldAmount);
        }
    }

}