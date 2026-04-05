using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicReflectDamage",
    menuName = "Game/Skills/Effects/Mechanic/ReflectDamage")]
public class SkillEffectMechanicReflectDamage : SkillEffectMechanic
{
    public enum ReflectMode
    {
        /// <summary>Always deal a set flat damage value.</summary>
        Flat,
        /// <summary>Deal a percentage of the damage that was just received (requires DamageContext).</summary>
        PercentOfIncoming,
    }

    [Tooltip("Flat: always deal this much damage back.\nPercentOfIncoming: deal this % of the hit that triggered the reflect (e.g. 25 = 25%).")]
    public ReflectMode mode = ReflectMode.Flat;

    [Tooltip("Flat mode: damage returned. PercentOfIncoming mode: percentage of incoming damage to return (e.g. 25 = 25%).")]
    [Min(0)]
    public int reflectAmount = 25;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        var instigator = castContext.instigator;
        if (instigator == null)
        {
            Debug.LogWarning(
                $"{nameof(SkillEffectMechanicReflectDamage)}: castContext.instigator is null; " +
                "no reflect damage applied. Ensure this mechanic is used with a trigger that " +
                "sets an instigator (e.g. OnCasterTakeDamageTrigger).");
            return targets;
        }

        int damage = reflectAmount;
        if (mode == ReflectMode.PercentOfIncoming && castContext.incomingDamage.HasValue)
        {
            damage = Mathf.Max(1, Mathf.RoundToInt(castContext.incomingDamage.Value * reflectAmount / 100f));
        }

        instigator.TakeDamage(DamageInstance.True(damage), castContext.caster);
        return new List<UnitController> { instigator };
    }
}
