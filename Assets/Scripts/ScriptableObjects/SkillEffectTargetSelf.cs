using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectTargetSelf", menuName = "Game/Skills/Effects/Target/Self")]
public class SkillEffectTargetSelf : SkillEffectTarget
{
    // Ensure sensible defaults when creating this asset: target self only
    private void OnValidate()
    {
        // Force self targeting for this effect type
        teamMask = TargetTeam.Self;
        // lifeMask is inherited and exposed; user can set Alive, Dead, or both (Either)
    }

    public override List<UnitController> GetTargets(CastContext castContext, List<UnitController> targets)
    {
        var caster = castContext?.caster;
        if (caster == null)
        {
            return new List<UnitController>(0);
        }

        var collected = new List<UnitController> { caster };
        var filtered = ApplyCommonFilters(castContext, collected);
        return new List<UnitController>(filtered);
    }
}