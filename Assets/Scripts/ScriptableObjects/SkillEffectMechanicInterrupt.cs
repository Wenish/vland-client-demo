using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicInterrupt", menuName = "Game/Skills/Effects/Mechanic/Interrupt")]
public class SkillEffectMechanicInterrupt : SkillEffectMechanic
{
    [Header("Interrupt Settings")]
    [Tooltip("Skip targets on the same team as the caster.")]
    public bool skipAllies = true;

    [Tooltip("Only interrupt targets that are currently performing an action (casting, channeling, attacking).")]
    public bool onlyIfActive = true;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        if (castContext == null || castContext.skillInstance == null) return targets;
        var caster = castContext.caster;
        if (caster == null) return targets;

        if (!castContext.skillInstance.isServer) return targets;

        if (targets == null || targets.Count == 0) return targets;

        var interrupted = new List<UnitController>();

        foreach (var target in targets)
        {
            if (target == null) continue;
            if (skipAllies && target.team == caster.team) continue;
            if (onlyIfActive && (target.unitActionState == null || !target.unitActionState.IsActive)) continue;

            target.InterruptAction();
            interrupted.Add(target);
        }

        return interrupted;
    }
}
