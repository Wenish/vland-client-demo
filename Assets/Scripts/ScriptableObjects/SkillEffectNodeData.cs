using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SkillEffectNodeData
{
    [SerializeReference]
    public SkillEffectData effect;

    [SerializeReference]
    public List<SkillEffectNodeData> children = new List<SkillEffectNodeData>();

    public void Execute(CastContext castContext, List<UnitController> targets)
    {
        if (effect == null) return;

        List<UnitController> nextTargets = effect.Execute(castContext, targets);
        foreach (var child in children)
        {
            child.Execute(castContext, nextTargets);
        }
    }
}