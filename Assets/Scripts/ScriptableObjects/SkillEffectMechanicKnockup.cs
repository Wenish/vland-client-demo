using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicKnockup", menuName = "Game/Skills/Effects/Mechanic/Knockup")]
public class SkillEffectMechanicKnockup : SkillEffectMechanic
{
    [Header("Knockup Settings")]
    [Tooltip("Maximum vertical height reached at the peak of the knockup arc.")]
    public float knockupHeight = 2f;

    [Tooltip("Total airborne duration in seconds.")]
    public float knockupDuration = 0.45f;

    [Tooltip("Interrupt active attack/cast/channel when applying knockup.")]
    public bool interruptAction = true;

    [Tooltip("Skip targets on the same team as the caster.")]
    public bool skipAllies = true;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        if (castContext == null || castContext.skillInstance == null) return targets;
        var caster = castContext.caster;
        if (caster == null) return targets;

        if (!castContext.skillInstance.isServer) return targets;

        if (targets == null || targets.Count == 0) return targets;

        var affected = new List<UnitController>();

        foreach (var target in targets)
        {
            if (target == null) continue;
            if (skipAllies && target.team == caster.team) continue;

            target.StartKnockup(knockupHeight, knockupDuration, interruptAction);
            affected.Add(target);
        }

        return affected;
    }
}
