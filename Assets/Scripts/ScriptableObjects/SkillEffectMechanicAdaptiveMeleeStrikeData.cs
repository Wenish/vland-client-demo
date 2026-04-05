using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicAdaptiveMeleeStrike",
    menuName = "Game/Skills/Effects/Mechanic/Adaptive Melee Strike")]
public class SkillEffectMechanicAdaptiveMeleeStrikeData : SkillEffectMechanic
{
    private class TokenBuff : Buff
    {
        public TokenBuff(string buffId, float duration, UniqueMode uniqueMode, UnitMediator caster, BuffType buffType)
            : base(buffId, duration, uniqueMode, caster, buffType)
        {
        }

        public override void OnApply(UnitMediator mediator) { }
        public override void OnRemove(UnitMediator mediator) { }
    }

    [Header("Damage")]
    [Min(0)] public int baseDamage = 40;
    [Min(0)] public int bonusDamageIfTargetHasToken = 30;

    [Tooltip("How many times damage is applied per target. Useful for flurry attacks.")]
    [Min(1)] public int hitsPerTarget = 1;

    [Header("Token Rules")]
    [Tooltip("Identifier used for token matching/apply/consume.")]
    public string tokenBuffId = "assassin_dagger_mark";

    [Tooltip("If true, target must have a matching token to be damaged.")]
    public bool requireTokenToDamage = false;

    [Tooltip("If true, consume one matching token from target after hit.")]
    public bool consumeTokenOnHit = false;

    [Tooltip("If true, only tokens applied by this caster count for checks/consumption.")]
    public bool tokenMustComeFromCaster = true;

    [Header("Token Application")]
    [Tooltip("If true, apply a token to every damaged target.")]
    public bool applyTokenOnHit = true;

    [Min(0f)] public float tokenDuration = 2f;
    public UniqueMode tokenUniqueMode = UniqueMode.PerCaster;
    public BuffType tokenBuffType;

    [Header("Directional Bonus")]
    [Tooltip("Enable extra multiplier based on relative position and target facing.")]
    public bool enableDirectionalBonus = true;

    public enum DirectionalMode
    {
        BehindTarget,
        InFrontOfTarget,
    }

    [Tooltip("Which target-relative facing region grants bonus damage.")]
    public DirectionalMode directionalMode = DirectionalMode.BehindTarget;

    [Tooltip("Half-angle of the directional cone in degrees.")]
    [Range(1f, 89f)] public float directionalHalfAngle = 60f;

    [Min(1f)] public float directionalDamageMultiplier = 1.5f;

    [Header("Target Limits")]
    [Tooltip("Optional cap in case previous target effects return too many targets. 0 = unlimited.")]
    [Min(0)] public int maxTargets = 0;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        var result = new List<UnitController>();
        if (castContext?.caster == null || targets == null || targets.Count == 0)
        {
            return result;
        }

        UnitController caster = castContext.caster;
        UnitMediator casterMediator = caster.unitMediator;
        if (casterMediator == null)
        {
            return result;
        }

        int processed = 0;
        foreach (var target in targets)
        {
            if (target == null || target == caster || target.unitMediator == null)
            {
                continue;
            }

            if (maxTargets > 0 && processed >= maxTargets)
            {
                break;
            }

            var existingToken = FindMatchingToken(target.unitMediator, casterMediator);
            bool hasToken = existingToken != null;
            if (requireTokenToDamage && !hasToken)
            {
                continue;
            }

            int damage = baseDamage;
            if (hasToken)
            {
                damage += bonusDamageIfTargetHasToken;
            }

            if (enableDirectionalBonus && IsWithinDirectionalBonus(caster.transform.position, target.transform))
            {
                damage = Mathf.RoundToInt(damage * directionalDamageMultiplier);
            }

            int hitCount = Mathf.Max(1, hitsPerTarget);

            // Adaptive: deal Physical if AttackPower >= AbilityPower, otherwise Magic
            float attackPower = casterMediator.Stats.GetStat(StatType.AttackPower);
            float abilityPower = casterMediator.Stats.GetStat(StatType.AbilityPower);
            bool isPhysical = attackPower >= abilityPower;

            for (int i = 0; i < hitCount; i++)
            {
                var damageInstance = isPhysical
                    ? DamageInstance.Physical(damage)
                    : DamageInstance.Magic(damage);
                target.TakeDamage(damageInstance, caster);
            }

            if (consumeTokenOnHit && existingToken != null)
            {
                target.unitMediator.Buffs.RemoveBuff(existingToken);
            }

            if (applyTokenOnHit)
            {
                var token = new TokenBuff(tokenBuffId, tokenDuration, tokenUniqueMode, casterMediator, tokenBuffType)
                {
                    SkillName = castContext.skillInstance != null
                        ? castContext.skillInstance.skillData.skillName
                        : string.Empty
                };
                target.unitMediator.Buffs.AddBuff(token);
            }

            result.Add(target);
            processed++;
        }

        return result;
    }

    private Buff FindMatchingToken(UnitMediator targetMediator, UnitMediator casterMediator)
    {
        if (targetMediator?.Buffs == null || string.IsNullOrWhiteSpace(tokenBuffId))
        {
            return null;
        }

        var tokens = targetMediator.Buffs.ActiveBuffs.Where(b => b.BuffId == tokenBuffId);
        if (!tokenMustComeFromCaster)
        {
            return tokens.FirstOrDefault();
        }

        return tokens.FirstOrDefault(b => b.Caster == casterMediator);
    }

    private bool IsWithinDirectionalBonus(Vector3 attackerPosition, Transform targetTransform)
    {
        Vector3 toAttacker = attackerPosition - targetTransform.position;
        toAttacker.y = 0f;
        if (toAttacker.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        Vector3 referenceDir = directionalMode == DirectionalMode.BehindTarget
            ? -targetTransform.forward
            : targetTransform.forward;
        referenceDir.y = 0f;

        float angle = Vector3.Angle(referenceDir.normalized, toAttacker.normalized);
        return angle <= directionalHalfAngle;
    }
}
