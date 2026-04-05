using UnityEngine;

/// <summary>
/// Resolves a DamageInstance into a final integer damage value, applying:
///   1. Armor / MagicResist via League-style diminishing returns: reduction% = stat / (stat + 100)
///   2. DamageReduction multiplicatively on top (Physical and Magic only)
///   3. True damage is never reduced
///   4. Each component has a minimum floor of 1 if the raw value was > 0
/// </summary>
public static class DamageCalculator
{
    public static int Calculate(DamageInstance raw, UnitController target)
    {
        float armor = 0f;
        float magicResist = 0f;
        float damageReduction = 0f;

        if (target.unitMediator != null)
        {
            armor = Mathf.Max(0f, target.unitMediator.Stats.GetStat(StatType.Armor));
            magicResist = Mathf.Max(0f, target.unitMediator.Stats.GetStat(StatType.MagicResist));
            damageReduction = Mathf.Clamp01(target.unitMediator.Stats.GetStat(StatType.DamageReduction));
        }

        float physReduction = armor / (armor + 100f);
        float magicReduction = magicResist / (magicResist + 100f);
        float drMultiplier = 1f - damageReduction;

        float physFinal = raw.physical * (1f - physReduction) * drMultiplier;
        float magicFinal = raw.magic * (1f - magicReduction) * drMultiplier;
        float trueFinal = raw.trueDamage;

        // Minimum floor: if the base component was > 0, it must deal at least 1
        if (raw.physical > 0f) physFinal = Mathf.Max(1f, physFinal);
        if (raw.magic > 0f) magicFinal = Mathf.Max(1f, magicFinal);
        if (raw.trueDamage > 0f) trueFinal = Mathf.Max(1f, trueFinal);

        return Mathf.CeilToInt(physFinal + magicFinal + trueFinal);
    }
}
