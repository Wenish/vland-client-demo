using System.Collections.Generic;
using UnityEngine;

public abstract class SkillEffectConditionData : SkillEffectData
{
    public abstract bool CheckCondition(UnitController caster, UnitController target);

    public override bool Execute(UnitController caster, List<UnitController> targets)
    {
        var validTargets = new List<UnitController>();

        foreach (var target in targets)
        {
            if (CheckCondition(caster, target))
            {
                validTargets.Add(target);
            }
        }
        var hasValidTargets = validTargets.Count > 0;
        if (!hasValidTargets)
        {
            Debug.LogWarning($"No valid targets for {name}.");
            return false;
        }
        var result = ExecuteChildren(caster, validTargets);
        return result;
    }
}