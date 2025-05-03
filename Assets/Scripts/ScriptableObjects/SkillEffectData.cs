using System.Collections.Generic;
using UnityEngine;

public abstract class SkillEffectData : ScriptableObject
{
    public List<SkillEffectData> children = new List<SkillEffectData>();

    public abstract bool Execute(UnitController caster, List<UnitController> targets);

    protected bool ExecuteChildren(UnitController caster, List<UnitController> targets)
    {
        var allSuccessfullyExecuted = true;
        foreach (var child in children)
        {
            var childSuccessfullyExecuted = child.Execute(caster, targets);
            if (!childSuccessfullyExecuted)
            {
                allSuccessfullyExecuted = false;
            }
        }
        return allSuccessfullyExecuted;
    }
}