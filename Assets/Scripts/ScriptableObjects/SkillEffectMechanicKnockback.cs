using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicKnockback", menuName = "Game/Skills/Effects/Mechanic/Knockback")]
public class SkillEffectMechanicKnockback : SkillEffectMechanic
{
    public enum KnockbackDirection
    {
        AwayFromCaster,
        TowardCaster
    }

    [Header("Knockback Settings")]
    public KnockbackDirection direction = KnockbackDirection.AwayFromCaster;

    [Tooltip("Distance targets are knocked back.")]
    public float knockbackDistance = 5f;

    [Tooltip("Speed of the knockback movement.")]
    public float knockbackSpeed = 20f;

    [Tooltip("Skip targets on the same team as the caster.")]
    public bool skipAllies = true;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        if (castContext == null || castContext.skillInstance == null) return targets;
        var caster = castContext.caster;
        if (caster == null) return targets;

        if (!castContext.skillInstance.isServer) return targets;

        if (targets == null || targets.Count == 0) return targets;

        foreach (var target in targets)
        {
            if (target == null) continue;
            if (skipAllies && target.team == caster.team) continue;

            var dir = (target.transform.position - caster.transform.position).normalized;
            dir.y = 0f;

            if (direction == KnockbackDirection.TowardCaster)
                dir = -dir;

            // Fallback if caster and target overlap
            if (dir == Vector3.zero)
                dir = caster.transform.forward;

            target.StartDash(dir, knockbackSpeed, knockbackDistance, UnitController.DashSpeedProfile.Constant);
        }

        return targets;
    }
}
