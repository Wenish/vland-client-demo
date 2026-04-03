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

        // If effect is disabled, skip it and its children
        if (!effect.enabled) yield break;

        List<UnitController> nextTargets = null;
        bool finished = false;

        // If this effect is the one that should count the cast, mark it now
        if (effect.countsAsCasted)
        {
            castContext.MarkCastCounted();
        }

        // 1) Run the effect coroutine. Contract: onComplete must be invoked before it returns.
        yield return castContext.skillInstance.StartCoroutine(
            effect.Execute(castContext, targets, (result) =>
            {
                // Ignore duplicate completion callbacks from buggy effects.
                if (finished) return;
                nextTargets = result;
                finished = true;
            })
        );

        // 2) Exit early on cancellation.
        if (castContext.IsCancelled) yield break;

        // 3) Deterministic contract check: if the effect returned without onComplete,
        // abort this node branch instead of hanging the chain.
        if (!finished)
        {
            var effectName = effect != null ? effect.name : "<null>";
            var skillName = castContext != null && castContext.skillInstance != null
                ? castContext.skillInstance.skillName
                : "<unknown skill>";
            Debug.LogWarning($"[SkillEffectNodeData] Effect '{effectName}' in skill '{skillName}' returned without calling onComplete; aborting this node.");
            yield break;
        }

        // 4) if effect has no children, we're done
        if (children.Count == 0) yield break;

        // 5) if next targets are null or empty, we're done
        if (nextTargets == null || nextTargets.Count == 0)
        {
            yield break;
        }

        // 6) run child coroutines
        foreach (var child in children)
        {
            yield return child.ExecuteCoroutine(castContext, nextTargets);
        }
    }
}
