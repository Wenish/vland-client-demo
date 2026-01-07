using System.Collections.Generic;
using NPCBehaviour;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicAddThreat", menuName = "Game/Skills/Effects/Mechanic/Add Threat")]
public class SkillEffectMechanicAddThreat : SkillEffectMechanic
{
    [Header("Threat Settings")]
    [Tooltip("Flat threat amount to add on each target's ThreatManager for the caster.")]
    public float amount = 100f;

    [Tooltip("Skip targets on the same team as the caster.")]
    public bool skipAllies = true;

    [Tooltip("Only add threat when the target has ThreatManager enabled.")]
    public bool requireThreatSystemEnabled = true;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        if (castContext == null || castContext.skillInstance == null) return targets;
        var caster = castContext.caster;
        if (caster == null) return targets;

        // Threat operations are server-side only
        if (!castContext.skillInstance.isServer) return targets;

        if (targets == null || targets.Count == 0) return targets;

        foreach (var target in targets)
        {
            if (target == null) continue;
            if (skipAllies && target.team == caster.team) continue;

            var tm = target.GetComponent<ThreatManager>();
            if (tm == null) continue;
            if (requireThreatSystemEnabled && !tm.IsEnabled) continue;

            tm.AddThreat(caster, amount);
        }

        return targets;
    }
}
