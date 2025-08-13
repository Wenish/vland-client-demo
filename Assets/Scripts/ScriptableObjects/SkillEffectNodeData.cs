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

        // If this effect is the one that should count the cast, mark it now
        if (effect.countsAsCasted)
        {
            castContext.MarkCastCounted();
        }

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

        // 3) if effect has no children, we're done
        if (children.Count == 0) yield break;

        // 4) if next targets are null or empty, we're done
        if (nextTargets == null || nextTargets.Count == 0)
        {
            Debug.LogWarning("Next targets are null or empty. Effect does not continue.");
            yield break;
        }

        // 5) run child coroutines
        foreach (var child in children)
        {
            yield return child.ExecuteCoroutine(castContext, nextTargets);
        }
    }
}