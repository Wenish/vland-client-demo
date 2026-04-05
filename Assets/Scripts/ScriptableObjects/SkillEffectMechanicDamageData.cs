using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicDamage", menuName = "Game/Skills/Effects/Mechanic/Damage")]
public class SkillEffectMechanicDamageData : SkillEffectMechanic
{
    public enum DamageAmountMode
    {
        /// <summary>amount is a flat value.</summary>
        Flat,
        /// <summary>amount is a percentage of the target's max health (e.g. 5 = 5%).</summary>
        PercentMaxHealth,
        /// <summary>amount is a percentage of the target's missing health (e.g. 10 = 10% of missing HP).</summary>
        PercentMissingHealth,
    }

    [Header("Physical Damage")]
    public DamageAmountMode physicalMode = DamageAmountMode.Flat;
    [Min(0)] public float physicalAmount = 0f;
    [Tooltip("% of caster AttackPower added to physical damage (e.g. 25 = 25%).")]
    [Min(0)] public float physicalAttackPowerScaling = 0f;
    [Tooltip("% of caster AbilityPower added to physical damage (e.g. 10 = 10%).")]
    [Min(0)] public float physicalAbilityPowerScaling = 0f;

    [Header("Magic Damage")]
    public DamageAmountMode magicMode = DamageAmountMode.Flat;
    [Min(0)] public float magicAmount = 0f;
    [Tooltip("% of caster AttackPower added to magic damage (e.g. 25 = 25%).")]
    [Min(0)] public float magicAttackPowerScaling = 0f;
    [Tooltip("% of caster AbilityPower added to magic damage (e.g. 10 = 10%).")]
    [Min(0)] public float magicAbilityPowerScaling = 0f;

    [Header("True Damage")]
    public DamageAmountMode trueDamageMode = DamageAmountMode.Flat;
    [Min(0)] public float trueDamageAmount = 0f;
    [Tooltip("% of caster AttackPower added to true damage (e.g. 25 = 25%).")]
    [Min(0)] public float trueDamageAttackPowerScaling = 0f;
    [Tooltip("% of caster AbilityPower added to true damage (e.g. 10 = 10%).")]
    [Min(0)] public float trueDamageAbilityPowerScaling = 0f;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        float attackPower = 0f;
        float abilityPower = 0f;

        if (castContext.caster?.unitMediator != null)
        {
            attackPower = castContext.caster.unitMediator.Stats.GetStat(StatType.AttackPower);
            abilityPower = castContext.caster.unitMediator.Stats.GetStat(StatType.AbilityPower);
        }

        float physicalScaling = attackPower * (physicalAttackPowerScaling / 100f)
                              + abilityPower * (physicalAbilityPowerScaling / 100f);
        float magicScaling    = attackPower * (magicAttackPowerScaling / 100f)
                              + abilityPower * (magicAbilityPowerScaling / 100f);
        float trueScaling     = attackPower * (trueDamageAttackPowerScaling / 100f)
                              + abilityPower * (trueDamageAbilityPowerScaling / 100f);

        foreach (var target in targets)
        {
            float physical = ResolveAmount(physicalAmount, physicalMode, target) + physicalScaling;
            float magic    = ResolveAmount(magicAmount,    magicMode,    target) + magicScaling;
            float trueDmg  = ResolveAmount(trueDamageAmount, trueDamageMode, target) + trueScaling;

            target.TakeDamage(new DamageInstance(physical, magic, trueDmg), castContext.caster);
        }

        return targets;
    }

    private static float ResolveAmount(float amount, DamageAmountMode mode, UnitController target)
    {
        if (amount <= 0f) return 0f;

        switch (mode)
        {
            case DamageAmountMode.PercentMaxHealth:
                return target.maxHealth * (amount / 100f);
            case DamageAmountMode.PercentMissingHealth:
                float missing = Mathf.Max(0, target.maxHealth - target.health);
                return missing * (amount / 100f);
            default:
                return amount;
        }
    }
}
