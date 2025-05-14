using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SkillEffectNodeData
{
    [SerializeReference]
    public SkillEffectData effect;

    [SerializeReference]
    public List<SkillEffectNodeData> children = new List<SkillEffectNodeData>();

    public IEnumerator ExecuteCoroutine(CastContext castContext, List<UnitController> targets)
    {
        if (effect == null) yield break;

        List<UnitController> nextTargets = null;
        bool finished = false;

        // 1) run the effect’s coroutine, passing it our onComplete callback
        yield return castContext.skillInstance.StartCoroutine(
            effect.Execute(castContext, targets, (result) =>
            {
                nextTargets = result;
                finished = true;
            })
        );

        // 2) wait for the effect to finish
        while (!finished)
        {
            yield return null;
        }

        // 3) if the effect has children, run their coroutines
        if (children.Count == 0) yield break;

        // 4) if the effect has children, run their coroutines
        if (nextTargets == null)
        {
            Debug.LogError("Next targets are null. Effect might not have been executed correctly.");
            yield break;
        }
        
        foreach (var child in children)
        {
            yield return child.ExecuteCoroutine(castContext, nextTargets);
        }
    }
}