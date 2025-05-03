using System.Collections.Generic;
using UnityEngine;

public abstract class SkillEffectMechanicData : SkillEffectData
{
    public bool countsAsCasted;
    public override bool Execute(UnitController caster, List<UnitController> targets)
    {
        var resultMechanic = ExecuteMechanic(caster, targets);
        if (!resultMechanic)
        {
            Debug.LogWarning($"SkillEffectMechanicData {name} failed to execute.");
            return false;
        }
        // Check if the mechanic counts as casted
        if (countsAsCasted)
        {
            // Handle the mechanic as a casted effect
            // This could involve updating cooldowns, mana costs, etc.
            // For example:
            // caster.UpdateCooldown(this);
            // caster.UpdateManaCost(this);
        }

        var resultChildren = ExecuteChildren(caster, targets);
        if (!resultChildren)
        {
            Debug.LogWarning($"SkillEffectMechanicData {name} failed to execute children.");
        }
        return resultChildren;
    }

    public abstract bool ExecuteMechanic(UnitController caster, List<UnitController> targets);
}